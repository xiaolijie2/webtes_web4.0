using System.Security.Cryptography;
using System.Text;

namespace MobileECommerceAPI.Services
{
    /// <summary>
    /// 统一的密码处理服务
    /// 提供密码哈希和验证功能，确保整个系统使用一致的密码加密方式
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// 对密码进行哈希加密
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <returns>加密后的密码</returns>
        string HashPassword(string password);

        /// <summary>
        /// 验证密码是否正确
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <param name="hashedPassword">存储的加密密码</param>
        /// <returns>密码是否匹配</returns>
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class PasswordService : IPasswordService
    {
        private const string SALT = "MobileECommerceSalt";

        /// <summary>
        /// 使用SHA256算法对密码进行哈希加密
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <returns>Base64编码的哈希密码</returns>
        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("密码不能为空", nameof(password));

            using var sha256 = SHA256.Create();
            var saltedPassword = password + SALT;
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// 验证明文密码与存储的哈希密码是否匹配
        /// </summary>
        /// <param name="password">明文密码</param>
        /// <param name="hashedPassword">存储的哈希密码</param>
        /// <returns>是否匹配</returns>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            var inputHash = HashPassword(password);
            return inputHash == hashedPassword;
        }
    }
}