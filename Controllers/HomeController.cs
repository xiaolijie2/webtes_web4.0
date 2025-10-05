using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MobileECommerceAPI.Models;
using MobileECommerceAPI.Services;
using System.Linq;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api")]
    public class HomeController : ControllerBase
    {
        private readonly string _dataPath = "Data";
        private readonly JwtService _jwtService;

        public HomeController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        // 获取幻灯片数据
        [HttpGet("slideshow")]
        public async Task<IActionResult> GetSlideshow()
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "slideshow.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    // 返回默认幻灯片数据
                    var defaultSlides = new[]
                    {
                        new {
                            id = 1,
                            title = "欢迎来到移动电商平台",
                            description = "开始您的购物之旅",
                            image_url = "https://trae-api-sg.mchost.guru/api/ide/v1/text_to_image?prompt=modern%20e-commerce%20mobile%20app%20banner%20with%20shopping%20cart%20and%20products%20blue%20gradient%20background&image_size=landscape_16_9",
                            link_url = "/start.html",
                            sort_order = 1,
                            is_active = true
                        },
                        new {
                            id = 2,
                            title = "新用户专享优惠",
                            description = "注册即送100元优惠券",
                            image_url = "https://trae-api-sg.mchost.guru/api/ide/v1/text_to_image?prompt=promotional%20banner%20with%20gift%20box%20and%20discount%20coupon%20orange%20gradient%20background&image_size=landscape_16_9",
                            link_url = "/register.html",
                            sort_order = 2,
                            is_active = true
                        },
                        new {
                            id = 3,
                            title = "邀请好友获奖励",
                            description = "每邀请一位好友获得50元奖励",
                            image_url = "https://trae-api-sg.mchost.guru/api/ide/v1/text_to_image?prompt=referral%20program%20banner%20with%20people%20icons%20and%20reward%20symbols%20green%20gradient%20background&image_size=landscape_16_9",
                            link_url = "/invite.html",
                            sort_order = 3,
                            is_active = true
                        }
                    };
                    
                    await System.IO.File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(defaultSlides, new JsonSerializerOptions { WriteIndented = true }));
                    return Ok(defaultSlides);
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var slides = JsonSerializer.Deserialize<JsonElement[]>(json);
                
                // 转换数据格式，统一使用前端期望的属性名
                var formattedSlides = slides.Select(slide => new
                {
                    id = slide.TryGetProperty("Id", out var idProp) ? idProp.GetString() : "",
                    title = slide.TryGetProperty("Title", out var titleProp) ? titleProp.GetString() : "",
                    description = slide.TryGetProperty("Description", out var descProp) ? descProp.GetString() : "",
                    image_url = slide.TryGetProperty("Image", out var imageProp) ? imageProp.GetString() : "",
                    link_url = slide.TryGetProperty("Link", out var linkProp) ? linkProp.GetString() : "",
                    sort_order = slide.TryGetProperty("Order", out var orderProp) ? orderProp.GetInt32() : 0,
                    is_active = true
                }).Where(s => !string.IsNullOrEmpty(s.image_url)) // 只返回有图片的幻灯片
                .OrderBy(s => s.sort_order)
                .ToArray();
                
                return Ok(formattedSlides);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "获取幻灯片数据失败", details = ex.Message });
            }
        }

        // 获取跑马灯数据
        [HttpGet("marquee")]
        public async Task<IActionResult> GetMarquee()
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "marquee.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    // 创建默认跑马灯数据
                    var defaultMarqueeData = new[]
                    {
                        new {
                            Id = "msg001",
                            Content = "恭喜 7346******** 赚取了＄1,128.50",
                            Type = "congratulation",
                            Link = (string)null,
                            Order = 1,
                            CreateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"),
                            IsActive = true
                        },
                        new {
                            Id = "msg002",
                            Content = "恭喜 1389******** 赚取了＄2,256.80",
                            Type = "congratulation",
                            Link = (string)null,
                            Order = 2,
                            CreateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"),
                            IsActive = true
                        },
                        new {
                            Id = "msg003",
                            Content = "恭喜 1520******** 赚取了＄1,089.20",
                            Type = "congratulation",
                            Link = (string)null,
                            Order = 3,
                            CreateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz"),
                            IsActive = true
                        }
                    };
                    
                    await System.IO.File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(defaultMarqueeData, new JsonSerializerOptions { WriteIndented = true }));
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var marqueeData = JsonSerializer.Deserialize<JsonElement[]>(json);
                
                // 提取有效的跑马灯消息
                var messages = new List<string>();
                
                foreach (var item in marqueeData)
                {
                    if (item.TryGetProperty("Content", out var contentProp) && 
                        item.TryGetProperty("IsActive", out var activeProp))
                    {
                        var content = contentProp.GetString();
                        var isActive = activeProp.ValueKind == JsonValueKind.True;
                        
                        if (!string.IsNullOrEmpty(content) && isActive)
                        {
                            messages.Add(content);
                        }
                    }
                }
                
                // 如果没有有效消息，使用默认消息
                if (messages.Count == 0)
                {
                    messages.AddRange(new[]
                    {
                        "恭喜 7346******** 赚取了＄1,128.50",
                        "恭喜 1389******** 赚取了＄2,256.80",
                        "恭喜 1520******** 赚取了＄1,089.20",
                        "恭喜 1876******** 赚取了＄2,345.60",
                        "恭喜 1395******** 赚取了＄1,198.30",
                        "恭喜 1588******** 赚取了＄2,567.90",
                        "恭喜 1777******** 赚取了＄1,078.40",
                        "恭喜 1666******** 赚取了＄2,423.70",
                        "恭喜 1888******** 赚取了＄1,156.20",
                        "恭喜 1999******** 赚取了＄2,289.50"
                    });
                }
                
                // 实现随机播放机制
                var random = new Random();
                var shuffledMessages = messages.OrderBy(x => random.Next()).ToArray();
                
                // 返回前端期望的格式
                return Ok(new { content = string.Join(",", shuffledMessages) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "获取跑马灯数据失败", details = ex.Message });
            }
        }

        // 获取用户余额信息
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            try
            {
                // 从JWT token中获取用户ID
                var token = GetTokenFromRequest();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { error = "未找到有效的身份验证令牌" });
                }

                var userId = _jwtService.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { error = "无效的身份验证令牌" });
                }

                // 从users.json中获取用户数据
                var usersFilePath = Path.Combine(_dataPath, "users.json");
                if (!System.IO.File.Exists(usersFilePath))
                {
                    return NotFound(new { error = "用户数据文件不存在" });
                }

                var usersJson = await System.IO.File.ReadAllTextAsync(usersFilePath);
                var users = JsonSerializer.Deserialize<User[]>(usersJson);
                var user = users?.FirstOrDefault(u => u.Id == userId.ToString());

                if (user == null)
                {
                    return NotFound(new { error = "用户不存在" });
                }

                // 返回用户余额信息
                var balanceInfo = new {
                    user_id = user.Id,
                    current_balance = user.CurrentBalance.ToString("F2"),
                    total_income = user.CurrentBalance.ToString("F2"), // 可以根据需要计算总收入
                    frozen_amount = user.FrozenAmount.ToString("F2"),
                    today_income = "0.00", // 可以根据需要计算今日收入
                    yesterday_income = "0.00", // 可以根据需要计算昨日收入
                    updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                return Ok(balanceInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "获取余额信息失败", details = ex.Message });
            }
        }

        // 获取VIP等级信息
        [HttpGet("vip/levels")]
        public async Task<IActionResult> GetVipLevels()
        {
            try
            {
                var filePath = Path.Combine(_dataPath, "vip_levels.json");
                
                if (!System.IO.File.Exists(filePath))
                {
                    var defaultVipLevels = new[]
                    {
                        new {
                            id = 1,
                            name = "VIP1",
                            deposit_requirement = 100,
                            daily_tasks = 5,
                            commission_rate = 1.5,
                            color = "#FFD700",
                            benefits = new[] { "基础佣金1.5%", "每日5个任务", "专属客服" },
                            is_active = true
                        },
                        new {
                            id = 2,
                            name = "VIP2",
                            deposit_requirement = 500,
                            daily_tasks = 8,
                            commission_rate = 2.0,
                            color = "#C0C0C0",
                            benefits = new[] { "佣金2.0%", "每日8个任务", "优先客服", "提现加速" },
                            is_active = true
                        },
                        new {
                            id = 3,
                            name = "VIP3",
                            deposit_requirement = 1000,
                            daily_tasks = 12,
                            commission_rate = 2.5,
                            color = "#CD7F32",
                            benefits = new[] { "佣金2.5%", "每日12个任务", "专属经理", "免费提现", "生日礼品" },
                            is_active = true
                        },
                        new {
                            id = 4,
                            name = "VIP4",
                            deposit_requirement = 2000,
                            daily_tasks = 15,
                            commission_rate = 3.0,
                            color = "#E5E4E2",
                            benefits = new[] { "佣金3.0%", "每日15个任务", "白金服务", "极速提现", "专属活动" },
                            is_active = true
                        },
                        new {
                            id = 5,
                            name = "VIP5",
                            deposit_requirement = 5000,
                            daily_tasks = 20,
                            commission_rate = 3.5,
                            color = "#FFD700",
                            benefits = new[] { "佣金3.5%", "每日20个任务", "钻石服务", "秒速提现", "定制礼品", "年度旅游" },
                            is_active = true
                        }
                    };
                    
                    await System.IO.File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(defaultVipLevels, new JsonSerializerOptions { WriteIndented = true }));
                    return Ok(defaultVipLevels);
                }
                
                var json = await System.IO.File.ReadAllTextAsync(filePath);
                var vipLevels = JsonSerializer.Deserialize<object[]>(json);
                return Ok(vipLevels);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "获取VIP等级信息失败", details = ex.Message });
            }
        }

        // 获取用户当前VIP信息
        [HttpGet("vip/current")]
        public async Task<IActionResult> GetCurrentVip()
        {
            try
            {
                // 从JWT token中获取用户ID
                var token = GetTokenFromRequest();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { error = "未找到有效的身份验证令牌" });
                }

                var userId = _jwtService.GetUserIdFromToken(token);
                if (userId == null)
                {
                    return Unauthorized(new { error = "无效的身份验证令牌" });
                }

                // 从users.json中获取用户数据
                var usersFilePath = Path.Combine(_dataPath, "users.json");
                if (!System.IO.File.Exists(usersFilePath))
                {
                    return NotFound(new { error = "用户数据文件不存在" });
                }

                var usersJson = await System.IO.File.ReadAllTextAsync(usersFilePath);
                var users = JsonSerializer.Deserialize<User[]>(usersJson);
                var user = users?.FirstOrDefault(u => u.Id == userId.ToString());

                if (user == null)
                {
                    return NotFound(new { error = "用户不存在" });
                }

                // 根据VIP等级获取VIP名称
                var vipName = user.VipLevel switch
                {
                    0 => "普通用户",
                    1 => "VIP1",
                    2 => "VIP2",
                    3 => "VIP3",
                    4 => "VIP4",
                    5 => "VIP5",
                    _ => "VIP" + user.VipLevel
                };

                // 计算下一级VIP要求
                var nextVipRequirement = user.VipLevel switch
                {
                    0 => 100,
                    1 => 500,
                    2 => 1000,
                    3 => 2000,
                    4 => 5000,
                    _ => 0
                };

                // 返回用户VIP信息
                var vipInfo = new {
                    user_id = user.Id,
                    current_vip_level = user.VipLevel,
                    vip_name = vipName,
                    deposit_amount = user.CurrentBalance,
                    tasks_completed_today = 0, // 可以根据需要从任务系统获取
                    tasks_required_today = user.VipLevel * 5, // 根据VIP等级计算
                    commission_rate = 1.0 + (user.VipLevel * 0.5), // 根据VIP等级计算佣金率
                    next_vip_requirement = nextVipRequirement,
                    vip_expires_at = user.VipExpireAt?.ToString("yyyy-MM-dd") ?? "",
                    updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                return Ok(vipInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "获取用户VIP信息失败", details = ex.Message });
            }
        }

        // 从HTTP请求中获取JWT token的辅助方法
        private string? GetTokenFromRequest()
        {
            // 首先尝试从Authorization header获取
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }

            // 然后尝试从Cookie获取
            var tokenFromCookie = Request.Cookies["auth_token"];
            if (!string.IsNullOrEmpty(tokenFromCookie))
            {
                return tokenFromCookie;
            }

            return null;
        }


    }
}