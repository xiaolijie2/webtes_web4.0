using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _ordersFile = "orders.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _usersFile = "users.json";

        public OrderController()
        {
            EnsureDataDirectoryExists();
        }

        // 获取用户订单列表
        [HttpGet("list/{userId}")]
        public async Task<IActionResult> GetOrderList(string userId, [FromQuery] int page = 1, [FromQuery] int limit = 20, [FromQuery] string? status = null)
        {
            try
            {
                var orders = await LoadOrders();
                var userOrders = orders.Where(o => o.UserId == userId).ToList();

                // 按状态筛选
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    userOrders = userOrders.Where(o => o.Status == status).ToList();
                }

                // 按时间降序排序
                userOrders = userOrders.OrderByDescending(o => o.CreateTime).ToList();

                // 分页
                var totalCount = userOrders.Count;
                var pagedOrders = userOrders.Skip((page - 1) * limit).Take(limit).ToList();

                return Ok(new
                {
                    orders = pagedOrders,
                    totalCount,
                    page,
                    limit,
                    hasMore = page * limit < totalCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单列表失败", error = ex.Message });
            }
        }

        // 获取订单详情
        [HttpGet("detail/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(string orderId)
        {
            try
            {
                var orders = await LoadOrders();
                var order = orders.FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单详情失败", error = ex.Message });
            }
        }

        // 创建订单
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // 验证请求
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.ProductName) || request.Amount <= 0)
                {
                    return BadRequest(new { message = "请求参数无效" });
                }

                // 检查用户余额
                var balances = await LoadBalances();
                var userBalance = balances.FirstOrDefault(b => b.UserId == request.UserId);
                if (userBalance == null || userBalance.Available < request.Amount)
                {
                    return BadRequest(new { message = "余额不足" });
                }

                // 创建订单
                var order = new Order
                {
                    OrderId = GenerateOrderId(),
                    UserId = request.UserId,
                    ProductName = request.ProductName,
                    Amount = request.Amount,
                    Commission = request.Amount * 0.1m, // 10%佣金
                    Platform = request.Platform ?? "默认平台",
                    Description = request.Description ?? $"{request.ProductName}的订单",
                    Status = "pending",
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                // 冻结用户余额
                userBalance.Available -= request.Amount;
                userBalance.Frozen += request.Amount;
                await SaveBalances(balances);

                // 保存订单
                var orders = await LoadOrders();
                orders.Add(order);
                await SaveOrders(orders);

                return Ok(new { message = "订单创建成功", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建订单失败", error = ex.Message });
            }
        }

        // 开始订单
        [HttpPost("start/{orderId}")]
        public async Task<IActionResult> StartOrder(string orderId, [FromBody] OrderActionRequest request)
        {
            try
            {
                var orders = await LoadOrders();
                var order = orders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == request.UserId);

                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "pending")
                {
                    return BadRequest(new { message = "订单状态不允许开始" });
                }

                // 更新订单状态
                order.Status = "processing";
                order.UpdateTime = DateTime.Now;
                order.StartTime = DateTime.Now;

                await SaveOrders(orders);

                return Ok(new { message = "订单已开始" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "开始订单失败", error = ex.Message });
            }
        }

        // 完成订单
        [HttpPost("complete/{orderId}")]
        public async Task<IActionResult> CompleteOrder(string orderId, [FromBody] OrderActionRequest request)
        {
            try
            {
                var orders = await LoadOrders();
                var order = orders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == request.UserId);

                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "processing")
                {
                    return BadRequest(new { message = "订单状态不允许完成" });
                }

                // 更新订单状态
                order.Status = "completed";
                order.UpdateTime = DateTime.Now;
                order.CompletedTime = DateTime.Now;

                // 解冻余额并添加佣金
                var balances = await LoadBalances();
                var userBalance = balances.FirstOrDefault(b => b.UserId == request.UserId);
                if (userBalance != null)
                {
                    userBalance.Frozen -= order.Amount;
                    userBalance.Available += order.Commission; // 添加佣金到可用余额
                    await SaveBalances(balances);
                }

                await SaveOrders(orders);

                return Ok(new { message = "订单已完成", commission = order.Commission });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "完成订单失败", error = ex.Message });
            }
        }

        // 取消订单
        [HttpPost("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(string orderId, [FromBody] OrderActionRequest request)
        {
            try
            {
                var orders = await LoadOrders();
                var order = orders.FirstOrDefault(o => o.OrderId == orderId && o.UserId == request.UserId);

                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status == "completed" || order.Status == "cancelled")
                {
                    return BadRequest(new { message = "订单状态不允许取消" });
                }

                // 更新订单状态
                order.Status = "cancelled";
                order.UpdateTime = DateTime.Now;

                // 解冻余额
                var balances = await LoadBalances();
                var userBalance = balances.FirstOrDefault(b => b.UserId == request.UserId);
                if (userBalance != null)
                {
                    userBalance.Frozen -= order.Amount;
                    userBalance.Available += order.Amount;
                    await SaveBalances(balances);
                }

                await SaveOrders(orders);

                return Ok(new { message = "订单已取消" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "取消订单失败", error = ex.Message });
            }
        }

        // 获取订单统计
        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetOrderStats(string userId)
        {
            try
            {
                var orders = await LoadOrders();
                var userOrders = orders.Where(o => o.UserId == userId).ToList();

                var stats = new
                {
                    totalOrders = userOrders.Count,
                    pendingOrders = userOrders.Count(o => o.Status == "pending"),
                    processingOrders = userOrders.Count(o => o.Status == "processing"),
                    completedOrders = userOrders.Count(o => o.Status == "completed"),
                    cancelledOrders = userOrders.Count(o => o.Status == "cancelled"),
                    totalEarnings = userOrders.Where(o => o.Status == "completed").Sum(o => o.Commission),
                    todayOrders = userOrders.Count(o => o.CreateTime.Date == DateTime.Today),
                    todayEarnings = userOrders.Where(o => o.Status == "completed" && o.CreateTime.Date == DateTime.Today).Sum(o => o.Commission)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单统计失败", error = ex.Message });
            }
        }

        // 获取可用订单（用于开始页面）
        [HttpGet("available/{userId}")]
        public async Task<IActionResult> GetAvailableOrders(string userId, [FromQuery] int limit = 10)
        {
            try
            {
                // 生成可用订单（模拟数据）
                var availableOrders = GenerateAvailableOrders(limit);
                
                // 检查用户VIP等级以确定可抓取订单数量
                var users = await LoadUsers();
                var user = users.FirstOrDefault(u => u.UserId == userId);
                var vipLevel = user?.VipLevel ?? 0;
                
                // 根据VIP等级限制订单数量
                var maxOrders = GetMaxOrdersByVipLevel(vipLevel);
                availableOrders = availableOrders.Take(maxOrders).ToList();

                return Ok(new
                {
                    orders = availableOrders,
                    maxOrders,
                    vipLevel
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取可用订单失败", error = ex.Message });
            }
        }

        // 抓取订单
        [HttpPost("grab/{orderId}")]
        public async Task<IActionResult> GrabOrder(string orderId, [FromBody] OrderActionRequest request)
        {
            try
            {
                // 检查用户今日已抓取订单数量
                var orders = await LoadOrders();
                var todayOrders = orders.Count(o => o.UserId == request.UserId && o.CreateTime.Date == DateTime.Today);
                
                var users = await LoadUsers();
                var user = users.FirstOrDefault(u => u.UserId == request.UserId);
                var vipLevel = user?.VipLevel ?? 0;
                var maxOrders = GetMaxOrdersByVipLevel(vipLevel);
                
                if (todayOrders >= maxOrders)
                {
                    return BadRequest(new { message = $"今日订单数量已达上限({maxOrders}个)" });
                }

                // 生成订单金额（随机）
                var random = new Random();
                var amount = (decimal)(random.NextDouble() * 100 + 10); // 10-110之间
                
                // 检查用户余额
                var balances = await LoadBalances();
                var userBalance = balances.FirstOrDefault(b => b.UserId == request.UserId);
                if (userBalance == null || userBalance.Available < amount)
                {
                    return BadRequest(new { message = "余额不足，请先充值" });
                }

                // 创建订单
                var order = new Order
                {
                    OrderId = GenerateOrderId(),
                    UserId = request.UserId,
                    ProductName = $"商品{random.Next(1, 100)}",
                    Amount = amount,
                    Commission = amount * 0.1m,
                    Platform = $"平台{random.Next(1, 5)}",
                    Description = "抓取的订单任务",
                    Status = "pending",
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                // 冻结用户余额
                userBalance.Available -= amount;
                userBalance.Frozen += amount;
                await SaveBalances(balances);

                // 保存订单
                orders.Add(order);
                await SaveOrders(orders);

                return Ok(new { message = "订单抓取成功", order });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "抓取订单失败", error = ex.Message });
            }
        }

        #region 私有方法

        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        private async Task<List<Order>> LoadOrders()
        {
            var filePath = Path.Combine(_dataDirectory, _ordersFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultOrders = CreateDefaultOrders();
                await SaveOrders(defaultOrders);
                return defaultOrders;
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<Order>>(json) ?? new List<Order>();
        }

        private async Task SaveOrders(List<Order> orders)
        {
            var filePath = Path.Combine(_dataDirectory, _ordersFile);
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

        private async Task<List<UserInfo>> LoadUsers()
        {
            var filePath = Path.Combine(_dataDirectory, _usersFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<UserInfo>();
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserInfo>>(json) ?? new List<UserInfo>();
        }

        private string GenerateOrderId()
        {
            return "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        private List<Order> CreateDefaultOrders()
        {
            var orders = new List<Order>();
            var random = new Random();
            var statuses = new[] { "pending", "processing", "completed", "cancelled" };
            var products = new[] { "商品A", "商品B", "商品C", "商品D", "商品E" };
            var platforms = new[] { "平台1", "平台2", "平台3", "平台4" };

            for (int i = 1; i <= 10; i++)
            {
                var amount = (decimal)(random.NextDouble() * 100 + 10);
                var createTime = DateTime.Now.AddDays(-random.Next(0, 30));
                var status = statuses[random.Next(statuses.Length)];

                orders.Add(new Order
                {
                    OrderId = $"ORD{DateTime.Now:yyyyMMdd}{i:D4}",
                    UserId = "user123",
                    ProductName = products[random.Next(products.Length)],
                    Amount = amount,
                    Commission = amount * 0.1m,
                    Platform = platforms[random.Next(platforms.Length)],
                    Description = $"这是{products[random.Next(products.Length)]}的订单描述",
                    Status = status,
                    CreateTime = createTime,
                    UpdateTime = createTime,
                    StartTime = status != "pending" ? createTime.AddMinutes(random.Next(1, 60)) : null,
                    CompletedTime = status == "completed" ? createTime.AddHours(random.Next(1, 24)) : null
                });
            }

            return orders;
        }

        private List<AvailableOrder> GenerateAvailableOrders(int count)
        {
            var orders = new List<AvailableOrder>();
            var random = new Random();
            var products = new[] { "商品A", "商品B", "商品C", "商品D", "商品E" };
            var platforms = new[] { "平台1", "平台2", "平台3", "平台4" };

            for (int i = 1; i <= count; i++)
            {
                var amount = (decimal)(random.NextDouble() * 100 + 10);
                orders.Add(new AvailableOrder
                {
                    OrderId = $"AV{DateTime.Now:yyyyMMddHHmmss}{i:D3}",
                    ProductName = products[random.Next(products.Length)],
                    Amount = amount,
                    Commission = amount * 0.1m,
                    Platform = platforms[random.Next(platforms.Length)],
                    Description = $"可抓取的{products[random.Next(products.Length)]}订单",
                    ExpireTime = DateTime.Now.AddMinutes(random.Next(30, 120))
                });
            }

            return orders;
        }

        private int GetMaxOrdersByVipLevel(int vipLevel)
        {
            return vipLevel switch
            {
                0 => 3,  // 普通用户
                1 => 5,  // VIP1
                2 => 8,  // VIP2
                3 => 12, // VIP3
                4 => 20, // VIP4
                5 => 30, // VIP5
                _ => 3
            };
        }

        #endregion
    }

    // 请求模型
    public class OrderActionRequest
    {
        public string UserId { get; set; } = string.Empty;
    }
}