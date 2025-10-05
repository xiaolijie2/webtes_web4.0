using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using MobileECommerceAPI.Models;
using MobileECommerceAPI.Services;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminManagementController : ControllerBase
    {
        private readonly ILogger<AdminManagementController> _logger;
        private readonly IPasswordService _passwordService;
        private readonly string _adminsFilePath;

        public AdminManagementController(ILogger<AdminManagementController> logger, IPasswordService passwordService)
        {
            _logger = logger;
            _passwordService = passwordService;
            _adminsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "regular_admins.json");
        }

        /// <summary>
        /// 获取管理员列表
        /// </summary>
        [HttpGet("administrators")]
        public async Task<IActionResult> GetAdministrators()
        {
            try
            {
                var admins = await LoadAdminsAsync();
                
                // 移除密码，只返回安全信息
                var safeAdmins = admins.Select(admin => new
                {
                    Id = admin.Id,
                    Username = admin.Username,
                    Phone = admin.Phone,
                    NickName = admin.NickName,
                    Name = admin.Name,
                    PermissionLevel = admin.PermissionLevel,
                    CreatedAt = admin.CreatedAt,
                    RegisterTime = admin.RegisterTime,
                    LastLoginTime = admin.LastLoginTime,
                    IsActive = admin.IsActive,
                    UserType = admin.UserType,
                    Status = admin.Status
                }).ToList();

                var response = new
                {
                    success = true,
                    data = safeAdmins,
                    message = "获取成功"
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    WriteIndented = true
                };
                
                var jsonString = System.Text.Json.JsonSerializer.Serialize(response, options);
                return Content(jsonString, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取管理员列表失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "获取管理员列表失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 添加管理员
        /// </summary>
        [HttpPost("administrators")]
        public async Task<IActionResult> AddAdministrator([FromBody] AddAdminRequest request)
        {
            try
            {
                // 验证输入
                if (string.IsNullOrWhiteSpace(request.Username) || 
                    string.IsNullOrWhiteSpace(request.Password) || 
                    string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "用户名、密码和姓名不能为空"
                    });
                }

                // 验证权限等级 (1-3: 普通管理员权限范围)
                if (request.PermissionLevel < 1 || request.PermissionLevel > 3)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "权限等级必须在1-3之间"
                    });
                }

                var admins = await LoadAdminsAsync();

                // 检查用户名是否已存在
                if (admins.Any(a => a.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase) ||
                                   a.Phone.Equals(request.Username, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "用户名已存在"
                    });
                }

                // 使用PasswordService加密密码
                var encryptedPassword = _passwordService.HashPassword(request.Password);

                // 创建新管理员 (使用User模型)
                var newAdmin = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = request.Username,
                    Phone = request.Username, // 使用用户名作为Phone
                    Password = encryptedPassword,
                    NickName = request.Name,
                    Name = request.Name,
                    CurrentBalance = 0.0m,
                    FrozenAmount = 0.0m,
                    CreditScore = 0,
                    VipLevel = 0,
                    VipExpireAt = null,
                    InviteCodeUsed = "",
                    InviterId = null,
                    IsActive = true,
                    IsAdmin = true,
                    RegisterTime = DateTime.Now,
                    LastLoginTime = DateTime.Now,
                    Email = "",
                    Avatar = "",
                    PhoneVerified = false,
                    BankCardCount = 0,
                    CreatedAt = DateTime.Now,
                    UserType = "admin",
                    Status = "active",
                    PermissionLevel = request.PermissionLevel,
                    FirstName = "",
                    LastName = "",
                    Country = "",
                    City = "",
                    State = "",
                    Address = "",
                    ZipCode = ""
                };

                admins.Add(newAdmin);
                await SaveAdminsAsync(admins);

                return Ok(new
                {
                    success = true,
                    message = "管理员添加成功",
                    data = new
                    {
                        id = newAdmin.Id,
                        username = newAdmin.Username,
                        name = newAdmin.Name,
                        permissionLevel = newAdmin.PermissionLevel,
                        createdAt = newAdmin.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加管理员失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "添加管理员失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 更新管理员信息
        /// </summary>
        [HttpPut("administrators/{id}")]
        public async Task<IActionResult> UpdateAdministrator(string id, [FromBody] UpdateAdminRequest request)
        {
            try
            {
                var admins = await LoadAdminsAsync();
                var admin = admins.FirstOrDefault(a => a.Id == id);

                if (admin == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "管理员不存在"
                    });
                }

                // 更新基本信息
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    admin.Name = request.Name;
                    admin.NickName = request.Name;
                }

                // 更新密码（如果提供）
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    admin.Password = _passwordService.HashPassword(request.Password);
                }

                admin.LastLoginTime = DateTime.Now;

                await SaveAdminsAsync(admins);

                return Ok(new
                {
                    success = true,
                    message = "管理员信息更新成功",
                    data = new
                    {
                        id = admin.Id,
                        username = admin.Username,
                        name = admin.Name,
                        permissionLevel = admin.PermissionLevel,
                        updatedAt = admin.LastLoginTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新管理员信息失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "更新管理员信息失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 删除管理员
        /// </summary>
        [HttpDelete("administrators/{id}")]
        public async Task<IActionResult> DeleteAdministrator(string id)
        {
            try
            {
                var admins = await LoadAdminsAsync();
                var admin = admins.FirstOrDefault(a => a.Id == id);

                if (admin == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "管理员不存在"
                    });
                }

                admins.Remove(admin);
                await SaveAdminsAsync(admins);

                return Ok(new
                {
                    success = true,
                    message = "管理员删除成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除管理员失败");
                return StatusCode(500, new
                {
                    success = false,
                    message = "删除管理员失败",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 获取权限等级列表
        /// </summary>
        [HttpGet("permission-levels")]
        public IActionResult GetPermissionLevels()
        {
            var permissionLevels = new[]
            {
                new { level = 1, name = "高级管理员", description = "拥有大部分管理权限" },
                new { level = 2, name = "普通管理员", description = "拥有基本管理权限" },
                new { level = 3, name = "业务员", description = "拥有业务相关权限" }
            };

            return Ok(new
            {
                success = true,
                data = permissionLevels,
                message = "获取成功"
            });
        }

        #region 私有方法

        private async Task<List<User>> LoadAdminsAsync()
        {
            try
            {
                if (!System.IO.File.Exists(_adminsFilePath))
                {
                    return new List<User>();
                }

                var jsonContent = await System.IO.File.ReadAllTextAsync(_adminsFilePath, Encoding.UTF8);
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    return new List<User>();
                }

                var admins = JsonConvert.DeserializeObject<List<User>>(jsonContent);
                return admins ?? new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载管理员数据失败");
                return new List<User>();
            }
        }

        private async Task SaveAdminsAsync(List<User> admins)
        {
            try
            {
                var directory = Path.GetDirectoryName(_adminsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonConvert.SerializeObject(admins, Formatting.Indented);
                await System.IO.File.WriteAllTextAsync(_adminsFilePath, jsonContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存管理员数据失败");
                throw;
            }
        }

        #endregion
    }

    #region 数据模型

    public class AddAdminRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public int PermissionLevel { get; set; } = 2; // 默认为普通管理员
    }

    public class UpdateAdminRequest
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
    }

    #endregion
}