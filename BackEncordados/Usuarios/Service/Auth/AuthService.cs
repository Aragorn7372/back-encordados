using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Common.Service.Email;
using BackEncordados.Common.Utils;
using BackEncordados.Infraestructure;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;
using BCrypt.Net;

namespace BackEncordados.Usuarios.Service.Auth;

/// <summary>
/// Servicio de autenticación que orquesta registro, inicio de sesión, cambio de contraseña
/// y recuperación de cuenta usando el patrón Result para manejo de errores tipados.
/// </summary>
/// <remarks>
/// <para><b>Dependencias inyectadas:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Parámetro</term>
///     <term>Tipo</term>
///     <description>Propósito</description>
///   </listheader>
///   <item>
///     <term><c>userRepository</c></term>
///     <term><see cref="IUserRepository"/></term>
///     <description>Acceso a datos de usuarios (búsqueda por username, email, ID; guardar y actualizar).</description>
///   </item>
///   <item>
///     <term><c>jwtService</c></term>
///     <term><see cref="IJwtService"/></term>
///     <description>Generación de tokens JWT firmados con la información del usuario.</description>
///   </item>
///   <item>
///     <term><c>logger</c></term>
///     <term><c>ILogger&lt;AuthService&gt;</c></term>
///     <description>Logging de eventos de autenticación (registros, inicios de sesión, cambios de contraseña).</description>
///   </item>
///   <item>
///     <term><c>cache</c></term>
///     <term><see cref="ICacheService"/></term>
///     <description>Almacenamiento temporal de GUIDs de cambio de contraseña con expiración de 60 minutos.</description>
///   </item>
///   <item>
///     <term><c>emailService</c></term>
///     <term><see cref="IEmailService"/></term>
///     <description>Envío de emails de bienvenida y recuperación de contraseña.</description>
///   </item>
/// </list>
/// <para>Sanitiza los usernames eliminando caracteres de nueva línea (\n, \r) antes de procesarlos.</para>
/// <para>Las contraseñas se hashean con BCrypt con factor de trabajo 11.</para>
/// </remarks>
public class AuthService(
    IUserRepository userRepository,
    IJwtService jwtService,
    ILogger<AuthService> logger,
    ICacheService cache,
    IEmailService emailService
) : IAuthService {
    private const string CacheKey = CacheKeys.PasswordChange;

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Sanitiza el username eliminando saltos de línea.</description></item>
    ///   <item><description>Verifica duplicados de username y email mediante <c>CheckDuplicatesAsync</c>.</description></item>
    ///   <item><description>Si hay duplicados, retorna <see cref="ConflictError"/>.</description></item>
    ///   <item><description>Genera el hash BCrypt de la contraseña con workFactor 11.</description></item>
    ///   <item><description>Crea la entidad <see cref="User"/> con rol USER y la persiste.</description></item>
    ///   <item><description>Genera el token JWT y la respuesta de autenticación.</description></item>
    ///   <item><description>Envía email de bienvenida de forma asíncrona (fire-and-forget con TapAsync).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="dto">Datos de registro: username, email y password.</param>
    /// <returns><see cref="AuthResponseDto"/> con token JWT y datos del usuario, o error de conflicto/validación.</returns>
    public async Task<Result<AuthResponseDto, AuthError>> SignUpAsync(RegisterDto dto)
    {
        var sanitizedUsername = dto.Username.Replace("\n", "").Replace("\r", "");
        logger.LogInformation("SignUp request for username: {Username}", sanitizedUsername);
        

        var duplicateCheck = await CheckDuplicatesAsync(dto);
        if (duplicateCheck.IsFailure)
        {
            return Result.Failure<AuthResponseDto, AuthError>(duplicateCheck.Error);
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 11);

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = User.UserRoles.USER,
            IsDeleted = false
        };

        var savedUser = await userRepository.SaveAsync(user);
        var authResponse = GenerateAuthResponse(savedUser);

        logger.LogInformation("User registered successfully: {Username}", sanitizedUsername);

        return await Result.Success<AuthResponseDto, AuthError>(authResponse).TapAsync(async _=> {
            logger.LogInformation("User registered successfully: {Username}", sanitizedUsername);
             await SendWelcomeEmail(dto.Username, dto.Email);
        });
    }

    /// <summary>
    /// Envía un email de bienvenida al nuevo usuario registrado.
    /// </summary>
    /// <param name="username">Nombre de usuario para personalizar el mensaje.</param>
    /// <param name="email">Dirección de correo del destinatario.</param>
    private async Task SendWelcomeEmail(string username, string email) {
        var message = new EmailMessage {
            To = email,
            Subject = "Bienvenido a Encordados Maestros",
            Body = EmailTemplates.AccountCreated(username, email),
            IsHtml = true
        };
        await emailService.EnqueueEmailAsync(message);
    }

    /// <summary>
    /// Inicia sesión con username y password.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Sanitiza el username eliminando saltos de línea.</description></item>
    ///   <item><description>Busca el usuario por username en el repositorio.</description></item>
    ///   <item><description>Si no existe, retorna <see cref="UnauthorizedError"/> (mensaje genérico "credenciales invalidas").</description></item>
    ///   <item><description>Verifica la contraseña con BCrypt.Verify.</description></item>
    ///   <item><description>Si la contraseña no coincide, retorna <see cref="UnauthorizedError"/>.</description></item>
    ///   <item><description>Genera el token JWT y la respuesta de autenticación.</description></item>
    /// </list>
    /// <para>Por seguridad, el mensaje de error es idéntico tanto si el usuario no existe como si la contraseña es incorrecta.</para>
    /// </remarks>
    /// <param name="dto">Credenciales de acceso: username y password.</param>
    /// <returns><see cref="AuthResponseDto"/> con token JWT, o <see cref="UnauthorizedError"/> si las credenciales son inválidas.</returns>
    public async Task<Result<AuthResponseDto, AuthError>> SignInAsync(LoginDto dto)
    {
        var sanitizedUsername = dto.Username.Replace("\n", "").Replace("\r", "");
        logger.LogInformation("SignIn request for username: {Username}", sanitizedUsername);
        

        var user = await userRepository.FindByUsernameAsync(dto.Username);
        if (user is null)
        {
            logger.LogWarning("SignIn fallido: Usuario no encontrado - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, AuthError>(
                new UnauthorizedError("credenciales invalidas")
            );
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            logger.LogWarning("SignIn fallido: Password inválido - {Username}", sanitizedUsername);
            return Result.Failure<AuthResponseDto, AuthError>(
                new UnauthorizedError("credenciales invalidas")
            );
        }

        var authResponse = GenerateAuthResponse(user);
        logger.LogInformation("Usuario inició sesión correctamente: {Username}", sanitizedUsername);

        return Result.Success<AuthResponseDto, AuthError>(authResponse);
    }

    /// <summary>
    /// Cambia la contraseña de un usuario usando un GUID de verificación almacenado en caché.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el GUID en caché para obtener el ULID del usuario.</description></item>
    ///   <item><description>Si la clave de caché expiró o no existe, retorna <see cref="PasswordChangeExpiredTimeout"/>.</description></item>
    ///   <item><description>Busca el usuario por ULID. Si no existe, retorna <see cref="PasswordChangeExpiredTimeout"/>.</description></item>
    ///   <item><description>Verifica que la nueva contraseña no sea igual a la anterior usando BCrypt.Verify.</description></item>
    ///   <item><description>Si es igual, retorna <see cref="ValidationError"/>.</description></item>
    ///   <item><description>Hashea la nueva contraseña y actualiza el usuario.</description></item>
    ///   <item><description>Invalida la clave de caché del GUID para evitar reuso.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="guid">GUID de verificación unique recibido por email.</param>
    /// <param name="dto">DTO con la nueva contraseña y su confirmación.</param>
    /// <returns>Unit en éxito, o error si el GUID expiró, el usuario no existe, o la contraseña se repite.</returns>
    public async Task<Result<Unit, AuthError>> ChangePasswordAsync(Guid guid, ChangePasswordRequestDto dto) {
        logger.LogInformation("ChangePassword request for guid: {Guid}", guid);
        var cached= await cache.GetAsync<Ulid?>(CacheKey+guid);
        if (cached is null) 
            return Result.Failure<Unit, AuthError>(new PasswordChangeExpiredTimeout())
                .TapError((() => logger.LogInformation("Change password request for guid not found on cache: {Guid}", guid)));
        var user= await userRepository.FindByIdAsync(cached.Value);
        if (user is null)
            return Result.Failure<Unit, AuthError>(new PasswordChangeExpiredTimeout())
                .TapError((() => logger.LogInformation("Change password request for guid: {Guid} not found a user with {Id}", guid, cached.Value)));
        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, user.PasswordHash)) {
            return Result.Failure<Unit, AuthError>(new ValidationError("no puedes usar una contraseña anteriormente usada"))
                .TapError((() => logger.LogInformation("Change password request for guid: {Guid} is the same that have before", guid)));
        }
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        return await  userRepository.UpdateAsync(user) is { } result
            ? await Result.Success<Unit, AuthError>(Unit.Value)
                .TapAsync((async _ => {
                    logger.LogInformation("Password has change for request {Guid}", guid);
                    await cache.RemoveAsync(CacheKey+guid);
                }))
            : Result.Failure<Unit, AuthError>(new PasswordChangeExpiredTimeout())
                .TapError((() => logger.LogInformation("user no found with {Id}",cached.Value)));
    }
    
    /// <summary>
    /// Inicia el proceso de recuperación de contraseña enviando un email con enlace de cambio.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Busca el usuario por email en el repositorio.</description></item>
    ///   <item><description>Si el email existe, genera un GUID, lo almacena en caché con expiración de 60 minutos asociado al ULID del usuario, y envía el email con el enlace.</description></item>
    ///   <item><description>Si el email no existe, retorna <see cref="UserNotFoundError"/>.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="userEmail">Dirección de correo electrónico registrada.</param>
    /// <returns>Unit en éxito (email enviado), o <see cref="UserNotFoundError"/> si el email no está registrado.</returns>
    public async Task<Result<Unit, AuthError>> GetEmailAsync(string userEmail) {
        return await userRepository.FindByEmailAsync(userEmail) is { } result
            ? await Result.Success<Unit, AuthError>(Unit.Value)
                .TapAsync(async (_) => {
                    var key = CacheKey + Guid.NewGuid();
                    await cache.SetAsync(key, result.Id,TimeSpan.FromMinutes(60));
                    await SendPasswordChangeEmail(userEmail, key);
                })
            : Result.Failure<Unit, AuthError>(new UserNotFoundError("el email no existe o es invalido"))
                .TapError((() => logger.LogInformation("user no found with email {Email}", userEmail)));
    }
    
    /// <summary>
    /// Envía un email con el enlace de cambio de contraseña.
    /// </summary>
    /// <param name="email">Dirección de correo del destinatario.</param>
    /// <param name="guid">GUID único para identificar la solicitud de cambio.</param>
    private Task SendPasswordChangeEmail(string email, string guid) {
        var passwordUrl= $"{AppConfig.Current.FrontendUrl}/changePassword?guid={guid}";
        var message = new EmailMessage {
            To = email,
            Subject = "Solicitud de cambio de contraseña",
            Body = EmailTemplates.PasswordReset(passwordUrl),
            IsHtml = true
        };
        return emailService.EnqueueEmailAsync(message);
    }
    
    /// <summary>
    /// Verifica que no existan duplicados de username ni email durante el registro.
    /// </summary>
    /// <param name="dto">DTO de registro con username y email.</param>
    /// <returns>UnitResult.Success si no hay duplicados, o <see cref="ConflictError"/> si existe conflicto.</returns>
    private async Task<UnitResult<AuthError>> CheckDuplicatesAsync(RegisterDto dto)
    {
        var existingUser = await userRepository.FindByUsernameAsync(dto.Username);
        if (existingUser is not null)
        {
            return UnitResult.Failure<AuthError>(new ConflictError("username ya en uso:" + existingUser.Username));
        }

        var existingEmail = await userRepository.FindByEmailAsync(dto.Email);
        if (existingEmail is not null)
        {
            return UnitResult.Failure<AuthError>(new ConflictError("email ya en uso" + existingEmail.Email));
        }

        return UnitResult.Success<AuthError>();
    }
    
    /// <summary>
    /// Genera la respuesta de autenticación combinando el token JWT con los datos del usuario.
    /// </summary>
    /// <param name="user">Usuario autenticado para el que se genera la respuesta.</param>
    /// <returns><see cref="AuthResponseDto"/> con token y datos del usuario (ID, username, email, rol, createdAt).</returns>
    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var token = jwtService.GenerateToken(user);

        var userDto = new UserDto(
            user.Id,
            user.Username,
            user.Email,
            user.Role,
            user.CreatedAt
        );

        return new AuthResponseDto(token, userDto);
    }
}