using MobileECommerceAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace MobileECommerceAPI.Services
{
    public class UserService
    {
        private readonly DataService _dataService;
        private readonly JwtService _jwtService;
        private readonly ILogger<UserService> _logger;
        private const string UsersFileName = "users";

        public UserService(DataService dataService, JwtService jwtService, ILogger<UserService> logger)
        {
            _dataService = dataService;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(string fullPhoneNumber, string password)
        {
            try
            {
                var users = await _dataService.LoadUsersAsync();
                
                // 使用完整的手机号码（区号+手机号码）进行精确匹配
                var user = users.FirstOrDefault(u => u.Phone == fullPhoneNumber);
                
                if (user == null)
                {
                    return new LoginResponse { Success = false, Message = "用户不存在或手机号码不匹配" };
                }
                
                if (!VerifyPassword(password, user.Password))
                {
                    return new LoginResponse { Success = false, Message = "密码错误" };
                }
                
                if (!user.IsActive)
                {
                    return new LoginResponse { Success = false, Message = "账户已被禁用" };
                }
                
                user.LastLoginTime = DateTime.Now;
                await _dataService.SaveUsersAsync(users);
                
                var token = GenerateJwtToken(user);
                user.Password = ""; // Don't return password
                
                return new LoginResponse
                {
                    Success = true,
                    Message = "登录成功",
                    Token = token,
                    User = user,
                    PermissionLevel = user.PermissionLevel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for phone {Phone}", fullPhoneNumber);
                return new LoginResponse { Success = false, Message = "登录失败，请稍后重试" };
            }
        }

        public async Task<LoginResponse> RegisterAsync(string fullPhoneNumber, string password, string inviteCode = "")
        {
            try
            {
                var users = await _dataService.LoadUsersAsync();
                
                // Check if user already exists (using full phone number with country code)
                if (users.Any(u => u.Phone == fullPhoneNumber))
                {
                    return new LoginResponse { Success = false, Message = "该手机号已注册" };
                }

                string inviterId = string.Empty;
                
                // Validate invite code if provided
                if (!string.IsNullOrEmpty(inviteCode))
                {
                    // Check in agents (only agents can have invite codes now)
                    var agents = await _dataService.LoadDataAsync<Agent>("agents");
                    var agentInviter = agents.FirstOrDefault(a => a.InviteCode == inviteCode && a.IsActive);
                    if (agentInviter != null)
                    {
                        inviterId = agentInviter.Id;
                    }
                    else
                    {
                        return new LoginResponse { Success = false, Message = "邀请码不存在" };
                    }
                }

                var user = new User
                {
                    Id = Guid.NewGuid().ToString(),
                    Phone = fullPhoneNumber, // 存储完整的手机号码（区号+手机号码）
                    Password = HashPassword(password),
                    InviteCodeUsed = inviteCode ?? string.Empty,
                    InviterId = inviterId,
                    RegisterTime = DateTime.Now,
                    IsActive = true,
                    VipLevel = 0,
                    VipExpireAt = DateTime.MinValue,
                    CurrentBalance = 10.0m, // 新用户默认赠送10余额
                    FrozenAmount = 0.0m,
                    CreditScore = 100
                };

                users.Add(user);
                await _dataService.SaveUsersAsync(users);

                // 如果使用了业务员邀请码，创建客户分配记录
                if (!string.IsNullOrEmpty(inviterId) && !string.IsNullOrEmpty(inviteCode))
                {
                    // 检查是否是业务员邀请码
                    var agents = await _dataService.LoadDataAsync<Agent>("agents");
                    var agent = agents.FirstOrDefault(a => a.Id == inviterId && a.InviteCode == inviteCode);
                    if (agent != null)
                    {
                        // 创建客户分配记录
                        var customerAssignments = await _dataService.LoadDataAsync<CustomerAssignment>("customer_assignments");
                        var assignment = new CustomerAssignment
                        {
                            Id = Guid.NewGuid().ToString(),
                            CustomerId = user.Id,
                            SalespersonId = agent.Id,
                            AssignedAt = DateTime.Now
                        };
                        customerAssignments.Add(assignment);
                        await _dataService.SaveDataAsync(customerAssignments, "customer_assignments");

                        // 更新业务员客户数量
                        agent.CustomerCount++;
                        await _dataService.SaveDataAsync(agents, "agents");
                    }
                }

                var token = GenerateJwtToken(user);
                
                return new LoginResponse 
                { 
                    Success = true, 
                    Message = "注册成功", 
                    Token = token,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for phone {Phone}", fullPhoneNumber);
                return new LoginResponse { Success = false, Message = "注册失败" };
            }
        }

        public async Task<User?> GetUserByIdAsync(string userId)
        {
            try
            {
                var users = await _dataService.LoadDataAsync<User>(UsersFileName);
                var user = users.FirstOrDefault(u => u.Id == userId && u.IsActive);
                if (user != null)
                {
                    user.Password = ""; // Don't return password
                }
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var users = await _dataService.LoadDataAsync<User>(UsersFileName);
                var existingUserIndex = users.FindIndex(u => u.Id == user.Id);
                
                if (existingUserIndex == -1)
                {
                    return false;
                }

                users[existingUserIndex] = user;
                await _dataService.SaveDataAsync(users, UsersFileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user.Id);
                return false;
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "MobileECommerceSalt"));
            return Convert.ToBase64String(hashedBytes);
        }

        private string GenerateInviteCode()
        {
            var random = new Random();
            const string chars = "0123456789";
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private bool IsValidInviteCode(string inviteCode)
        {
            if (string.IsNullOrWhiteSpace(inviteCode))
                return false;

            if (inviteCode.Length != 6)
                return false;

            return inviteCode.All(char.IsLetterOrDigit);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            var inputHash = HashPassword(password);
            return inputHash == hashedPassword;
        }

        private string GenerateJwtToken(User user)
        {
            return _jwtService.GenerateToken(user);
        }
    }
}