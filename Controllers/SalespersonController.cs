using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalespersonController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly string _dataDirectory = "Data";
        private readonly string _agentsFile = "agents.json";
        private readonly string _usersFile = "users.json";
        private readonly string _assignmentsFile = "customer_assignments.json";
        private readonly string _statsFile = "salesperson_stats.json";
        private readonly string _balancesFile = "balances.json";

        public SalespersonController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                var salespersons = LoadSalespersons();
                var customers = LoadCustomers();
                var assignments = LoadAssignments();
                
                var activeSalespersons = salespersons.Count(s => s.Status == "active");
                var totalCommission = CalculateTotalCommission();
                
                var stats = new
                {
                    total_salespersons = salespersons.Count,
                    active_salespersons = activeSalespersons,
                    total_customers = customers.Count,
                    total_commission = totalCommission.ToString("F2")
                };
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取统计数据失败", error = ex.Message });
            }
        }

        [HttpGet("list")]
        public IActionResult GetSalespersonList()
        {
            try
            {
                var salespersons = LoadSalespersons();
                var assignments = LoadAssignments();
                
                // 计算每个业务员的客户数量，包含明文密码字段（按用户要求显示）
                var salespersonList = salespersons.Select(s => new
                {
                    Id = s.Id,
                    Username = s.Username,
                    Nickname = s.Nickname,
                    PlainPassword = s.PlainPassword, // 返回明文密码
                    InviteCode = s.InviteCode,
                    CommissionRate = s.CommissionRate,
                    Status = s.Status,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    CustomerCount = assignments.Count(a => a.SalespersonId == s.Id)
                }).ToList();
                
                return Ok(new { success = true, data = salespersonList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "获取业务员列表失败", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] SalespersonLoginRequest request)
        {
            try
            {
                var salespersons = LoadSalespersons();
                
                // 查找匹配的业务员
                var salesperson = salespersons.FirstOrDefault(s => 
                    s.Username == request.Username && 
                    s.Status == "active"
                );
                
                if (salesperson == null)
                {
                    return BadRequest(new { success = false, message = "用户名或密码错误" });
                }
                
                // 验证密码
                if (!VerifyPassword(request.Password, salesperson.PasswordHash))
                {
                    return BadRequest(new { success = false, message = "用户名或密码错误" });
                }
                
                // 返回业务员信息（不包含密码）
                var agentInfo = new
                {
                    Id = salesperson.Id,
                    Username = salesperson.Username,
                    Nickname = salesperson.Nickname,
                    InviteCode = salesperson.InviteCode,
                    CommissionRate = salesperson.CommissionRate,
                    Status = salesperson.Status,
                    CreatedAt = salesperson.CreatedAt,
                    CustomerCount = salesperson.CustomerCount
                };
                
                return Ok(new { success = true, message = "登录成功", agent = agentInfo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "登录失败", error = ex.Message });
            }
        }

        [HttpGet("customers")]
        public IActionResult GetCustomers()
        {
            try
            {
                var customers = LoadCustomers();
                var assignments = LoadAssignments();
                var salespersons = LoadSalespersons();
                
                // 为每个客户添加业务员信息
                foreach (var customer in customers)
                {
                    var assignment = assignments.FirstOrDefault(a => a.CustomerId == customer.Id);
                    if (assignment != null)
                    {
                        customer.SalespersonId = assignment.SalespersonId;
                        var salesperson = salespersons.FirstOrDefault(s => s.Id == assignment.SalespersonId);
                        customer.SalespersonName = salesperson?.Nickname;
                    }
                }
                
                return Ok(new { customers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取客户列表失败", error = ex.Message });
            }
        }



        [HttpPost("create")]
        public IActionResult CreateSalesperson([FromBody] CreateSalespersonRequest request)
        {
            try
            {
                var salespersons = LoadSalespersons();
                
                // 检查用户名是否已存在
                if (salespersons.Any(s => s.Username == request.Username))
                {
                    return BadRequest(new { message = "用户名已存在" });
                }
                
                // 检查邀请码是否已存在
                if (salespersons.Any(s => s.InviteCode == request.InviteCode))
                {
                    return BadRequest(new { message = "邀请码已存在" });
                }
                
                // 验证邀请码格式（6位数字）
                if (!IsValidInviteCode(request.InviteCode))
                {
                    return BadRequest(new { message = "邀请码必须是6位数字" });
                }
                
                var newSalesperson = new Salesperson
                {
                    Id = Guid.NewGuid().ToString(),
                    Username = request.Username,
                    Nickname = request.Nickname,
                    PasswordHash = HashPassword(request.Password),
                    PlainPassword = request.Password,
                    InviteCode = request.InviteCode,
                    CommissionRate = request.CommissionRate,
                    Status = "active",
                    CreatedAt = DateTime.Now,
                    CustomerCount = 0
                };
                
                salespersons.Add(newSalesperson);
                SaveSalespersons(salespersons);
                
                // 返回时不包含密码哈希
                var responseData = new
                {
                    Id = newSalesperson.Id,
                    Username = newSalesperson.Username,
                    Nickname = newSalesperson.Nickname,
                    InviteCode = newSalesperson.InviteCode,
                    CommissionRate = newSalesperson.CommissionRate,
                    Status = newSalesperson.Status,
                    CreatedAt = newSalesperson.CreatedAt,
                    CustomerCount = newSalesperson.CustomerCount
                };
                
                return Ok(new { success = true, message = "业务员创建成功", data = responseData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建业务员失败", error = ex.Message });
            }
        }

        [HttpGet("generate-invite-code")]
        public IActionResult GenerateInviteCode()
        {
            try
            {
                var salespersons = LoadSalespersons();
                string inviteCode;
                
                // 生成唯一的6位数字邀请码
                do
                {
                    inviteCode = GenerateRandomInviteCode();
                } while (salespersons.Any(s => s.InviteCode == inviteCode));
                
                return Ok(new { success = true, data = new { inviteCode } });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "生成邀请码失败", error = ex.Message });
            }
        }

        [HttpPost("validate-invite-code")]
        public IActionResult ValidateInviteCode([FromBody] ValidateInviteCodeRequest request)
        {
            try
            {
                // 验证邀请码格式
                if (!IsValidInviteCode(request.InviteCode))
                {
                    return Ok(new { isValid = false, message = "邀请码必须是6位数字" });
                }
                
                var salespersons = LoadSalespersons();
                
                // 检查邀请码是否已被使用（排除指定的业务员ID）
                var isUsed = salespersons.Any(s => s.InviteCode == request.InviteCode && s.Id != request.ExcludeId);
                
                if (isUsed)
                {
                    return Ok(new { isValid = false, message = "邀请码已被使用" });
                }
                
                return Ok(new { isValid = true, message = "邀请码可用" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "验证邀请码失败", error = ex.Message });
            }
        }

        [HttpPut("update/{id}")]
        public IActionResult UpdateSalesperson(string id, [FromBody] UpdateSalespersonRequest request)
        {
            try
            {
                var salespersons = LoadSalespersons();
                var salesperson = salespersons.FirstOrDefault(s => s.Id == id);
                
                if (salesperson == null)
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 检查手机号是否被其他业务员使用
                if (!string.IsNullOrEmpty(request.Phone) && salespersons.Any(s => s.Phone == request.Phone && s.Id != id))
                {
                    return BadRequest(new { message = "手机号已被其他业务员使用" });
                }
                
                // 检查邀请码是否被其他业务员使用
                if (!string.IsNullOrEmpty(request.InviteCode))
                {
                    if (!IsValidInviteCode(request.InviteCode))
                    {
                        return BadRequest(new { message = "邀请码必须是6位数字" });
                    }
                    
                    if (salespersons.Any(s => s.InviteCode == request.InviteCode && s.Id != id))
                    {
                        return BadRequest(new { message = "邀请码已被其他业务员使用" });
                    }
                    
                    salesperson.InviteCode = request.InviteCode;
                }
                
                // 更新其他字段
                if (!string.IsNullOrEmpty(request.Name))
                    salesperson.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Phone))
                    salesperson.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Email))
                    salesperson.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Department))
                    salesperson.Department = request.Department;
                if (request.CommissionRate.HasValue)
                    salesperson.CommissionRate = request.CommissionRate.Value;
                if (!string.IsNullOrEmpty(request.Status))
                    salesperson.Status = request.Status;
                if (!string.IsNullOrEmpty(request.Password))
                {
                    salesperson.PasswordHash = HashPassword(request.Password);
                    salesperson.PlainPassword = request.Password;
                }
                
                salesperson.UpdatedAt = DateTime.Now;
                
                SaveSalespersons(salespersons);
                
                // 返回时不包含密码哈希
                var responseData = new
                {
                    Id = salesperson.Id,
                    Username = salesperson.Username,
                    Name = salesperson.Name,
                    InviteCode = salesperson.InviteCode,
                    Phone = salesperson.Phone,
                    Email = salesperson.Email,
                    Department = salesperson.Department,
                    CommissionRate = salesperson.CommissionRate,
                    Status = salesperson.Status,
                    CreatedAt = salesperson.CreatedAt,
                    UpdatedAt = salesperson.UpdatedAt,
                    CustomerCount = salesperson.CustomerCount
                };
                
                return Ok(new { success = true, message = "业务员更新成功", data = responseData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新业务员失败", error = ex.Message });
            }
        }

        [HttpDelete("delete/{id}")]
        public IActionResult DeleteSalesperson(string id, [FromQuery] bool force = false)
        {
            try
            {
                var salespersons = LoadSalespersons();
                var salesperson = salespersons.FirstOrDefault(s => s.Id == id);
                
                if (salesperson == null)
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 检查是否有分配的客户
                var assignments = LoadAssignments();
                var customerAssignments = assignments.Where(a => a.SalespersonId == id).ToList();
                
                if (customerAssignments.Any() && !force)
                {
                    // 返回客户信息，让前端决定是否转移
                    var customers = LoadCustomers();
                    var users = LoadUsers();
                    var customerList = customerAssignments
                        .Join(customers, a => a.CustomerId, c => c.Id, (a, c) => new
                        {
                            id = c.Id,
                            username = c.Username,
                            email = c.Email,
                            inviteCodeUsed = users.FirstOrDefault(u => u.Id == c.Id)?.InviteCodeUsed ?? ""
                        })
                        .ToList();
                    
                    return BadRequest(new { 
                        message = "该业务员还有分配的客户，无法删除",
                        hasCustomers = true,
                        customerCount = customerList.Count,
                        customers = customerList,
                        salesperson = new {
                            id = salesperson.Id,
                            nickname = salesperson.Nickname,
                            inviteCode = salesperson.InviteCode
                        }
                    });
                }
                
                salespersons.RemoveAll(s => s.Id == id);
                SaveSalespersons(salespersons);
                
                return Ok(new { message = "业务员删除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除业务员失败", error = ex.Message });
            }
        }

        [HttpPost("transfer-customers")]
        public IActionResult TransferCustomers([FromBody] TransferCustomersRequest request)
        {
            try
            {
                var salespersons = LoadSalespersons();
                var sourceSalesperson = salespersons.FirstOrDefault(s => s.Id == request.SourceSalespersonId);
                var targetSalesperson = salespersons.FirstOrDefault(s => s.Id == request.TargetSalespersonId);
                
                if (sourceSalesperson == null)
                {
                    return NotFound(new { message = "源业务员不存在" });
                }
                
                if (targetSalesperson == null)
                {
                    return NotFound(new { message = "目标业务员不存在" });
                }
                
                // 加载客户分配数据
                var assignments = LoadAssignments();
                var customersToTransfer = assignments.Where(a => a.SalespersonId == request.SourceSalespersonId).ToList();
                
                if (!customersToTransfer.Any())
                {
                    return BadRequest(new { message = "该业务员没有分配的客户" });
                }
                
                // 加载客户数据
                var customers = LoadCustomers();
                
                // 更新客户分配
                foreach (var assignment in customersToTransfer)
                {
                    assignment.SalespersonId = request.TargetSalespersonId;
                    assignment.AssignedAt = DateTime.Now;
                }
                
                // 更新用户的邀请人信息（在users.json中）
                var users = LoadUsers();
                foreach (var assignment in customersToTransfer)
                {
                    var user = users.FirstOrDefault(u => u.Id == assignment.CustomerId);
                    if (user != null)
                    {
                        user.InviteCodeUsed = targetSalesperson.InviteCode;
                        user.InviterId = targetSalesperson.Id;
                    }
                }
                
                // 保存更新后的数据
                SaveAssignments(assignments);
                SaveUsers(users);
                
                return Ok(new { 
                    message = "客户转移成功", 
                    transferredCount = customersToTransfer.Count,
                    targetSalesperson = new { 
                        id = targetSalesperson.Id, 
                        nickname = targetSalesperson.Nickname,
                        inviteCode = targetSalesperson.InviteCode
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "客户转移失败", error = ex.Message });
            }
        }

        [HttpGet("customers/{salespersonId}")]
        public IActionResult GetSalespersonCustomers(string salespersonId)
        {
            try
            {
                // 验证业务员ID
                if (string.IsNullOrEmpty(salespersonId))
                {
                    return BadRequest(new { message = "业务员ID不能为空", customers = new List<object>() });
                }

                var users = LoadUsers();
                
                // 验证业务员是否存在（检查agents.json）
                var agents = LoadAgents();
                var currentAgent = agents.FirstOrDefault(a => a.Id == salespersonId);
                if (currentAgent == null)
                {
                    return NotFound(new { message = "业务员不存在", customers = new List<object>() });
                }
                
                Console.WriteLine($"查找业务员 {salespersonId} 的客户，邀请码：{currentAgent.InviteCode}");
                
                // 双重查找机制：先通过InviterId查找，再通过邀请码查找
                var salespersonCustomers = users
                    .Where(u => u.UserType == "user" && (
                        u.InviterId == salespersonId || // 通过InviterId匹配
                        (!string.IsNullOrEmpty(u.InviteCodeUsed) && u.InviteCodeUsed == currentAgent.InviteCode) // 通过邀请码匹配
                    ))
                    .Select(u => {
                        // 为空字段提供合理的fallback值
                        var displayUsername = !string.IsNullOrEmpty(u.Username) ? u.Username :
                                            !string.IsNullOrEmpty(u.Phone) ? u.Phone :
                                            $"用户{u.Id.Substring(Math.Max(0, u.Id.Length - 4))}";
                        
                        var displayNickname = !string.IsNullOrEmpty(u.NickName) ? u.NickName :
                                            !string.IsNullOrEmpty(displayUsername) ? displayUsername :
                                            "未设置昵称";
                        
                        var displayEmail = !string.IsNullOrEmpty(u.Email) ? u.Email : "未设置邮箱";
                        
                        return new
                        {
                            id = u.Id,
                            username = displayUsername,
                            nickname = displayNickname,
                            email = displayEmail,
                            phone = u.Phone ?? "未设置手机",
                            balance = u.CurrentBalance,
                            historyBalance = 0.0, // 可以从其他地方获取历史余额
                            creditScore = u.CreditScore,
                            vipLevel = u.VipLevel,
                            isActive = u.IsActive,
                            createdAt = u.CreatedAt,
                            registerTime = u.RegisterTime,
                            inviteCodeUsed = u.InviteCodeUsed ?? "",
                            assignedAt = u.RegisterTime // 使用注册时间作为分配时间
                        };
                    })
                    .ToList();
                
                Console.WriteLine($"找到 {salespersonCustomers.Count} 个客户");
                
                // 返回统一格式，包装在customers字段中，即使为空也返回空数组
                return Ok(new { 
                    success = true,
                    message = salespersonCustomers.Count == 0 ? "该业务员暂无分配的客户" : "获取客户列表成功",
                    customers = salespersonCustomers,
                    count = salespersonCustomers.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取业务员客户列表失败: {ex.Message}");
                return StatusCode(500, new { 
                    success = false,
                    message = "获取客户列表失败", 
                    error = ex.Message,
                    customers = new List<object>()
                });
            }
        }

        [HttpPost("assign-customer")]
        public IActionResult AssignCustomer([FromBody] AssignCustomerRequest request)
        {
            try
            {
                var assignments = LoadAssignments();
                var salespersons = LoadSalespersons();
                var customers = LoadCustomers();
                
                // 验证业务员和客户是否存在
                if (!string.IsNullOrEmpty(request.SalespersonId) && !salespersons.Any(s => s.Id == request.SalespersonId))
                {
                    return BadRequest(new { message = "业务员不存在" });
                }
                
                if (!customers.Any(c => c.Id == request.CustomerId))
                {
                    return BadRequest(new { message = "客户不存在" });
                }
                
                // 移除现有分配
                assignments.RemoveAll(a => a.CustomerId == request.CustomerId);
                
                // 添加新分配（如果指定了业务员）
                if (!string.IsNullOrEmpty(request.SalespersonId))
                {
                    assignments.Add(new CustomerAssignment
                    {
                        Id = Guid.NewGuid().ToString(),
                        CustomerId = request.CustomerId,
                        SalespersonId = request.SalespersonId,
                        AssignedAt = DateTime.Now
                    });
                }
                
                SaveAssignments(assignments);
                
                return Ok(new { message = "客户分配成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "分配客户失败", error = ex.Message });
            }
        }

        [HttpPost("auto-assign")]
        public IActionResult AutoAssignCustomers()
        {
            try
            {
                var customers = LoadCustomers();
                var salespersons = LoadSalespersons().Where(s => s.Status == "active").ToList();
                var assignments = LoadAssignments();
                
                if (!salespersons.Any())
                {
                    return BadRequest(new { message = "没有可用的业务员" });
                }
                
                // 获取未分配的客户
                var assignedCustomerIds = assignments.Select(a => a.CustomerId).ToHashSet();
                var unassignedCustomers = customers.Where(c => !assignedCustomerIds.Contains(c.Id)).ToList();
                
                if (!unassignedCustomers.Any())
                {
                    return Ok(new { message = "没有需要分配的客户", assigned_count = 0 });
                }
                
                // 平均分配客户
                int assignedCount = 0;
                for (int i = 0; i < unassignedCustomers.Count; i++)
                {
                    var customer = unassignedCustomers[i];
                    var salesperson = salespersons[i % salespersons.Count];
                    
                    assignments.Add(new CustomerAssignment
                    {
                        Id = Guid.NewGuid().ToString(),
                        CustomerId = customer.Id,
                        SalespersonId = salesperson.Id,
                        AssignedAt = DateTime.Now
                    });
                    
                    assignedCount++;
                }
                
                SaveAssignments(assignments);
                
                return Ok(new { message = "自动分配完成", assigned_count = assignedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "自动分配失败", error = ex.Message });
            }
        }

        [HttpGet("performance/{id}")]
        public IActionResult GetSalespersonPerformance(string id)
        {
            try
            {
                var assignments = LoadAssignments();
                var customers = LoadCustomers();
                
                var assignedCustomers = assignments
                    .Where(a => a.SalespersonId == id)
                    .Join(customers, a => a.CustomerId, c => c.Id, (a, c) => new
                    {
                        customer = c,
                        assigned_at = a.AssignedAt
                    })
                    .ToList();
                
                var performance = new
                {
                    customer_count = assignedCustomers.Count,
                    total_commission = CalculateSalespersonCommission(id),
                    customers = assignedCustomers
                };
                
                return Ok(performance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取业绩数据失败", error = ex.Message });
            }
        }

        [HttpPut("customers/{customerId}")]
        public IActionResult UpdateCustomer(string customerId, [FromBody] UpdateCustomerRequest request)
        {
            try
            {
                var customers = LoadCustomers();
                var customer = customers.FirstOrDefault(c => c.Id == customerId);
                
                if (customer == null)
                {
                    return NotFound(new { message = "客户不存在" });
                }
                
                // 更新客户信息
                if (request.Balance.HasValue)
                {
                    customer.Balance = request.Balance.Value;
                }
                
                if (request.CreditScore.HasValue)
                {
                    customer.CreditScore = request.CreditScore.Value;
                }
                
                if (request.VipLevel.HasValue)
                {
                    customer.VipLevel = request.VipLevel.Value;
                }
                
                SaveCustomers(customers);
                
                return Ok(new { message = "客户信息更新成功", customer });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新客户信息失败", error = ex.Message });
            }
        }

        private List<Agent> LoadAgents()
        {
            var filePath = Path.Combine(_dataDirectory, _agentsFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<Agent>();
            }
            
            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<Agent>>(json, options) ?? new List<Agent>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载业务员数据失败: {ex.Message}");
                return new List<Agent>();
            }
        }

        private List<Salesperson> LoadSalespersons()
        {
            var filePath = Path.Combine(_dataDirectory, _agentsFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<Salesperson>();
            }
            
            try
            {
                var json = System.IO.File.ReadAllText(filePath);
                
                // 直接反序列化为Salesperson列表，因为agents.json的结构与Salesperson匹配
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var salespersons = JsonSerializer.Deserialize<List<Salesperson>>(json, options) ?? new List<Salesperson>();
                
                // 确保所有必要字段都有默认值
                foreach (var salesperson in salespersons)
                {
                    if (salesperson.CommissionRate == 0)
                        salesperson.CommissionRate = 5.0; // 默认佣金率5%
                    
                    if (string.IsNullOrEmpty(salesperson.Status))
                        salesperson.Status = "active";
                }
                
                return salespersons;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载业务员数据失败: {ex.Message}");
                Console.WriteLine($"错误详情: {ex.StackTrace}");
                return new List<Salesperson>();
            }
        }

        private void SaveSalespersons(List<Salesperson> salespersons)
        {
            var filePath = Path.Combine(_dataDirectory, _agentsFile);
            var json = JsonSerializer.Serialize(salespersons, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private List<Customer> LoadCustomers()
        {
            var filePath = Path.Combine(_dataDirectory, _usersFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<Customer>();
            }
            
            var json = System.IO.File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<List<Customer>>(json, options) ?? new List<Customer>();
        }

        private void SaveCustomers(List<Customer> customers)
        {
            var filePath = Path.Combine(_dataDirectory, _usersFile);
            var json = JsonSerializer.Serialize(customers, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private List<CustomerAssignment> LoadAssignments()
        {
            var filePath = Path.Combine(_dataDirectory, _assignmentsFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<CustomerAssignment>();
            }
            
            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<CustomerAssignment>>(json) ?? new List<CustomerAssignment>();
        }

        private void SaveAssignments(List<CustomerAssignment> assignments)
        {
            var filePath = Path.Combine(_dataDirectory, _assignmentsFile);
            var json = JsonSerializer.Serialize(assignments, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private double CalculateTotalCommission()
        {
            // 这里应该根据实际的订单和佣金计算
            // 暂时返回模拟数据
            return 12580.50;
        }

        private double CalculateSalespersonCommission(string salespersonId)
        {
            // 这里应该根据该业务员的客户订单计算佣金
            // 暂时返回模拟数据
            return Random.Shared.Next(1000, 5000);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "MobileECommerceSalt"));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string storedPassword)
        {
            // 统一使用加密密码验证
            var hashToVerify = HashPassword(password);
            return hashToVerify == storedPassword;
        }

        private string GenerateRandomInviteCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private bool IsValidInviteCode(string inviteCode)
        {
            return !string.IsNullOrEmpty(inviteCode) && 
                   inviteCode.Length == 6 && 
                   inviteCode.All(char.IsDigit);
        }

        private List<User> LoadUsers()
        {
            try
            {
                // 使用Data目录下的users.json文件
                var filePath = Path.Combine(_dataDirectory, _usersFile);
                if (!System.IO.File.Exists(filePath))
                {
                    return new List<User>();
                }
                
                var json = System.IO.File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载用户数据失败: {ex.Message}");
                return new List<User>();
            }
        }

        private void SaveUsers(List<User> users)
        {
            try
            {
                // 使用Data目录下的users.json文件
                var filePath = Path.Combine(_dataDirectory, _usersFile);
                var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常
                Console.WriteLine($"保存用户数据失败: {ex.Message}");
            }
        }
    }



    public class Salesperson
    {
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("Account")]
        public string Username { get; set; } = string.Empty;
        
        [JsonPropertyName("NickName")]
        public string Nickname { get; set; } = string.Empty;
        
        public string PasswordHash { get; set; } = string.Empty;
        
        [JsonPropertyName("Password")]
        public string PlainPassword { get; set; } = string.Empty;
        
        public string InviteCode { get; set; } = string.Empty;
        public double CommissionRate { get; set; }
        public string Status { get; set; } = "active";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int CustomerCount { get; set; }
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Department { get; set; }
    }

    public class Customer
    {
        [JsonPropertyName("Id")]
        public string Id { get; set; } = string.Empty;
        [JsonPropertyName("Name")]
        public string Username { get; set; } = string.Empty;
        public string Nickname => Username; // 别名，用于前端显示
        [JsonPropertyName("Phone")]
        public string Phone { get; set; } = string.Empty;
        [JsonPropertyName("Email")]
        public string Email { get; set; } = string.Empty;
        [JsonPropertyName("CurrentBalance")]
        public decimal Balance { get; set; }
        public decimal HistoryBalance { get; set; }
        [JsonPropertyName("CreditScore")]
        public int CreditScore { get; set; }
        [JsonPropertyName("VipLevel")]
        public int VipLevel { get; set; }
        [JsonPropertyName("CreatedAt")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("RegisterTime")]
        public DateTime RegisterTime { get; set; }
        [JsonPropertyName("LastLoginTime")]
        public DateTime LastLoginTime { get; set; }
        [JsonPropertyName("IsActive")]
        public bool IsActive { get; set; } = true; // 默认为活跃状态
        public string Status => IsActive ? "active" : "inactive";
        public string? SalespersonId { get; set; }
        public string? SalespersonName { get; set; }
    }

    public class CustomerAssignment
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string SalespersonId { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class CreateSalespersonRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public double CommissionRate { get; set; }
    }

    public class SalespersonLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AssignCustomerRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string SalespersonId { get; set; } = string.Empty;
    }

    public class UpdateSalespersonRequest
    {
        public string? Name { get; set; }
        public string? InviteCode { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Department { get; set; }
        public double? CommissionRate { get; set; }
        public string? Status { get; set; }
        public string? Password { get; set; }
    }

    public class ValidateInviteCodeRequest
    {
        public string InviteCode { get; set; } = string.Empty;
        public string? ExcludeId { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public decimal? Balance { get; set; }
        public int? CreditScore { get; set; }
        public int? VipLevel { get; set; }
    }

    public class TransferCustomersRequest
    {
        public string SourceSalespersonId { get; set; } = string.Empty;
        public string TargetSalespersonId { get; set; } = string.Empty;
    }
}