using BackEncordados.Common.Dto;
using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Common.Service.Cloudinary;
using BackEncordados.Common.Service.Email;
using BackEncordados.Common.Utils;
using BackEncordados.Infraestructure;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Mapper;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace BackEncordados.Usuarios.Service.CrudService;

/// <summary>
/// Implementación de <see cref="IUserService"/> que orquesta las operaciones CRUD
/// sobre la entidad <see cref="User"/> coordinando el repositorio, Cloudinary, email y caché.
/// </summary>
/// <remarks>
/// <para>Actúa como fachada (Facade pattern) centralizando la lógica de negocio de usuarios:</para>
/// <list type="table">
///   <listheader>
///     <term>Parámetro</term>
///     <term>Tipo</term>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>logger</c></term>
///     <term><c>ILogger&lt;UserService&gt;</c></term>
///     <description>Logging de todas las operaciones de usuarios.</description>
///   </item>
///   <item>
///     <term><c>repository</c></term>
///     <term><see cref="IUserRepository"/></term>
///     <description>Acceso a datos de usuarios (CRUD + consultas filtradas).</description>
///   </item>
///   <item>
///     <term><c>cloudinary</c></term>
///     <term><see cref="ICloudinaryService"/></term>
///     <description>Gestión de imágenes de avatar (subida, borrado, resolución de URLs).</description>
///   </item>
///   <item>
///     <term><c>emailService</c></term>
///     <term><see cref="IEmailService"/></term>
///     <description>Envío de emails de bienvenida y cambio de contraseña.</description>
///   </item>
///   <item>
///     <term><c>cache</c></term>
///     <term><see cref="ICacheService"/></term>
///     <description>Caché de claves de cambio de contraseña con expiración de 60 minutos.</description>
///   </item>
/// </list>
/// <para>Utiliza el patrón Result de CSharpFunctionalExtensions para manejo de errores tipados.</para>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones de usuario.</param>
/// <param name="repository">Repositorio de usuarios para acceso a datos.</param>
/// <param name="cloudinary">Servicio de Cloudinary para gestión de imágenes de avatar.</param>
/// <param name="emailService">Servicio de email para notificaciones (bienvenida, cambio de contraseña).</param>
/// <param name="cache">Servicio de caché para almacenar claves de restablecimiento de contraseña.</param>
public class UserService(
    ILogger<UserService> logger, 
    IUserRepository repository,
    ICloudinaryService cloudinary,
    IEmailService emailService,
    ICacheService cache
    ) : IUserService {
    private const string CacheKey = CacheKeys.PasswordChange;

    /// <summary>
    /// Obtiene un usuario por su identificador ULID.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Consulta el usuario en el repositorio mediante <see cref="IUserRepository.FindByIdAsync"/>.</description></item>
    ///   <item><description>Si existe, mapea a <see cref="UserResponseDto"/> usando <see cref="UserMapper.ToDto"/> con resolución de imagen Cloudinary.</description></item>
    ///   <item><description>Si no existe, retorna <see cref="UserNotFoundError"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">Identificador ULID del usuario a buscar.</param>
    /// <returns>DTO público del usuario o error <see cref="UserNotFoundError"/>.</returns>
    public async Task<Result<UserResponseDto, AuthError>> FindByIdAsync(Ulid id)
    {
        return await repository.FindByIdAsync(id) is { } user
            ? Result.Success<UserResponseDto, AuthError>(user.ToDto(cloudinary))
            : Result.Failure<UserResponseDto, AuthError>(new UserNotFoundError("User not found"));
    }

    /// <summary>
    /// Elimina un usuario lógicamente (soft delete) y su avatar de Cloudinary.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el usuario por ID en el repositorio.</description></item>
    ///   <item><description>Si no existe, retorna <see cref="UserNotFoundError"/>.</description></item>
    ///   <item><description>Llama a <c>repository.DeleteAsync</c> que aplica soft delete (marca <c>IsDeleted=true</c> y reemplaza username).</description></item>
    ///   <item><description>Si el avatar no es la imagen por defecto, lo elimina de Cloudinary mediante <c>cloudinary.DeleteAsync</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">Identificador ULID del usuario a eliminar.</param>
    /// <returns>Unit en éxito o <see cref="UserNotFoundError"/> si no existe.</returns>
    public async Task<Result<Unit,AuthError>> DeleteUserAsync(Ulid id)
    {
        return await repository.FindByIdAsync(id) is { } user
        ? await Result.Success<Unit, AuthError>(Unit.Value).TapAsync(async _ => {
            logger.LogInformation("Deletando usuario con ID: {Id}", id);
            await repository.DeleteAsync(id);
            if (user.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_USUARIOS) 
                await cloudinary.DeleteAsync(user.CloudinaryPublicId!);
        })
        : Result.Failure<Unit, AuthError>(new UserNotFoundError("User not found"))
            .TapError((() => logger.LogInformation("Deletando usuario con ID: {Id}", id)));
        
    }

    /// <summary>
    /// Asigna un nuevo rol a un usuario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el usuario por ID.</description></item>
    ///   <item><description>Si no existe, retorna <see cref="UserNotFoundError"/>.</description></item>
    ///   <item><description>Si ya tiene ese rol, retorna <see cref="ConflictError"/>.</description></item>
    ///   <item><description>Persiste el cambio mediante <c>repository.UserChageRoleAsync</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">Identificador ULID del usuario.</param>
    /// <param name="role">Nombre del rol a asignar (ADMIN, USER, OWNER, ENCORDER).</param>
    /// <returns><c>true</c> si el cambio fue exitoso, o error si no existe o ya tiene el rol.</returns>
    public async Task<Result<bool, AuthError>> GiveRoleToUserAsync(Ulid id, string role)
    {
        var user = await repository.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<bool, AuthError>(new UserNotFoundError("User not found"));
        if (user.TournamentId != null) {
            return Result.Failure<bool, AuthError>(new ValidationError("That user is already associated with a tournament") );
        }
        if(user.Role!= User.UserRoles.USER) return Result.Failure<bool, AuthError>(new ValidationError("That user already has a role"));
        if (user.Role == role)
            return Result.Failure<bool, AuthError>(new ConflictError("User already has that role"));

        return await repository.UserChageRoleAsync(id, role)
            ? Result.Success<bool, AuthError>(true)
            : Result.Failure<bool, AuthError>(new AuthError("Failed to update role"));
    }

    /// <summary>
    /// Obtiene una lista paginada de usuarios aplicando filtros opcionales.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Delega la consulta filtrada y paginada al repositorio mediante <c>repository.FindAllAsync</c>.</description></item>
    ///   <item><description>Calcula el número total de páginas a partir del <c>TotalCount</c> retornado.</description></item>
    ///   <item><description>Mapea cada usuario a <see cref="UserWithIdDto"/> mediante <see cref="UserMapper.ToDtoWithId"/> con resolución de imagen Cloudinary.</description></item>
    ///   <item><description>Retorna un <see cref="PageResponseDto{T}"/> con los metadatos de paginación.</description></item>
    /// </list>
    /// <para><b>Caso borde:</b> Si <c>filter.Size</c> es 0, <c>TotalPages</c> se calcula como 0 para evitar división por cero.</para>
    /// </remarks>
    /// <param name="filter">DTO con filtros de búsqueda, tipo de usuario y paginación.</param>
    /// <returns>Página de resultados con metadatos de paginación.</returns>
    public async Task<PageResponseDto<UserWithIdDto>> GetAllUsersAsync(FilterUserDto filter)
    {
        var paged = await repository.FindAllAsync(filter);
        int totalPages = filter.Size > 0 ? (int)Math.Ceiling(paged.TotalCount / (double)filter.Size) : 0;
        return new PageResponseDto<UserWithIdDto>(
            Content: paged.Items.Select(item => item.ToDtoWithId(cloudinary)).ToList(),
            TotalPages: totalPages,
            TotalElements: paged.TotalCount,
            PageSize: filter.Size,
            PageNumber: filter.Page,
            TotalPageElements: paged.Items.Count(),
            SortBy: filter.SortBy,
            Direction: filter.Direction
        );
    }

    /// <summary>
    /// Actualiza parcialmente los datos de un usuario (nombre, email, teléfono, username, avatar).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el usuario por ID. Si no existe, retorna <see cref="UserNotFoundError"/>.</description></item>
    ///   <item><description>Si se proporciona un nuevo <c>Avatar</c>, elimina el anterior de Cloudinary (si no es la imagen por defecto) y sube el nuevo.</description></item>
    ///   <item><description>Aplica cada campo del DTO solo si no es <c>null</c> y cumple las condiciones de validación.</description></item>
    ///   <item><description>Persiste los cambios mediante <c>repository.UpdateAsync</c>.</description></item>
    ///   <item><description>Retorna el DTO actualizado con la URL de imagen resuelta.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="id">Identificador ULID del usuario a actualizar.</param>
    /// <param name="request">DTO con campos opcionales a modificar (Name, Email, Telefono, Username, Avatar).</param>
    /// <returns>DTO actualizado del usuario o error <see cref="UserNotFoundError"/>.</returns>
    public async Task<Result<UserResponseDto, AuthError>> PatchUserAsync(Ulid id, UserRequestDto request)
    {
        logger.LogInformation("Actualizando usuario con ID: {Id}", id);

        var user = await repository.FindByIdAsync(id);
        if (user is null)
            return Result.Failure<UserResponseDto, AuthError>(new UserNotFoundError("User not found"));

        string? username = request.Username?.Trim();
        string? email = request.Email?.Trim();
        if(request.Avatar is not null) {
            if(user.ImageUrl != CloudinaryConstants.DEFAULT_IMAGE_USUARIOS) await cloudinary.DeleteAsync(user.CloudinaryPublicId!);
            var upload= await cloudinary.UploadWithAutoNameAsync(request.Avatar,id.ToString(),CloudinaryConstants.FOLDER_USUARIOS);
            user.ImageUrl = upload.ImageUrl;
            user.CloudinaryPublicId = upload.PublicId;
        }

        if (request.Name is not null && request.Name.Trim().Length > 0)
            user.Name = request.Name.Trim();

        if (request.Telefono is not null)
            user.Phone = request.Telefono.Trim();

        if (!string.IsNullOrEmpty(username))
            user.Username = username;

        if (!string.IsNullOrEmpty(email))
            user.Email = email;

        var updated = await repository.UpdateAsync(user);
        return Result.Success<UserResponseDto, AuthError>(updated.ToDto(cloudinary));
    }

    /// <summary>
    /// Crea un nuevo contacto visitante en el sistema asociado a un torneo.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Convierte el <see cref="ContactoPostRequestDto"/> a entidad <see cref="User"/> mediante <see cref="UserMapper.ToModel"/>.</description></item>
    ///   <item><description>Persiste el usuario con rol USER mediante <c>repository.SaveAsync</c>.</description></item>
    ///   <item><description>Si se proporcionó email, genera una clave de caché con expiración de 60 minutos y envía un email de cambio de contraseña.</description></item>
    ///   <item><description>Retorna el DTO del contacto creado o error de conflicto si falla la persistencia.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="request">DTO con nombre, email/teléfono opcional y TournamentId.</param>
    /// <returns>DTO del contacto creado o <see cref="ConflictError"/> si falla la creación.</returns>
    public async Task<Result<UserResponseDto, AuthError>> CreateContacto(ContactoPostRequestDto request) {
        logger.LogInformation("Creando contacto para el usuario con nombre: {Name}",request.Name);
        return await repository.SaveAsync(request.ToModel()) is { } user
            ? await Result.Success<UserResponseDto, AuthError>(user.ToDto(cloudinary))
                .TapAsync((async _=> {
                    logger.LogInformation("Contacto creado con ID: {Id}", user.Id);
                    if(request.Email is not null) {
                        var key = CacheKey + Guid.NewGuid();
                        await cache.SetAsync(key, user.Id,TimeSpan.FromMinutes(60));
                        await SendPasswordChangeEmail(request.Email, key);
                    }
                }))
            : Result.Failure<UserResponseDto, AuthError>(new ConflictError("Error creating contact"))
                .TapError((() => logger.LogError("Error creating contact for user with name: {Name}", request.Name)));
    }
    
    /// <summary>
    /// Envía un email con el enlace para cambio de contraseña.
    /// </summary>
    /// <param name="email">Dirección de correo del destinatario.</param>
    /// <param name="guid">Identificador único de la solicitud de cambio (clave de caché).</param>
    private async Task SendPasswordChangeEmail(string email, string guid) {
        var passwordUrl= $"{AppConfig.Current.FrontendUrl}/changePassword?guid={guid}";
        var message = new EmailMessage {
            To = email,
            Subject = "Cambio de contraseña en nuevo contacto",
            Body = EmailTemplates.PasswordReset(passwordUrl),
            IsHtml = true
        };
        await emailService.EnqueueEmailAsync(message);
    }

    /// <summary>
    /// Promociona un usuario existente al rol de Encordador (ENCORDER).
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el usuario por ID. Si no existe, retorna <see cref="UserNotFoundError"/>.</description></item>
    ///   <item><description>Si ya tiene rol ENCORDER, retorna <see cref="ConflictError"/>.</description></item>
    ///   <item><description>Asigna el rol <c>User.UserRoles.ENCORDER</c> y persiste mediante <c>repository.UpdateAsync</c>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userId">Identificador ULID del usuario a promocionar.</param>
    /// <returns>Unit en éxito, o error si el usuario no existe o ya es ENCORDER.</returns>
    public async Task<Result<Unit, AuthError>> CreateEncoderAsync(Ulid userId) {
        logger.LogInformation("Creando encordador para usuario con ID: {UserId}", userId);
        
        var user = await repository.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<Unit, AuthError>(new UserNotFoundError("User not found"));
        if(user.TournamentId!= null)
            return Result.Failure<Unit, AuthError>(new ValidationError("User is already associated with a tournament"));
                
        if (user.Role == User.UserRoles.ENCORDER)
            return Result.Failure<Unit, AuthError>(new ConflictError("User already is an encorder"));
        
        user.Role = User.UserRoles.ENCORDER;
        
        await repository.UpdateAsync(user);
        logger.LogInformation("Usuario {UserId} asignado como ENCORDER", userId);
        
        return Result.Success<Unit, AuthError>(Unit.Value);
    }

    /// <summary>
    /// Incrementa el saldo de bonos de un usuario.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Valida que la cantidad sea mayor a 0. Si no, retorna <see cref="ValidationError"/>.</description></item>
    ///   <item><description>Busca el usuario por ID. Si no existe, retorna <see cref="UserNotFoundError"/>.</description></item>
    ///   <item><description>Incrementa <c>user.Bonos</c> con la cantidad indicada.</description></item>
    ///   <item><description>Persiste los cambios mediante <c>repository.UpdateAsync</c>.</description></item>
    ///   <item><description>Retorna el DTO actualizado con el nuevo saldo de bonos.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userId">Identificador ULID del usuario.</param>
    /// <param name="cantidad">Cantidad positiva de bonos a añadir. Debe ser mayor a 0.</param>
    /// <returns>DTO actualizado del usuario con el nuevo saldo, o error si la cantidad es inválida o el usuario no existe.</returns>
    public async Task<Result<UserResponseDto, AuthError>> AddBonosAsync(Ulid userId, double cantidad)
    {
        logger.LogInformation("Añadiendo {Cantidad} bonos al usuario con ID: {UserId}", cantidad, userId);

        if (cantidad <= 0)
            return Result.Failure<UserResponseDto, AuthError>(new ValidationError("La cantidad de bonos debe ser mayor a 0"));

        var user = await repository.FindByIdAsync(userId);
        if (user is null)
            return Result.Failure<UserResponseDto, AuthError>(new UserNotFoundError("User not found"));

        user.Bonos += cantidad;
        await repository.UpdateAsync(user);

        logger.LogInformation("Bonos añadidos correctamente. Nuevo saldo: {Bonos}", user.Bonos);
        return Result.Success<UserResponseDto, AuthError>(user.ToDto(cloudinary));
    }
}