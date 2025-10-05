using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MobileECommerceAPI.Models;
using TaskPlatform.Controllers;

namespace MobileECommerceAPI.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration, ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateToken(User user)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.MobilePhone, user.Phone),
                    new Claim(ClaimTypes.Name, user.NickName),
                    new Claim("VipLevel", user.VipLevel.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "1440");
                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
                throw;
            }
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var tokenHandler = new JwtSecurityTokenHandler();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return null;
            }
        }

        public int? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// 从令牌中获取用户ID（字符串类型）
        /// </summary>
        public string? GetUserIdStringFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// 为管理员生成JWT令牌
        /// </summary>
        public string GenerateAdminToken(User admin)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, admin.Id),
                    new Claim(ClaimTypes.Name, admin.Username),
                    new Claim("DisplayName", admin.Name),
                    new Claim("PermissionLevel", admin.PermissionLevel.ToString()),
                    new Claim("UserType", "admin"),
                    new Claim(ClaimTypes.Email, admin.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var expireMinutes = int.Parse(_configuration["Jwt:ExpireMinutes"] ?? "1440");
                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                    signingCredentials: credentials
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for admin {AdminId}", admin.Id);
                throw;
            }
        }

        /// <summary>
        /// 从令牌中获取管理员ID
        /// </summary>
        public string? GetAdminIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// 从令牌中获取用户类型
        /// </summary>
        public string? GetUserTypeFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            return principal.FindFirst("UserType")?.Value;
        }

        /// <summary>
        /// 从令牌中获取权限等级
        /// </summary>
        public int? GetPermissionLevelFromToken(string token)
        {
            var principal = ValidateToken(token);
            if (principal == null) return null;

            var permissionLevelClaim = principal.FindFirst("PermissionLevel")?.Value;
            return int.TryParse(permissionLevelClaim, out var level) ? level : null;
        }
    }
}