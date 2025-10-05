using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using MobileECommerceAPI.Models;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WithdrawController : ControllerBase
    {
        private readonly string _dataDirectory = "Data";
        private readonly string _withdrawConfigFile = "withdraw_config.json";
        private readonly string _withdrawOrdersFile = "withdraw_orders.json";
        private readonly string _bankCardsFile = "bank_cards.json";
        private readonly string _balancesFile = "balances.json";
        private readonly string _transactionsFile = "transactions.json";
        private readonly string _usersFile = "users.json";

        public WithdrawController()
        {
            EnsureDataDirectoryExists();
        }

        [HttpGet("config")]
        public async Task<IActionResult> GetWithdrawConfig()
        {
            try
            {
                var config = await LoadWithdrawConfig();
                
                return Ok(new
                {
                    minAmount = config.MinAmount,
                    maxAmount = config.MaxAmount,
                    dailyLimit = config.DailyLimit,
                    fee = config.Fee,
                    feeType = config.FeeType,
                    workingHours = config.WorkingHours,
                    processingTime = config.ProcessingTime,
                    notice = config.Notice,
                    rules = config.Rules
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取提款配置失败", error = ex.Message });
            }
        }

        [HttpGet("bank-cards/{userId}")]
        public async Task<IActionResult> GetUserBankCards(string userId)
        {
            try
            {
                var bankCards = await LoadBankCards();
                var userCards = bankCards.Where(c => c.UserId == userId && !c.IsDeleted).ToList();
                
                return Ok(new
                {
                    cards = userCards.Select(c => new
                    {
                        id = c.Id,
                        bankName = c.BankName,
                        cardNumber = MaskCardNumber(c.CardNumber),
                        cardHolder = c.CardHolder,
                        isDefault = c.IsDefault,
                        createdAt = c.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取银行卡列表失败", error = ex.Message });
            }
        }

        [HttpPost("bank-cards")]
        public async Task<IActionResult> AddBankCard([FromBody] AddBankCardRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CardNumber?.Trim()) || 
                    string.IsNullOrEmpty(request.CardHolder?.Trim()) ||
                    string.IsNullOrEmpty(request.BankName?.Trim()))
                {
                    return BadRequest(new { message = "银行卡信息不完整" });
                }
                
                // 验证银行卡号格式
                if (!IsValidCardNumber(request.CardNumber.Trim()))
                {
                    return BadRequest(new { message = "银行卡号格式不正确" });
                }
                
                var bankCards = await LoadBankCards();
                
                // 检查是否已存在相同银行卡
                if (bankCards.Any(c => c.UserId == request.UserId && c.CardNumber == request.CardNumber.Trim() && !c.IsDeleted))
                {
                    return BadRequest(new { message = "该银行卡已存在" });
                }
                
                // 如果是第一张卡或设置为默认卡，则取消其他默认卡
                var userCards = bankCards.Where(c => c.UserId == request.UserId && !c.IsDeleted).ToList();
                if (userCards.Count == 0 || request.IsDefault)
                {
                    foreach (var card in userCards)
                    {
                        card.IsDefault = false;
                    }
                }
                
                var bankCard = new BankCard
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = request.UserId,
                    BankName = request.BankName.Trim(),
                    CardNumber = request.CardNumber.Trim(),
                    CardHolder = request.CardHolder.Trim(),
                    IsDefault = userCards.Count == 0 || request.IsDefault,
                    CreatedAt = DateTime.Now
                };
                
                bankCards.Add(bankCard);
                await SaveBankCards(bankCards);
                
                return Ok(new
                {
                    message = "银行卡添加成功",
                    card = new
                    {
                        id = bankCard.Id,
                        bankName = bankCard.BankName,
                        cardNumber = MaskCardNumber(bankCard.CardNumber),
                        cardHolder = bankCard.CardHolder,
                        isDefault = bankCard.IsDefault,
                        createdAt = bankCard.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "添加银行卡失败", error = ex.Message });
            }
        }

        [HttpPut("bank-cards/{cardId}/default")]
        public async Task<IActionResult> SetDefaultBankCard(string cardId, [FromBody] SetDefaultCardRequest request)
        {
            try
            {
                var bankCards = await LoadBankCards();
                var card = bankCards.FirstOrDefault(c => c.Id == cardId && c.UserId == request.UserId && !c.IsDeleted);
                
                if (card == null)
                {
                    return NotFound(new { message = "银行卡不存在" });
                }
                
                // 取消其他默认卡
                var userCards = bankCards.Where(c => c.UserId == request.UserId && !c.IsDeleted).ToList();
                foreach (var userCard in userCards)
                {
                    userCard.IsDefault = userCard.Id == cardId;
                }
                
                await SaveBankCards(bankCards);
                
                return Ok(new { message = "默认银行卡设置成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "设置默认银行卡失败", error = ex.Message });
            }
        }

        [HttpDelete("bank-cards/{cardId}")]
        public async Task<IActionResult> DeleteBankCard(string cardId, [FromQuery] string userId)
        {
            try
            {
                var bankCards = await LoadBankCards();
                var card = bankCards.FirstOrDefault(c => c.Id == cardId && c.UserId == userId && !c.IsDeleted);
                
                if (card == null)
                {
                    return NotFound(new { message = "银行卡不存在" });
                }
                
                card.IsDeleted = true;
                
                // 如果删除的是默认卡，设置第一张卡为默认
                if (card.IsDefault)
                {
                    var userCards = bankCards.Where(c => c.UserId == userId && !c.IsDeleted && c.Id != cardId).ToList();
                    if (userCards.Count > 0)
                    {
                        userCards.First().IsDefault = true;
                    }
                }
                
                await SaveBankCards(bankCards);
                
                return Ok(new { message = "银行卡删除成功" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "删除银行卡失败", error = ex.Message });
            }
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateWithdrawOrder([FromBody] CreateWithdrawOrderRequest request)
        {
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "提款金额必须大于0" });
                }
                
                var config = await LoadWithdrawConfig();
                
                if (request.Amount < config.MinAmount || request.Amount > config.MaxAmount)
                {
                    return BadRequest(new { message = $"提款金额必须在{config.MinAmount}-{config.MaxAmount}之间" });
                }
                
                // 检查用户余额
                var userBalance = await GetUserBalance(request.UserId);
                if (userBalance.Available < request.Amount)
                {
                    return BadRequest(new { message = "余额不足" });
                }
                
                // 检查每日提款限额
                var todayWithdraws = await GetTodayWithdraws(request.UserId);
                if (todayWithdraws + request.Amount > config.DailyLimit)
                {
                    return BadRequest(new { message = $"超出每日提款限额{config.DailyLimit}元" });
                }
                
                // 验证银行卡
                var bankCards = await LoadBankCards();
                var bankCard = bankCards.FirstOrDefault(c => c.Id == request.BankCardId && c.UserId == request.UserId && !c.IsDeleted);
                
                if (bankCard == null)
                {
                    return BadRequest(new { message = "银行卡不存在" });
                }
                
                // 计算手续费
                decimal fee = 0;
                if (config.FeeType == "fixed")
                {
                    fee = config.Fee;
                }
                else if (config.FeeType == "percentage")
                {
                    fee = request.Amount * config.Fee / 100;
                }
                
                var actualAmount = request.Amount - fee;
                
                // 冻结用户余额
                await FreezeUserBalance(request.UserId, request.Amount);
                
                var orders = await LoadWithdrawOrders();
                var order = new WithdrawOrder
                {
                    Id = GenerateOrderId(),
                    UserId = request.UserId,
                    BankCardId = request.BankCardId,
                    BankName = bankCard.BankName,
                    CardNumber = bankCard.CardNumber,
                    CardHolder = bankCard.CardHolder,
                    Amount = request.Amount,
                    Fee = fee,
                    ActualAmount = actualAmount,
                    Status = "pending",
                    CreatedAt = DateTime.Now,
                    Remark = request.Remark
                };
                
                orders.Add(order);
                await SaveWithdrawOrders(orders);
                
                // 记录交易
                await RecordTransaction(request.UserId, -request.Amount, "withdraw_freeze", order.Id, $"提款申请冻结：{request.Amount:F2}元");
                
                return Ok(new
                {
                    message = "提款申请提交成功",
                    order = new
                    {
                        id = order.Id,
                        amount = order.Amount,
                        fee = order.Fee,
                        actualAmount = order.ActualAmount,
                        status = order.Status,
                        createdAt = order.CreatedAt,
                        bankInfo = new
                        {
                            bankName = order.BankName,
                            cardNumber = MaskCardNumber(order.CardNumber),
                            cardHolder = order.CardHolder
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "创建提款订单失败", error = ex.Message });
            }
        }

        [HttpGet("orders/{userId}")]
        public async Task<IActionResult> GetUserWithdrawOrders(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null)
        {
            try
            {
                var orders = await LoadWithdrawOrders();
                var userOrders = orders.Where(o => o.UserId == userId);
                
                if (!string.IsNullOrEmpty(status))
                {
                    userOrders = userOrders.Where(o => o.Status == status);
                }
                
                var orderedOrders = userOrders.OrderByDescending(o => o.CreatedAt).ToList();
                
                var totalCount = orderedOrders.Count;
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var pagedOrders = orderedOrders
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                return Ok(new
                {
                    orders = pagedOrders.Select(o => new
                    {
                        id = o.Id,
                        amount = o.Amount,
                        fee = o.Fee,
                        actualAmount = o.ActualAmount,
                        status = o.Status,
                        createdAt = o.CreatedAt,
                        processedAt = o.ProcessedAt,
                        bankInfo = new
                        {
                            bankName = o.BankName,
                            cardNumber = MaskCardNumber(o.CardNumber),
                            cardHolder = o.CardHolder
                        },
                        remark = o.Remark
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
                return StatusCode(500, new { message = "获取提款记录失败", error = ex.Message });
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetWithdrawOrderDetail(string orderId)
        {
            try
            {
                var orders = await LoadWithdrawOrders();
                var order = orders.FirstOrDefault(o => o.Id == orderId);
                
                if (order == null)
                {
                    return NotFound(new { message = "提款订单不存在" });
                }
                
                return Ok(new
                {
                    id = order.Id,
                    userId = order.UserId,
                    amount = order.Amount,
                    fee = order.Fee,
                    actualAmount = order.ActualAmount,
                    status = order.Status,
                    createdAt = order.CreatedAt,
                    processedAt = order.ProcessedAt,
                    bankInfo = new
                    {
                        bankName = order.BankName,
                        cardNumber = order.CardNumber, // 管理员可以看到完整卡号
                        cardHolder = order.CardHolder
                    },
                    remark = order.Remark
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取订单详情失败", error = ex.Message });
            }
        }

        [HttpPost("approve/{orderId}")]
        public async Task<IActionResult> ApproveWithdraw(string orderId, [FromBody] ApproveWithdrawRequest request)
        {
            try
            {
                var orders = await LoadWithdrawOrders();
                var order = orders.FirstOrDefault(o => o.Id == orderId);
                
                if (order == null)
                {
                    return NotFound(new { message = "提款订单不存在" });
                }
                
                if (order.Status != "pending")
                {
                    return BadRequest(new { message = "订单状态不允许审核" });
                }
                
                if (request.Approved)
                {
                    // 审核通过
                    order.Status = "approved";
                    order.ProcessedAt = DateTime.Now;
                    order.Remark = request.Remark ?? "审核通过，正在处理转账";
                    
                    // 扣除冻结余额
                    await DeductFrozenBalance(order.UserId, order.Amount);
                    
                    // 记录交易
                    await RecordTransaction(order.UserId, -order.Amount, "withdraw", order.Id, $"提款成功：{order.ActualAmount:F2}元到账");
                }
                else
                {
                    // 审核拒绝，解冻余额
                    order.Status = "rejected";
                    order.ProcessedAt = DateTime.Now;
                    order.Remark = request.Remark ?? "审核未通过";
                    
                    await UnfreezeUserBalance(order.UserId, order.Amount);
                    
                    // 记录交易
                    await RecordTransaction(order.UserId, order.Amount, "withdraw_unfreeze", order.Id, $"提款申请被拒绝，解冻：{order.Amount:F2}元");
                }
                
                await SaveWithdrawOrders(orders);
                
                return Ok(new
                {
                    message = request.Approved ? "提款审核通过" : "提款审核拒绝",
                    orderId = order.Id,
                    status = order.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "审核提款失败", error = ex.Message });
            }
        }

        [HttpPost("cancel/{orderId}")]
        public async Task<IActionResult> CancelWithdraw(string orderId, [FromBody] CancelWithdrawRequest request)
        {
            try
            {
                var orders = await LoadWithdrawOrders();
                var order = orders.FirstOrDefault(o => o.Id == orderId);
                
                if (order == null)
                {
                    return NotFound(new { message = "提款订单不存在" });
                }
                
                if (order.UserId != request.UserId)
                {
                    return Forbid("无权限操作此订单");
                }
                
                if (order.Status != "pending")
                {
                    return BadRequest(new { message = "只能取消待审核的订单" });
                }
                
                order.Status = "cancelled";
                order.ProcessedAt = DateTime.Now;
                order.Remark = "用户取消";
                
                // 解冻余额
                await UnfreezeUserBalance(order.UserId, order.Amount);
                
                // 记录交易
                await RecordTransaction(order.UserId, order.Amount, "withdraw_unfreeze", order.Id, $"提款申请取消，解冻：{order.Amount:F2}元");
                
                await SaveWithdrawOrders(orders);
                
                return Ok(new
                {
                    message = "提款订单已取消",
                    orderId = order.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "取消提款失败", error = ex.Message });
            }
        }

        [HttpGet("stats/{userId}")]
        public async Task<IActionResult> GetWithdrawStats(string userId)
        {
            try
            {
                var orders = await LoadWithdrawOrders();
                var userOrders = orders.Where(o => o.UserId == userId).ToList();
                
                var totalAmount = userOrders.Where(o => o.Status == "approved").Sum(o => o.ActualAmount);
                var totalCount = userOrders.Count(o => o.Status == "approved");
                var pendingCount = userOrders.Count(o => o.Status == "pending");
                
                var today = DateTime.Today;
                var todayAmount = userOrders
                    .Where(o => o.Status == "approved" && o.ProcessedAt?.Date == today)
                    .Sum(o => o.ActualAmount);
                
                var thisMonth = new DateTime(today.Year, today.Month, 1);
                var monthlyAmount = userOrders
                    .Where(o => o.Status == "approved" && o.ProcessedAt >= thisMonth)
                    .Sum(o => o.ActualAmount);
                
                return Ok(new
                {
                    totalAmount = totalAmount,
                    totalCount = totalCount,
                    pendingCount = pendingCount,
                    todayAmount = todayAmount,
                    monthlyAmount = monthlyAmount,
                    lastWithdrawTime = userOrders
                        .Where(o => o.Status == "approved")
                        .OrderByDescending(o => o.ProcessedAt)
                        .FirstOrDefault()?.ProcessedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "获取提款统计失败", error = ex.Message });
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

        private async Task<WithdrawConfig> LoadWithdrawConfig()
        {
            var filePath = Path.Combine(_dataDirectory, _withdrawConfigFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultConfig = CreateDefaultWithdrawConfig();
                await SaveWithdrawConfig(defaultConfig);
                return defaultConfig;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<WithdrawConfig>(json) ?? CreateDefaultWithdrawConfig();
        }

        private async Task SaveWithdrawConfig(WithdrawConfig config)
        {
            var filePath = Path.Combine(_dataDirectory, _withdrawConfigFile);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<BankCard>> LoadBankCards()
        {
            var filePath = Path.Combine(_dataDirectory, _bankCardsFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultCards = new List<BankCard>();
                await SaveBankCards(defaultCards);
                return defaultCards;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<BankCard>>(json) ?? new List<BankCard>();
        }

        private async Task SaveBankCards(List<BankCard> cards)
        {
            var filePath = Path.Combine(_dataDirectory, _bankCardsFile);
            var json = JsonSerializer.Serialize(cards, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<List<WithdrawOrder>> LoadWithdrawOrders()
        {
            var filePath = Path.Combine(_dataDirectory, _withdrawOrdersFile);
            if (!System.IO.File.Exists(filePath))
            {
                var defaultOrders = new List<WithdrawOrder>();
                await SaveWithdrawOrders(defaultOrders);
                return defaultOrders;
            }
            
            var json = await System.IO.File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<WithdrawOrder>>(json) ?? new List<WithdrawOrder>();
        }

        private async Task SaveWithdrawOrders(List<WithdrawOrder> orders)
        {
            var filePath = Path.Combine(_dataDirectory, _withdrawOrdersFile);
            var json = JsonSerializer.Serialize(orders, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(filePath, json);
        }

        private async Task<UserBalance> GetUserBalance(string userId)
        {
            var balancesFile = Path.Combine(_dataDirectory, _balancesFile);
            var balances = new List<UserBalance>();
            
            if (System.IO.File.Exists(balancesFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(balancesFile);
                balances = JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
            }
            
            return balances.FirstOrDefault(b => b.UserId == userId) ?? new UserBalance
            {
                UserId = userId,
                Available = 0,
                Frozen = 0,
                Total = 0
            };
        }

        private async Task FreezeUserBalance(string userId, decimal amount)
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
            
            userBalance.Available -= amount;
            userBalance.Frozen += amount;
            
            var balanceJson = JsonSerializer.Serialize(balances, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(balancesFile, balanceJson);
        }

        private async Task UnfreezeUserBalance(string userId, decimal amount)
        {
            var balancesFile = Path.Combine(_dataDirectory, _balancesFile);
            var balances = new List<UserBalance>();
            
            if (System.IO.File.Exists(balancesFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(balancesFile);
                balances = JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
            }
            
            var userBalance = balances.FirstOrDefault(b => b.UserId == userId);
            if (userBalance != null)
            {
                userBalance.Available += amount;
                userBalance.Frozen -= amount;
                
                var balanceJson = JsonSerializer.Serialize(balances, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(balancesFile, balanceJson);
            }
        }

        private async Task DeductFrozenBalance(string userId, decimal amount)
        {
            var balancesFile = Path.Combine(_dataDirectory, _balancesFile);
            var balances = new List<UserBalance>();
            
            if (System.IO.File.Exists(balancesFile))
            {
                var json = await System.IO.File.ReadAllTextAsync(balancesFile);
                balances = JsonSerializer.Deserialize<List<UserBalance>>(json) ?? new List<UserBalance>();
            }
            
            var userBalance = balances.FirstOrDefault(b => b.UserId == userId);
            if (userBalance != null)
            {
                userBalance.Frozen -= amount;
                userBalance.Total = userBalance.Available + userBalance.Frozen;
                
                var balanceJson = JsonSerializer.Serialize(balances, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(balancesFile, balanceJson);
            }
        }

        private async Task<decimal> GetTodayWithdraws(string userId)
        {
            var orders = await LoadWithdrawOrders();
            var today = DateTime.Today;
            
            return orders
                .Where(o => o.UserId == userId && o.Status == "approved" && o.ProcessedAt?.Date == today)
                .Sum(o => o.Amount);
        }

        private async Task RecordTransaction(string userId, decimal amount, string type, string orderId, string description)
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
                OrderId = orderId,
                CreateTime = DateTime.Now
            };
            
            transactions.Add(transaction);
            
            var transactionJson = JsonSerializer.Serialize(transactions, new JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(transactionsFile, transactionJson);
        }

        private string GenerateOrderId()
        {
            return "W" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 8)
                return cardNumber;
            
            return cardNumber.Substring(0, 4) + "****" + cardNumber.Substring(cardNumber.Length - 4);
        }

        private bool IsValidCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return false;
            
            // 移除空格和连字符
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");
            
            // 检查是否只包含数字且长度在13-19之间
            return cardNumber.All(char.IsDigit) && cardNumber.Length >= 13 && cardNumber.Length <= 19;
        }

        private WithdrawConfig CreateDefaultWithdrawConfig()
        {
            return new WithdrawConfig
            {
                MinAmount = 100,
                MaxAmount = 50000,
                DailyLimit = 100000,
                Fee = 5,
                FeeType = "fixed",
                WorkingHours = "9:00-18:00（工作日）",
                ProcessingTime = "1-3个工作日",
                Notice = "1. 提款申请提交后，资金将被冻结\n2. 审核通过后1-3个工作日内到账\n3. 请确保银行卡信息正确\n4. 如有疑问，请联系客服",
                Rules = new[]
                {
                    "每日提款限额：100,000元",
                    "单笔提款限额：100-50,000元",
                    "提款手续费：5元/笔",
                    "工作时间：9:00-18:00（工作日）",
                    "到账时间：1-3个工作日",
                    "银行卡必须为本人实名认证"
                }
            };
        }
    }
}