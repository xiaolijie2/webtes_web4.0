using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RechargeController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _rechargeConfigFile;
        private readonly string _rechargeOrdersFile;
        private readonly string _balancesFile;
        private readonly string _transactionsFile;

        public RechargeController()
        {
            _rechargeConfigFile = Path.Combine(_dataDirectory, "recharge_config.json");
            _rechargeOrdersFile = Path.Combine(_dataDirectory, "recharge_orders.json");
            _balancesFile = Path.Combine(_dataDirectory, "balances.json");
            _transactionsFile = Path.Combine(_dataDirectory, "transactions.json");
            EnsureDataDirectoryExists();
        }

        [HttpGet("config")]
        public async Task<IActionResult> GetRechargeConfig()
        {
            try
            {
                var config = await LoadRechargeConfig();
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取充值配置失败", error = ex.Message });
            }
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateRechargeOrder([FromBody] CreateRechargeOrderRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.MethodId) || request.Amount <= 0)
                {
                    return BadRequest(new { message = "请求参数无效" });
                }

                var config = await LoadRechargeConfig();
                var method = config.Methods.FirstOrDefault(m => m.Id == request.MethodId && m.IsEnabled);
                if (method == null)
                {
                    return BadRequest(new { message = "充值方式不存在或已禁用" });
                }

                if (request.Amount < method.MinAmount || request.Amount > method.MaxAmount)
                {
                    return BadRequest(new { message = $"充值金额必须在 {method.MinAmount} - {method.MaxAmount} 之间" });
                }

                var fee = method.FeeType == "percentage" ? request.Amount * method.Fee / 100 : method.Fee;
                var actualAmount = request.Amount + fee;

                var order = new RechargeOrder
                {
                    Id = GenerateOrderId(),
                    UserId = request.UserId,
                    MethodId = request.MethodId,
                    MethodName = method.Name,
                    Amount = request.Amount,
                    Fee = fee,
                    ActualAmount = actualAmount,
                    Status = "pending",
                    CreatedAt = DateTime.Now,
                    ExpiredAt = DateTime.Now.AddHours(24),
                    PaymentInfo = GeneratePaymentInfo(method, actualAmount)
                };

                await SaveRechargeOrder(order);

                return Ok(new { orderId = order.Id, order });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建充值订单失败", error = ex.Message });
            }
        }

        [HttpGet("orders/{userId}")]
        public async Task<IActionResult> GetUserRechargeOrders(string userId)
        {
            try
            {
                var orders = await LoadRechargeOrders();
                var userOrders = orders.Where(o => o.UserId == userId)
                                     .OrderByDescending(o => o.CreatedAt)
                                     .ToList();
                return Ok(userOrders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取充值订单失败", error = ex.Message });
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetRechargeOrderDetail(string orderId)
        {
            try
            {
                var orders = await LoadRechargeOrders();
                var order = orders.FirstOrDefault(o => o.Id == orderId);
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

        [HttpPost("order/{orderId}/confirm")]
        public async Task<IActionResult> ConfirmPayment(string orderId, [FromBody] ConfirmPaymentRequest request)
        {
            try
            {
                var orders = await LoadRechargeOrders();
                var order = orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "pending")
                {
                    return BadRequest(new { message = "订单状态不允许确认支付" });
                }

                if (DateTime.Now > order.ExpiredAt)
                {
                    order.Status = "expired";
                    await SaveRechargeOrders(orders);
                    return BadRequest(new { message = "订单已过期" });
                }

                order.Status = "processing";
                order.PaymentProof = request.PaymentProof;
                order.Remark = request.Remark;

                await SaveRechargeOrders(orders);

                return Ok(new { message = "支付凭证已提交，等待审核" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "确认支付失败", error = ex.Message });
            }
        }

        [HttpPost("order/{orderId}/approve")]
        public async Task<IActionResult> ApproveRecharge(string orderId, [FromBody] ApproveRechargeRequest request)
        {
            try
            {
                var orders = await LoadRechargeOrders();
                var order = orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "processing")
                {
                    return BadRequest(new { message = "订单状态不允许审核" });
                }

                if (request.Approved)
                {
                    order.Status = "completed";
                    order.CompletedAt = DateTime.Now;
                    order.Remark = request.Remark;

                    // 增加用户余额
                    await AddUserBalance(order.UserId, order.Amount);

                    // 记录交易
                    await RecordTransaction(order.UserId, "recharge", order.Amount, $"充值成功 - {order.MethodName}", order.Id);
                }
                else
                {
                    order.Status = "rejected";
                    order.Remark = request.Remark;
                }

                await SaveRechargeOrders(orders);

                return Ok(new { message = request.Approved ? "充值审核通过" : "充值审核拒绝" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "审核充值失败", error = ex.Message });
            }
        }

        [HttpPost("order/{orderId}/cancel")]
        public async Task<IActionResult> CancelRechargeOrder(string orderId)
        {
            try
            {
                var orders = await LoadRechargeOrders();
                var order = orders.FirstOrDefault(o => o.Id == orderId);
                if (order == null)
                {
                    return NotFound(new { message = "订单不存在" });
                }

                if (order.Status != "pending")
                {
                    return BadRequest(new { message = "只能取消待支付的订单" });
                }

                order.Status = "cancelled";
                await SaveRechargeOrders(orders);

                return Ok(new { message = "订单已取消" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "取消订单失败", error = ex.Message });
            }
        }

        // 私有辅助方法
        private void EnsureDataDirectoryExists()
        {
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        private async Task<RechargeConfig> LoadRechargeConfig()
        {
            if (!System.IO.File.Exists(_rechargeConfigFile))
            {
                var defaultConfig = CreateDefaultRechargeConfig();
                await SaveRechargeConfig(defaultConfig);
                return defaultConfig;
            }

            var json = await System.IO.File.ReadAllTextAsync(_rechargeConfigFile);
            return JsonSerializer.Deserialize<RechargeConfig>(json) ?? CreateDefaultRechargeConfig();
        }

        private async Task SaveRechargeConfig(RechargeConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_rechargeConfigFile, json);
        }

        private async Task<List<RechargeOrder>> LoadRechargeOrders()
        {
            if (!System.IO.File.Exists(_rechargeOrdersFile))
            {
                return new List<RechargeOrder>();
            }

            var json = await System.IO.File.ReadAllTextAsync(_rechargeOrdersFile);
            return JsonSerializer.Deserialize<List<RechargeOrder>>(json) ?? new List<RechargeOrder>();
        }

        private async Task SaveRechargeOrders(List<RechargeOrder> orders)
        {
            var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_rechargeOrdersFile, json);
        }

        private async Task SaveRechargeOrder(RechargeOrder order)
        {
            var orders = await LoadRechargeOrders();
            orders.Add(order);
            await SaveRechargeOrders(orders);
        }

        private async Task<UserBalance> LoadUserBalance(string userId)
        {
            if (!System.IO.File.Exists(_balancesFile))
            {
                return new UserBalance { UserId = userId, Available = 0, Frozen = 0, Total = 0 };
            }

            var json = await System.IO.File.ReadAllTextAsync(_balancesFile);
            var balances = JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
            return balances.FirstOrDefault(b => b.UserId == userId) ?? new UserBalance { UserId = userId, Available = 0, Frozen = 0, Total = 0 };
        }

        private async Task SaveUserBalance(UserBalance balance)
        {
            List<UserBalance> balances;
            if (System.IO.File.Exists(_balancesFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(_balancesFile);
                balances = JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
            }
            else
            {
                balances = new List<UserBalance>();
            }

            var existingBalance = balances.FirstOrDefault(b => b.UserId == balance.UserId);
            if (existingBalance != null)
            {
                existingBalance.Available = balance.Available;
                existingBalance.Frozen = balance.Frozen;
                existingBalance.Total = balance.Total;
            }
            else
            {
                balances.Add(balance);
            }

            var balanceJson = JsonSerializer.Serialize(balances, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_balancesFile, balanceJson);
        }

        private async Task AddUserBalance(string userId, decimal amount)
        {
            var balance = await LoadUserBalance(userId);
            balance.Available += amount;
            balance.Total = balance.Available + balance.Frozen;
            await SaveUserBalance(balance);
        }

        private async Task RecordTransaction(string userId, string type, decimal amount, string description, string orderId)
        {
            List<Transaction> transactions;
            if (System.IO.File.Exists(_transactionsFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(_transactionsFile);
                transactions = JsonSerializer.Deserialize<List<Transaction>>(json) ?? new List<Transaction>();
            }
            else
            {
                transactions = new List<Transaction>();
            }
            
            var transaction = new Transaction
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Type = type,
                Amount = amount,
                Description = description,
                RelatedId = orderId,
                CreatedAt = DateTime.Now
            };
            
            transactions.Add(transaction);
            
            var transactionJson = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_transactionsFile, transactionJson);
        }

        private string GenerateOrderId()
        {
            return "R" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        private PaymentInfo GeneratePaymentInfo(RechargeMethod method, decimal amount)
        {
            var paymentInfo = new PaymentInfo
            {
                Method = method.Name,
                Amount = amount
            };
            
            switch (method.Id)
            {
                case "bank":
                    paymentInfo.BankName = "中国工商银行";
                    paymentInfo.AccountName = "某某科技有限公司";
                    paymentInfo.AccountNumber = "6222021234567890123";
                    paymentInfo.Instructions = new[] { "请使用网银或手机银行转账", "转账时请备注您的用户ID", "转账完成后请上传转账凭证" };
                    break;
                case "alipay":
                    paymentInfo.AccountName = "某某科技";
                    paymentInfo.AccountNumber = "service@example.com";
                    paymentInfo.QrCode = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
                    paymentInfo.Instructions = new[] { "请扫描二维码或转账到指定支付宝账号", "转账时请备注您的用户ID", "转账完成后请截图上传" };
                    break;
                case "wechat":
                    paymentInfo.AccountName = "某某科技";
                    paymentInfo.QrCode = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
                    paymentInfo.Instructions = new[] { "请扫描二维码进行微信转账", "转账时请备注您的用户ID", "转账完成后请截图上传" };
                    break;
            }
            
            return paymentInfo;
        }

        private RechargeConfig CreateDefaultRechargeConfig()
        {
            return new RechargeConfig
            {
                Methods = new List<RechargeMethod>
                {
                    new RechargeMethod
                    {
                        Id = "bank",
                        Name = "银行卡转账",
                        Icon = "credit-card",
                        MinAmount = 10,
                        MaxAmount = 50000,
                        Fee = 0,
                        FeeType = "fixed",
                        IsEnabled = true,
                        Description = "支持各大银行转账，到账时间1-3个工作日",
                        Instructions = new[] { "请使用网银或手机银行转账", "转账时请备注您的用户ID", "转账完成后请上传转账凭证" }
                    },
                    new RechargeMethod
                    {
                        Id = "alipay",
                        Name = "支付宝转账",
                        Icon = "smartphone",
                        MinAmount = 1,
                        MaxAmount = 10000,
                        Fee = 0,
                        FeeType = "fixed",
                        IsEnabled = true,
                        Description = "支付宝转账，实时到账",
                        Instructions = new[] { "请扫描二维码或转账到指定支付宝账号", "转账时请备注您的用户ID", "转账完成后请截图上传" }
                    },
                    new RechargeMethod
                    {
                        Id = "wechat",
                        Name = "微信转账",
                        Icon = "message-circle",
                        MinAmount = 1,
                        MaxAmount = 10000,
                        Fee = 0,
                        FeeType = "fixed",
                        IsEnabled = true,
                        Description = "微信转账，实时到账",
                        Instructions = new[] { "请扫描二维码进行微信转账", "转账时请备注您的用户ID", "转账完成后请截图上传" }
                    }
                },
                QuickAmounts = new[] { 100, 200, 500, 1000, 2000, 5000 },
                Notice = "1. 请确保转账金额与订单金额一致\n2. 转账时请务必备注您的用户ID\n3. 上传转账凭证后，我们会在1-3个工作日内处理\n4. 如有疑问，请联系客服",
                CustomerService = "如需帮助，请联系在线客服或拨打客服热线：400-888-8888"
            };
        }
    }
}