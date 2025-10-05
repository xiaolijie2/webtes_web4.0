using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MobileECommerceAPI.Models
{
    public class Transaction
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // recharge, withdraw, commission
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = string.Empty; // pending, completed, failed
        public DateTime CreateTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Description { get; set; }
        public string? OrderId { get; set; }
        public string? RelatedId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? BankInfo { get; set; }
    }

    public class AvailableOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }
        public string Platform { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpireTime { get; set; }
    }

    public class UserOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string OriginalOrderId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // pending, processing, completed, expired
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? SubmitContent { get; set; }
    }

    public class VipInfo
    {
        public int Level { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public int DailyOrderLimit { get; set; }
        public decimal BonusRate { get; set; }
    }

    public class TodayStats
    {
        public int GrabbedCount { get; set; }
        public int TakenCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal TodayEarnings { get; set; }
    }

    public class GrabOrderRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class TakeOrderRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    // User related models
    public class User
    {
        public string Id { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string NickName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal FrozenAmount { get; set; }
        public int CreditScore { get; set; } = 100;
        public int VipLevel { get; set; } = 1;
        public DateTime? VipExpireAt { get; set; }
        public string InviteCodeUsed { get; set; } = string.Empty;
        public string? InviterId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; } = false;
        public DateTime RegisterTime { get; set; }
        public DateTime LastLoginTime { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public bool PhoneVerified { get; set; } = false;
        public int BankCardCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        
        // 管理员相关字段
        public string UserType { get; set; } = "user"; // user, admin, super_admin
        public string Status { get; set; } = "active"; // active, inactive
        public int PermissionLevel { get; set; } = 3; // 权限等级：0-超级管理员，1-管理员，2-业务员，3-注册用户
        
        // 个人资料相关字段
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    public class UserBalance
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Available { get; set; }
        public decimal Frozen { get; set; }
        public decimal Total { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class UserStats
    {
        public string UserId { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public decimal TotalEarnings { get; set; }
        public int InviteCount { get; set; }
        public int LoginDays { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }

    // Order related models
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // pending, processing, completed, cancelled
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? CompletedTime { get; set; }
        
        // 订单池相关字段
        public string CreatedBy { get; set; } = string.Empty; // 创建者（业务员）ID
        public string CreatedByName { get; set; } = string.Empty; // 创建者（业务员）名称
        public string AssignedTo { get; set; } = string.Empty; // 分配给的客户ID
        public string AssignedToName { get; set; } = string.Empty; // 分配给的客户名称
        public bool IsAssigned { get; set; } = false; // 是否已分配
        public DateTime? AssignedTime { get; set; } // 分配时间
        public string AssignedBy { get; set; } = string.Empty; // 分配操作者（管理员）ID
    }

    public class UserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int VipLevel { get; set; }
        public DateTime? VipExpireTime { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class UpdateBalanceRequest
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty; // add, subtract
        public string Reason { get; set; } = string.Empty;
    }

    public class OrderRecord
    {
        public string OrderId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // task, recharge, withdraw, bonus
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty; // pending, processing, completed, failed, cancelled
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class OrderConfig
    {
        public decimal TaskRewardRate { get; set; } = 0.1m;
        public decimal MinTaskAmount { get; set; } = 10m;
        public decimal MaxTaskAmount { get; set; } = 1000m;
        public int AutoCompleteTime { get; set; } = 300;
        public bool AllowCancel { get; set; } = true;
        public bool RequireApproval { get; set; } = false;
    }

    public class CreateOrderRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class BatchProcessRequest
    {
        public List<string> OrderIds { get; set; } = new List<string>();
        public string Action { get; set; } = string.Empty;
    }

    public class AssignOrderRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty; // 管理员ID
    }

    public class OrderPoolResponse
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public string AssignedToName { get; set; } = string.Empty;
        public DateTime? AssignedTime { get; set; }
        public string Platform { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class VipConfig
    {
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Duration { get; set; } // 天数
        public int TaskBonus { get; set; } // 百分比
        public decimal WithdrawFee { get; set; } // 百分比
        public int DailyTaskLimit { get; set; }
        public bool PrioritySupport { get; set; }
        public List<string> Benefits { get; set; } = new List<string>();
    }

    public class VipOrder
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int FromLevel { get; set; }
        public int ToLevel { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class VipUpgradeRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int TargetLevel { get; set; }
    }

    public class VipLevelUpdateRequest
    {
        public decimal MinDeposit { get; set; }
        public int DailyOrderLimit { get; set; }
        public decimal CommissionRate { get; set; }
        public string[] Benefits { get; set; } = Array.Empty<string>();
    }

    public class WithdrawConfig
    {
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal DailyLimit { get; set; }
        public decimal Fee { get; set; }
        public string FeeType { get; set; } = string.Empty; // fixed, percentage
        public string WorkingHours { get; set; } = string.Empty;
        public string ProcessingTime { get; set; } = string.Empty;
        public string Notice { get; set; } = string.Empty;
        public string[] Rules { get; set; } = Array.Empty<string>();
    }

    public class BankCard
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolder { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WithdrawOrder
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string BankCardId { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolder { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal ActualAmount { get; set; }
        public string Status { get; set; } = string.Empty; // pending, approved, rejected, cancelled
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class AddBankCardRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public string CardHolder { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class SetDefaultCardRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class CreateWithdrawOrderRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string BankCardId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class ApproveWithdrawRequest
    {
        public bool Approved { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class CancelWithdrawRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class InviteConfig
    {
        public InviteLevel[] Levels { get; set; } = Array.Empty<InviteLevel>();
        public string[] Rules { get; set; } = Array.Empty<string>();
        public string Notice { get; set; } = string.Empty;
    }

    public class InviteLevel
    {
        public int Level { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Reward { get; set; }
        public decimal Commission { get; set; }
        public int Requirement { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class InviteRecord
    {
        public string Id { get; set; } = string.Empty;
        public string InviterId { get; set; } = string.Empty;
        public string InviteeId { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public decimal Reward { get; set; }
        public bool IsValid { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ValidatedAt { get; set; }
    }

    public class InviteReward
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // invite_reward, commission, level_upgrade
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string RelatedId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RegisterWithInviteRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
    }

    public class GiveCommissionRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class RechargeConfig
    {
        public List<RechargeMethod> Methods { get; set; } = new List<RechargeMethod>();
        public int[] QuickAmounts { get; set; } = Array.Empty<int>();
        public string Notice { get; set; } = string.Empty;
        public string CustomerService { get; set; } = string.Empty;
    }

    public class RechargeMethod
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal Fee { get; set; }
        public string FeeType { get; set; } = string.Empty; // fixed, percentage
        public bool IsEnabled { get; set; }
        public string Description { get; set; } = string.Empty;
        public string[] Instructions { get; set; } = Array.Empty<string>();
    }

    public class RechargeOrder
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string MethodId { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal ActualAmount { get; set; }
        public string Status { get; set; } = string.Empty; // pending, processing, completed, rejected, cancelled, expired
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime ExpiredAt { get; set; }
        public PaymentInfo PaymentInfo { get; set; } = new PaymentInfo();
        public string PaymentProof { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }

    public class PaymentInfo
    {
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string[] Instructions { get; set; } = Array.Empty<string>();
    }

    public class CreateRechargeOrderRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string MethodId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class ConfirmPaymentRequest
    {
        public string PaymentProof { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
    }

    public class ApproveRechargeRequest
    {
        public bool Approved { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "+86";
        public string FullPhoneNumber { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Phone { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "+86";
        public string InviteCode { get; set; } = string.Empty;
        public string FullPhoneNumber { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public User? User { get; set; }
        public int PermissionLevel { get; set; } = 3; // 默认为普通用户权限
    }

    // 业务员管理相关模型
    public class Agent
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonPropertyName("nickName")]
        public string NickName { get; set; } = string.Empty;
        
        [JsonPropertyName("account")]
        public string Account { get; set; } = string.Empty;
        
        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;
        
        [JsonPropertyName("inviteCode")]
        public string InviteCode { get; set; } = string.Empty;
        
        [JsonPropertyName("customerCount")]
        public int CustomerCount { get; set; } = 0;
        
        [JsonPropertyName("monthlyPerformance")]
        public decimal MonthlyPerformance { get; set; } = 0;
        
        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; } = true;
        
        [JsonPropertyName("registerTime")]
        public DateTime RegisterTime { get; set; }
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateAgentRequest
    {
        public string NickName { get; set; } = string.Empty;
        public string Account { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
    }

    public class AgentInviteCode
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string AgentNickName { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public int UsedCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class GenerateInviteCodeRequest
    {
        public string AgentId { get; set; } = string.Empty;
    }

    public class UpdateAgentStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class UserAgentMapping
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string InviteCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TransferCustomerRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string FromAgentId { get; set; } = string.Empty;
        public string ToAgentId { get; set; } = string.Empty;
    }

    // 客户分配相关模型
    public class CustomerAssignment
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string SalespersonId { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }

    // 聊天记录相关模型
    public class ChatMessage
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsFromUser { get; set; } = true;
        public string MessageType { get; set; } = "text";
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = "sent"; // sent, received, read
    }

    // 管理员登录请求模型
    public class AdminLoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    // 超级管理员相关模型
    public class SuperAdmin
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserType { get; set; } = "super_admin";
        public int PermissionLevel { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public SuperAdminSecuritySettings SecuritySettings { get; set; } = new SuperAdminSecuritySettings();
    }

    public class SuperAdminSecuritySettings
    {
        public bool PasswordExpires { get; set; } = false;
        public bool TwoFactorEnabled { get; set; } = false;
        public int LoginAttemptsLimit { get; set; } = 5;
    }

    public class SuperAdminData
    {
        public List<SuperAdmin> SuperAdmins { get; set; } = new List<SuperAdmin>();
        public SuperAdminMetadata Metadata { get; set; } = new SuperAdminMetadata();
    }

    public class SuperAdminMetadata
    {
        public string Version { get; set; } = "1.0";
        public DateTime LastUpdated { get; set; }
        public int TotalCount { get; set; }
    }

    public class SuperAdminLoginRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class SuperAdminLoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public SuperAdminInfo? SuperAdmin { get; set; }
    }

    public class SuperAdminInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string UserType { get; set; } = "super_admin";
        public int PermissionLevel { get; set; } = 0;
        public DateTime? LastLogin { get; set; }
    }

    public class SuperAdminVerifyResponse
    {
        public bool Valid { get; set; }
        public SuperAdminInfo? SuperAdmin { get; set; }
    }
}