using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InviteController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _inviteConfigFile = "invite_config.json";
        private readonly string _inviteRecordsFile = "invite_records.json";
        private readonly string _inviteRewardsFile = "invite_rewards.json";
        private readonly string _usersFile = "users.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _transactionsFile = "transactions.json";
        private readonly string _agentsFile = "agents.json";

        public InviteController()
        {
            EnsureDataDirectoryExists();
        }

        [HttpGet("config")]
        public async Task<IActionResult> GetInviteConfig()
        {
            try
            {
                var config = await LoadInviteConfig();
                
                return Ok(new
                {
                    levels = config.Levels.Select(l => new
                    {
                        level = l.Level,
                        name = l.Name,
                        reward = l.Reward,
                        commission = l.Commission,
                        requirement = l.Requirement,
                        description = l.Description
                    }),
                    rules = config.Rules,
                    notice = config.Notice
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取邀请配置失败", error = ex.Message });
            }
        }

        [HttpGet("info/{userId}")]
        public async Task<IActionResult> GetUserInviteInfo(string userId)
        {
            try
            {
                var users = await LoadUsers();
                var user = users.FirstOrDefault(u => u.Id == userId);
                
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }
                
                // 普通用户不再有邀请码，只显示他们使用的邀请码
                var inviteCodeUsed = user.InviteCodeUsed;
                var baseUrl = GetBaseUrl();
                var inviteLink = "";
                
                // 获取邀请统计
                var inviteRecords = await LoadInviteRecords();
                var userInvites = inviteRecords.Where(r => r.InviterId == userId).ToList();
                
                var totalInvites = userInvites.Count;
                var todayInvites = userInvites.Count(r => r.CreatedAt.Date == DateTime.Today);
                var validInvites = userInvites.Count(r => r.IsValid);
                
                // 获取邀请奖励统计
                var inviteRewards = await LoadInviteRewards();
                var userRewards = inviteRewards.Where(r => r.UserId == userId).ToList();
                
                var totalReward = userRewards.Sum(r => r.Amount);
                var todayReward = userRewards.Where(r => r.CreatedAt.Date == DateTime.Today).Sum(r => r.Amount);
                var thisMonthReward = userRewards.Where(r => r.CreatedAt.Month == DateTime.Now.Month && r.CreatedAt.Year == DateTime.Now.Year).Sum(r => r.Amount);
                
                // 获取邀请等级
                var config = await LoadInviteConfig();
                var currentLevel = GetUserInviteLevel(validInvites, config);
                var nextLevel = config.Levels.FirstOrDefault(l => l.Level == currentLevel.Level + 1);
                
                return Ok(new
                {
                    inviteCodeUsed = inviteCodeUsed,
                    inviteLink = inviteLink,
                    qrCodeUrl = "",
                    stats = new
                    {
                        totalInvites = totalInvites,
                        todayInvites = todayInvites,
                        validInvites = validInvites,
                        totalReward = totalReward,
                        todayReward = todayReward,
                        thisMonthReward = thisMonthReward
                    },
                    level = new
                    {
                        current = new
                        {
                            level = currentLevel.Level,
                            name = currentLevel.Name,
                            reward = currentLevel.Reward,
                            commission = currentLevel.Commission
                        },
                        next = nextLevel != null ? new
                        {
                            level = nextLevel.Level,
                            name = nextLevel.Name,
                            reward = nextLevel.Reward,
                            commission = nextLevel.Commission,
                            requirement = nextLevel.Requirement,
                            progress = validInvites,
                            target = nextLevel.Requirement
                        } : null
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取邀请信息失败", error = ex.Message });
            }
        }

        [HttpGet("records/{userId}")]
        public async Task<IActionResult> GetUserInviteRecords(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var inviteRecords = await LoadInviteRecords();
                var userRecords = inviteRecords.Where(r => r.InviterId == userId).OrderByDescending(r => r.CreatedAt).ToList();
                
                var totalCount = userRecords.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var pagedRecords = userRecords
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                // 获取被邀请用户信息
                var users = await LoadUsers();
                var userDict = users.ToDictionary(u => u.Id, u => u);
                
                return Ok(new
                {
                    records = pagedRecords.Select(r => new
                    {
                        id = r.Id,
                        inviteeId = r.InviteeId,
                        inviteeName = userDict.ContainsKey(r.InviteeId) ? userDict[r.InviteeId].Username : "未知用户",
                        inviteePhone = userDict.ContainsKey(r.InviteeId) ? MaskPhone(userDict[r.InviteeId].Phone) : "",
                        isValid = r.IsValid,
                        reward = r.Reward,
                        createdAt = r.CreatedAt,
                        validatedAt = r.ValidatedAt,
                        status = r.IsValid ? "已生效" : "待生效"
                    }),
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
                return StatusCode(500, new { message = "获取邀请记录失败", error = ex.Message });
            }
        }

        [HttpGet("rewards/{userId}")]
        public async Task<IActionResult> GetUserInviteRewards(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var inviteRewards = await LoadInviteRewards();
                var userRewards = inviteRewards.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToList();
                
                var totalCount = userRewards.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var pagedRewards = userRewards
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                return Ok(new
                {
                    rewards = pagedRewards.Select(r => new
                    {
                        id = r.Id,
                        type = r.Type,
                        amount = r.Amount,
                        description = r.Description,
                        relatedId = r.RelatedId,
                        createdAt = r.CreatedAt
                    }),
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
                return StatusCode(500, new { message = "获取邀请奖励失败", error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterWithInvite([FromBody] RegisterWithInviteRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.InviteCode))
                {
                    return BadRequest(new { message = "邀请码不能为空" });
                }
                
                // 现在邀请码只能来自业务员
                var agents = await LoadAgents();
                var inviter = agents.FirstOrDefault(a => a.InviteCode == request.InviteCode && a.IsActive);
                
                if (inviter == null)
                {
                    return BadRequest(new { message = "邀请码无效" });
                }
                
                if (inviter.Id == request.UserId)
                {
                    return BadRequest(new { message = "不能邀请自己" });
                }
                
                // 检查是否已经被邀请过
                var inviteRecords = await LoadInviteRecords();
                if (inviteRecords.Any(r => r.InviteeId == request.UserId))
                {
                    return BadRequest(new { message = "该用户已被邀请过" });
                }
                
                // 创建邀请记录
                var config = await LoadInviteConfig();
                var inviterLevel = GetUserInviteLevel(inviteRecords.Count(r => r.InviterId == inviter.Id && r.IsValid), config);
                
                var inviteRecord = new InviteRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    InviterId = inviter.Id,
                    InviteeId = request.UserId,
                    InviteCode = request.InviteCode,
                    Reward = inviterLevel.Reward,
                    IsValid = false, // 需要被邀请用户完成首次任务才生效
                    CreatedAt = DateTime.Now
                };
                
                inviteRecords.Add(inviteRecord);
                await SaveInviteRecords(inviteRecords);
                
                return Ok(new
                {
                    message = "邀请关系建立成功",
                    inviteId = inviteRecord.Id,
                    reward = inviteRecord.Reward
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "处理邀请失败", error = ex.Message });
            }
        }

        [HttpPost("validate/{inviteId}")]
        public async Task<IActionResult> ValidateInvite(string inviteId)
        {
            try
            {
                var inviteRecords = await LoadInviteRecords();
                var inviteRecord = inviteRecords.FirstOrDefault(r => r.Id == inviteId);
                
                if (inviteRecord == null)
                {
                    return NotFound(new { message = "邀请记录不存在" });
                }
                
                if (inviteRecord.IsValid)
                {
                    return BadRequest(new { message = "邀请已经生效" });
                }
                
                // 标记邀请为有效
                inviteRecord.IsValid = true;
                inviteRecord.ValidatedAt = DateTime.Now;
                
                await SaveInviteRecords(inviteRecords);
                
                // 发放邀请奖励
                await GiveInviteReward(inviteRecord.InviterId, inviteRecord.Reward, "invite_reward", inviteRecord.Id, $"邀请用户奖励：{inviteRecord.Reward:F2}元");
                
                // 检查是否升级邀请等级
                await CheckInviteLevelUpgrade(inviteRecord.InviterId);
                
                return Ok(new
                {
                    message = "邀请生效成功",
                    reward = inviteRecord.Reward
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "验证邀请失败", error = ex.Message });
            }
        }

        [HttpPost("commission")]
        public async Task<IActionResult> GiveCommission([FromBody] GiveCommissionRequest request)
        {
            try
            {
                // 查找邀请关系
                var inviteRecords = await LoadInviteRecords();
                var inviteRecord = inviteRecords.FirstOrDefault(r => r.InviteeId == request.UserId && r.IsValid);
                
                if (inviteRecord == null)
                {
                    return Ok(new { message = "无邀请关系，无需发放佣金" });
                }
                
                // 计算佣金
                var config = await LoadInviteConfig();
                var inviterLevel = GetUserInviteLevel(inviteRecords.Count(r => r.InviterId == inviteRecord.InviterId && r.IsValid), config);
                var commission = request.Amount * inviterLevel.Commission / 100;
                
                if (commission > 0)
                {
                    // 发放佣金
                    await GiveInviteReward(inviteRecord.InviterId, commission, "commission", request.OrderId, $"下级任务佣金：{commission:F2}元");
                }
                
                return Ok(new
                {
                    message = "佣金发放成功",
                    commission = commission,
                    inviterId = inviteRecord.InviterId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "发放佣金失败", error = ex.Message });
            }
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetInviteLeaderboard([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var inviteRecords = await LoadInviteRecords();
                var users = await LoadUsers();
                
                // 统计每个用户的邀请数据
                var inviteStats = inviteRecords
                    .Where(r => r.IsValid)
                    .GroupBy(r => r.InviterId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        InviteCount = g.Count(),
                        TotalReward = g.Sum(r => r.Reward)
                    })
                    .OrderByDescending(s => s.InviteCount)
                    .ToList();
                
                var totalCount = inviteStats.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var pagedStats = inviteStats
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                var userDict = users.ToDictionary(u => u.Id, u => u);
                
                return Ok(new
                {
                    leaderboard = pagedStats.Select((s, index) => new
                    {
                        rank = (page - 1) * pageSize + index + 1,
                        userId = s.UserId,
                        username = userDict.ContainsKey(s.UserId) ? userDict[s.UserId].Username : "未知用户",
                        inviteCount = s.InviteCount,
                        totalReward = s.TotalReward
                    }),
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
                return StatusCode(500, new { message = "获取邀请排行榜失败", error = ex.Message });
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

        private async Task<InviteConfig> LoadInviteConfig()
        {
            var filePath = Path.Combine(_dataDirectory, _inviteConfigFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultConfig = CreateDefaultInviteConfig();
                await SaveInviteConfig(defaultConfig);
                return defaultConfig;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<InviteConfig>(json) ?? CreateDefaultInviteConfig();
        }

        private async Task SaveInviteConfig(InviteConfig config)
        {
            var filePath = Path.Combine(_dataDirectory, _inviteConfigFile);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<InviteRecord>> LoadInviteRecords()
        {
            var filePath = Path.Combine(_dataDirectory, _inviteRecordsFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultRecords = new List<InviteRecord>();
                await SaveInviteRecords(defaultRecords);
                return defaultRecords;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<InviteRecord>>(json) ?? new List<InviteRecord>();
        }

        private async Task SaveInviteRecords(List<InviteRecord> records)
        {
            var filePath = Path.Combine(_dataDirectory, _inviteRecordsFile);
            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<InviteReward>> LoadInviteRewards()
        {
            var filePath = Path.Combine(_dataDirectory, _inviteRewardsFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultRewards = new List<InviteReward>();
                await SaveInviteRewards(defaultRewards);
                return defaultRewards;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<InviteReward>>(json) ?? new List<InviteReward>();
        }

        private async Task SaveInviteRewards(List<InviteReward> rewards)
        {
            var filePath = Path.Combine(_dataDirectory, _inviteRewardsFile);
            var json = JsonSerializer.Serialize(rewards, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<User>> LoadUsers()
        {
            var filePath = Path.Combine(_dataDirectory, _usersFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<User>();
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

        private async Task GiveInviteReward(string userId, decimal amount, string type, string relatedId, string description)
        {
            // 更新用户余额
            await UpdateUserBalance(userId, amount);
            
            // 记录邀请奖励
            var inviteRewards = await LoadInviteRewards();
            var reward = new InviteReward
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = type,
                Amount = amount,
                Description = description,
                RelatedId = relatedId,
                CreatedAt = DateTime.Now
            };
            
            inviteRewards.Add(reward);
            await SaveInviteRewards(inviteRewards);
            
            // 记录交易
            await RecordTransaction(userId, amount, type, relatedId, description);
        }

        private async Task UpdateUserBalance(string userId, decimal amount)
        {
            var balancesFile = Path.Combine(_dataDirectory, _balancesFile);
            var balances = new List<UserBalance>();
            
            if (System.IO.File.Exists(balancesFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(balancesFile);
                balances = JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
            }
            
            var userBalance = balances.FirstOrDefault(b => b.UserId == userId);
            if (userBalance == null)
            {
                userBalance = new UserBalance { UserId = userId, Available = 0, Frozen = 0, Total = 0 };
                balances.Add(userBalance);
            }
            
            userBalance.Available += amount;
            userBalance.Total = userBalance.Available + userBalance.Frozen;
            
            var balanceJson = JsonSerializer.Serialize(balances, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(balancesFile, balanceJson);
        }

        private async Task RecordTransaction(string userId, decimal amount, string type, string relatedId, string description)
        {
            var transactionsFile = Path.Combine(_dataDirectory, _transactionsFile);
            var transactions = new List<Transaction>();
            
            if (System.IO.File.Exists(transactionsFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(transactionsFile);
                transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = type,
                Amount = amount,
                Description = description,
                RelatedId = relatedId,
                CreatedAt = DateTime.Now
            };
            
            transactions.Add(transaction);
            
            var transactionJson = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(transactionsFile, transactionJson);
        }

        private async Task CheckInviteLevelUpgrade(string userId)
        {
            var inviteRecords = await LoadInviteRecords();
            var validInvites = inviteRecords.Count(r => r.InviterId == userId && r.IsValid);
            
            var config = await LoadInviteConfig();
            var currentLevel = GetUserInviteLevel(validInvites, config);
            
            // 检查是否达到下一级别要求
            var nextLevel = config.Levels.FirstOrDefault(l => l.Level == currentLevel.Level + 1);
            if (nextLevel != null && validInvites >= nextLevel.Requirement)
            {
                // 发放升级奖励
                var upgradeReward = nextLevel.Reward - currentLevel.Reward;
                if (upgradeReward > 0)
                {
                    await GiveInviteReward(userId, upgradeReward, "level_upgrade", "", $"邀请等级升级奖励：{upgradeReward:F2}元");
                }
            }
        }

        private InviteLevel GetUserInviteLevel(int validInvites, InviteConfig config)
        {
            return config.Levels
                .Where(l => validInvites >= l.Requirement)
                .OrderByDescending(l => l.Level)
                .FirstOrDefault() ?? config.Levels.First();
        }

        private string GenerateInviteCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string GetBaseUrl()
        {
            var request = HttpContext.Request;
            return $"{request.Scheme}://{request.Host}";
        }

        private string GenerateQRCodeUrl(string content)
        {
            // 使用在线二维码生成服务
            var encodedContent = Uri.EscapeDataString(content);
            return $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={encodedContent}";
        }

        private string MaskPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone) || phone.Length < 7)
                return phone;
            
            return phone.Substring(0, 3) + "****" + phone.Substring(phone.Length - 4);
        }

        private async Task<List<Agent>> LoadAgents()
        {
            var filePath = Path.Combine(_dataDirectory, _agentsFile);
            
            if (!System.IO.File.Exists(filePath))
            {
                return new List<Agent>();
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            
            if (string.IsNullOrEmpty(json))
            {
                return new List<Agent>();
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            
            return JsonSerializer.Deserialize<List<Agent>>(json, options) ?? new List<Agent>();
        }

        private InviteConfig CreateDefaultInviteConfig()
        {
            return new InviteConfig
            {
                Levels = new[]
                {
                    new InviteLevel
                    {
                        Level = 1,
                        Name = "初级推广员",
                        Reward = 10,
                        Commission = 5,
                        Requirement = 0,
                        Description = "邀请1人即可获得10元奖励，下级任务5%佣金"
                    },
                    new InviteLevel
                    {
                        Level = 2,
                        Name = "中级推广员",
                        Reward = 20,
                        Commission = 8,
                        Requirement = 10,
                        Description = "邀请10人可获得20元奖励，下级任务8%佣金"
                    },
                    new InviteLevel
                    {
                        Level = 3,
                        Name = "高级推广员",
                        Reward = 50,
                        Commission = 12,
                        Requirement = 50,
                        Description = "邀请50人可获得50元奖励，下级任务12%佣金"
                    },
                    new InviteLevel
                    {
                        Level = 4,
                        Name = "金牌推广员",
                        Reward = 100,
                        Commission = 15,
                        Requirement = 100,
                        Description = "邀请100人可获得100元奖励，下级任务15%佣金"
                    }
                },
                Rules = new[]
                {
                    "1. 邀请好友注册并完成首次任务即可获得奖励",
                    "2. 被邀请用户每完成一个任务，邀请人可获得相应佣金",
                    "3. 邀请等级根据有效邀请人数自动升级",
                    "4. 邀请奖励和佣金实时到账",
                    "5. 严禁刷单、作弊等违规行为"
                },
                Notice = "邀请好友一起赚钱，共享收益！邀请越多，奖励越丰厚！"
            };
        }
    }
}