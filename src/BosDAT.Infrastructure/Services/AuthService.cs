using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BosDAT.Core.DTOs;
using BosDAT.Core.Entities;
using BosDAT.Core.Interfaces;
using BosDAT.Infrastructure.Data;

namespace BosDAT.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
        {
            return null;
        }

        if (!user.IsActive)
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingUser != null)
        {
            return null;
        }

        var user = new ApplicationUser
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            return null;
        }

        await _userManager.AddToRoleAsync(user, "User");

        return await GenerateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponseDto?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _context.Set<RefreshToken>()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (token == null || !token.IsActive)
        {
            return null;
        }

        // Revoke old token
        token.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var response = await GenerateAuthResponseAsync(token.User, cancellationToken);

        token.ReplacedByToken = response.RefreshToken;
        await _context.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _context.Set<RefreshToken>()
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (token == null || !token.IsActive)
        {
            return false;
        }

        token.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        return result.Succeeded;
    }

    public async Task<UserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Roles = roles.ToList()
        };
    }

    private async Task<AuthResponseDto> GenerateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = GenerateAccessToken(user, roles);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id, cancellationToken);

        return new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            }
        };
    }

    private string GenerateAccessToken(ApplicationUser user, IList<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.Email!)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
        };

        _context.Set<RefreshToken>().Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        return token;
    }
}
