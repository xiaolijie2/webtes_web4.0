using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class StartController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _ordersFile = "orders.json";
        private readonly string _availableOrdersFile = "available_orders.json";
        private readonly string _userOrdersFile = "user_orders.json";
        private readonly string _vipConfigFile = "vip_config.json";
        private readonly string _balancesFile = "balances.json";

        public StartController()
        {
            EnsureDataDirectoryExists();
        }

        // 获取可用订单
        [HttpGet("available")]
        public IActionResult GetAvailableOrders([FromQuery] int limit = 10)
        {
            try
            {
                var availableOrders = LoadAvailableOrders();
                var orders = availableOrders.Take(limit).ToList();
                
                return Ok(new
                {
                    orders = orders,
                    totalCount = availableOrders.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取可用订单失败", error = ex.Message });
            }
        }

        // 抓取订单
        [HttpPost("grab")]
        public IActionResult GrabOrder([FromBody] GrabOrderRequest request)
        {
            try
            {
                // 获取用户VIP信息
                var vipInfo = GetUserVipInfo(request.UserId);
                
                // 获取今日统计
                var todayStats = GetUserTodayStats(request.UserId);
                
                // 检查今日抓取限额
                if (todayStats.GrabbedCount >= vipInfo.DailyOrderLimit)
                {
                    return BadRequest(new { message = "今日抓取次数已达限额" });
                }
                
                // 获取可用订单
                var availableOrders = LoadAvailableOrders();
                if (availableOrders.Count == 0)
                {
                    return BadRequest(new { message = "暂无可用订单" });
                }
                
                // 随机抓取1-3个订单
                var random = new Random();
                var grabCount = Math.Min(random.Next(1, 4), availableOrders.Count);
                var grabbedOrders = availableOrders.Take(grabCount).ToList();
                
                // 将抓取的订单分配给用户
                var userOrders = LoadUserOrders();
                foreach (var order in grabbedOrders)
                {
                    var userOrder = new UserOrder
                    {
                        OrderId = Guid.NewGuid().ToString(),
                        UserId = request.UserId,
                        OriginalOrderId = order.OrderId,
                        Title = order.Title,
                        Description = order.Description,
                        Amount = order.Amount,
                        Status = "pending",
                        CreatedAt = DateTime.Now,
                        ExpiresAt = DateTime.Now.AddHours(24)
                    };
                    userOrders.Add(userOrder);
                }
                
                // 从可用订单中移除已抓取的订单
                var remainingOrders = availableOrders.Skip(grabCount).ToList();
                SaveAvailableOrders(remainingOrders);
                
                // 保存用户订单
                SaveUserOrders(userOrders);
                
                // 补充新的可用订单
                ReplenishAvailableOrders();
                
                return Ok(new
                {
                    GrabbedCount = grabCount,
                    message = $"成功抓取到 {grabCount} 个订单"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "抓取订单失败", error = ex.Message });
            }
        }

        // 接单
        [HttpPost("take/{orderId}")]
        public IActionResult TakeOrder(string orderId, [FromBody] TakeOrderRequest request)
        {
            try
            {
                var availableOrders = LoadAvailableOrders();
                var order = availableOrders.FirstOrDefault(o => o.OrderId == orderId);
                
                if (order == null)
                {
                    return NotFound(new { message = "订单不存在或已被抢单" });
                }
                
                // 检查用户今日接单限额
                var vipInfo = GetUserVipInfo(request.UserId);
                var todayStats = GetUserTodayStats(request.UserId);
                
                if (todayStats.TakenCount >= vipInfo.DailyOrderLimit)
                {
                    return BadRequest(new { message = "今日接单次数已达限额" });
                }
                
                // 创建用户订单
                var userOrders = LoadUserOrders();
                var userOrder = new UserOrder
                {
                    OrderId = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    OriginalOrderId = order.OrderId,
                    Title = order.Title,
                    Description = order.Description,
                    Amount = order.Amount,
                    Status = "processing",
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(24)
                };
                
                userOrders.Add(userOrder);
                SaveUserOrders(userOrders);
                
                // 从可用订单中移除
                availableOrders.Remove(order);
                SaveAvailableOrders(availableOrders);
                
                // 补充新订单
                ReplenishAvailableOrders();
                
                return Ok(new { message = "接单成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "接单失败", error = ex.Message });
            }
        }

        // 获取今日统计
        [HttpGet("today-stats/{userId}")]
        public IActionResult GetTodayStats(string userId)
        {
            try
            {
                var stats = GetUserTodayStats(userId);
                var vipInfo = GetUserVipInfo(userId);
                
                return Ok(new
                {
                    GrabbedCount = stats.GrabbedCount,
                    TakenCount = stats.TakenCount,
                    CompletedCount = stats.CompletedCount,
                    TodayEarnings = stats.TodayEarnings,
                    remainingCount = Math.Max(0, vipInfo.DailyOrderLimit - stats.GrabbedCount),
                    successRate = stats.CompletedCount > 0 ? (int)((double)stats.CompletedCount / stats.TakenCount * 100) : 95,
                    avgEarnings = stats.CompletedCount > 0 ? stats.TodayEarnings / stats.CompletedCount : 5.50m
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取统计失败", error = ex.Message });
            }
        }

        // 私有方法
        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
            
            // 确保文件存在
            EnsureFileExists(_ordersFile, "[]");
            EnsureFileExists(_availableOrdersFile, "[]");
            EnsureFileExists(_userOrdersFile, "[]");
            EnsureFileExists(_vipConfigFile, GetDefaultVipConfig());
            EnsureFileExists(_balancesFile, "{}");
            
            // 初始化可用订单
            var availableOrders = LoadAvailableOrders();
            if (availableOrders.Count == 0)
            {
                ReplenishAvailableOrders();
            }
        }

        private void EnsureFileExists(string fileName, string defaultContent)
        {
            var filePath = Path.Combine(_dataDirectory, fileName);
            if (!System.IO.File.Exists(filePath))
            {
                System.IO.File.WriteAllText(filePath, defaultContent);
            }
        }

        private List<AvailableOrder> LoadAvailableOrders()
        {
            var filePath = Path.Combine(_dataDirectory, _availableOrdersFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<AvailableOrder>();
            }
            
            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<AvailableOrder>>(json) ?? new List<AvailableOrder>();
        }

        private void SaveAvailableOrders(List<AvailableOrder> orders)
        {
            var filePath = Path.Combine(_dataDirectory, _availableOrdersFile);
            var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private List<UserOrder> LoadUserOrders()
        {
            var filePath = Path.Combine(_dataDirectory, _userOrdersFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<UserOrder>();
            }
            
            var json = System.IO.File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<UserOrder>>(json) ?? new List<UserOrder>();
        }

        private void SaveUserOrders(List<UserOrder> orders)
        {
            var filePath = Path.Combine(_dataDirectory, _userOrdersFile);
            var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(filePath, json);
        }

        private VipInfo GetUserVipInfo(string userId)
        {
            // 简化实现，返回默认VIP信息
            return new VipInfo
            {
                Level = 0,
                    LevelName = "普通用户",
                    DailyOrderLimit = 10,
                    BonusRate = 1.0m
            };
        }

        private TodayStats GetUserTodayStats(string userId)
        {
            var userOrders = LoadUserOrders();
            var today = DateTime.Today;
            
            var todayOrders = userOrders.Where(o => o.UserId == userId && o.CreatedAt.Date == today).ToList();
            
            return new TodayStats
            {
                GrabbedCount = todayOrders.Count,
                    TakenCount = todayOrders.Count(o => o.Status != "pending"),
                    CompletedCount = todayOrders.Count(o => o.Status == "completed"),
                    TodayEarnings = todayOrders.Where(o => o.Status == "completed").Sum(o => o.Amount)
            };
        }

        private void ReplenishAvailableOrders()
        {
            var availableOrders = LoadAvailableOrders();
            var random = new Random();
            
            // 生成新的可用订单
            var orderTemplates = new[]
            {
                new { title = "商品评价任务", description = "为指定商品撰写真实评价", baseAmount = 3.50m },
                new { title = "APP下载体验", description = "下载指定APP并体验3分钟", baseAmount = 4.20m },
                new { title = "关注点赞任务", description = "关注指定账号并点赞最新动态", baseAmount = 2.80m },
                new { title = "问卷调查任务", description = "完成指定问卷调查", baseAmount = 5.60m },
                new { title = "视频观看任务", description = "观看指定视频并点赞评论", baseAmount = 3.20m },
                new { title = "注册认证任务", description = "注册指定平台并完成实名认证", baseAmount = 8.80m },
                new { title = "分享转发任务", description = "分享指定内容到朋友圈", baseAmount = 4.50m },
                new { title = "签到打卡任务", description = "在指定APP连续签到3天", baseAmount = 6.30m }
            };
            
            // 补充到20个订单
            while (availableOrders.Count < 20)
            {
                var template = orderTemplates[random.Next(orderTemplates.Length)];
                var order = new AvailableOrder
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Title = template.title,
                    Description = template.description,
                    Amount = template.baseAmount + (decimal)(random.NextDouble() * 2 - 1), // ±1元随机浮动
                    CreatedAt = DateTime.Now
                };
                
                availableOrders.Add(order);
            }
            
            SaveAvailableOrders(availableOrders);
        }

        private string GetDefaultVipConfig()
        {
            var config = new
            {
                levels = new[]
                {
                    new { level = 0, name = "普通用户", dailyOrderLimit = 10, bonusRate = 1.0m, price = 0 },
                new { level = 1, name = "VIP1", dailyOrderLimit = 20, bonusRate = 1.2m, price = 98 },
                new { level = 2, name = "VIP2", dailyOrderLimit = 50, bonusRate = 1.5m, price = 298 },
                new { level = 3, name = "VIP3", dailyOrderLimit = 100, bonusRate = 2.0m, price = 598 }
                }
            };
            
            return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        }
    }


}