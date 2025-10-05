namespace MobileECommerceAPI.Models
{
    public class VipLevel
    {
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MinAmount { get; set; }
        public int DailyTaskLimit { get; set; }
        public decimal CommissionRate { get; set; }
        public List<string> Benefits { get; set; } = new List<string>();
        public string Color { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    public class VipBenefit
    {
        public int Id { get; set; }
        public int VipLevel { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class SubscriptionDescription
    {
        public int Id { get; set; }
        public int VipLevel { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime UpdateTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}