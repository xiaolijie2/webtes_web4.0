using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MobileECommerceAPI.Models;
using MobileECommerceAPI.Services;
using System.Linq;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/super-admin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _superAdminsFile = "super_admins.json";
        private readonly IPasswordService _passwordService;

        public SuperAdminController(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        /// <summary>
        /// 超级管理员登录API
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SuperAdminLoginRequest request)
        {
            try
            {
                Console.WriteLine($"Super admin login attempt for username: {request.Username}");
                
                var filePath = Path.Combine(_dataDirectory, _superAdminsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new SuperAdminLoginResponse 
                    { 
                        Success = false, 
                        Message = "超级管理员数据文件不存在" 
                    });
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var superAdmins = JsonSerializer.Deserialize<List<User>>(json, options);
                if (superAdmins == null)
                {
                    return BadRequest(new SuperAdminLoginResponse 
                    { 
                        Success = false, 
                        Message = "超级管理员数据格式错误" 
                    });
                }
                
                // 查找超级管理员账户
                var superAdmin = superAdmins.FirstOrDefault(sa => 
                    (sa.Username == request.Username || sa.Phone == request.Username) && 
                    sa.UserType == "super_admin" && 
                    sa.PermissionLevel == 0 &&
                    sa.IsActive
                );
                
                if (superAdmin == null)
                {
                    return NotFound(new SuperAdminLoginResponse 
                    { 
                        Success = false, 
                        Message = "超级管理员账户不存在或已被禁用" 
                    });
                }
                
                // 验证密码 - 使用统一的PasswordService
                bool isPasswordValid = false;
                if (!string.IsNullOrEmpty(superAdmin.Password))
                {
                    isPasswordValid = _passwordService.VerifyPassword(request.Password, superAdmin.Password);
                }
                
                if (!isPasswordValid)
                {
                    return BadRequest(new SuperAdminLoginResponse 
                    { 
                        Success = false, 
                        Message = "密码错误" 
                    });
                }
                
                // 更新最后登录时间
                superAdmin.LastLoginTime = DateTime.Now;
                
                // 保存更新后的数据
                var updatedJson = JsonSerializer.Serialize(superAdmins, new JsonSerializerOptions 
                { 
                    WriteIndented = true
                });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                // 生成简单的token（在实际生产环境中应该使用JWT）
                var superAdminForToken = new SuperAdmin
                {
                    Id = superAdmin.Id,
                    Username = superAdmin.Username ?? superAdmin.Phone,
                    UserType = superAdmin.UserType,
                    PermissionLevel = superAdmin.PermissionLevel
                };
                var token = GenerateToken(superAdminForToken);
                
                // 返回超级管理员信息（不包含密码）
                var adminInfo = new SuperAdminInfo
                {
                    Id = superAdmin.Id,
                    Username = superAdmin.Username ?? superAdmin.Phone,
                    UserType = superAdmin.UserType,
                    PermissionLevel = superAdmin.PermissionLevel,
                    LastLogin = superAdmin.LastLoginTime
                };
                
                return Ok(new SuperAdminLoginResponse 
                { 
                    Success = true, 
                    Message = "登录成功", 
                    SuperAdmin = adminInfo,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Super admin login error: {ex.Message}");
                return StatusCode(500, new SuperAdminLoginResponse 
                { 
                    Success = false, 
                    Message = "登录失败", 
                });
            }
        }

        /// <summary>
        /// 超级管理员权限验证API
        /// </summary>
        [HttpGet("verify")]
        public async Task<IActionResult> Verify()
        {
            try
            {
                // 从请求头获取Authorization token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new SuperAdminVerifyResponse 
                    { 
                        Valid = false 
                    });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // 验证token并获取超级管理员信息
                var adminInfo = ValidateToken(token);
                if (adminInfo == null)
                {
                    return Unauthorized(new SuperAdminVerifyResponse 
                    { 
                        Valid = false 
                    });
                }

                return Ok(new SuperAdminVerifyResponse 
                { 
                    Valid = true, 
                    SuperAdmin = adminInfo 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Super admin verify error: {ex.Message}");
                return StatusCode(500, new SuperAdminVerifyResponse 
                { 
                    Valid = false 
                });
            }
        }

        /// <summary>
        /// 生成简单的token（在实际生产环境中应该使用JWT）
        /// </summary>
        private string GenerateToken(SuperAdmin superAdmin)
        {
            var tokenData = new
            {
                id = superAdmin.Id,
                username = superAdmin.Username,
                userType = superAdmin.UserType,
                permissionLevel = superAdmin.PermissionLevel,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            var tokenJson = JsonSerializer.Serialize(tokenData);
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenJson));
        }

        /// <summary>
        /// 获取当前用户信息API
        /// </summary>
        [HttpGet("current-user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // 从请求头获取Authorization token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { success = false, message = "未授权访问" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // 验证token并获取超级管理员信息
                var adminInfo = ValidateToken(token);
                if (adminInfo == null)
                {
                    return Unauthorized(new { success = false, message = "Token无效或已过期" });
                }

                // 从文件中获取完整的用户信息
                var filePath = Path.Combine(_dataDirectory, _superAdminsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "用户数据文件不存在" });
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var superAdmins = JsonSerializer.Deserialize<List<User>>(json, options);
                var superAdmin = superAdmins?.FirstOrDefault(sa => sa.Id == adminInfo.Id);
                
                if (superAdmin == null)
                {
                    return NotFound(new { success = false, message = "用户不存在" });
                }

                return Ok(new 
                { 
                    success = true, 
                    data = new 
                    {
                        id = superAdmin.Id,
                        username = superAdmin.Username ?? superAdmin.Phone,
                        userType = superAdmin.UserType,
                        permissionLevel = superAdmin.PermissionLevel,
                        lastLogin = superAdmin.LastLoginTime,
                        createdAt = superAdmin.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get current user error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "获取用户信息失败" });
            }
        }

        /// <summary>
        /// 获取超级管理员列表API
        /// </summary>
        [HttpGet("administrators")]
        public async Task<IActionResult> GetSuperAdministrators()
        {
            try
            {
                // 从请求头获取Authorization token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { success = false, message = "未授权访问" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                
                // 验证token并获取超级管理员信息
                var adminInfo = ValidateToken(token);
                if (adminInfo == null)
                {
                    return Unauthorized(new { success = false, message = "Token无效或已过期" });
                }

                // 从文件中获取超级管理员数据
                var filePath = Path.Combine(_dataDirectory, _superAdminsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "超级管理员数据文件不存在" });
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var superAdmins = JsonSerializer.Deserialize<List<User>>(json, options);
                if (superAdmins == null)
                {
                    return BadRequest(new { success = false, message = "超级管理员数据格式错误" });
                }

                // 返回安全的超级管理员信息（不包含密码）
                var safeSuperAdmins = superAdmins
                    .Where(sa => sa.IsActive) // 只返回活跃的超级管理员
                    .Select(admin => new
                    {
                        Id = admin.Id,
                        Username = admin.Username ?? admin.Phone,
                        Name = admin.NickName ?? "超级管理员", // 使用NickName或固定显示名称
                        PermissionLevel = admin.PermissionLevel,
                        UserType = admin.UserType,
                        CreatedAt = admin.CreatedAt,
                        LastLogin = admin.LastLoginTime,
                        IsActive = admin.IsActive
                    }).ToList();

                return Ok(new 
                { 
                    success = true, 
                    data = safeSuperAdmins,
                    message = "获取超级管理员列表成功"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Get super administrators error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "获取超级管理员列表失败" });
            }
        }

        /// <summary>
        /// 修改用户名API
        /// </summary>
        [HttpPost("change-username")]
        public async Task<IActionResult> ChangeUsername([FromBody] ChangeUsernameRequest request)
        {
            try
            {
                // 验证请求参数
                if (string.IsNullOrWhiteSpace(request.NewUsername) || string.IsNullOrWhiteSpace(request.ConfirmUsername) || string.IsNullOrWhiteSpace(request.CurrentPassword))
                {
                    return BadRequest(new { success = false, message = "新用户名、确认用户名和当前密码不能为空" });
                }

                // 验证两次输入的用户名是否一致
                if (request.NewUsername != request.ConfirmUsername)
                {
                    return BadRequest(new { success = false, message = "两次输入的用户名不一致" });
                }

                // 验证用户名格式
                if (request.NewUsername.Length < 3 || request.NewUsername.Length > 20)
                {
                    return BadRequest(new { success = false, message = "用户名长度必须在3-20个字符之间" });
                }

                // 从请求头获取Authorization token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { success = false, message = "未授权访问" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var adminInfo = ValidateToken(token);
                if (adminInfo == null)
                {
                    return Unauthorized(new { success = false, message = "Token无效或已过期" });
                }

                var filePath = Path.Combine(_dataDirectory, _superAdminsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "用户数据文件不存在" });
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var superAdmins = JsonSerializer.Deserialize<List<User>>(json, options);
                if (superAdmins == null)
                {
                    return BadRequest(new { success = false, message = "数据格式错误" });
                }

                var currentUser = superAdmins.FirstOrDefault(sa => sa.Id == adminInfo.Id);
                if (currentUser == null)
                {
                    return NotFound(new { success = false, message = "用户不存在" });
                }

                // 验证当前密码 - 使用统一的PasswordService
                bool isPasswordValid = false;
                if (!string.IsNullOrEmpty(currentUser.Password))
                {
                    isPasswordValid = _passwordService.VerifyPassword(request.CurrentPassword, currentUser.Password);
                }

                if (!isPasswordValid)
                {
                    return BadRequest(new { success = false, message = "当前密码错误" });
                }

                // 检查新用户名是否已存在
                var existingUser = superAdmins.FirstOrDefault(sa => 
                    sa.Username == request.NewUsername && sa.Id != adminInfo.Id);
                if (existingUser != null)
                {
                    return BadRequest(new { success = false, message = "用户名已存在" });
                }

                // 更新用户名
                currentUser.Username = request.NewUsername;

                // 保存更新后的数据
                var updatedJson = JsonSerializer.Serialize(superAdmins, new JsonSerializerOptions 
                { 
                    WriteIndented = true
                });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);

                return Ok(new { success = true, message = "用户名修改成功", newUsername = request.NewUsername });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Change username error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "修改用户名失败" });
            }
        }

        /// <summary>
        /// 修改密码API
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // 验证请求参数
                if (string.IsNullOrWhiteSpace(request.CurrentPassword) || 
                    string.IsNullOrWhiteSpace(request.NewPassword) || 
                    string.IsNullOrWhiteSpace(request.ConfirmPassword))
                {
                    return BadRequest(new { success = false, message = "所有密码字段都不能为空" });
                }

                if (request.NewPassword != request.ConfirmPassword)
                {
                    return BadRequest(new { success = false, message = "新密码和确认密码不匹配" });
                }

                // 验证新密码强度
                if (request.NewPassword.Length < 6)
                {
                    return BadRequest(new { success = false, message = "新密码长度至少为6个字符" });
                }

                // 从请求头获取Authorization token
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized(new { success = false, message = "未授权访问" });
                }

                var token = authHeader.Substring("Bearer ".Length).Trim();
                var adminInfo = ValidateToken(token);
                if (adminInfo == null)
                {
                    return Unauthorized(new { success = false, message = "Token无效或已过期" });
                }

                var filePath = Path.Combine(_dataDirectory, _superAdminsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "用户数据文件不存在" });
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var superAdmins = JsonSerializer.Deserialize<List<User>>(json, options);
                if (superAdmins == null)
                {
                    return BadRequest(new { success = false, message = "数据格式错误" });
                }

                var currentUser = superAdmins.FirstOrDefault(sa => sa.Id == adminInfo.Id);
                if (currentUser == null)
                {
                    return NotFound(new { success = false, message = "用户不存在" });
                }

                // 验证当前密码 - 使用统一的PasswordService
                bool isPasswordValid = false;
                if (!string.IsNullOrEmpty(currentUser.Password))
                {
                    isPasswordValid = _passwordService.VerifyPassword(request.CurrentPassword, currentUser.Password);
                }

                if (!isPasswordValid)
                {
                    return BadRequest(new { success = false, message = "当前密码错误" });
                }

                // 更新密码 - 使用PasswordService加密存储
                currentUser.Password = _passwordService.HashPassword(request.NewPassword);

                // 保存更新后的数据
                var updatedJson = JsonSerializer.Serialize(superAdmins, new JsonSerializerOptions 
                { 
                    WriteIndented = true
                });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);

                return Ok(new { success = true, message = "密码修改成功" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Change password error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "修改密码失败" });
            }
        }

        /// <summary>
        /// 验证token并返回超级管理员信息
        /// </summary>
        private SuperAdminInfo? ValidateToken(string token)
        {
            try
            {
                var tokenJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenJson);
                
                // 检查token是否过期（延长到7天，提供更好的用户体验）
                var timestamp = tokenData.GetProperty("timestamp").GetInt64();
                var tokenTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                var currentTime = DateTimeOffset.UtcNow;
                var timeDifference = currentTime.Subtract(tokenTime);
                
                // 记录时间信息用于调试
                Console.WriteLine($"Token时间: {tokenTime}, 当前时间: {currentTime}, 时间差: {timeDifference.TotalHours}小时");
                
                if (timeDifference.TotalHours > 168) // 7天 = 168小时
                {
                    Console.WriteLine($"Token已过期，时间差: {timeDifference.TotalHours}小时");
                    return null;
                }
                
                // 验证权限级别
                var permissionLevel = tokenData.GetProperty("permissionLevel").GetInt32();
                var userType = tokenData.GetProperty("userType").GetString();
                
                if (permissionLevel != 0 || userType != "super_admin")
                {
                    Console.WriteLine($"权限验证失败，权限级别: {permissionLevel}, 用户类型: {userType}");
                    return null;
                }
                
                return new SuperAdminInfo
                {
                    Id = tokenData.GetProperty("id").GetString() ?? "",
                    Username = tokenData.GetProperty("username").GetString() ?? "",
                    UserType = userType ?? "",
                    PermissionLevel = permissionLevel
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token验证异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 一键清理所有用户数据
        /// </summary>
        [HttpPost("clear-all-users")]
        public async Task<IActionResult> ClearAllUsers([FromBody] ClearUsersRequest request)
        {
            try
            {
                // 验证确认文本
                if (request.ConfirmationText != "CLEAR ALL USERS")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "确认文本不正确，请输入 'CLEAR ALL USERS'"
                    });
                }

                var results = new List<string>();
                var errors = new List<string>();

                // 清理 agents.json
                try
                {
                    var agentsPath = Path.Combine(_dataDirectory, "agents.json");
                    await System.IO.File.WriteAllTextAsync(agentsPath, "[]");
                    results.Add("agents.json 已清理");
                }
                catch (Exception ex)
                {
                    errors.Add($"清理 agents.json 失败: {ex.Message}");
                }

                // 清理 users.json
                try
                {
                    var usersPath = Path.Combine(_dataDirectory, "users.json");
                    await System.IO.File.WriteAllTextAsync(usersPath, "[]");
                    results.Add("users.json 已清理");
                }
                catch (Exception ex)
                {
                    errors.Add($"清理 users.json 失败: {ex.Message}");
                }

                // 清理 regular_admins.json
                try
                {
                    var adminsPath = Path.Combine(_dataDirectory, "regular_admins.json");
                    await System.IO.File.WriteAllTextAsync(adminsPath, "[]");
                    results.Add("regular_admins.json 已清理");
                }
                catch (Exception ex)
                {
                    errors.Add($"清理 regular_admins.json 失败: {ex.Message}");
                }

                // 记录操作日志
                var logEntry = new
                {
                    timestamp = DateTime.Now,
                    operation = "CLEAR_ALL_USERS",
                    operatorId = request.OperatorId ?? "unknown",
                    results = results,
                    errors = errors
                };

                Console.WriteLine($"用户数据清理操作: {JsonSerializer.Serialize(logEntry)}");

                return Ok(new
                {
                    success = true,
                    message = "用户数据清理完成",
                    results = results,
                    errors = errors,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理用户数据时发生错误: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "清理用户数据时发生错误",
                    error = ex.Message
                });
            }
        }
    }

    // 请求模型类
    public class ChangeUsernameRequest
    {
        public string NewUsername { get; set; } = "";
        public string ConfirmUsername { get; set; } = "";
        public string CurrentPassword { get; set; } = "";
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }

    public class ClearUsersRequest
    {
        public string ConfirmationText { get; set; } = "";
        public string OperatorId { get; set; } = "";
    }
}