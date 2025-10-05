using System.ComponentModel.DataAnnotations;

namespace MobileECommerceAPI.Models
{
    public class PaymentAccount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [StringLength(100)]
        public string WalletName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(10)]
        public string AccountType { get; set; } = string.Empty; // 数字货币类型 (USDT, BTC, ETH, TRX, BNB)
        
        [Required]
        [StringLength(20)]
        public string NetworkType { get; set; } = string.Empty; // 网络类型 (TRC20, ERC20, BEP20, etc.)
        
        [Required]
        [StringLength(500)]
        public string AccountNumber { get; set; } = string.Empty; // 钱包地址
        
        [StringLength(100)]
        public string AccountIdentifier { get; set; } = string.Empty; // 账户标识符
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "enabled"; // enabled/disabled
        
        public bool IsDefault { get; set; } = false; // 是否为默认账户
        
        [StringLength(500)]
        public string Remarks { get; set; } = string.Empty; // 备注
        
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdateTime { get; set; } = DateTime.UtcNow;
        
        // 兼容性属性 - 保持与现有代码的兼容性
        public string CurrencyType 
        { 
            get => AccountType; 
            set => AccountType = value; 
        }
        
        public string CurrencyName 
        { 
            get => AccountType; 
            set => AccountType = value; 
        }
        
        public string Address 
        { 
            get => AccountNumber; 
            set => AccountNumber = value; 
        }
        
        public bool IsActive 
        { 
            get => Status == "enabled"; 
            set => Status = value ? "enabled" : "disabled"; 
        }
    }

    public class PaymentAccountRequest
    {
        [Required]
        [StringLength(100)]
        public string WalletName { get; set; } = string.Empty;
        
        [Required]
        public string AccountType { get; set; } = string.Empty;
        
        [Required]
        public string NetworkType { get; set; } = string.Empty;
        
        [Required]
        public string AccountNumber { get; set; } = string.Empty;
        
        public string AccountIdentifier { get; set; } = string.Empty;
        
        [Required]
        public string Status { get; set; } = "enabled";
        
        public bool IsDefault { get; set; } = false;
        
        public string Remarks { get; set; } = string.Empty;
        
        // 兼容性属性
        public string CurrencyType 
        { 
            get => AccountType; 
            set => AccountType = value; 
        }
        
        public string CurrencyName 
        { 
            get => AccountType; 
            set => AccountType = value; 
        }
        
        public string Address 
        { 
            get => AccountNumber; 
            set => AccountNumber = value; 
        }
        
        public bool IsActive 
        { 
            get => Status == "enabled"; 
            set => Status = value ? "enabled" : "disabled"; 
        }
    }

    public class PaymentAccountResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}