using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _usersFile = "users.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _statsFile = "user_stats.json";
        private readonly string _transactionsFile = "transactions.json";

        public AccountController()
        {
            EnsureDataDirectoryExists();
        }

        // 获取用户信息
        [HttpGet("info/{userId}")]
        public async Task<IActionResult> GetUserInfo(string userId)
        {
            try
            {
                var users = await LoadUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    // 创建默认用户
                    user = new User
                    {
                        Id = userId,
                        Name = "任务用户",
                        Phone = "",
                        Email = "",
                        Avatar = "",
                        VipLevel = 1,
                        PhoneVerified = false,
                        BankCardCount = 0,
                        RegisterTime = DateTime.Now,
                        LastLoginTime = DateTime.Now
                    };
                    
                    users.Add(user);
                    await SaveUsers(users);
                }

                return Ok(new
                {
                    id = user.Id,
                    name = user.Name,
                    phone = user.Phone,
                    email = user.Email,
                    avatar = user.Avatar,
                    vipLevel = user.VipLevel,
                    phoneVerified = user.PhoneVerified,
                    bankCardCount = user.BankCardCount,
                    registerTime = user.RegisterTime,
                    lastLoginTime = user.LastLoginTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取用户信息失败", error = ex.Message });
            }
        }

        // 获取用户余额
        [HttpGet("balance/{userId}")]
        public async Task<IActionResult> GetUserBalance(string userId)
        {
            try
            {
                var balances = await LoadBalances();
                var balance = balances.FirstOrDefault(b => b.UserId == userId);
                
                if (balance == null)
                {
                    // 创建默认余额
                    balance = new UserBalance
                    {
                        UserId = userId,
                        Available = 168.50m,
                        Frozen = 0m,
                        Total = 168.50m,
                        UpdateTime = DateTime.Now
                    };
                    
                    balances.Add(balance);
                    await SaveBalances(balances);
                }

                return Ok(new
                {
                    available = balance.Available,
                    frozen = balance.Frozen,
                    total = balance.Total,
                    updateTime = balance.UpdateTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取余额失败", error = ex.Message });
            }
        }

        // 获取用户统计
        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetUserStats(string userId)
        {
            try
            {
                var stats = await LoadUserStats();
                var userStat = stats.FirstOrDefault(s => s.UserId == userId);
                
                if (userStat == null)
                {
                    // 创建默认统计
                    userStat = new UserStats
                    {
                        UserId = userId,
                        TotalTasks = 15,
                        TotalEarnings = 320.00m,
                        InviteCount = 3,
                        LoginDays = 7,
                        LastUpdateTime = DateTime.Now
                    };
                    
                    stats.Add(userStat);
                    await SaveUserStats(stats);
                }

                return Ok(new
                {
                    totalTasks = userStat.TotalTasks,
                    totalEarnings = userStat.TotalEarnings,
                    inviteCount = userStat.InviteCount,
                    loginDays = userStat.LoginDays,
                    lastUpdateTime = userStat.LastUpdateTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取统计失败", error = ex.Message });
            }
        }

        // 获取交易记录
        [HttpGet("transactions/{userId}")]
        public async Task<IActionResult> GetTransactions(string userId, int page = 1, int limit = 20, string? type = null)
        {
            try
            {
                var transactions = await LoadTransactions();
                var userTransactions = transactions.Where(t => t.UserId == userId);
                
                if (!string.IsNullOrEmpty(type))
                {
                    userTransactions = userTransactions.Where(t => t.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
                }
                
                var totalCount = userTransactions.Count();
                var pagedTransactions = userTransactions
                    .OrderByDescending(t => t.CreateTime)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToList();

                return Ok(new
                {
                    transactions = pagedTransactions.Select(t => new
                    {
                        id = t.Id,
                        type = t.Type,
                        amount = t.Amount,
                        description = t.Description,
                        status = t.Status,
                        createTime = t.CreateTime
                    }),
                    pagination = new
                    {
                        page,
                        limit,
                        total = totalCount,
                        pages = (int)Math.Ceiling((double)totalCount / limit)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取交易记录失败", error = ex.Message });
            }
        }

        // 更新用户信息
        [HttpPost("update-info/{userId}")]
        public async Task<IActionResult> UpdateUserInfo(string userId, [FromBody] UpdateUserInfoRequest request)
        {
            try
            {
                var users = await LoadUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                // 更新用户信息
                if (!string.IsNullOrEmpty(request.Name))
                    user.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Phone))
                    user.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Email))
                    user.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Avatar))
                    user.Avatar = request.Avatar;

                await SaveUsers(users);

                return Ok(new { message = "用户信息更新成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新用户信息失败", error = ex.Message });
            }
        }

        // 更新余额
        [HttpPost("update-balance/{userId}")]
        public async Task<IActionResult> UpdateBalance(string userId, [FromBody] UpdateBalanceRequest request)
        {
            try
            {
                var balances = await LoadBalances();
                var balance = balances.FirstOrDefault(b => b.UserId == userId);
                
                if (balance == null)
                {
                    balance = new UserBalance
                    {
                        UserId = userId,
                        Available = 0,
                        Frozen = 0,
                        Total = 0,
                        UpdateTime = DateTime.Now
                    };
                    balances.Add(balance);
                }

                // 更新余额
                balance.Available += request.AvailableChange;
                balance.Frozen += request.FrozenChange;
                balance.Total = balance.Available + balance.Frozen;
                balance.UpdateTime = DateTime.Now;

                await SaveBalances(balances);

                // 记录交易
                await RecordTransaction(userId, request.Type, request.AvailableChange, request.Description);

                return Ok(new
                {
                    available = balance.Available,
                    frozen = balance.Frozen,
                    total = balance.Total
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新余额失败", error = ex.Message });
            }
        }

        // 记录交易
        private async Task RecordTransaction(string userId, string type, decimal amount, string description)
        {
            try
            {
                var transactions = await LoadTransactions();
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Type = type,
                    Amount = amount,
                    Description = description,
                    Status = "completed",
                    CreateTime = DateTime.Now
                };
                
                transactions.Add(transaction);
                await SaveTransactions(transactions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"记录交易失败: {ex.Message}");
            }
        }

        // 数据加载和保存方法
        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        private async Task<List<User>> LoadUsers()
        {
            var filePath = Path.Combine(_dataDirectory, _usersFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultUsers = CreateDefaultUsers();
                await SaveUsers(defaultUsers);
                return defaultUsers;
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
        }

        private async Task SaveUsers(List<User> users)
        {
            var filePath = Path.Combine(_dataDirectory, _usersFile);
            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<UserBalance>> LoadBalances()
        {
            var filePath = Path.Combine(_dataDirectory, _balancesFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultBalances = CreateDefaultBalances();
                await SaveBalances(defaultBalances);
                return defaultBalances;
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
        }

        private async Task SaveBalances(List<UserBalance> balances)
        {
            var filePath = Path.Combine(_dataDirectory, _balancesFile);
            var json = JsonSerializer.Serialize(balances, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<UserStats>> LoadUserStats()
        {
            var filePath = Path.Combine(_dataDirectory, _statsFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultStats = CreateDefaultStats();
                await SaveUserStats(defaultStats);
                return defaultStats;
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserStats>>(json) ?? new List<UserStats>();
        }

        private async Task SaveUserStats(List<UserStats> stats)
        {
            var filePath = Path.Combine(_dataDirectory, _statsFile);
            var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<Transaction>> LoadTransactions()
        {
            var filePath = Path.Combine(_dataDirectory, _transactionsFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultTransactions = CreateDefaultTransactions();
                await SaveTransactions(defaultTransactions);
                return defaultTransactions;
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
        }

        private async Task SaveTransactions(List<Transaction> transactions)
        {
            var filePath = Path.Combine(_dataDirectory, _transactionsFile);
            var json = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        // 创建默认数据
        private List<User> CreateDefaultUsers()
        {
            return new List<User>
            {
                new User
                {
                    Id = "user123",
                    Name = "任务用户",
                    Phone = "138****8888",
                    Email = "user@example.com",
                    Avatar = "",
                    VipLevel = 1,
                    PhoneVerified = true,
                    BankCardCount = 1,
                    RegisterTime = DateTime.Now.AddDays(-30),
                    LastLoginTime = DateTime.Now
                }
            };
        }

        private List<UserBalance> CreateDefaultBalances()
        {
            return new List<UserBalance>
            {
                new UserBalance
                {
                    UserId = "user123",
                    Available = 168.50m,
                    Frozen = 0m,
                    Total = 168.50m,
                    UpdateTime = DateTime.Now
                }
            };
        }

        private List<UserStats> CreateDefaultStats()
        {
            return new List<UserStats>
            {
                new UserStats
                {
                    UserId = "user123",
                    TotalTasks = 15,
                    TotalEarnings = 320.00m,
                    InviteCount = 3,
                    LoginDays = 7,
                    LastUpdateTime = DateTime.Now
                }
            };
        }

        private List<Transaction> CreateDefaultTransactions()
        {
            return new List<Transaction>
            {
                new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = "user123",
                    Type = "recharge",
                    Amount = 100.00m,
                    Description = "账户充值",
                    Status = "completed",
                    CreateTime = DateTime.Now.AddDays(-2)
                },
                new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = "user123",
                    Type = "task_reward",
                    Amount = 68.50m,
                    Description = "任务奖励",
                    Status = "completed",
                    CreateTime = DateTime.Now.AddDays(-1)
                }
            };
        }
    }

    // 数据模型

    // 请求模型
    public class UpdateUserInfoRequest
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Avatar { get; set; }
    }

    public class UpdateBalanceRequest
    {
        public decimal AvailableChange { get; set; }
        public decimal FrozenChange { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}