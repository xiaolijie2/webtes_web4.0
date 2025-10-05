using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VipController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _usersFile = "users.json";
        private readonly string _vipConfigFile = "vip_config.json";
        private readonly string _vipOrdersFile = "vip_orders.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _transactionsFile = "transactions.json";

        public VipController()
        {
            EnsureDataDirectoryExists();
        }

        [HttpGet("info/{userId}")]
        public async Task<IActionResult> GetVipInfo(string userId)
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
                        Name = "用户" + userId.Substring(0, Math.Min(4, userId.Length)),
                        Phone = "",
                        Email = "",
                        VipLevel = 1,
                        VipExpireAt = DateTime.Now.AddDays(30),
                        CreatedAt = DateTime.Now
                    };
                    
                    users.Add(user);
                    await SaveUsers(users);
                }
                
                var vipConfig = await LoadVipConfig();
                var currentVipConfig = vipConfig.FirstOrDefault(v => v.Level == user.VipLevel) ?? vipConfig.First();
                
                var vipInfo = new
                {
                    level = user.VipLevel,
                    expireAt = user.VipExpireAt,
                    taskBonus = currentVipConfig.TaskBonus,
                    withdrawFee = currentVipConfig.WithdrawFee,
                    dailyTaskLimit = currentVipConfig.DailyTaskLimit,
                    prioritySupport = currentVipConfig.PrioritySupport,
                    benefits = currentVipConfig.Benefits
                };
                
                return Ok(vipInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取VIP信息失败", error = ex.Message });
            }
        }

        [HttpGet("config")]
        public async Task<IActionResult> GetVipConfig()
        {
            try
            {
                var vipConfig = await LoadVipConfig();
                return Ok(new { levels = vipConfig });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取VIP配置失败", error = ex.Message });
            }
        }

        [HttpPost("upgrade")]
        public async Task<IActionResult> UpgradeVip([FromBody] VipUpgradeRequest request)
        {
            try
            {
                var users = await LoadUsers();
                var user = users.FirstOrDefault(u => u.Id == request.UserId);
                
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }
                
                var vipConfig = await LoadVipConfig();
                var targetVipConfig = vipConfig.FirstOrDefault(v => v.Level == request.TargetLevel);
                
                if (targetVipConfig == null)
                {
                    return BadRequest(new { message = "目标VIP等级不存在" });
                }
                
                if (request.TargetLevel <= user.VipLevel)
                {
                    return BadRequest(new { message = "只能升级到更高等级" });
                }
                
                // 检查用户余额
                var balances = await LoadBalances();
                var userBalance = balances.FirstOrDefault(b => b.UserId == request.UserId);
                
                if (userBalance == null || userBalance.Available < targetVipConfig.Price)
                {
                    return BadRequest(new { message = "余额不足" });
                }
                
                // 扣除费用
                userBalance.Available -= targetVipConfig.Price;
                userBalance.Total = userBalance.Available + userBalance.Frozen;
                
                // 升级VIP
                user.VipLevel = request.TargetLevel;
                user.VipExpireAt = DateTime.Now.AddDays(targetVipConfig.Duration);
                
                // 创建VIP订单记录
                var vipOrders = await LoadVipOrders();
                var vipOrder = new VipOrder
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    FromLevel = user.VipLevel,
                    ToLevel = request.TargetLevel,
                    Price = targetVipConfig.Price,
                    Duration = targetVipConfig.Duration,
                    Status = "completed",
                    CreatedAt = DateTime.Now
                };
                
                vipOrders.Add(vipOrder);
                
                // 记录交易
                var transactions = await LoadTransactions();
                var transaction = new Transaction
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Type = "vip_upgrade",
                    Amount = -targetVipConfig.Price,
                    Balance = userBalance.Available,
                    Description = $"VIP升级到等级{request.TargetLevel}",
                    Status = "completed",
                    CreatedAt = DateTime.Now
                };
                
                transactions.Add(transaction);
                
                // 保存数据
                await SaveUsers(users);
                await SaveBalances(balances);
                await SaveVipOrders(vipOrders);
                await SaveTransactions(transactions);
                
                return Ok(new { 
                    message = "VIP升级成功",
                    newLevel = request.TargetLevel,
                    expireAt = user.VipExpireAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "VIP升级失败", error = ex.Message });
            }
        }

        [HttpGet("orders/{userId}")]
        public async Task<IActionResult> GetVipOrders(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var vipOrders = await LoadVipOrders();
                var userOrders = vipOrders
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();
                
                var totalCount = userOrders.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var pagedOrders = userOrders
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                return Ok(new
                {
                    orders = pagedOrders,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取VIP订单失败", error = ex.Message });
            }
        }

        // 辅助方法
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
                var defaultUsers = new List<User>();
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

        private async Task<List<VipConfig>> LoadVipConfig()
        {
            var filePath = Path.Combine(_dataDirectory, _vipConfigFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultConfig = CreateDefaultVipConfig();
                await SaveVipConfig(defaultConfig);
                return defaultConfig;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<VipConfig>>(json) ?? new List<VipConfig>();
        }

        private async Task SaveVipConfig(List<VipConfig> config)
        {
            var filePath = Path.Combine(_dataDirectory, _vipConfigFile);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<VipOrder>> LoadVipOrders()
        {
            var filePath = Path.Combine(_dataDirectory, _vipOrdersFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultOrders = new List<VipOrder>();
                await SaveVipOrders(defaultOrders);
                return defaultOrders;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<VipOrder>>(json) ?? new List<VipOrder>();
        }

        private async Task SaveVipOrders(List<VipOrder> orders)
        {
            var filePath = Path.Combine(_dataDirectory, _vipOrdersFile);
            var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<UserBalance>> LoadBalances()
        {
            var filePath = Path.Combine(_dataDirectory, _balancesFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<UserBalance>();
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

        private async Task<List<Transaction>> LoadTransactions()
        {
            var filePath = Path.Combine(_dataDirectory, _transactionsFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<Transaction>();
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

        private List<VipConfig> CreateDefaultVipConfig()
        {
            return new List<VipConfig>
            {
                new VipConfig
                {
                    Level = 1,
                    Name = "青铜会员",
                    Price = 0,
                    Duration = 30,
                    TaskBonus = 20,
                    WithdrawFee = 0.5m,
                    DailyTaskLimit = 10,
                    PrioritySupport = true,
                    Benefits = new List<string>
                    {
                        "任务奖励+20%",
                        "每日10个任务",
                        "提现手续费0.5%",
                        "优先客服支持"
                    }
                },
                new VipConfig
                {
                    Level = 2,
                    Name = "白银会员",
                    Price = 99,
                    Duration = 30,
                    TaskBonus = 30,
                    WithdrawFee = 0.3m,
                    DailyTaskLimit = 15,
                    PrioritySupport = true,
                    Benefits = new List<string>
                    {
                        "任务奖励+30%",
                        "每日15个任务",
                        "提现手续费0.3%",
                        "优先客服支持",
                        "专属任务池"
                    }
                },
                new VipConfig
                {
                    Level = 3,
                    Name = "黄金会员",
                    Price = 299,
                    Duration = 30,
                    TaskBonus = 50,
                    WithdrawFee = 0.2m,
                    DailyTaskLimit = 20,
                    PrioritySupport = true,
                    Benefits = new List<string>
                    {
                        "任务奖励+50%",
                        "每日20个任务",
                        "提现手续费0.2%",
                        "专属客服经理",
                        "高价值任务优先",
                        "每月额外奖励"
                    }
                },
                new VipConfig
                {
                    Level = 4,
                    Name = "铂金会员",
                    Price = 599,
                    Duration = 30,
                    TaskBonus = 80,
                    WithdrawFee = 0.1m,
                    DailyTaskLimit = 30,
                    PrioritySupport = true,
                    Benefits = new List<string>
                    {
                        "任务奖励+80%",
                        "每日30个任务",
                        "提现手续费0.1%",
                        "专属客服经理",
                        "独家高价值任务",
                        "每月丰厚奖励",
                        "生日特别礼品"
                    }
                },
                new VipConfig
                {
                    Level = 5,
                    Name = "钻石会员",
                    Price = 1299,
                    Duration = 30,
                    TaskBonus = 100,
                    WithdrawFee = 0,
                    DailyTaskLimit = 50,
                    PrioritySupport = true,
                    Benefits = new List<string>
                    {
                        "任务奖励+100%",
                        "每日50个任务",
                        "免费提现",
                        "专属客服团队",
                        "顶级独家任务",
                        "每月超值奖励",
                        "专属活动邀请",
                        "年度豪华礼品"
                    }
                }
            };
        }
    }
}