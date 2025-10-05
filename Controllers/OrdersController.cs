using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly string _dataPath = "data";
        private readonly string _ordersFile = "orders.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _accountFile = "account_info.json";
        private readonly string _orderConfigFile = "order_config.json";

        public OrdersController()
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }

        // 获取用户订单列表
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserOrders(string userId, [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var orders = await LoadOrdersAsync();
                var userOrders = orders.Where(o => o.UserId == userId).ToList();

                // 按状态筛选
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    userOrders = userOrders.Where(o => o.Status == status).ToList();
                }

                // 按创建时间倒序排列
                userOrders = userOrders.OrderByDescending(o => o.CreatedAt).ToList();

                // 分页
                var totalCount = userOrders.Count;
                var pagedOrders = userOrders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var hasMore = page * pageSize < totalCount;

                return Ok(new
                {
                    orders = pagedOrders,
                    totalCount,
                    page,
                    pageSize,
                    hasMore
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
                var orders = await LoadOrdersAsync();
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

        // 创建新订单
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                // 验证请求数据
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Type) || request.Amount <= 0)
                {
                    return BadRequest(new { message = "请求参数无效" });
                }

                // 检查用户余额（如果是需要扣费的订单类型）
                if (request.Type == "task" && request.Amount > 0)
                {
                    var balances = await LoadBalancesAsync();
                    var userBalance = balances.FirstOrDefault(b => b.UserId == request.UserId);
                    
                    if (userBalance == null || userBalance.Available < request.Amount)
                    {
                        return BadRequest(new { message = "余额不足" });
                    }
                }

                // 创建订单
                var order = new OrderRecord
                {
                    OrderId = GenerateOrderId(),
                    UserId = request.UserId,
                    Type = request.Type,
                    Title = request.Title,
                    Description = request.Description,
                    Amount = request.Amount,
                    Status = "pending",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Remark = request.Remark
                };

                // 保存订单
                var orders = await LoadOrdersAsync();
                orders.Add(order);
                await SaveOrdersAsync(orders);

                // 如果是任务订单，冻结用户余额
                if (request.Type == "task" && request.Amount > 0)
                {
                    await FreezeUserBalance(request.UserId, request.Amount);
                }

                return Ok(new { message = "订单创建成功", orderId = order.OrderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建订单失败", error = ex.Message });
            }
        }

        // 处理订单（开始处理）
        [HttpPost("process/{orderId}")]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            try
            {
                var orders = await LoadOrdersAsync();
                var order = orders.FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "pending")
                {
                    return BadRequest(new { message = "订单状态不允许此操作" });
                }

                // 更新订单状态
                order.Status = "processing";
                order.UpdatedAt = DateTime.Now;
                order.StartedAt = DateTime.Now;

                await SaveOrdersAsync(orders);

                return Ok(new { message = "订单开始处理" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "处理订单失败", error = ex.Message });
            }
        }

        // 完成订单
        [HttpPost("complete/{orderId}")]
        public async Task<IActionResult> CompleteOrder(string orderId)
        {
            try
            {
                var orders = await LoadOrdersAsync();
                var order = orders.FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "processing")
                {
                    return BadRequest(new { message = "订单状态不允许此操作" });
                }

                // 更新订单状态
                order.Status = "completed";
                order.UpdatedAt = DateTime.Now;
                order.CompletedAt = DateTime.Now;

                await SaveOrdersAsync(orders);

                // 如果是任务订单，释放冻结余额并添加奖励
                if (order.Type == "task")
                {
                    await CompleteTaskOrder(order.UserId, order.Amount);
                }

                return Ok(new { message = "订单已完成" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "完成订单失败", error = ex.Message });
            }
        }

        // 取消订单
        [HttpPost("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            try
            {
                var orders = await LoadOrdersAsync();
                var order = orders.FirstOrDefault(o => o.OrderId == orderId);

                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "pending" && order.Status != "processing")
                {
                    return BadRequest(new { message = "订单状态不允许取消" });
                }

                // 更新订单状态
                order.Status = "cancelled";
                order.UpdatedAt = DateTime.Now;
                order.CancelledAt = DateTime.Now;

                await SaveOrdersAsync(orders);

                // 如果是任务订单，释放冻结余额
                if (order.Type == "task" && order.Amount > 0)
                {
                    await UnfreezeUserBalance(order.UserId, order.Amount);
                }

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
                var orders = await LoadOrdersAsync();
                var userOrders = orders.Where(o => o.UserId == userId).ToList();

                var today = DateTime.Today;
                var thisMonth = new DateTime(today.Year, today.Month, 1);

                var stats = new
                {
                    totalOrders = userOrders.Count,
                    completedOrders = userOrders.Count(o => o.Status == "completed"),
                    processingOrders = userOrders.Count(o => o.Status == "processing"),
                    pendingOrders = userOrders.Count(o => o.Status == "pending"),
                    todayOrders = userOrders.Count(o => o.CreatedAt.Date == today),
                    monthOrders = userOrders.Count(o => o.CreatedAt >= thisMonth),
                    totalAmount = userOrders.Where(o => o.Status == "completed").Sum(o => o.Amount),
                    todayAmount = userOrders.Where(o => o.Status == "completed" && o.CreatedAt.Date == today).Sum(o => o.Amount),
                    monthAmount = userOrders.Where(o => o.Status == "completed" && o.CreatedAt >= thisMonth).Sum(o => o.Amount)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单统计失败", error = ex.Message });
            }
        }

        // 获取订单配置
        [HttpGet("config")]
        public async Task<IActionResult> GetOrderConfig()
        {
            try
            {
                var config = await LoadOrderConfigAsync();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单配置失败", error = ex.Message });
            }
        }

        // 管理员：获取所有订单
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAllOrders([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var orders = await LoadOrdersAsync();

                // 按状态筛选
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    orders = orders.Where(o => o.Status == status).ToList();
                }

                // 按创建时间倒序排列
                orders = orders.OrderByDescending(o => o.CreatedAt).ToList();

                // 分页
                var totalCount = orders.Count;
                var pagedOrders = orders.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var hasMore = page * pageSize < totalCount;

                return Ok(new
                {
                    orders = pagedOrders,
                    totalCount,
                    page,
                    pageSize,
                    hasMore
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单列表失败", error = ex.Message });
            }
        }

        // 管理员：批量处理订单
        [HttpPost("admin/batch-process")]
        public async Task<IActionResult> BatchProcessOrders([FromBody] BatchProcessRequest request)
        {
            try
            {
                var orders = await LoadOrdersAsync();
                var processedCount = 0;

                foreach (var orderId in request.OrderIds)
                {
                    var order = orders.FirstOrDefault(o => o.OrderId == orderId);
                    if (order != null && order.Status == "pending")
                    {
                        order.Status = request.Action;
                        order.UpdatedAt = DateTime.Now;
                        
                        if (request.Action == "completed")
                        {
                            order.CompletedAt = DateTime.Now;
                            if (order.Type == "task")
                            {
                                await CompleteTaskOrder(order.UserId, order.Amount);
                            }
                        }
                        else if (request.Action == "cancelled")
                        {
                            order.CancelledAt = DateTime.Now;
                            if (order.Type == "task" && order.Amount > 0)
                            {
                                await UnfreezeUserBalance(order.UserId, order.Amount);
                            }
                        }
                        
                        processedCount++;
                    }
                }

                await SaveOrdersAsync(orders);

                return Ok(new { message = $"成功处理 {processedCount} 个订单" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "批量处理订单失败", error = ex.Message });
            }
        }

        // 私有方法：加载订单数据
        private async Task<List<OrderRecord>> LoadOrdersAsync()
        {
            var filePath = Path.Combine(_dataPath, _ordersFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<OrderRecord>();
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<OrderRecord>>(json) ?? new List<OrderRecord>();
        }

        // 私有方法：保存订单数据
        private async Task SaveOrdersAsync(List<OrderRecord> orders)
        {
            var filePath = Path.Combine(_dataPath, _ordersFile);
            var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        // 私有方法：加载余额数据
        private async Task<List<UserBalance>> LoadBalancesAsync()
        {
            var filePath = Path.Combine(_dataPath, _balancesFile);
            if (!System.IO.File.Exists(filePath))
            {
                return new List<UserBalance>();
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
        }

        // 私有方法：保存余额数据
        private async Task SaveBalancesAsync(List<UserBalance> balances)
        {
            var filePath = Path.Combine(_dataPath, _balancesFile);
            var json = JsonSerializer.Serialize(balances, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        // 私有方法：加载订单配置
        private async Task<OrderConfig> LoadOrderConfigAsync()
        {
            var filePath = Path.Combine(_dataPath, _orderConfigFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultConfig = new OrderConfig
                {
                    TaskRewardRate = 0.1m,
                    MinTaskAmount = 10m,
                    MaxTaskAmount = 1000m,
                    AutoCompleteTime = 300, // 5分钟
                    AllowCancel = true,
                    RequireApproval = false
                };
                
                var defaultJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(filePath, defaultJson);
                return defaultConfig;
            }

            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<OrderConfig>(json) ?? new OrderConfig();
        }

        // 私有方法：冻结用户余额
        private async Task FreezeUserBalance(string userId, decimal amount)
        {
            var balances = await LoadBalancesAsync();
            var userBalance = balances.FirstOrDefault(b => b.UserId == userId);
            
            if (userBalance != null)
            {
                userBalance.Available -= amount;
                userBalance.Frozen += amount;
                await SaveBalancesAsync(balances);
            }
        }

        // 私有方法：解冻用户余额
        private async Task UnfreezeUserBalance(string userId, decimal amount)
        {
            var balances = await LoadBalancesAsync();
            var userBalance = balances.FirstOrDefault(b => b.UserId == userId);
            
            if (userBalance != null)
            {
                userBalance.Available += amount;
                userBalance.Frozen -= amount;
                await SaveBalancesAsync(balances);
            }
        }

        // 私有方法：完成任务订单
        private async Task CompleteTaskOrder(string userId, decimal amount)
        {
            var config = await LoadOrderConfigAsync();
            var reward = amount * config.TaskRewardRate;
            
            var balances = await LoadBalancesAsync();
            var userBalance = balances.FirstOrDefault(b => b.UserId == userId);
            
            if (userBalance != null)
            {
                // 解冻原金额
                userBalance.Frozen -= amount;
                // 添加奖励到可用余额
                userBalance.Available += reward;
                userBalance.Total += reward;
                
                await SaveBalancesAsync(balances);
            }
        }

        // 私有方法：生成订单ID
        private string GenerateOrderId()
        {
            return $"ORD{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        }
    }
}