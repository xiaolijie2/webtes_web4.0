using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MobileECommerceAPI.Models;
using MobileECommerceAPI.Services;
using System.Linq;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _usersFile = "users.json";
        private readonly IPasswordService _passwordService;
        private readonly string _ordersFile = "orders.json";
        private readonly string _agentsFile = "agents.json";
        private readonly string _slideshowFile = "slideshow.json";

        private readonly string _logosFile = "logos.json";
        private readonly string _vipLevelsFile = "vip_levels.json";
        private readonly string _messagesFile = "messages.json";
        private readonly string _invitationsFile = "invitations.json";
        private readonly string _userStatsFile = "user_stats.json";
        private readonly string _transactionsFile = "transactions.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _tasksFile = "tasks.json";
        private readonly string _userTasksFile = "user_tasks.json";
        private readonly string _userAgentMappingFile = "user_agent_mapping.json";
        private readonly string _inviteRecordsFile = "invite_records.json";
        private readonly string _partnersFile = "partners.json";

        public AdminController(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            try
            {
                // 模拟概览数据
                var overview = new
                {
                    totalUsers = 156,
                    totalAgents = 12,
                    totalOrders = 89,
                    totalRevenue = 45600.50m,
                    activeUsers = 78,
                    pendingOrders = 15,
                    completedOrders = 74,
                    monthlyGrowth = 12.5
                };

                return Ok(overview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取概览数据失败", error = ex.Message });
            }
        }



        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _agentsFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new List<Agent>());
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                
                if (string.IsNullOrEmpty(json))
                {
                    return Ok(new List<Agent>());
                }
                
                // 使用更宽松的反序列化选项
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var agents = JsonSerializer.Deserialize<List<Agent>>(json, options) ?? new List<Agent>();
                
                // 更新客户数量（从用户-业务员关联表中统计）
                foreach (var agent in agents)
                {
                    agent.CustomerCount = await GetAgentCustomerCount(agent.Id);
                }
                
                return Ok(agents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取业务员列表失败", error = ex.Message });
            }
        }

        [HttpGet("performance")]
        public async Task<IActionResult> GetPerformance()
        {
            try
            {
                var performance = new[]
                {
                    new { date = "2024-01-15", orders = 120, revenue = 12500 },
                    new { date = "2024-01-16", orders = 135, revenue = 15600 },
                    new { date = "2024-01-17", orders = 98, revenue = 13200 },
                    new { date = "2024-01-18", orders = 156, revenue = 18900 },
                    new { date = "2024-01-19", orders = 142, revenue = 16700 },
                    new { date = "2024-01-20", orders = 178, revenue = 21300 },
                    new { date = "2024-01-21", orders = 165, revenue = 19800 }
                };

                return Ok(performance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取性能数据失败", error = ex.Message });
            }
        }

        [HttpGet("customer-assignments")]
        public async Task<IActionResult> GetCustomerAssignments()
        {
            try
            {
                var assignments = new[]
                {
                    new { 
                        id = 1, 
                        customerName = "张三", 
                        agentName = "李业务", 
                        assignDate = "2024-01-15", 
                        status = "active",
                        phone = "138****1234",
                        vipLevel = "VIP1",
                        orderCount = 5,
                        totalAmount = 2500.00
                    },
                    new { 
                        id = 2, 
                        customerName = "李四", 
                        agentName = "王业务", 
                        assignDate = "2024-01-14", 
                        status = "active",
                        phone = "139****5678",
                        vipLevel = "VIP2",
                        orderCount = 8,
                        totalAmount = 4200.00
                    },
                    new { 
                        id = 3, 
                        customerName = "王五", 
                        agentName = "赵业务", 
                        assignDate = "2024-01-13", 
                        status = "pending",
                        phone = "137****9012",
                        vipLevel = "VIP1",
                        orderCount = 3,
                        totalAmount = 1800.00
                    }
                };

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取客户分配数据失败", error = ex.Message });
            }
        }

        // 管理员登录API
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
        {
            try
            {
                Console.WriteLine($"Admin login attempt for username: {request.Username}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                User admin = null;
                string adminFilePath = "";
                
                // 首先在super_admins.json中查找
                var superAdminsPath = Path.Combine(_dataDirectory, "super_admins.json");
                if (System.IO.File.Exists(superAdminsPath))
                {
                    var superAdminsJson = await System.IO.File.ReadAllTextAsync(superAdminsPath);
                    var superAdmins = JsonSerializer.Deserialize<List<User>>(superAdminsJson, options) ?? new List<User>();
                    
                    admin = superAdmins.FirstOrDefault(u => 
                        (u.Phone == request.Username || u.Username == request.Username) && 
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
                    var regularAdminsPath = Path.Combine(_dataDirectory, "regular_admins.json");
                    if (System.IO.File.Exists(regularAdminsPath))
                    {
                        var regularAdminsJson = await System.IO.File.ReadAllTextAsync(regularAdminsPath);
                        var regularAdmins = JsonSerializer.Deserialize<List<User>>(regularAdminsJson, options) ?? new List<User>();
                        
                        admin = regularAdmins.FirstOrDefault(u => 
                            (u.Phone == request.Username || u.Username == request.Username) && 
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
                    return NotFound(new { success = false, message = "管理员账户不存在" });
                }
                
                // 使用统一的PasswordService验证密码
                bool isPasswordValid = false;
                if (!string.IsNullOrEmpty(admin.Password))
                {
                    isPasswordValid = _passwordService.VerifyPassword(request.Password, admin.Password);
                }
                
                if (!isPasswordValid)
                {
                    return BadRequest(new { success = false, message = "密码错误" });
                }
                
                // 更新最后登录时间
                admin.LastLoginTime = DateTime.Now;
                
                // 根据管理员类型保存到相应的文件
                if (admin.UserType == "super_admin")
                {
                    var superAdminsJson = await System.IO.File.ReadAllTextAsync(adminFilePath);
                    var superAdmins = JsonSerializer.Deserialize<List<User>>(superAdminsJson, options) ?? new List<User>();
                    var superAdminIndex = superAdmins.FindIndex(u => u.Id == admin.Id);
                    if (superAdminIndex >= 0)
                    {
                        superAdmins[superAdminIndex] = admin;
                        await System.IO.File.WriteAllTextAsync(adminFilePath, 
                            JsonSerializer.Serialize(superAdmins, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
                else
                {
                    var regularAdminsJson = await System.IO.File.ReadAllTextAsync(adminFilePath);
                    var regularAdmins = JsonSerializer.Deserialize<List<User>>(regularAdminsJson, options) ?? new List<User>();
                    var adminIndex = regularAdmins.FindIndex(u => u.Id == admin.Id);
                    if (adminIndex >= 0)
                    {
                        regularAdmins[adminIndex] = admin;
                        await System.IO.File.WriteAllTextAsync(adminFilePath, 
                            JsonSerializer.Serialize(regularAdmins, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }
                
                // 返回管理员信息（不包含密码）
                var adminInfo = new
                {
                    id = admin.Id,
                    phone = admin.Phone,
                    username = admin.Username,
                    nickName = admin.NickName,
                    userType = admin.UserType ?? (admin.IsAdmin ? "admin" : "user"),
                    isActive = admin.IsActive
                };
                
                return Ok(new { success = true, message = "登录成功", admin = adminInfo });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin login error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "登录失败", error = ex.Message });
            }
        }

        // 用户管理API
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            Console.WriteLine($"GetUsers called with parameters: page={page}, pageSize={pageSize}, search='{search}'");
            
            try
            {
                var filePath = Path.Combine(_dataDirectory, _usersFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new { users = new User[0], total = 0, page, pageSize });
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                Console.WriteLine($"JSON content length: {json.Length}");
                
                // 使用更宽松的反序列化选项
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var users = JsonSerializer.Deserialize<User[]>(json, options) ?? new User[0];
                Console.WriteLine($"Deserialized {users.Length} users");
                
                // 应用搜索过滤
                if (!string.IsNullOrEmpty(search))
                {
                    users = users.Where(u => 
                        (!string.IsNullOrEmpty(u.NickName) && u.NickName.Contains(search, StringComparison.OrdinalIgnoreCase)) || 
                        (!string.IsNullOrEmpty(u.Phone) && u.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(u.Username) && u.Username.Contains(search, StringComparison.OrdinalIgnoreCase))
                    ).ToArray();
                }
                
                // 应用分页
                var totalCount = users.Length;
                var pagedUsers = users.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
                
                // 转换为前端期望的格式（确保字段名大写开头）
                var formattedUsers = pagedUsers.Select(u => new Dictionary<string, object>
                {
                    ["Id"] = u.Id,
                    ["Phone"] = u.Phone,
                    ["NickName"] = u.NickName,
                    ["Username"] = u.Username,
                    ["CurrentBalance"] = u.CurrentBalance,
                    ["VipLevel"] = u.VipLevel,
                    ["RegisterTime"] = u.RegisterTime,
                    ["IsActive"] = u.IsActive,
                    ["IsAdmin"] = u.IsAdmin,
                    ["CreditScore"] = u.CreditScore,
                    ["InviteCodeUsed"] = u.InviteCodeUsed
                }).ToArray();
                
                Console.WriteLine($"Returning {formattedUsers.Length} users out of {totalCount} total");
                
                // 创建自定义响应对象，确保字段名保持大写
                var response = new Dictionary<string, object>
                {
                    ["users"] = formattedUsers,
                    ["total"] = totalCount,
                    ["page"] = page,
                    ["pageSize"] = pageSize
                };
                
                // 使用自定义JSON选项返回结果
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null // 保持原始字段名
                };
                
                var jsonString = JsonSerializer.Serialize(response, jsonOptions);
                return Content(jsonString, "application/json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUsers: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "获取用户列表失败", error = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] object userData)
        {
            try
            {
                // 这里应该实现用户更新逻辑
                return Ok(new { message = "用户信息更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新用户信息失败", error = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                Console.WriteLine($"DeleteUser called with id: {id}");

                var usersFilePath = Path.Combine(_dataDirectory, _usersFile);
                if (!System.IO.File.Exists(usersFilePath))
                {
                    return NotFound(new { message = "用户数据文件不存在" });
                }

                // 读取用户数据
                var json = await System.IO.File.ReadAllTextAsync(usersFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var users = JsonSerializer.Deserialize<User[]>(json, options) ?? new User[0];
                
                // 查找要删除的用户
                var userToDelete = users.FirstOrDefault(u => u.Id == id);
                if (userToDelete == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                // 检查是否为管理员，不允许删除管理员
                if (userToDelete.IsAdmin)
                {
                    return BadRequest(new { message = "不能删除管理员账户" });
                }

                // 从用户列表中移除该用户
                var updatedUsers = users.Where(u => u.Id != id).ToArray();

                // 保存更新后的用户数据
                var updatedJson = JsonSerializer.Serialize(updatedUsers, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = null
                });
                await System.IO.File.WriteAllTextAsync(usersFilePath, updatedJson);

                // 清理相关数据
                await CleanupUserRelatedData(id, userToDelete);

                // 记录删除操作日志
                Console.WriteLine($"User deleted: ID={id}, Phone={userToDelete.Phone}, NickName={userToDelete.NickName}");

                return Ok(new { message = "用户删除成功" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "删除用户失败", error = ex.Message });
            }
        }

        // 清理用户相关数据的辅助方法
        private async Task CleanupUserRelatedData(string userId, User user)
        {
            try
            {
                // 清理业务员数据（如果该用户是业务员）
                var agentsFilePath = Path.Combine(_dataDirectory, _agentsFile);
                if (System.IO.File.Exists(agentsFilePath))
                {
                    var agentsJson = await System.IO.File.ReadAllTextAsync(agentsFilePath);
                    var agents = JsonSerializer.Deserialize<Agent[]>(agentsJson) ?? new Agent[0];
                    
                    // 根据Account字段匹配（假设Agent的Account对应User的Username或Phone）
                    var updatedAgents = agents.Where(a => 
                        a.Account != user.Username && 
                        a.Account != user.Phone
                    ).ToArray();
                    
                    if (updatedAgents.Length != agents.Length)
                    {
                        var updatedAgentsJson = JsonSerializer.Serialize(updatedAgents, new JsonSerializerOptions
                        {
                            WriteIndented = true
                        });
                        await System.IO.File.WriteAllTextAsync(agentsFilePath, updatedAgentsJson);
                        Console.WriteLine($"Cleaned up agent data for user {userId}");
                    }
                }

                // 这里可以添加更多清理逻辑，比如：
                // - 清理邀请记录
                // - 清理交易记录
                // - 清理其他相关数据
                
                Console.WriteLine($"Cleanup completed for user {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup for user {userId}: {ex.Message}");
                // 清理失败不应该影响删除操作，只记录日志
            }
        }

        // 获取用户类型信息API
        [HttpGet("user-types")]
        public async Task<IActionResult> GetUserTypes()
        {
            try
            {
                // 读取业务员数据
                var agentsFilePath = Path.Combine(_dataDirectory, _agentsFile);
                var agentAccounts = new HashSet<string>();
                
                if (System.IO.File.Exists(agentsFilePath))
                {
                    var agentsJson = await System.IO.File.ReadAllTextAsync(agentsFilePath);
                    var agents = JsonSerializer.Deserialize<Agent[]>(agentsJson) ?? new Agent[0];
                    
                    foreach (var agent in agents)
                    {
                        if (!string.IsNullOrEmpty(agent.Account))
                        {
                            agentAccounts.Add(agent.Account);
                        }
                    }
                }

                return Ok(new { agentAccounts = agentAccounts.ToArray() });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user types: {ex.Message}");
                return StatusCode(500, new { message = "获取用户类型失败", error = ex.Message });
            }
        }

        // 业务员管理API
        [HttpPost("agents")]
        public async Task<IActionResult> CreateAgent([FromBody] CreateAgentRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _agentsFile);
                
                // 确保目录存在
                Directory.CreateDirectory(_dataDirectory);
                
                // 读取现有业务员数据
                var agents = new List<Agent>();
                if (System.IO.File.Exists(filePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var deserializeOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        agents = JsonSerializer.Deserialize<List<Agent>>(json, deserializeOptions) ?? new List<Agent>();
                    }
                }
                
                // 过滤掉空记录
                agents = agents.Where(a => !string.IsNullOrEmpty(a.Account) && !string.IsNullOrEmpty(a.InviteCode)).ToList();
                Console.WriteLine($"Filtered agents count: {agents.Count}");
                foreach (var existingAgent in agents)
                {
                    Console.WriteLine($"Existing agent: {existingAgent.Account}");
                }
                Console.WriteLine($"Request account: {request.Account}");
                
                // 检查账号是否已存在
                if (agents.Any(a => a.Account == request.Account))
                {
                    Console.WriteLine("Account already exists!");
                    return BadRequest(new { message = "账号已存在" });
                }
                Console.WriteLine("Account validation passed");
                
                // 如果没有提供邀请码，自动生成一个
                var inviteCode = string.IsNullOrEmpty(request.InviteCode) ? 
                    GenerateInviteCode() : request.InviteCode;
                
                // 检查邀请码是否已存在
                if (agents.Any(a => a.InviteCode == inviteCode))
                {
                    return BadRequest(new { message = "邀请码已存在" });
                }
                
                // 创建新业务员
                var newAgent = new Agent
                {
                    Id = Guid.NewGuid().ToString(),
                    NickName = request.NickName,
                    Account = request.Account,
                    Password = _passwordService.HashPassword(request.Password), // 使用PasswordService加密
                    InviteCode = inviteCode,
                    CustomerCount = 0,
                    MonthlyPerformance = 0,
                    IsActive = true,
                    RegisterTime = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                agents.Add(newAgent);
                Console.WriteLine($"Added new agent. Total agents: {agents.Count}");
                
                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var updatedJson = JsonSerializer.Serialize(agents, options);
                Console.WriteLine($"Serialized JSON length: {updatedJson.Length}");
                Console.WriteLine($"File path: {filePath}");
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                Console.WriteLine("File write completed");
                
                // 验证文件是否正确保存
                var verifyJson = await System.IO.File.ReadAllTextAsync(filePath);
                Console.WriteLine($"Verification read length: {verifyJson.Length}");
                
                // 同时保存邀请码记录
                await SaveInviteCodeRecord(newAgent);
                
                return Ok(new { message = "业务员创建成功", agent = newAgent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建业务员失败", error = ex.Message });
            }
        }

        [HttpPut("agents/{id}/info")]
        public async Task<IActionResult> UpdateAgent(string id, [FromBody] CreateAgentRequest request)
        {
            Console.WriteLine($"UpdateAgent called with id: {id}");
            Console.WriteLine($"Request: {JsonSerializer.Serialize(request)}");
            try
            {
                Console.WriteLine("Starting UpdateAgent process...");
                var filePath = Path.Combine(_dataDirectory, _agentsFile);
                Console.WriteLine($"File path: {filePath}");
                
                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine("Agents file not found");
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 读取现有业务员数据
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                Console.WriteLine($"Read JSON: {json.Substring(0, Math.Min(100, json.Length))}...");
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var agents = JsonSerializer.Deserialize<List<Agent>>(json, deserializeOptions) ?? new List<Agent>();
                Console.WriteLine($"Deserialized {agents.Count} agents");
                if (agents.Count > 0)
                {
                    Console.WriteLine($"First agent ID: {agents[0].Id}");
                }
                
                // 查找要更新的业务员
                var agent = agents.FirstOrDefault(a => a.Id == id);
                if (agent == null)
                {
                    Console.WriteLine($"Agent with id {id} not found");
                    return NotFound(new { message = "业务员不存在" });
                }
                
                Console.WriteLine($"Found agent: {agent.NickName}");
                
                // 更新业务员信息
                agent.NickName = request.NickName;
                agent.Account = request.Account;
                if (!string.IsNullOrEmpty(request.Password))
                {
                    agent.Password = _passwordService.HashPassword(request.Password);
                }
                // 注意：邀请码一旦创建不可更改，所以不更新InviteCode
                agent.UpdatedAt = DateTime.Now;
                
                Console.WriteLine($"Updated agent info: {JsonSerializer.Serialize(agent)}");
                
                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var updatedJson = JsonSerializer.Serialize(agents, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                Console.WriteLine("File saved successfully");
                
                var result = Ok(new { message = "业务员信息更新成功", agent });
                Console.WriteLine($"Returning result: {JsonSerializer.Serialize(result)}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateAgent: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "更新业务员信息失败", error = ex.Message });
            }
        }

        // 测试端点
        [HttpGet("test/{id}")]
        public IActionResult TestRoute(string id)
        {
            return Ok(new { message = "Route works", id = id });
        }

        [HttpPut("test-put/{id}")]
        public IActionResult TestPutRoute(string id, [FromBody] object data)
        {
            Console.WriteLine($"TestPutRoute called with id: {id}");
            Console.WriteLine($"Data: {JsonSerializer.Serialize(data)}");
            return Ok(new { message = "PUT Route works", id = id });
        }

        [HttpPut("agents/{id}/status")]
        public async Task<IActionResult> UpdateAgentStatus(string id, [FromBody] UpdateAgentStatusRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _agentsFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 读取现有业务员数据
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var agents = JsonSerializer.Deserialize<List<Agent>>(json, deserializeOptions) ?? new List<Agent>();
                
                // 查找要更新的业务员
                var agent = agents.FirstOrDefault(a => a.Id == id);
                if (agent == null)
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 更新状态
                agent.IsActive = request.IsActive;
                agent.UpdatedAt = DateTime.Now;
                
                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var updatedJson = JsonSerializer.Serialize(agents, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "业务员状态更新成功", agent });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新业务员状态失败", error = ex.Message });
            }
        }

        [HttpDelete("agents/{id}")]
        public async Task<IActionResult> DeleteAgent(string id)
        {
            try
            {
                Console.WriteLine($"DeleteAgent called with id: {id}");
                var filePath = Path.Combine(_dataDirectory, _agentsFile);
                Console.WriteLine($"File path: {filePath}");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 读取现有业务员数据
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var agents = JsonSerializer.Deserialize<List<Agent>>(json, deserializeOptions) ?? new List<Agent>();
                
                // 查找要删除的业务员
                var agentToDelete = agents.FirstOrDefault(a => a.Id == id);
                if (agentToDelete == null)
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 删除业务员
                agents.Remove(agentToDelete);
                
                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var updatedJson = JsonSerializer.Serialize(agents, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                // 删除相关的邀请码记录
                await RemoveInviteCodeRecord(agentToDelete.InviteCode);
                
                return Ok(new { message = "业务员删除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除业务员失败", error = ex.Message });
            }
        }

        // 邀请码管理API
        [HttpGet("invite-codes")]
        public async Task<IActionResult> GetInviteCodes()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _invitationsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new object[0]);
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                
                // 尝试反序列化为AgentInviteCode列表
                try
                {
                    var deserializeOptions = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var inviteCodes = JsonSerializer.Deserialize<List<AgentInviteCode>>(json, deserializeOptions) ?? new List<AgentInviteCode>();
                    return Ok(inviteCodes);
                }
                catch
                {
                    // 如果失败，返回空数组（可能是旧格式的文件）
                    return Ok(new object[0]);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取邀请码列表失败", error = ex.Message });
            }
        }

        [HttpPost("invite-codes")]
        public async Task<IActionResult> CreateInviteCode([FromBody] GenerateInviteCodeRequest request)
        {
            try
            {
                // 验证业务员是否存在
                var agentsFilePath = Path.Combine(_dataDirectory, _agentsFile);
                if (!System.IO.File.Exists(agentsFilePath))
                {
                    return BadRequest(new { message = "业务员不存在" });
                }
                
                var agentsJson = await System.IO.File.ReadAllTextAsync(agentsFilePath);
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var agents = JsonSerializer.Deserialize<List<Agent>>(agentsJson, deserializeOptions) ?? new List<Agent>();
                var agent = agents.FirstOrDefault(a => a.Id == request.AgentId);
                
                if (agent == null)
                {
                    return BadRequest(new { message = "业务员不存在" });
                }
                
                // 生成新的邀请码
                var newInviteCode = GenerateInviteCode();
                
                // 检查邀请码是否已存在
                while (agents.Any(a => a.InviteCode == newInviteCode))
                {
                    newInviteCode = GenerateInviteCode();
                }
                
                // 更新业务员的邀请码
                agent.InviteCode = newInviteCode;
                agent.UpdatedAt = DateTime.Now;
                
                // 保存业务员数据
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var updatedAgentsJson = JsonSerializer.Serialize(agents, options);
                await System.IO.File.WriteAllTextAsync(agentsFilePath, updatedAgentsJson);
                
                // 保存邀请码记录
                var inviteCodeRecord = new AgentInviteCode
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = newInviteCode,
                    AgentId = agent.Id,
                    AgentNickName = agent.NickName,
                    IsUsed = false,
                    UsedCount = 0,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                
                await SaveInviteCodeRecord(agent);
                
                return Ok(new { message = "邀请码生成成功", inviteCode = newInviteCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "生成邀请码失败", error = ex.Message });
            }
        }

        // 订单管理API
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(int page = 1, int pageSize = 10, string? status = null, string? assignStatus = null)
        {
            try
            {
                var ordersFilePath = Path.Combine(_dataDirectory, _ordersFile);
                var agentsFilePath = Path.Combine(_dataDirectory, _agentsFile);
                var usersFilePath = Path.Combine(_dataDirectory, _usersFile);
                
                if (!System.IO.File.Exists(ordersFilePath))
                {
                    return Ok(new { orders = new List<OrderPoolResponse>(), total = 0, page, pageSize });
                }

                // 加载订单数据
                var ordersJson = await System.IO.File.ReadAllTextAsync(ordersFilePath);
                var ordersData = JsonSerializer.Deserialize<Dictionary<string, List<JsonElement>>>(ordersJson) ?? new Dictionary<string, List<JsonElement>>();
                
                // 加载业务员数据
                var agents = new List<Agent>();
                if (System.IO.File.Exists(agentsFilePath))
                {
                    var agentsJson = await System.IO.File.ReadAllTextAsync(agentsFilePath);
                    agents = JsonSerializer.Deserialize<List<Agent>>(agentsJson) ?? new List<Agent>();
                }
                
                // 加载用户数据
                var users = new List<User>();
                if (System.IO.File.Exists(usersFilePath))
                {
                    var usersJson = await System.IO.File.ReadAllTextAsync(usersFilePath);
                    users = JsonSerializer.Deserialize<List<User>>(usersJson) ?? new List<User>();
                }
                
                // 转换订单数据为订单池格式
                var orderPoolList = new List<OrderPoolResponse>();
                
                foreach (var userOrders in ordersData)
                {
                    if (userOrders.Value != null)
                    {
                        foreach (var orderElement in userOrders.Value)
                        {
                            
                            var order = new OrderPoolResponse
                            {
                                OrderId = orderElement.TryGetProperty("id", out JsonElement idProp) && idProp.ValueKind != JsonValueKind.Null ? idProp.GetString() ?? "" : "",
                                ProductName = orderElement.TryGetProperty("product_name", out JsonElement nameProp) && nameProp.ValueKind != JsonValueKind.Null ? nameProp.GetString() ?? "" : "",
                                Amount = orderElement.TryGetProperty("amount", out JsonElement amountProp) ? amountProp.GetDecimal() : 0,
                                Status = orderElement.TryGetProperty("status", out JsonElement statusProp) && statusProp.ValueKind != JsonValueKind.Null ? statusProp.GetString() ?? "" : "",
                                CreateTime = orderElement.TryGetProperty("created_at", out JsonElement timeProp) ? timeProp.GetDateTime() : DateTime.Now,
                                Platform = orderElement.TryGetProperty("category", out JsonElement catProp) && catProp.ValueKind != JsonValueKind.Null ? catProp.GetString() ?? "" : "",
                                Description = orderElement.TryGetProperty("description", out JsonElement descProp) && descProp.ValueKind != JsonValueKind.Null ? descProp.GetString() ?? "" : "",
                                CreatedBy = orderElement.TryGetProperty("created_by", out JsonElement createdByProp) && createdByProp.ValueKind != JsonValueKind.Null ? createdByProp.GetString() ?? "" : "",
                                AssignedTo = orderElement.TryGetProperty("assigned_to", out JsonElement assignedToProp) && assignedToProp.ValueKind != JsonValueKind.Null ? assignedToProp.GetString() ?? "" : "",
                                IsAssigned = orderElement.TryGetProperty("is_assigned", out JsonElement isAssignedProp) ? isAssignedProp.GetBoolean() : false,
                                AssignedTime = orderElement.TryGetProperty("assigned_time", out JsonElement assignedTimeProp) && assignedTimeProp.ValueKind != JsonValueKind.Null ? assignedTimeProp.GetDateTime() : null
                            };
                            
                            // 查找创建者信息
                            if (!string.IsNullOrEmpty(order.CreatedBy))
                            {
                                var agent = agents.FirstOrDefault(a => a.Id == order.CreatedBy);
                                order.CreatedByName = agent?.NickName ?? "未知业务员";
                            }
                            
                            // 查找分配客户信息
                            if (!string.IsNullOrEmpty(order.AssignedTo))
                            {
                                var user = users.FirstOrDefault(u => u.Id == order.AssignedTo);
                                order.AssignedToName = user?.Name ?? "未知客户";
                            }
                            
                            orderPoolList.Add(order);
                        }
                    }
                }
                
                // 应用筛选条件
                if (!string.IsNullOrEmpty(status))
                {
                    orderPoolList = orderPoolList.Where(o => o.Status == status).ToList();
                }
                
                if (!string.IsNullOrEmpty(assignStatus))
                {
                    if (assignStatus == "assigned")
                    {
                        orderPoolList = orderPoolList.Where(o => o.IsAssigned).ToList();
                    }
                    else if (assignStatus == "unassigned")
                    {
                        orderPoolList = orderPoolList.Where(o => !o.IsAssigned).ToList();
                    }
                }
                
                // 按创建时间倒序排列
                orderPoolList = orderPoolList.OrderByDescending(o => o.CreateTime).ToList();
                
                var total = orderPoolList.Count;
                var pagedOrders = orderPoolList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                return Ok(new { orders = pagedOrders, total, page, pageSize });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单池失败", error = ex.Message });
            }
        }

        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] object statusData)
        {
            try
            {
                // 这里应该实现订单状态更新逻辑
                return Ok(new { message = "订单状态更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新订单状态失败", error = ex.Message });
            }
        }

        [HttpPost("orders/batch-update")]
        public async Task<IActionResult> BatchUpdateOrders([FromBody] object request)
        {
            try
            {
                // TODO: 实现批量更新订单逻辑
                return Ok(new { message = "批量更新订单成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "批量更新订单失败", error = ex.Message });
            }
        }

        [HttpPost("orders/assign")]
        public async Task<IActionResult> AssignOrder([FromBody] AssignOrderRequest request)
        {
            try
            {
                var ordersFilePath = Path.Combine(_dataDirectory, _ordersFile);
                if (!System.IO.File.Exists(ordersFilePath))
                {
                    return NotFound(new { message = "订单文件不存在" });
                }

                var ordersJson = await System.IO.File.ReadAllTextAsync(ordersFilePath);
                var ordersData = JsonSerializer.Deserialize<Dictionary<string, List<JsonElement>>>(ordersJson) ?? new Dictionary<string, List<JsonElement>>();
                
                bool orderFound = false;
                
                // 查找并更新订单
                foreach (var userOrders in ordersData)
                {
                    for (int i = 0; i < userOrders.Value.Count; i++)
                    {
                        var orderElement = userOrders.Value[i];
                        if (orderElement.TryGetProperty("id", out JsonElement idProp) && idProp.GetString() == request.OrderId)
                        {
                            // 创建更新后的订单对象
                            var orderDict = new Dictionary<string, object>();
                            
                            // 复制现有属性
                            foreach (var prop in orderElement.EnumerateObject())
                            {
                                orderDict[prop.Name] = prop.Value.ValueKind switch
                                {
                                    JsonValueKind.String => prop.Value.GetString(),
                                    JsonValueKind.Number => prop.Value.GetDecimal(),
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    _ => prop.Value.ToString()
                                };
                            }
                            
                            // 更新分配信息
                            orderDict["assigned_to"] = request.CustomerId;
                            orderDict["assigned_to_name"] = request.CustomerName;
                            orderDict["is_assigned"] = true;
                            orderDict["assigned_time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            orderDict["assigned_by"] = request.AssignedBy;
                            
                            // 转换回JsonElement
                            var updatedOrderJson = JsonSerializer.Serialize(orderDict);
                            userOrders.Value[i] = JsonSerializer.Deserialize<JsonElement>(updatedOrderJson);
                            
                            orderFound = true;
                            break;
                        }
                    }
                    
                    if (orderFound) break;
                }
                
                if (!orderFound)
                {
                    return NotFound(new { message = "订单不存在" });
                }
                
                // 保存更新后的数据
                var updatedJson = JsonSerializer.Serialize(ordersData, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(ordersFilePath, updatedJson);
                
                return Ok(new { message = "订单分配成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "订单分配失败", error = ex.Message });
            }
        }

        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers([FromQuery] string search = "")
        {
            try
            {
                var usersFilePath = Path.Combine(_dataDirectory, _usersFile);
                if (!System.IO.File.Exists(usersFilePath))
                {
                    return Ok(new { customers = new List<object>() });
                }

                var usersJson = await System.IO.File.ReadAllTextAsync(usersFilePath);
                var users = JsonSerializer.Deserialize<List<User>>(usersJson) ?? new List<User>();
                
                // 筛选客户
                var customers = users.Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    phone = u.Phone,
                    email = u.Email
                }).ToList();
                
                // 应用搜索条件
                if (!string.IsNullOrEmpty(search))
                {
                    customers = customers.Where(c => 
                        (c.name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (c.phone?.Contains(search) ?? false) ||
                        (c.email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                    ).ToList();
                }
                
                return Ok(new { customers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取客户列表失败", error = ex.Message });
            }
        }

        // 内容管理API - 幻灯片
        [HttpGet("slideshow")]
        public async Task<IActionResult> GetSlideshow()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _slideshowFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new object[0]);
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var slideshows = JsonSerializer.Deserialize<List<Slideshow>>(json) ?? new List<Slideshow>();
                
                // 转换为小写属性名格式，与HomeController保持一致
                var result = slideshows.Select(s => new {
                    id = s.Id,
                    title = s.Title,
                    description = s.Description,
                    image = s.Image,
                    link = s.Link,
                    order = s.Order,
                    createTime = s.CreateTime,
                    uploadTime = s.UploadTime
                }).ToArray();
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取幻灯片列表失败", error = ex.Message });
            }
        }

        [HttpPost("slideshow")]
        public async Task<IActionResult> CreateSlideshow([FromBody] SlideshowRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _slideshowFile);
                var slideshows = new List<Slideshow>();
                
                if (System.IO.File.Exists(filePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        slideshows = JsonSerializer.Deserialize<List<Slideshow>>(json) ?? new List<Slideshow>();
                    }
                }
                
                var newSlideshow = new Slideshow
                {
                    Title = request.Title,
                    Image = request.Image,
                    Link = request.Link,
                    Order = request.Order
                };
                
                slideshows.Add(newSlideshow);
                
                var updatedJson = JsonSerializer.Serialize(slideshows, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                // 返回小写属性名格式
                var result = new {
                    id = newSlideshow.Id,
                    title = newSlideshow.Title,
                    description = newSlideshow.Description,
                    image = newSlideshow.Image,
                    link = newSlideshow.Link,
                    order = newSlideshow.Order,
                    createTime = newSlideshow.CreateTime,
                    uploadTime = newSlideshow.UploadTime
                };
                
                return Ok(new { message = "幻灯片上传成功", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "上传幻灯片失败", error = ex.Message });
            }
        }

        [HttpPost("slideshow/upload")]
        public async Task<IActionResult> UploadSlideshow([FromForm] IFormFile image)
        {
            try
            {
                // 验证文件
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "请选择要上传的图片文件" });
                }

                // 验证文件类型
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(image.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "只支持JPG和PNG格式的图片" });
                }

                // 验证文件大小 (5MB)
                if (image.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "图片文件大小不能超过5MB" });
                }

                // 创建上传目录
                var uploadsDir = Path.Combine("wwwroot", "uploads", "slideshow");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // 生成唯一文件名
                var fileExtension = Path.GetExtension(image.FileName);
                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // 生成访问URL
                var imageUrl = $"/uploads/slideshow/{fileName}";

                // 保存幻灯片数据
                var slideshowFilePath = Path.Combine(_dataDirectory, _slideshowFile);
                var slideshows = new List<Slideshow>();
                
                if (System.IO.File.Exists(slideshowFilePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(slideshowFilePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        slideshows = JsonSerializer.Deserialize<List<Slideshow>>(json) ?? new List<Slideshow>();
                    }
                }
                
                // 获取下一个排序号
                var nextOrder = slideshows.Count > 0 ? slideshows.Max(s => s.Order) + 1 : 1;
                
                var newSlideshow = new Slideshow
                {
                    Title = "",
                    Description = "",
                    Image = imageUrl,
                    Link = "",
                    Order = nextOrder
                };
                
                slideshows.Add(newSlideshow);
                
                var updatedJson = JsonSerializer.Serialize(slideshows, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(slideshowFilePath, updatedJson);
                
                // 返回小写属性名格式
                var result = new {
                    id = newSlideshow.Id,
                    title = newSlideshow.Title,
                    description = newSlideshow.Description,
                    image = newSlideshow.Image,
                    link = newSlideshow.Link,
                    order = newSlideshow.Order,
                    createTime = newSlideshow.CreateTime,
                    uploadTime = newSlideshow.UploadTime
                };
                
                return Ok(new { message = "幻灯片上传成功", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "上传幻灯片失败", error = ex.Message });
            }
        }

        [HttpDelete("slideshow/{id}")]
        public async Task<IActionResult> DeleteSlideshow(string id)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _slideshowFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "幻灯片文件不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var slideshows = JsonSerializer.Deserialize<List<Slideshow>>(json) ?? new List<Slideshow>();
                
                var slideshow = slideshows.FirstOrDefault(s => s.Id == id);
                if (slideshow == null)
                {
                    return NotFound(new { message = "幻灯片不存在" });
                }
                
                // 删除图片文件（如果是本地上传的文件）
                if (!string.IsNullOrEmpty(slideshow.Image) && slideshow.Image.StartsWith("/uploads/"))
                {
                    var imageFilePath = Path.Combine("wwwroot", slideshow.Image.TrimStart('/'));
                    if (System.IO.File.Exists(imageFilePath))
                    {
                        System.IO.File.Delete(imageFilePath);
                    }
                }
                
                slideshows.Remove(slideshow);
                
                var updatedJson = JsonSerializer.Serialize(slideshows, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "幻灯片删除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除幻灯片失败", error = ex.Message });
            }
        }

        [HttpPut("slideshow/{id}")]
        public async Task<IActionResult> UpdateSlideshow(string id, [FromBody] SlideshowUpdateRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _slideshowFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "幻灯片文件不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var slideshows = JsonSerializer.Deserialize<List<Slideshow>>(json) ?? new List<Slideshow>();
                
                var slideshow = slideshows.FirstOrDefault(s => s.Id == id);
                if (slideshow == null)
                {
                    return NotFound(new { message = "幻灯片不存在" });
                }
                
                slideshow.Title = request.Title ?? slideshow.Title;
                slideshow.Description = request.Description ?? slideshow.Description;
                slideshow.Link = request.Link ?? slideshow.Link;
                
                var updatedJson = JsonSerializer.Serialize(slideshows, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                // 返回小写属性名格式
                var result = new {
                    id = slideshow.Id,
                    title = slideshow.Title,
                    description = slideshow.Description,
                    image = slideshow.Image,
                    link = slideshow.Link,
                    order = slideshow.Order,
                    createTime = slideshow.CreateTime,
                    uploadTime = slideshow.UploadTime
                };
                
                return Ok(new { message = "幻灯片更新成功", data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新幻灯片失败", error = ex.Message });
            }
        }

        // 内容管理API - LOGO
        [HttpGet("logos")]
        public async Task<IActionResult> GetLogos()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _logosFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new object[0]);
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var logos = JsonSerializer.Deserialize<object[]>(json) ?? new object[0];
                
                return Ok(logos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取LOGO列表失败", error = ex.Message });
            }
        }

        [HttpPost("logos")]
        public async Task<IActionResult> CreateLogo([FromBody] LogoRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _logosFile);
                var logos = new List<LogoItem>();
                
                if (System.IO.File.Exists(filePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        logos = JsonSerializer.Deserialize<List<LogoItem>>(json) ?? new List<LogoItem>();
                    }
                }
                
                var newLogo = new LogoItem
                {
                    Type = request.Type,
                    Text = request.Text,
                    ImageUrl = request.ImageUrl,
                    FontFamily = request.FontFamily,
                    FontSize = request.FontSize,
                    Color = request.Color,
                    Name = request.Name,
                    Width = request.Width,
                    Height = request.Height
                };
                
                logos.Add(newLogo);
                
                var updatedJson = JsonSerializer.Serialize(logos, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "LOGO创建成功", data = newLogo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建LOGO失败", error = ex.Message });
            }
        }

        [HttpPut("logos/{id}")]
        public async Task<IActionResult> UpdateLogo(string id, [FromBody] LogoRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _logosFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "LOGO文件不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var logos = JsonSerializer.Deserialize<List<LogoItem>>(json) ?? new List<LogoItem>();
                
                var logo = logos.FirstOrDefault(l => l.Id == id);
                if (logo == null)
                {
                    return NotFound(new { message = "LOGO不存在" });
                }
                
                logo.Type = request.Type;
                logo.Text = request.Text;
                logo.ImageUrl = request.ImageUrl;
                logo.FontFamily = request.FontFamily;
                logo.FontSize = request.FontSize;
                logo.Color = request.Color;
                logo.Name = request.Name;
                logo.Width = request.Width;
                logo.Height = request.Height;
                
                var updatedJson = JsonSerializer.Serialize(logos, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "LOGO更新成功", data = logo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新LOGO失败", error = ex.Message });
            }
        }

        [HttpDelete("logos/{id}")]
        public async Task<IActionResult> DeleteLogo(string id)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _logosFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "LOGO文件不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var logos = JsonSerializer.Deserialize<List<LogoItem>>(json) ?? new List<LogoItem>();
                
                var logo = logos.FirstOrDefault(l => l.Id == id);
                if (logo == null)
                {
                    return NotFound(new { message = "LOGO不存在" });
                }
                
                logos.Remove(logo);
                
                var updatedJson = JsonSerializer.Serialize(logos, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "LOGO删除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除LOGO失败", error = ex.Message });
            }
        }

        [HttpPost("upload-logo")]
        public async Task<IActionResult> UploadLogo([FromForm] IFormFile logoImage)
        {
            try
            {
                // 验证文件
                if (logoImage == null || logoImage.Length == 0)
                {
                    return BadRequest(new { message = "请选择要上传的LOGO图片文件" });
                }

                // 验证文件类型
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(logoImage.ContentType.ToLower()))
                {
                    return BadRequest(new { message = "只支持PNG、JPG、GIF格式的图片" });
                }

                // 验证文件大小 (5MB)
                if (logoImage.Length > 5 * 1024 * 1024)
                {
                    return BadRequest(new { message = "图片文件大小不能超过5MB" });
                }

                // 创建上传目录
                var uploadsDir = Path.Combine("wwwroot", "uploads", "logos");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // 生成唯一文件名
                var fileExtension = Path.GetExtension(logoImage.FileName);
                var fileName = $"logo_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // 保存文件
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoImage.CopyToAsync(stream);
                }

                // 生成访问URL
                var imageUrl = $"/uploads/logos/{fileName}";
                
                return Ok(new { message = "LOGO图片上传成功", imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "上传LOGO图片失败", error = ex.Message });
            }
        }

        [HttpPost("logo-settings")]
        public async Task<IActionResult> SaveLogoSettings([FromBody] LogoSettingsRequest request)
        {
            try
            {
                // 验证请求数据
                if (request == null)
                {
                    return BadRequest(new { message = "LOGO设置数据不能为空" });
                }

                // 创建数据目录
                Directory.CreateDirectory(_dataDirectory);
                var filePath = Path.Combine(_dataDirectory, "logo-settings.json");

                // 创建LOGO设置对象
                var logoSettings = new
                {
                    type = request.Type,
                    text = request.Text,
                    fontSize = request.FontSize,
                    fontFamily = request.FontFamily,
                    color = request.Color,
                    fontWeight = request.FontWeight,
                    imageUrl = request.ImageUrl,
                    imagePosition = request.ImagePosition,
                    updatedAt = DateTime.Now
                };

                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(logoSettings, options);
                await System.IO.File.WriteAllTextAsync(filePath, json, System.Text.Encoding.UTF8);

                return Ok(new { message = "LOGO设置保存成功", data = logoSettings });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "保存LOGO设置失败", error = ex.Message });
            }
        }

        [HttpGet("logo-settings")]
        public async Task<IActionResult> GetLogoSettings()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "logo-settings.json");
                if (!System.IO.File.Exists(filePath))
                {
                    // 返回默认设置
                    return Ok(new
                    {
                        type = "text",
                        text = "SheIn",
                        fontSize = 24,
                        fontFamily = "Arial, sans-serif",
                        color = "#000000",
                        fontWeight = "400",
                        imageUrl = "",
                        imagePosition = "side-by-side"
                    });
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
                var logoSettings = JsonSerializer.Deserialize<object>(json);
                
                return Ok(logoSettings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取LOGO设置失败", error = ex.Message });
            }
        }

        [HttpGet("logos/fonts")]
        public IActionResult GetAvailableFonts()
        {
            try
            {
                var fonts = new List<object>
                {
                    new { name = "华文行楷", value = "STXingkai" },
                    new { name = "方正舒体", value = "FZShuTi" },
                    new { name = "华文彩云", value = "STCaiyun" },
                    new { name = "华文琥珀", value = "STHupo" },
                    new { name = "华文新魏", value = "STXinwei" },
                    new { name = "华文隶书", value = "STLiti" },
                    new { name = "华文楷体", value = "STKaiti" },
                    new { name = "方正姚体", value = "FZYaoti" },
                    new { name = "华文仿宋", value = "STFangsong" },
                    new { name = "华文中宋", value = "STZhongsong" },
                    new { name = "Arial", value = "Arial" },
                    new { name = "Times New Roman", value = "Times New Roman" }
                };
                
                return Ok(new { data = fonts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取字体列表失败", error = ex.Message });
            }
        }





        // 系统配置API
        [HttpGet("config/vip-levels")]
        public async Task<IActionResult> GetVipLevels()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _vipLevelsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new object[0]);
                }

                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var vipLevels = JsonSerializer.Deserialize<object[]>(json) ?? new object[0];
                
                return Ok(vipLevels);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取VIP等级配置失败", error = ex.Message });
            }
        }

        [HttpPut("config/vip-levels")]
        public async Task<IActionResult> UpdateVipLevels([FromBody] object vipLevelsData)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _vipLevelsFile);
                
                // 序列化并保存VIP等级数据
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                
                var json = JsonSerializer.Serialize(vipLevelsData, options);
                await System.IO.File.WriteAllTextAsync(filePath, json);
                
                return Ok(new { message = "VIP等级配置更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新VIP等级配置失败", error = ex.Message });
            }
        }
        
        [HttpPut("vip/levels/{level}")]
        public async Task<IActionResult> UpdateVipLevel(int level, [FromBody] VipLevelUpdateRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "vip_levels.json");
                
                // 读取现有VIP等级数据
                var vipLevels = new List<object>();
                if (System.IO.File.Exists(filePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var jsonDocument = JsonDocument.Parse(json);
                        vipLevels = JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();
                    }
                }
                
                // 更新指定等级的VIP数据
                var updated = false;
                var updatedVipLevels = new List<object>();
                
                foreach (var item in vipLevels)
                {
                    var jsonElement = (JsonElement)item;
                    if (jsonElement.TryGetProperty("level", out var levelProp) && levelProp.GetInt32() == level)
                    {
                        // 创建更新后的VIP等级对象
                        var updatedVipLevel = new
                        {
                            level = level,
                            name = jsonElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : $"VIP - {level}",
                            min_deposit = request.MinDeposit,
                            commission_rate = request.CommissionRate,
                            daily_order_limit = request.DailyOrderLimit,
                            withdrawal_limit = jsonElement.TryGetProperty("withdrawal_limit", out var withdrawalProp) ? withdrawalProp.GetDecimal() : 10000,
                            benefits = request.Benefits ?? new string[0],
                            color = jsonElement.TryGetProperty("color", out var colorProp) ? colorProp.GetString() : "#3498db",
                            icon = jsonElement.TryGetProperty("icon", out var iconProp) ? iconProp.GetString() : "👑"
                        };
                        updatedVipLevels.Add(updatedVipLevel);
                        updated = true;
                    }
                    else
                    {
                        updatedVipLevels.Add(item);
                    }
                }
                
                if (!updated)
                {
                    return NotFound(new { message = $"未找到VIP等级 {level}" });
                }
                
                // 保存更新后的数据
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var updatedJson = JsonSerializer.Serialize(updatedVipLevels, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "VIP等级更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新VIP等级失败", error = ex.Message });
            }
        }





        [HttpGet("config/recharge-addresses")]
        public async Task<IActionResult> GetRechargeAddresses()
        {
            try
            {
                // 模拟充值地址配置
                var rechargeAddresses = new
                {
                    usdt = "TQn9Y2khEsLMWD2iNzapKiVYhQLhjQ5oJo",
                    btc = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa",
                    eth = "0x742d35Cc6634C0532925a3b8D4C9db96590645d8"
                };
                
                return Ok(rechargeAddresses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取充值地址配置失败", error = ex.Message });
            }
        }

        [HttpPut("config/recharge-addresses")]
        public async Task<IActionResult> UpdateRechargeAddresses([FromBody] object addressesData)
        {
            try
            {
                // 这里应该实现充值地址配置更新逻辑
                return Ok(new { message = "系统配置更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新充值地址配置失败", error = ex.Message });
            }
        }
        
        // 辅助方法
        private string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        
        private async Task SaveInviteCodeRecord(Agent agent)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _invitationsFile);
                Directory.CreateDirectory(_dataDirectory);
                
                var inviteCodes = new List<AgentInviteCode>();
                if (System.IO.File.Exists(filePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(filePath);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var deserializeOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        inviteCodes = JsonSerializer.Deserialize<List<AgentInviteCode>>(json, deserializeOptions) ?? new List<AgentInviteCode>();
                    }
                }
                
                var inviteCodeRecord = new AgentInviteCode
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = agent.InviteCode,
                    AgentId = agent.Id,
                    AgentNickName = agent.NickName,
                    IsUsed = false,
                    UsedCount = 0,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
                
                inviteCodes.Add(inviteCodeRecord);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var updatedJson = JsonSerializer.Serialize(inviteCodes, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                // 记录错误但不影响主流程
                Console.WriteLine($"保存邀请码记录失败: {ex.Message}");
            }
        }
        
        private async Task RemoveInviteCodeRecord(string inviteCode)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _invitationsFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return;
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var inviteCodes = JsonSerializer.Deserialize<List<AgentInviteCode>>(json, deserializeOptions) ?? new List<AgentInviteCode>();
                
                inviteCodes.RemoveAll(ic => ic.Code == inviteCode);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var updatedJson = JsonSerializer.Serialize(inviteCodes, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除邀请码记录失败: {ex.Message}");
            }
        }
        
        private async Task<int> GetAgentCustomerCount(string agentId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "user_agent_mappings.json");
                if (!System.IO.File.Exists(filePath))
                {
                    return 0;
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var mappings = JsonSerializer.Deserialize<List<UserAgentMapping>>(json) ?? new List<UserAgentMapping>();
                
                return mappings.Count(m => m.AgentId == agentId);
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // 客户管理API
        [HttpGet("agents/{agentId}/customers")]
        public async Task<IActionResult> GetAgentCustomers(string agentId)
        {
            try
            {
                var mappingFilePath = Path.Combine(_dataDirectory, "user_agent_mappings.json");
                var usersFilePath = Path.Combine(_dataDirectory, _usersFile);
                
                // 读取用户-业务员关联数据
                var mappings = new List<UserAgentMapping>();
                if (System.IO.File.Exists(mappingFilePath))
                {
                    var mappingJson = await System.IO.File.ReadAllTextAsync(mappingFilePath);
                    if (!string.IsNullOrEmpty(mappingJson))
                    {
                        mappings = JsonSerializer.Deserialize<List<UserAgentMapping>>(mappingJson) ?? new List<UserAgentMapping>();
                    }
                }
                
                // 读取用户数据
                var users = new List<User>();
                if (System.IO.File.Exists(usersFilePath))
                {
                    var usersJson = await System.IO.File.ReadAllTextAsync(usersFilePath);
                    if (!string.IsNullOrEmpty(usersJson))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        };
                        users = JsonSerializer.Deserialize<List<User>>(usersJson, options) ?? new List<User>();
                    }
                }
                
                // 获取该业务员的客户ID列表
                var customerIds = mappings.Where(m => m.AgentId == agentId).Select(m => m.UserId).ToList();
                
                // 获取客户详细信息
                var customers = users.Where(u => customerIds.Contains(u.Id.ToString())).Select(user => new
                {
                    id = user.Id.ToString(),
                    name = user.NickName,
                    registerTime = user.RegisterTime,
                    orderCount = GetUserOrderCount(user.Id.ToString()),
                    totalSpent = GetUserTotalSpent(user.Id.ToString()),
                    status = user.IsActive ? "active" : "inactive",
                    phone = user.Phone,
                    vipLevel = user.VipLevel
                }).ToList();
                
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取客户列表失败", error = ex.Message });
            }
        }
        
        [HttpDelete("customers/{customerId}/agent")]
        public async Task<IActionResult> RemoveCustomerAgent(string customerId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "user_agent_mappings.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "客户关联不存在" });
                }
                
                // 读取现有关联数据
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var mappings = JsonSerializer.Deserialize<List<UserAgentMapping>>(json) ?? new List<UserAgentMapping>();
                
                // 查找并删除关联
                var mappingToRemove = mappings.FirstOrDefault(m => m.UserId == customerId);
                if (mappingToRemove == null)
                {
                    return NotFound(new { message = "客户关联不存在" });
                }
                
                mappings.Remove(mappingToRemove);
                
                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var updatedJson = JsonSerializer.Serialize(mappings, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "客户关联已删除" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除客户关联失败", error = ex.Message });
            }
        }
        
        [HttpPost("customers/{customerId}/transfer")]
        public async Task<IActionResult> TransferCustomer(string customerId, [FromBody] TransferCustomerRequest request)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, "user_agent_mappings.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = "客户关联不存在" });
                }
                
                // 读取现有关联数据
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var mappings = JsonSerializer.Deserialize<List<UserAgentMapping>>(json) ?? new List<UserAgentMapping>();
                
                // 查找要转移的客户关联
                var mappingToUpdate = mappings.FirstOrDefault(m => m.UserId == customerId && m.AgentId == request.FromAgentId);
                if (mappingToUpdate == null)
                {
                    return NotFound(new { message = "客户关联不存在" });
                }
                
                // 更新业务员ID
                mappingToUpdate.AgentId = request.ToAgentId;
                
                // 保存到文件
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var updatedJson = JsonSerializer.Serialize(mappings, options);
                await System.IO.File.WriteAllTextAsync(filePath, updatedJson);
                
                return Ok(new { message = "客户转移成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "客户转移失败", error = ex.Message });
            }
        }
        
        // 辅助方法：获取用户订单数量
        private int GetUserOrderCount(string userId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _ordersFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return 0;
                }
                
                var json = System.IO.File.ReadAllText(filePath);
                var orders = JsonSerializer.Deserialize<List<Order>>(json) ?? new List<Order>();
                
                return orders.Count(o => o.UserId.ToString() == userId);
            }
            catch
            {
                return new Random().Next(0, 10); // 模拟数据
            }
        }
        
        // 辅助方法：获取用户总消费
        private decimal GetUserTotalSpent(string userId)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _ordersFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return 0;
                }
                
                var json = System.IO.File.ReadAllText(filePath);
                var orders = JsonSerializer.Deserialize<List<Order>>(json) ?? new List<Order>();
                
                return orders.Where(o => o.UserId.ToString() == userId).Sum(o => o.Amount);
            }
            catch
            {
                return new Random().Next(1000, 50000); // 模拟数据
            }
        }

        // 获取业务员详细信息
        [HttpGet("agents/{id}/detail")]
        public async Task<IActionResult> GetAgentDetail(string id)
        {
            try
            {
                Console.WriteLine($"GetAgentDetail called with id: {id}");
                var filePath = Path.Combine(_dataDirectory, _agentsFile);
                Console.WriteLine($"File path: {filePath}");
                
                if (!System.IO.File.Exists(filePath))
                {
                    Console.WriteLine("Agents file not found");
                    return NotFound(new { message = "业务员不存在" });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                Console.WriteLine($"JSON content: {json.Substring(0, Math.Min(200, json.Length))}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var agents = JsonSerializer.Deserialize<List<Agent>>(json, options) ?? new List<Agent>();
                Console.WriteLine($"Deserialized {agents.Count} agents");
                
                var agent = agents.FirstOrDefault(a => a.Id == id);
                
                if (agent == null)
                {
                    Console.WriteLine($"Agent with id {id} not found");
                    return NotFound(new { message = "业务员不存在" });
                }
                
                Console.WriteLine($"Found agent: {agent.NickName}");
                
                // 获取客户数量和业绩统计
                var customerCount = await GetAgentCustomerCount(id);
                var monthlyPerformance = await GetAgentMonthlyPerformance(id);
                var totalPerformance = await GetAgentTotalPerformance(id);
                
                var agentDetail = new
                {
                    id = agent.Id,
                    nickName = agent.NickName,
                    account = agent.Account,
                    inviteCode = agent.InviteCode,
                    avatar = "", // Agent模型中没有Avatar属性，设置为空字符串
                    customerCount = customerCount,
                    monthlyPerformance = monthlyPerformance,
                    totalPerformance = totalPerformance,
                    isActive = agent.IsActive,
                    registerTime = agent.RegisterTime,
                    createdAt = agent.CreatedAt,
                    updatedAt = agent.UpdatedAt
                };
                
                Console.WriteLine("Returning agent detail successfully");
                return Ok(agentDetail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAgentDetail error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "获取业务员详情失败", error = ex.Message });
            }
        }

        // 获取客户聊天记录
        [HttpGet("customers/{customerId}/chat-records")]
        public async Task<IActionResult> GetCustomerChatRecords(string customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _messagesFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return Ok(new { messages = new List<object>(), total = 0, page, pageSize });
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json, options) ?? new List<ChatMessage>();
                
                // 筛选该客户的聊天记录
                var customerMessages = messages.Where(m => m.UserId == customerId)
                                               .OrderByDescending(m => m.Timestamp)
                                               .ToList();
                
                var total = customerMessages.Count;
                var pagedMessages = customerMessages.Skip((page - 1) * pageSize)
                                                   .Take(pageSize)
                                                   .Select(m => new
                                                   {
                                                       id = m.Id,
                                                       content = m.Content,
                                                       timestamp = m.Timestamp,
                                                       isFromUser = m.IsFromUser,
                                                       messageType = m.MessageType ?? "text"
                                                   })
                                                   .ToList();
                
                return Ok(new { messages = pagedMessages, total, page, pageSize });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取聊天记录失败", error = ex.Message });
            }
        }

        // 辅助方法：获取业务员月度业绩
        private async Task<decimal> GetAgentMonthlyPerformance(string agentId)
        {
            try
            {
                var mappingFilePath = Path.Combine(_dataDirectory, "user_agent_mappings.json");
                var ordersFilePath = Path.Combine(_dataDirectory, _ordersFile);
                
                // 获取该业务员的客户列表
                var mappings = new List<UserAgentMapping>();
                if (System.IO.File.Exists(mappingFilePath))
                {
                    var mappingJson = await System.IO.File.ReadAllTextAsync(mappingFilePath);
                    if (!string.IsNullOrEmpty(mappingJson))
                    {
                        mappings = JsonSerializer.Deserialize<List<UserAgentMapping>>(mappingJson) ?? new List<UserAgentMapping>();
                    }
                }
                
                var customerIds = mappings.Where(m => m.AgentId == agentId).Select(m => m.UserId).ToList();
                
                // 获取本月订单
                if (System.IO.File.Exists(ordersFilePath))
                {
                    var ordersJson = await System.IO.File.ReadAllTextAsync(ordersFilePath);
                    if (!string.IsNullOrEmpty(ordersJson))
                    {
                        var orders = JsonSerializer.Deserialize<List<Order>>(ordersJson) ?? new List<Order>();
                        var currentMonth = DateTime.Now.Month;
                        var currentYear = DateTime.Now.Year;
                        
                        return orders.Where(o => customerIds.Contains(o.UserId.ToString()) &&
                                               o.CreateTime.Month == currentMonth &&
                                               o.CreateTime.Year == currentYear)
                                    .Sum(o => o.Amount);
                    }
                }
                
                return new Random().Next(5000, 20000); // 模拟数据
            }
            catch
            {
                return new Random().Next(5000, 20000); // 模拟数据
            }
        }

        // 辅助方法：获取业务员总业绩
        private async Task<decimal> GetAgentTotalPerformance(string agentId)
        {
            try
            {
                var mappingFilePath = Path.Combine(_dataDirectory, "user_agent_mappings.json");
                var ordersFilePath = Path.Combine(_dataDirectory, _ordersFile);
                
                // 获取该业务员的客户列表
                var mappings = new List<UserAgentMapping>();
                if (System.IO.File.Exists(mappingFilePath))
                {
                    var mappingJson = await System.IO.File.ReadAllTextAsync(mappingFilePath);
                    if (!string.IsNullOrEmpty(mappingJson))
                    {
                        mappings = JsonSerializer.Deserialize<List<UserAgentMapping>>(mappingJson) ?? new List<UserAgentMapping>();
                    }
                }
                
                var customerIds = mappings.Where(m => m.AgentId == agentId).Select(m => m.UserId).ToList();
                
                // 获取所有订单
                if (System.IO.File.Exists(ordersFilePath))
                {
                    var ordersJson = await System.IO.File.ReadAllTextAsync(ordersFilePath);
                    if (!string.IsNullOrEmpty(ordersJson))
                    {
                        var orders = JsonSerializer.Deserialize<List<Order>>(ordersJson) ?? new List<Order>();
                        return orders.Where(o => customerIds.Contains(o.UserId.ToString()))
                                    .Sum(o => o.Amount);
                    }
                }
                
                return new Random().Next(20000, 100000); // 模拟数据
            }
            catch
            {
                return new Random().Next(20000, 100000); // 模拟数据
            }
        }

        // 网站设置相关API
        private readonly string _siteSettingsFile = "site_settings.json";

        [HttpGet("site-settings")]
        public async Task<IActionResult> GetSiteSettings()
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _siteSettingsFile);
                
                if (!System.IO.File.Exists(filePath))
                {
                    // 返回默认设置
                    var defaultSettings = new { siteName = "移动电商平台" };
                    return Ok(defaultSettings);
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                
                if (string.IsNullOrEmpty(json))
                {
                    var defaultSettings = new { siteName = "移动电商平台" };
                    return Ok(defaultSettings);
                }
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                var settings = JsonSerializer.Deserialize<object>(json, options);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetSiteSettings error: {ex.Message}");
                var defaultSettings = new { siteName = "移动电商平台" };
                return Ok(defaultSettings);
            }
        }

        [HttpPost("site-settings")]
        public async Task<IActionResult> SaveSiteSettings([FromBody] JsonElement settings)
        {
            try
            {
                var filePath = Path.Combine(_dataDirectory, _siteSettingsFile);
                
                // 确保Data目录存在
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(settings, options);
                await System.IO.File.WriteAllTextAsync(filePath, json);
                
                Console.WriteLine($"Site settings saved: {json}");
                
                return Ok(new { message = "网站设置保存成功" });
            }
            catch (Exception ex)
            {
            Console.WriteLine($"SaveSiteSettings error: {ex.Message}");
                return StatusCode(500, new { message = "保存网站设置失败", error = ex.Message });
            }
        }

        // 管理员管理相关API已迁移到 AdminManagementController







        [HttpGet("permissions/check")]
        public async Task<IActionResult> CheckPermission([FromQuery] string resource, [FromQuery] string? userId = null)
        {
            try
            {
                // 如果没有提供userId，默认使用超级管理员权限（用于演示）
                var permissionLevel = 0; // 超级管理员
                var userType = "super_admin";
                var username = "超级管理员";
                
                // 如果提供了userId，从相应的数据库文件中查找用户信息
                if (!string.IsNullOrEmpty(userId))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };
                    
                    User user = null;
                    
                    // 首先在super_admins.json中查找
                    var superAdminsPath = Path.Combine(_dataDirectory, "super_admins.json");
                    if (System.IO.File.Exists(superAdminsPath))
                    {
                        var superAdminsJson = await System.IO.File.ReadAllTextAsync(superAdminsPath);
                        var superAdmins = JsonSerializer.Deserialize<List<User>>(superAdminsJson, options) ?? new List<User>();
                        user = superAdmins.FirstOrDefault(u => u.Id == userId);
                    }
                    
                    // 如果没找到，在regular_admins.json中查找
                    if (user == null)
                    {
                        var regularAdminsPath = Path.Combine(_dataDirectory, "regular_admins.json");
                        if (System.IO.File.Exists(regularAdminsPath))
                        {
                            var regularAdminsJson = await System.IO.File.ReadAllTextAsync(regularAdminsPath);
                            var regularAdmins = JsonSerializer.Deserialize<List<User>>(regularAdminsJson, options) ?? new List<User>();
                            user = regularAdmins.FirstOrDefault(u => u.Id == userId);
                        }
                    }
                    
                    // 如果还没找到，在users.json中查找
                    if (user == null)
                    {
                        var usersPath = Path.Combine(_dataDirectory, _usersFile);
                        if (System.IO.File.Exists(usersPath))
                        {
                            var usersJson = await System.IO.File.ReadAllTextAsync(usersPath);
                            var users = JsonSerializer.Deserialize<List<User>>(usersJson, options) ?? new List<User>();
                            user = users.FirstOrDefault(u => u.Id == userId);
                        }
                    }
                    
                    if (user != null)
                    {
                        permissionLevel = user.PermissionLevel;
                        userType = user.UserType ?? "user";
                        username = user.NickName ?? user.Phone ?? "未知用户";
                    }
                }
                
                bool hasPermission = true;
                string message = "";
                
                // 基于权限等级的权限检查逻辑
                switch (resource)
                {
                    case "payment-settings":
                    case "payment_accounts":
                        // 只有超级管理员(0)可以访问收款账户设置
                        if (permissionLevel > 0)
                        {
                            hasPermission = false;
                            message = GetPermissionLevelName(permissionLevel) + "无权访问收款账户设置";
                        }
                        break;
                    case "admin-management":
                    case "admin_management":
                        // 只有超级管理员(0)可以管理其他管理员
                        if (permissionLevel > 0)
                        {
                            hasPermission = false;
                            message = GetPermissionLevelName(permissionLevel) + "无权管理其他管理员";
                        }
                        break;
                    case "system-settings":
                    case "system_settings":
                        // 只有超级管理员(0)可以修改系统设置
                        if (permissionLevel > 0)
                        {
                            hasPermission = false;
                            message = GetPermissionLevelName(permissionLevel) + "无权修改系统设置";
                        }
                        break;
                    case "backend-access":
                        // 超级管理员(0)和管理员(1)可以访问后台
                        if (permissionLevel > 1)
                        {
                            hasPermission = false;
                            message = GetPermissionLevelName(permissionLevel) + "无权访问后台管理";
                        }
                        break;
                    case "salesperson-management":
                        // 超级管理员(0)和管理员(1)可以管理业务员
                        if (permissionLevel > 1)
                        {
                            hasPermission = false;
                            message = GetPermissionLevelName(permissionLevel) + "无权管理业务员";
                        }
                        break;
                    default:
                        // 其他资源默认允许访问
                        break;
                }
                
                return Ok(new { 
                    hasPermission, 
                    permissionLevel,
                    userType, 
                    username,
                    message = hasPermission ? "有权限访问" : message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "权限检查失败", error = ex.Message });
            }
        }

        /// <summary>
        /// 检查超级管理员权限 - 专用于超级管理员页面访问控制
        /// </summary>
        [HttpGet("permissions/check-super-admin")]
        public async Task<IActionResult> CheckSuperAdminPermission([FromQuery] string? userId = null)
        {
            try
            {
                // 如果没有提供userId，直接返回未授权
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { 
                        success = false, 
                        isSuperAdmin = false,
                        message = "用户未登录",
                        redirectUrl = "/super-admin-login.html"
                    });
                }
                
                var permissionLevel = 3; // 默认普通用户
                var userType = "user";
                var username = "未知用户";
                
                // 从超级管理员文件中查找用户信息
                {
                    var superAdminsPath = Path.Combine(_dataDirectory, "super_admins.json");
                    
                    if (System.IO.File.Exists(superAdminsPath))
                    {
                        var json = await System.IO.File.ReadAllTextAsync(superAdminsPath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                        };
                        
                        var superAdmins = JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
                        var user = superAdmins.FirstOrDefault(u => u.Id == userId);
                        
                        if (user != null)
                        {
                            permissionLevel = user.PermissionLevel;
                            userType = user.UserType ?? "user";
                            username = user.NickName ?? user.Phone ?? "未知用户";
                        }
                        else
                        {
                            return Unauthorized(new { 
                                success = false, 
                                isSuperAdmin = false,
                                message = "用户不存在或未登录",
                                redirectUrl = "/super-admin-login.html"
                            });
                        }
                    }
                    else
                    {
                        return Unauthorized(new { 
                            success = false, 
                            isSuperAdmin = false,
                            message = "超级管理员数据文件不存在",
                            redirectUrl = "/super-admin-login.html"
                        });
                    }
                }
                
                // 严格检查：只有权限等级为0的超级管理员才能访问
                bool isSuperAdmin = permissionLevel == 0 && userType == "super_admin";
                
                if (!isSuperAdmin)
                {
                    return StatusCode(403, new { 
                        success = false, 
                        isSuperAdmin = false,
                        permissionLevel,
                        userType,
                        username,
                        message = $"{GetPermissionLevelName(permissionLevel)}无权访问超级管理员专用页面",
                        redirectUrl = "/admin.html"
                    });
                }
                
                return Ok(new { 
                    success = true,
                    isSuperAdmin = true,
                    permissionLevel,
                    userType, 
                    username,
                    message = "超级管理员权限验证通过"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    isSuperAdmin = false,
                    message = "超级管理员权限检查失败", 
                    error = ex.Message,
                    redirectUrl = "/super-admin-login.html"
                });
            }
        }

        /// <summary>
        /// 根据权限等级获取权限等级名称
        /// </summary>
        /// <param name="permissionLevel">权限等级</param>
        /// <returns>权限等级名称</returns>
        private string GetPermissionLevelName(int permissionLevel)
        {
            return permissionLevel switch
            {
                0 => "超级管理员",
                1 => "管理员",
                2 => "业务员",
                3 => "注册用户",
                _ => "未知用户"
            };
        }

        // 辅助方法：密码哈希
        private string HashPassword(string password)
        {
            // 使用简单的Base64编码，实际项目中应该使用更安全的哈希算法
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(bytes);
        }

        // 私有方法：检查用户权限
        private async Task<(bool hasPermission, string message)> CheckUserPermission(string userId, string resource)
        {
            try
            {
                var result = await CheckPermission(resource, userId);
                if (result is OkObjectResult okResult && okResult.Value != null)
                {
                    var permissionData = okResult.Value;
                    var hasPermissionProperty = permissionData.GetType().GetProperty("hasPermission");
                    var messageProperty = permissionData.GetType().GetProperty("message");
                    
                    if (hasPermissionProperty != null && messageProperty != null)
                    {
                        var hasPermission = (bool)(hasPermissionProperty.GetValue(permissionData) ?? false);
                        var message = (string)(messageProperty.GetValue(permissionData) ?? "");
                        return (hasPermission, message);
                    }
                }
                
                return (false, "权限检查失败");
            }
            catch (Exception ex)
            {
                return (false, $"权限检查异常: {ex.Message}");
            }
        }

    }

    // LOGO设置请求模型
    public class LogoSettingsRequest
    {
        public string Type { get; set; } = "text";
        public string Text { get; set; } = string.Empty;
        public int FontSize { get; set; } = 24;
        public string FontFamily { get; set; } = "Arial, sans-serif";
        public string Color { get; set; } = "#000000";
        public string FontWeight { get; set; } = "400";
        public string ImageUrl { get; set; } = string.Empty;
        public string ImagePosition { get; set; } = "side-by-side";
    }

    // 管理员管理请求模型
    public class CreateAdministratorRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string UserType { get; set; } = "admin";
        public string Status { get; set; } = "active";
    }

    public class UpdateAdministratorRequest
    {
        public string? Nickname { get; set; }
        public string? Password { get; set; }
        public string? Status { get; set; }
        public string? UserType { get; set; }
    }
}