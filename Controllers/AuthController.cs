using Microsoft.AspNetCore.Mvc;
using MobileECommerceAPI.Models;
using MobileECommerceAPI.Services;
using Newtonsoft.Json;
using BCrypt.Net;
using System.Text;
using System.Text.Json;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly string _adminsFilePath;
        private readonly JwtService _jwtService;
        private readonly IPasswordService _passwordService;

        public AuthController(UserService userService, JwtService jwtService, IPasswordService passwordService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _passwordService = passwordService;
            _logger = logger;
            _adminsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "admins.json");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _userService.LoginAsync(request.Phone, request.Password);
                
                if (result.Success)
                {
                    var redirectUrl = GetRedirectUrlByPermissionLevel(result.PermissionLevel);
                    
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        token = result.Token,
                        user = result.User,
                        permissionLevel = result.PermissionLevel,
                        redirectUrl = redirectUrl
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = result.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "登录失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 根据权限等级获取跳转URL
        /// </summary>
        /// <param name="permissionLevel">权限等级：0-超级管理员，1-管理员，2-业务员，3-注册用户</param>
        /// <returns>跳转URL</returns>
        private string GetRedirectUrlByPermissionLevel(int permissionLevel)
        {
            return permissionLevel switch
            {
                0 => "/admin.html",      // 超级管理员 -> 后台管理
                1 => "/admin.html",      // 管理员 -> 后台管理
                2 => "/salesperson.html", // 业务员 -> 业务员工作页面
                3 => "/home.html",       // 注册用户 -> 主页
                _ => "/login.html"       // 未知权限 -> 重新登录
            };
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // 调试日志：记录接收到的请求数据
                Console.WriteLine($"Register request received:");
                Console.WriteLine($"Request is null: {request == null}");
                if (request != null)
                {
                    Console.WriteLine($"FullPhoneNumber: '{request.FullPhoneNumber}'");
                    Console.WriteLine($"Phone: '{request.Phone}'");
                    Console.WriteLine($"Password: '{request.Password}'");
                    Console.WriteLine($"InviteCode: '{request.InviteCode}'");
                    Console.WriteLine($"CountryCode: '{request.CountryCode}'");
                }
                
                // 检查基本字段是否为空（邀请码为可选）
                if (request == null || 
                    string.IsNullOrWhiteSpace(request.Phone) || 
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    Console.WriteLine("Returning: 手机号和密码不能为空");
                    return BadRequest(new { success = false, message = "手机号和密码不能为空", token = "", user = (object?)null });
                }
                
                // 验证手机号码格式：必须为数字，长度不低于8位
                if (!IsValidPhoneNumber(request.Phone))
                {
                    return BadRequest(new { success = false, message = "手机号码格式不正确，必须为数字且长度不低于8位" });
                }

                // 组合完整的手机号码（区号+手机号码）
                string fullPhoneNumber = request.CountryCode + request.Phone;
                Console.WriteLine($"Generated FullPhoneNumber: '{fullPhoneNumber}'");

                // 验证密码长度
                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                {
                    return BadRequest(new { success = false, message = "密码长度不能少于6位" });
                }

                // 验证邀请码格式
                if (!string.IsNullOrWhiteSpace(request.InviteCode) && !IsValidInviteCode(request.InviteCode))
                {
                    return BadRequest(new { success = false, message = "邀请码格式不正确" });
                }
                
                var result = await _userService.RegisterAsync(fullPhoneNumber, request.Password, request.InviteCode);
                
                if (result.Success)
                {
                    return Ok(new { success = true, message = "注册成功", token = result.Token, user = result.User });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册失败");
                return StatusCode(500, new { success = false, message = "服务器内部错误" });
            }
        }

        private static bool IsValidPhoneNumber(string phone)
        {
            // 验证手机号码：必须为数字，长度不低于8位
            return !string.IsNullOrWhiteSpace(phone) && phone.Length >= 8 && phone.All(char.IsDigit);
        }

        private bool IsValidInviteCode(string inviteCode)
        {
            // Validate 6-character alphanumeric invite code
            return inviteCode.Length == 6 && inviteCode.All(c => char.IsLetterOrDigit(c));
        }

        /// <summary>
        /// 管理员登录验证
        /// </summary>
        [HttpPost("admin-login")]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "用户名和密码不能为空"
                    });
                }

                // 从独立数据库验证管理员
                var adminResult = await ValidateAdminAsync(request.Username, request.Password);
                
                if (adminResult.Success)
                {
                    // 更新最后登录时间
                    await UpdateAdminLastLoginAsync(adminResult.Admin.Id);

                    var redirectUrl = GetAdminRedirectUrl(adminResult.Admin.PermissionLevel);
                    
                    return Ok(new
                    {
                        success = true,
                        message = "登录成功",
                        token = _jwtService.GenerateAdminToken(adminResult.Admin),
                        adminInfo = new
                        {
                            id = adminResult.Admin.Id,
                            username = adminResult.Admin.Username,
                            name = adminResult.Admin.Name,
                            permissionLevel = adminResult.Admin.PermissionLevel,
                            userType = "admin"
                        },
                        redirectUrl = redirectUrl
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = adminResult.Message
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "管理员登录失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "登录失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 验证管理员凭据
        /// </summary>
        private async Task<AdminValidationResult> ValidateAdminAsync(string username, string password)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                User admin = null;
                string adminFilePath = "";

                // 首先在super_admins.json中查找
                var superAdminsPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "super_admins.json");
                if (System.IO.File.Exists(superAdminsPath))
                {
                    var superAdminsJson = await System.IO.File.ReadAllTextAsync(superAdminsPath);
                    var superAdmins = System.Text.Json.JsonSerializer.Deserialize<List<User>>(superAdminsJson, options) ?? new List<User>();
                    
                    admin = superAdmins.FirstOrDefault(u => 
                        (u.Phone == username || u.Username == username) && 
                        u.UserType == "super_admin" &&
                        u.IsActive != false
                    );
                    
                    if (admin != null)
                    {
                        adminFilePath = superAdminsPath;
                    }
                }

                // 如果在super_admins.json中没找到，在regular_admins.json中查找
                if (admin == null)
                {
                    var regularAdminsPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "regular_admins.json");
                    if (System.IO.File.Exists(regularAdminsPath))
                    {
                        var regularAdminsJson = await System.IO.File.ReadAllTextAsync(regularAdminsPath);
                        var regularAdmins = System.Text.Json.JsonSerializer.Deserialize<List<User>>(regularAdminsJson, options) ?? new List<User>();
                        
                        admin = regularAdmins.FirstOrDefault(u => 
                            (u.Phone == username || u.Username == username) && 
                            u.UserType == "admin" &&
                            u.IsActive != false
                        );
                        
                        if (admin != null)
                        {
                            adminFilePath = regularAdminsPath;
                        }
                    }
                }

                if (admin == null)
                {
                    return new AdminValidationResult { Success = false, Message = "用户名或密码错误" };
                }

                // 使用统一的PasswordService验证密码
                bool isPasswordValid = false;
                if (!string.IsNullOrEmpty(admin.Password))
                {
                    isPasswordValid = _passwordService.VerifyPassword(password, admin.Password);
                }
                
                if (!isPasswordValid)
                {
                    return new AdminValidationResult { Success = false, Message = "用户名或密码错误" };
                }

                return new AdminValidationResult 
                { 
                    Success = true, 
                    Message = "验证成功", 
                    Admin = admin
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证管理员失败");
                return new AdminValidationResult { Success = false, Message = "验证失败" };
            }
        }

        /// <summary>
        /// 更新管理员最后登录时间
        /// </summary>
        private async Task UpdateAdminLastLoginAsync(string adminId)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                // 首先在super_admins.json中查找
                var superAdminsPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "super_admins.json");
                if (System.IO.File.Exists(superAdminsPath))
                {
                    var superAdminsJson = await System.IO.File.ReadAllTextAsync(superAdminsPath);
                    var superAdmins = System.Text.Json.JsonSerializer.Deserialize<List<User>>(superAdminsJson, options) ?? new List<User>();
                    
                    var superAdminIndex = superAdmins.FindIndex(u => u.Id == adminId);
                    if (superAdminIndex >= 0)
                    {
                        superAdmins[superAdminIndex].LastLoginTime = DateTime.Now;
                        var updatedJson = System.Text.Json.JsonSerializer.Serialize(superAdmins, new JsonSerializerOptions { WriteIndented = true });
                        await System.IO.File.WriteAllTextAsync(superAdminsPath, updatedJson, Encoding.UTF8);
                        return;
                    }
                }

                // 如果在super_admins.json中没找到，在regular_admins.json中查找
                var regularAdminsPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "regular_admins.json");
                if (System.IO.File.Exists(regularAdminsPath))
                {
                    var regularAdminsJson = await System.IO.File.ReadAllTextAsync(regularAdminsPath);
                    var regularAdmins = System.Text.Json.JsonSerializer.Deserialize<List<User>>(regularAdminsJson, options) ?? new List<User>();
                    
                    var adminIndex = regularAdmins.FindIndex(u => u.Id == adminId);
                    if (adminIndex >= 0)
                    {
                        regularAdmins[adminIndex].LastLoginTime = DateTime.Now;
                        var updatedJson = System.Text.Json.JsonSerializer.Serialize(regularAdmins, new JsonSerializerOptions { WriteIndented = true });
                        await System.IO.File.WriteAllTextAsync(regularAdminsPath, updatedJson, Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新管理员登录时间失败");
            }
        }



        /// <summary>
        /// 根据管理员权限等级获取跳转URL
        /// </summary>
        private string GetAdminRedirectUrl(int permissionLevel)
        {
            return permissionLevel switch
            {
                0 => "/super-admin.html",  // 超级管理员 -> 超级管理员后台
                1 => "/admin.html",        // 高级管理员 -> 管理员后台
                2 => "/admin.html",        // 普通管理员 -> 管理员后台
                3 => "/admin.html",        // 初级管理员 -> 管理员后台
                _ => "/admin-login.html"   // 未知权限 -> 重新登录
            };
        }
    }

    #region 管理员相关数据模型

    public class AdminLoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class AdminValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public User? Admin { get; set; }
    }

    #endregion
}