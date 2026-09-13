using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SGInsurance.Application.DTOs;
using SGInsurance.Application.Interfaces;
using SGInsurance.Domain.Entities;

namespace SGInsurance.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserDto?> GetByIdAsync(Guid userId);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ICustomerRepository _customers;
    private readonly INotificationService _notifications;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        ICustomerRepository customers,
        INotificationService notifications,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _users = users;
        _roles = roles;
        _customers = customers;
        _notifications = notifications;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _users.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email is already registered.");

        var customerRole = await _roles.GetByCodeAsync("CUSTOMER")
            ?? throw new InvalidOperationException("CUSTOMER role not seeded.");

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Mobile = request.Mobile,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        user.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = customerRole.RoleId });

        await _users.AddAsync(user);
        await _users.SaveChangesAsync();

        // Also create a linked customer record so the user can immediately request quotes.
        var mobile = string.IsNullOrWhiteSpace(request.Mobile) ? $"9{Random.Shared.Next(100000000, 999999999)}" : request.Mobile!;
        var customer = new Customer
        {
            CustomerId = Guid.NewGuid(),
            UserId = user.UserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Mobile = mobile,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _customers.AddAsync(customer);
        await _customers.SaveChangesAsync();

        _logger.LogInformation("User registered: {Email}", request.Email);

        await _notifications.SendAsync("WELCOME_EMAIL", customer.CustomerId, user.Email,
            new Dictionary<string, string?> { ["firstName"] = user.FirstName });

        return BuildAuthResponse(user, new List<string> { "CUSTOMER" });
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        var roles = user.UserRoles.Select(ur => ur.Role.RoleCode).ToList();
        _logger.LogInformation("User logged in: {Email}", request.Email);
        return BuildAuthResponse(user, roles);
    }

    public async Task<UserDto?> GetByIdAsync(Guid userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user == null) return null;
        var full = await _users.GetByEmailAsync(user.Email);
        var roles = full?.UserRoles.Select(ur => ur.Role.RoleCode).ToList() ?? new List<string>();
        return new UserDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Mobile = user.Mobile,
            Roles = roles
        };
    }

    private AuthResponse BuildAuthResponse(User user, List<string> roles)
    {
        var jwtSection = _config.GetSection("Jwt");
        var key = jwtSection["Key"] ?? "sginsurance-dev-super-secret-key-change-me-32chars";
        var issuer = jwtSection["Issuer"] ?? "SGInsuranceAPI";
        var audience = jwtSection["Audience"] ?? "SGInsuranceClients";
        var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var m) ? m : 480;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expires = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires.UtcDateTime, signingCredentials: creds);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires,
            User = new UserDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Mobile = user.Mobile,
                Roles = roles
            }
        };
    }
}
