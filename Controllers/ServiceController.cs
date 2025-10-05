using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _agentsFile = "agents.json";
        private readonly string _messagesFile = "messages.json";
        private readonly string _userAgentMappingFile = "user_agent_mapping.json";
        private readonly string _usersFile = "users.json";

        public ServiceController()
        {
            EnsureDataDirectoryExists();
        }

        [HttpGet("agent/{userId}")]
        public async Task<IActionResult> GetUserAgent(string userId)
        {
            try
            {
                var mapping = await LoadUserAgentMapping();
                var userMapping = mapping.FirstOrDefault(m => m.UserId == userId);
                
                if (userMapping == null)
                {
                    // 为用户分配一个业务员
                    var agents = await LoadAgents();
                    var availableAgent = agents.OrderBy(a => a.ClientCount).FirstOrDefault();
                    
                    if (availableAgent == null)
                    {
                        // 创建默认业务员
                        availableAgent = CreateDefaultAgent();
                        agents.Add(availableAgent);
                        await SaveAgents(agents);
                    }
                    
                    userMapping = new UserAgentMapping
                    {
                        UserId = userId,
                        AgentId = availableAgent.Id,
                        AssignedAt = DateTime.Now
                    };
                    
                    mapping.Add(userMapping);
                    
                    // 增加业务员客户数
                    availableAgent.ClientCount++;
                    await SaveAgents(agents);
                    await SaveUserAgentMapping(mapping);
                }
                
                var agents2 = await LoadAgents();
                var agent = agents2.FirstOrDefault(a => a.Id == userMapping.AgentId);
                
                if (agent == null)
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                return Ok(new
                {
                    id = agent.Id,
                    name = agent.Name,
                    title = agent.Title,
                    avatar = agent.Avatar,
                    workTime = agent.WorkTime,
                    rating = agent.Rating,
                    responseTime = agent.ResponseTime,
                    status = agent.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取业务员信息失败", error = ex.Message });
            }
        }

        [HttpGet("messages/{userId}")]
        public async Task<IActionResult> GetUserMessages(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var messages = await LoadMessages();
                var userMessages = messages.Where(m => m.UserId == userId).OrderBy(m => m.Timestamp).ToList();
                
                var totalCount = userMessages.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var pagedMessages = userMessages
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                return Ok(new
                {
                    messages = pagedMessages,
                    pagination = new
                    {
                        currentPage = page,
                        totalPages,
                        totalCount,
                        pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取消息失败", error = ex.Message });
            }
        }

        [HttpGet("agent/{agentId}/messages")]
        public async Task<IActionResult> GetAgentMessages(string agentId)
        {
            try
            {
                var messages = await LoadMessages();
                var agentMessages = messages.Where(m => m.AgentId == agentId).OrderByDescending(m => m.Timestamp).ToList();
                
                return Ok(new { messages = agentMessages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取业务员消息失败", error = ex.Message });
            }
        }

        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Content?.Trim()))
                {
                    return BadRequest(new { message = "消息内容不能为空" });
                }
                
                var messages = await LoadMessages();
                
                // 创建用户消息
                var userMessage = new ServiceMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Sender = "user",
                    Content = request.Content.Trim(),
                    Timestamp = DateTime.Now,
                    Status = "sent"
                };
                
                messages.Add(userMessage);
                
                // 生成自动回复
                var reply = GenerateAutoReply(request.Content);
                var agentMessage = new ServiceMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Sender = "agent",
                    Content = reply,
                    Timestamp = DateTime.Now.AddSeconds(1),
                    Status = "sent"
                };
                
                messages.Add(agentMessage);
                
                await SaveMessages(messages);
                
                return Ok(new
                {
                    message = "消息发送成功",
                    reply = reply
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "发送消息失败", error = ex.Message });
            }
        }

        [HttpPost("agent/reply")]
        public async Task<IActionResult> SendAgentReply([FromBody] AgentReplyRequest request)
        {
            try
            {
                var messages = await LoadMessages();
                
                // 添加业务员回复消息
                var agentReply = new ServiceMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    Sender = "agent",
                    Content = request.Content,
                    Timestamp = DateTime.Now,
                    Status = "sent"
                };
                
                messages.Add(agentReply);
                await SaveMessages(messages);
                
                return Ok(new { message = "回复发送成功", reply = agentReply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "发送回复失败", error = ex.Message });
            }
        }

        [HttpPut("messages/{messageId}/read")]
        public async Task<IActionResult> MarkMessageAsRead(string messageId)
        {
            try
            {
                var messages = await LoadMessages();
                var message = messages.FirstOrDefault(m => m.Id == messageId);
                
                if (message == null)
                {
                    return NotFound(new { message = "消息不存在" });
                }
                
                await SaveMessages(messages);
                
                return Ok(new { message = "消息已标记为已读" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "标记消息失败", error = ex.Message });
            }
        }

        [HttpGet("agents")]
        public async Task<IActionResult> GetAllAgents([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var agents = await LoadAgents();
                
                var totalCount = agents.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var pagedAgents = agents
                    .OrderBy(a => a.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                return Ok(new
                {
                    agents = pagedAgents.Select(a => new
                    {
                        id = a.Id,
                        name = a.Name,
                        title = a.Title,
                        avatar = a.Avatar,
                        workTime = a.WorkTime,
                        rating = a.Rating,
                        responseTime = a.ResponseTime,
                        status = a.Status,
                        clientCount = a.ClientCount,
                        createdAt = a.CreatedAt
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
                return StatusCode(500, new { message = "获取业务员列表失败", error = ex.Message });
            }
        }

        [HttpPost("agents")]
        public async Task<IActionResult> CreateAgent([FromBody] CreateServiceAgentRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Name?.Trim()))
                {
                    return BadRequest(new { message = "业务员姓名不能为空" });
                }
                
                var agents = await LoadAgents();
                
                // 检查姓名是否已存在
                if (agents.Any(a => a.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new { message = "业务员姓名已存在" });
                }
                
                var agent = new ServiceAgent
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = request.Name.Trim(),
                    Title = request.Title?.Trim() ?? "客服专员",
                    Avatar = request.Avatar?.Trim() ?? "",
                    WorkTime = request.WorkTime?.Trim() ?? "9:00-21:00",
                    Rating = 5.0,
                    ResponseTime = "平均2分钟",
                    Status = "online",
                    ClientCount = 0,
                    CreatedAt = DateTime.Now
                };
                
                agents.Add(agent);
                await SaveAgents(agents);
                
                return Ok(new
                {
                    message = "业务员创建成功",
                    agent = new
                    {
                        id = agent.Id,
                        name = agent.Name,
                        title = agent.Title,
                        avatar = agent.Avatar,
                        workTime = agent.WorkTime,
                        rating = agent.Rating,
                        responseTime = agent.ResponseTime,
                        status = agent.Status,
                        clientCount = agent.ClientCount,
                        createdAt = agent.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建业务员失败", error = ex.Message });
            }
        }

        [HttpPut("agents/{agentId}")]
        public async Task<IActionResult> UpdateAgent(string agentId, [FromBody] UpdateAgentRequest request)
        {
            try
            {
                var agents = await LoadAgents();
                var agent = agents.FirstOrDefault(a => a.Id == agentId);
                
                if (agent == null)
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                if (!string.IsNullOrEmpty(request.Name?.Trim()))
                {
                    agent.Name = request.Name.Trim();
                }
                
                if (!string.IsNullOrEmpty(request.Title?.Trim()))
                {
                    agent.Title = request.Title.Trim();
                }
                
                if (!string.IsNullOrEmpty(request.Avatar?.Trim()))
                {
                    agent.Avatar = request.Avatar.Trim();
                }
                
                if (!string.IsNullOrEmpty(request.WorkTime?.Trim()))
                {
                    agent.WorkTime = request.WorkTime.Trim();
                }
                
                if (!string.IsNullOrEmpty(request.Status?.Trim()))
                {
                    agent.Status = request.Status.Trim();
                }
                
                await SaveAgents(agents);
                
                return Ok(new
                {
                    message = "业务员信息更新成功",
                    agent = new
                    {
                        id = agent.Id,
                        name = agent.Name,
                        title = agent.Title,
                        avatar = agent.Avatar,
                        workTime = agent.WorkTime,
                        rating = agent.Rating,
                        responseTime = agent.ResponseTime,
                        status = agent.Status,
                        clientCount = agent.ClientCount,
                        createdAt = agent.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "更新业务员信息失败", error = ex.Message });
            }
        }

        [HttpDelete("agents/{agentId}")]
        public async Task<IActionResult> DeleteAgent(string agentId)
        {
            try
            {
                var agents = await LoadAgents();
                var agent = agents.FirstOrDefault(a => a.Id == agentId);
                
                if (agent == null)
                {
                    return NotFound(new { message = "业务员不存在" });
                }
                
                // 检查是否有客户分配给该业务员
                var mapping = await LoadUserAgentMapping();
                var hasClients = mapping.Any(m => m.AgentId == agentId);
                
                if (hasClients)
                {
                    return BadRequest(new { message = "该业务员还有客户，无法删除" });
                }
                
                agents.Remove(agent);
                await SaveAgents(agents);
                
                return Ok(new { message = "业务员删除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除业务员失败", error = ex.Message });
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

        private async Task<List<ServiceAgent>> LoadAgents()
        {
            var filePath = Path.Combine(_dataDirectory, _agentsFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultAgents = CreateDefaultAgents();
                await SaveAgents(defaultAgents);
                return defaultAgents;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<ServiceAgent>>(json) ?? new List<ServiceAgent>();
        }

        private async Task SaveAgents(List<ServiceAgent> agents)
        {
            var filePath = Path.Combine(_dataDirectory, _agentsFile);
            var json = JsonSerializer.Serialize(agents, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<ServiceMessage>> LoadMessages()
        {
            var filePath = Path.Combine(_dataDirectory, _messagesFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultMessages = new List<ServiceMessage>();
                await SaveMessages(defaultMessages);
                return defaultMessages;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<ServiceMessage>>(json) ?? new List<ServiceMessage>();
        }

        private async Task SaveMessages(List<ServiceMessage> messages)
        {
            var filePath = Path.Combine(_dataDirectory, _messagesFile);
            var json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<UserAgentMapping>> LoadUserAgentMapping()
        {
            var filePath = Path.Combine(_dataDirectory, _userAgentMappingFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultMapping = new List<UserAgentMapping>();
                await SaveUserAgentMapping(defaultMapping);
                return defaultMapping;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserAgentMapping>>(json) ?? new List<UserAgentMapping>();
        }

        private async Task SaveUserAgentMapping(List<UserAgentMapping> mapping)
        {
            var filePath = Path.Combine(_dataDirectory, _userAgentMappingFile);
            var json = JsonSerializer.Serialize(mapping, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private ServiceAgent CreateDefaultAgent()
        {
            var names = new[] { "李小美", "王小雅", "张小慧", "刘小婷", "陈小芳" };
            var titles = new[] { "高级客服专员", "客服专员", "资深客服顾问", "客服经理" };
            
            var random = new Random();
            
            return new ServiceAgent
            {
                Id = Guid.NewGuid().ToString(),
                Name = names[random.Next(names.Length)],
                Title = titles[random.Next(titles.Length)],
                Avatar = "",
                WorkTime = "9:00-21:00",
                Rating = 4.5 + random.NextDouble() * 0.5, // 4.5-5.0
                ResponseTime = "平均2分钟",
                Status = "online",
                ClientCount = 0,
                CreatedAt = DateTime.Now
            };
        }

        private List<ServiceAgent> CreateDefaultAgents()
        {
            return new List<ServiceAgent>
            {
                new ServiceAgent
                {
                    Id = "agent001",
                    Name = "李小美",
                    Title = "高级客服专员",
                    Avatar = "",
                    WorkTime = "9:00-21:00",
                    Rating = 5.0,
                    ResponseTime = "平均2分钟",
                    Status = "online",
                    ClientCount = 0,
                    CreatedAt = DateTime.Now
                },
                new ServiceAgent
                {
                    Id = "agent002",
                    Name = "王小雅",
                    Title = "客服专员",
                    Avatar = "",
                    WorkTime = "9:00-21:00",
                    Rating = 4.8,
                    ResponseTime = "平均3分钟",
                    Status = "online",
                    ClientCount = 0,
                    CreatedAt = DateTime.Now
                }
            };
        }

        private string GenerateAutoReply(string userMessage)
        {
            var replies = new Dictionary<string, string>
            {
                { "充值", "您可以通过以下方式充值：\n1. 银行卡转账\n2. 支付宝转账\n3. 微信转账\n\n具体操作请点击首页的\"充值\"按钮。" },
                { "提现", "提现流程：\n1. 进入\"我的\"页面\n2. 点击\"提现\"\n3. 填写提现金额和银行卡信息\n4. 提交申请\n\n提现将在1-3个工作日内到账。" },
                { "任务", "任务规则说明：\n1. 每日任务数量根据VIP等级确定\n2. 完成任务后获得相应奖励\n3. 任务必须在规定时间内完成\n4. 恶意刷单将被封号处理" },
                { "VIP", "VIP会员享有以下权益：\n1. 更高的任务奖励加成\n2. 更多的每日任务数量\n3. 更低的提现手续费\n4. 专属客服支持\n\n详情请查看VIP升级页面。" },
                { "人工", "正在为您转接人工客服，请稍候...\n\n工作时间：9:00-21:00\n如非工作时间，请留言，我们会尽快回复。" }
            };

            // 检查是否有匹配的关键词
            foreach (var keyword in replies.Keys)
            {
                if (userMessage.Contains(keyword))
                {
                    return replies[keyword];
                }
            }

            // 默认回复
            var defaultReplies = new[]
            {
                "感谢您的咨询，我已收到您的问题，正在为您查询相关信息...",
                "您好，我是您的专属客服，很高兴为您服务！请问有什么可以帮助您的吗？",
                "我已记录您的问题，稍后会有专业客服为您详细解答。",
                "如需紧急处理，请拨打客服热线：400-888-8888"
            };

            var random = new Random();
            return defaultReplies[random.Next(defaultReplies.Length)];
        }
    }

    // 数据模型
    public class ServiceAgent
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string WorkTime { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string ResponseTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ClientCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ServiceMessage
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty; // user, agent
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty; // sent, received, read
    }

    public class UserAgentMapping
    {
        public string UserId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    public class SendMessageRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AgentReplyRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class CreateServiceAgentRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string WorkTime { get; set; } = string.Empty;
    }

    public class UpdateAgentRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string WorkTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}