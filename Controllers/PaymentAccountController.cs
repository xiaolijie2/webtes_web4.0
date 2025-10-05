using Microsoft.AspNetCore.Mvc;
using MobileECommerceAPI.Models;
using System.Text.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace MobileECommerceAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class PaymentAccountController : ControllerBase
    {
        private readonly string _dataFilePath;
        private readonly string _usersFilePath;
        private readonly string _superAdminsFilePath;
        private readonly ILogger<PaymentAccountController> _logger;

        public PaymentAccountController(ILogger<PaymentAccountController> logger)
        {
            _logger = logger;
            _dataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "payment_accounts.json");
            _usersFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "users.json");
            _superAdminsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "super_admins.json");
        }

        // 权限检查方法
        private async Task<(bool hasPermission, string message)> CheckPaymentAccountPermissionAsync(string? userId = null)
        {
            try
            {
                _logger.LogInformation($"开始权限检查，传入userId: {userId}");
                
                // 如果没有提供userId，从请求头中获取
                if (string.IsNullOrEmpty(userId))
                {
                    userId = Request.Headers["X-User-Id"].FirstOrDefault();
                    _logger.LogInformation($"从请求头获取userId: {userId}");
                }

                // 如果仍然没有userId，拒绝访问
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("未提供用户ID");
                    return (false, "未提供用户ID");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                // 首先检查super_admins.json文件
                if (System.IO.File.Exists(_superAdminsFilePath))
                {
                    _logger.LogInformation($"检查超级管理员文件: {_superAdminsFilePath}");
                    var superAdminJson = await System.IO.File.ReadAllTextAsync(_superAdminsFilePath);
                    _logger.LogInformation($"读取超级管理员数据文件成功，内容长度: {superAdminJson.Length}");
                    
                    try
                    {
                        var superAdmins = JsonSerializer.Deserialize<List<User>>(superAdminJson, options) ?? new List<User>();
                        
                        foreach (var admin in superAdmins)
                        {
                            _logger.LogInformation($"检查超级管理员: id={admin.Id}, userType={admin.UserType}, isActive={admin.IsActive}");
                            
                            // 支持ID映射：1 -> super_001, 2 -> super_002
                            var mappedUserId = userId;
                            if (userId == "1") mappedUserId = "super_001";
                            else if (userId == "2") mappedUserId = "super_002";
                            
                            if (admin.Id == mappedUserId && admin.UserType == "super_admin" && admin.IsActive != false)
                            {
                                _logger.LogInformation($"在超级管理员文件中找到用户: userId={userId}, mappedUserId={mappedUserId}, userType={admin.UserType}");
                                return (true, "超级管理员权限验证通过");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析超级管理员数据失败，继续检查普通用户数据");
                    }
                }

                // 如果在super_admins.json中没找到，检查users.json文件
                if (!System.IO.File.Exists(_usersFilePath))
                {
                    _logger.LogWarning($"用户数据文件不存在: {_usersFilePath}");
                    return (false, "用户数据文件不存在");
                }

                var json = await System.IO.File.ReadAllTextAsync(_usersFilePath);
                _logger.LogInformation($"读取用户数据文件成功，内容长度: {json.Length}");

                var users = JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
                _logger.LogInformation($"解析用户数据成功，用户数量: {users.Count}");
                
                var user = users.FirstOrDefault(u => u.Id == userId);
                _logger.LogInformation($"查找用户结果: userId={userId}, 找到用户={user != null}");

                if (user == null)
                {
                    _logger.LogWarning($"用户不存在: {userId}");
                    return (false, "用户不存在");
                }

                var userType = user.UserType ?? "admin";
                _logger.LogInformation($"用户类型: {userType}");

                // 只有超级管理员可以访问收款账户管理
                if (userType != "super_admin")
                {
                    _logger.LogWarning($"用户权限不足: userId={userId}, userType={userType}");
                    return (false, "普通管理员无权访问收款账户设置");
                }

                _logger.LogInformation($"权限检查通过: userId={userId}, userType={userType}");
                return (true, "有权限访问");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "权限检查失败");
                return (false, "权限检查失败");
            }
        }

        // GET /api/payment-accounts - 获取所有收款账户（用户端）
        [HttpGet("payment-accounts")]
        public async Task<IActionResult> GetPaymentAccounts()
        {
            try
            {
                var accounts = await LoadPaymentAccountsAsync();
                var activeAccounts = accounts.Where(a => a.IsActive).ToList();
                
                return Ok(new PaymentAccountResponse
                {
                    Success = true,
                    Message = "获取收款账户成功",
                    Data = activeAccounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收款账户失败");
                return StatusCode(500, new PaymentAccountResponse
                {
                    Success = false,
                    Message = "获取收款账户失败"
                });
            }
        }

        // GET /api/admin/payment-accounts - 获取所有收款账户（管理员端）
        [HttpGet("admin/payment-accounts")]
        public async Task<IActionResult> GetAllPaymentAccounts([FromQuery] string? userId = null)
        {
            try
            {
                _logger.LogInformation($"收到获取收款账户请求，userId参数: {userId}");
                
                // 权限检查
                var (hasPermission, message) = await CheckPaymentAccountPermissionAsync(userId);
                _logger.LogInformation($"权限检查结果: hasPermission={hasPermission}, message={message}");
                
                if (!hasPermission)
                {
                    return StatusCode(403, new PaymentAccountResponse
                    {
                        Success = false,
                        Message = message
                    });
                }

                var accounts = await LoadPaymentAccountsAsync();
                
                return Ok(new PaymentAccountResponse
                {
                    Success = true,
                    Message = "获取收款账户成功",
                    Data = accounts
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取收款账户失败");
                return StatusCode(500, new PaymentAccountResponse
                {
                    Success = false,
                    Message = "获取收款账户失败"
                });
            }
        }

        // POST /api/admin/payment-accounts - 添加收款账户
        [HttpPost("admin/payment-accounts")]
        public async Task<IActionResult> CreatePaymentAccount([FromBody] PaymentAccountRequest request, [FromQuery] string? userId = null)
        {
            try
            {
                // 权限检查
                var (hasPermission, message) = await CheckPaymentAccountPermissionAsync(userId);
                if (!hasPermission)
                {
                    return StatusCode(403, new PaymentAccountResponse
                    {
                        Success = false,
                        Message = message
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new PaymentAccountResponse
                    {
                        Success = false,
                        Message = "请求参数无效"
                    });
                }

                var accounts = await LoadPaymentAccountsAsync();
                
                // 检查是否已存在相同的收款地址
                if (accounts.Any(a => a.AccountNumber == request.AccountNumber))
                {
                    return BadRequest(new PaymentAccountResponse
                    {
                        Success = false,
                        Message = "该收款地址已存在"
                    });
                }

                // 如果设置为默认账户，需要将其他账户的默认状态取消
                if (request.IsDefault)
                {
                    foreach (var account in accounts)
                    {
                        account.IsDefault = false;
                    }
                }

                var newAccount = new PaymentAccount
                {
                    Id = Guid.NewGuid().ToString(),
                    WalletName = request.WalletName,
                    AccountType = request.AccountType,
                    NetworkType = request.NetworkType,
                    AccountNumber = request.AccountNumber,
                    AccountIdentifier = request.AccountIdentifier,
                    Status = request.Status,
                    IsDefault = request.IsDefault,
                    Remarks = request.Remarks,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };

                accounts.Add(newAccount);
                await SavePaymentAccountsAsync(accounts);

                return Ok(new PaymentAccountResponse
                {
                    Success = true,
                    Message = "添加收款账户成功",
                    Data = newAccount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加收款账户失败");
                return StatusCode(500, new PaymentAccountResponse
                {
                    Success = false,
                    Message = "添加收款账户失败"
                });
            }
        }

        // PUT /api/admin/payment-accounts/{id} - 更新收款账户
        [HttpPut("admin/payment-accounts/{id}")]
        public async Task<IActionResult> UpdatePaymentAccount(string id, [FromBody] PaymentAccountRequest request, [FromQuery] string? userId = null)
        {
            try
            {
                // 权限检查
                var (hasPermission, message) = await CheckPaymentAccountPermissionAsync(userId);
                if (!hasPermission)
                {
                    return StatusCode(403, new PaymentAccountResponse
                    {
                        Success = false,
                        Message = message
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new PaymentAccountResponse
                    {
                        Success = false,
                        Message = "请求参数无效"
                    });
                }

                var accounts = await LoadPaymentAccountsAsync();
                var account = accounts.FirstOrDefault(a => a.Id == id);
                
                if (account == null)
                {
                    return NotFound(new PaymentAccountResponse
                    {
                        Success = false,
                        Message = "收款账户不存在"
                    });
                }

                // 检查是否已存在相同的收款地址（排除当前账户）
                if (accounts.Any(a => a.AccountNumber == request.AccountNumber && a.Id != id))
                {
                    return BadRequest(new PaymentAccountResponse
                    {
                        Success = false,
                        Message = "该收款地址已存在"
                    });
                }

                // 如果设置为默认账户，需要将其他账户的默认状态取消
                if (request.IsDefault)
                {
                    foreach (var acc in accounts.Where(a => a.Id != id))
                    {
                        acc.IsDefault = false;
                    }
                }

                account.WalletName = request.WalletName;
                account.AccountType = request.AccountType;
                account.NetworkType = request.NetworkType;
                account.AccountNumber = request.AccountNumber;
                account.AccountIdentifier = request.AccountIdentifier;
                account.Status = request.Status;
                account.IsDefault = request.IsDefault;
                account.Remarks = request.Remarks;
                account.UpdateTime = DateTime.UtcNow;

                await SavePaymentAccountsAsync(accounts);

                return Ok(new PaymentAccountResponse
                {
                    Success = true,
                    Message = "更新收款账户成功",
                    Data = account
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新收款账户失败");
                return StatusCode(500, new PaymentAccountResponse
                {
                    Success = false,
                    Message = "更新收款账户失败"
                });
            }
        }

        // DELETE /api/admin/payment-accounts/{id} - 删除收款账户
        [HttpDelete("admin/payment-accounts/{id}")]
        public async Task<IActionResult> DeletePaymentAccount(string id, [FromQuery] string? userId = null)
        {
            try
            {
                // 权限检查
                var (hasPermission, message) = await CheckPaymentAccountPermissionAsync(userId);
                if (!hasPermission)
                {
                    return StatusCode(403, new PaymentAccountResponse
                    {
                        Success = false,
                        Message = message
                    });
                }

                var accounts = await LoadPaymentAccountsAsync();
                var account = accounts.FirstOrDefault(a => a.Id == id);
                
                if (account == null)
                {
                    return NotFound(new PaymentAccountResponse
                    {
                        Success = false,
                        Message = "收款账户不存在"
                    });
                }

                accounts.Remove(account);
                await SavePaymentAccountsAsync(accounts);

                return Ok(new PaymentAccountResponse
                {
                    Success = true,
                    Message = "删除收款账户成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除收款账户失败");
                return StatusCode(500, new PaymentAccountResponse
                {
                    Success = false,
                    Message = "删除收款账户失败"
                });
            }
        }

        private async Task<List<PaymentAccount>> LoadPaymentAccountsAsync()
        {
            try
            {
                if (!System.IO.File.Exists(_dataFilePath))
                {
                    return new List<PaymentAccount>();
                }

                var jsonContent = await System.IO.File.ReadAllTextAsync(_dataFilePath, Encoding.UTF8);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var data = JsonSerializer.Deserialize<Dictionary<string, List<PaymentAccount>>>(jsonContent, options);
                
                return data?.ContainsKey("paymentAccounts") == true ? data["paymentAccounts"] : new List<PaymentAccount>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载收款账户数据失败");
                return new List<PaymentAccount>();
            }
        }

        private async Task SavePaymentAccountsAsync(List<PaymentAccount> accounts)
        {
            try
            {
                var directory = Path.GetDirectoryName(_dataFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                var data = new Dictionary<string, List<PaymentAccount>>
                {
                    ["paymentAccounts"] = accounts
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var jsonContent = JsonSerializer.Serialize(data, options);
                
                // 确保目录存在并使用UTF-8 BOM写入文件
                var utf8WithBom = new UTF8Encoding(true);
                await System.IO.File.WriteAllTextAsync(_dataFilePath, jsonContent, utf8WithBom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存收款账户数据失败");
                throw;
            }
        }
    }
}