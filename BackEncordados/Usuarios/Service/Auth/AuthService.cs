using BackEncordados.Common.Service.Cache;
using BackEncordados.Common.Service.Cache.keys;
using BackEncordados.Common.Utils;
using BackEncordados.Usuarios.Dto;
using BackEncordados.Usuarios.Errors;
using BackEncordados.Usuarios.Model;
using BackEncordados.Usuarios.Repository;
using CSharpFunctionalExtensions;
using BCrypt.Net;

namespace BackEncordados.Usuarios.Service.Auth;

/// <summary>
/// Servicio de autenticación usando Patrón Result.
/// Encapsula la lógica de autenticación con Programación Orientada al Resultado.
/// </summary>
public class AuthService(
    IUserRepository userRepository,
    IJwtService jwtService,
    ILogger<AuthService> logger,
    ICacheService cache
) : IAuthService
{
    private const string CacheKey = CacheKeys.PasswordChange;
    /// <summary>
    /// Registra un nuevo usuario.
    /// Devuelve: Result.Success(AuthResponseDto) | Result.Failure(Validation/Conflict)
    /// </summary>
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

        return Result.Success<AuthResponseDto, AuthError>(authResponse);
    }

    /// <summary>
    /// Autentica un usuario existente.
    /// Devuelve: Result.Success(AuthResponseDto) | Result.Failure(Validation/Unauthorized/NotFound)
    /// </summary>
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
            ? Result.Success<Unit, AuthError>(Unit.Value)
                .Tap((() => logger.LogInformation("Password has change for request {Guid}", guid)))
            : Result.Failure<Unit, AuthError>(new PasswordChangeExpiredTimeout())
                .TapError((() => logger.LogInformation("user no found with {Id}",cached.Value)));
    }
    

    public async Task<Result<Unit, AuthError>> GetEmailAsync(string userEmail) {
        return await userRepository.FindByEmailAsync(userEmail) is { } result
            ? await Result.Success<Unit, AuthError>(Unit.Value)
                .TapAsync(async (_) => {
                    var key = CacheKey + Guid.NewGuid();
                    await cache.SetAsync(key, result.Id);
                })
            : Result.Failure<Unit, AuthError>(new UserNotFoundError("el email no existe o es invalido"))
                .TapError((() => logger.LogInformation("user no found with email {Email}", userEmail)));
    }


    /// <summary>
    /// Verifica duplicados de username y email.
    /// Devuelve: UnitResult.Success | UnitResult.Failure(Conflict)
    /// </summary>
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
    /// Genera la respuesta de autenticación con token JWT.
    /// Devuelve: AuthResponseDto
    /// </summary>
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