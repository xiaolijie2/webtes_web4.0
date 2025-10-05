using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobileECommerceAPI.Models;
using MobileECommerceAPI.Services;
using System.Security.Claims;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(UserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("account-info")]
        public async Task<ActionResult> GetAccountInfo()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "无效的用户令牌" });
                }

                var user = await _userService.GetUserByIdAsync(userIdClaim);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                return Ok(new
                {
                    phone = user.Phone,
                    currentBalance = user.CurrentBalance,
                    frozenAmount = user.FrozenAmount,
                    creditScore = user.CreditScore,
                    inviteCodeUsed = user.InviteCodeUsed,
                    nickName = user.NickName,
                    vipLevel = user.VipLevel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account info");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }

        [HttpGet("balance")]
        public async Task<ActionResult> GetBalance()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "无效的用户令牌" });
                }

                var user = await _userService.GetUserByIdAsync(userIdClaim);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                return Ok(new
                {
                    currentBalance = user.CurrentBalance,
                    frozenAmount = user.FrozenAmount,
                    availableBalance = user.CurrentBalance - user.FrozenAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user balance");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }

        [HttpGet("invite-code")]
        public async Task<ActionResult> GetInviteCode()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "无效的用户令牌" });
                }

                var user = await _userService.GetUserByIdAsync(userIdClaim);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                return Ok(new
                {
                    inviteCodeUsed = user.InviteCodeUsed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invite code");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }

        [HttpGet("profile")]
        public async Task<ActionResult> GetUserProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "无效的用户令牌" });
                }

                var user = await _userService.GetUserByIdAsync(userIdClaim);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                return Ok(new
                {
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    country = user.Country,
                    city = user.City,
                    state = user.State,
                    address = user.Address,
                    zipCode = user.ZipCode,
                    avatar = user.Avatar
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }

        [HttpPost("profile")]
        public async Task<ActionResult> UpdateUserProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "无效的用户令牌" });
                }

                var user = await _userService.GetUserByIdAsync(userIdClaim);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                // 更新用户资料
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;
                user.Country = request.Country;
                user.City = request.City;
                user.State = request.State;
                user.Address = request.Address;
                user.ZipCode = request.ZipCode;
                user.Avatar = request.Avatar;

                await _userService.UpdateUserAsync(user);

                return Ok(new { message = "个人资料更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "服务器内部错误" });
            }
        }
    }

    // 个人资料请求模型
    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }
}