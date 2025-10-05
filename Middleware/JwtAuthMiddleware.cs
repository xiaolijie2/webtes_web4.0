using MobileECommerceAPI.Services;
using System.IdentityModel.Tokens.Jwt;

namespace MobileECommerceAPI.Middleware
{
    public class JwtAuthMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JwtAuthMiddleware> _logger;

        // 需要认证的页面列表
        private readonly HashSet<string> _protectedPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/home.html",
            "/account.html",
            "/orders.html",
            "/recharge.html",
            "/withdraw.html",
            "/service.html",
            "/invite.html",
            "/start.html",
            "/order.html"
        };

        // 公开页面列表（不需要认证）
        private readonly HashSet<string> _publicPages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/login.html",
            "/register.html",
            "/admin.html"
        };

        public JwtAuthMiddleware(RequestDelegate next, IServiceProvider serviceProvider, ILogger<JwtAuthMiddleware> logger)
        {
            _next = next;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // 如果是API请求，跳过此中间件
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // 如果是静态资源（CSS、JS、图片等），跳过此中间件
            if (IsStaticResource(path))
            {
                await _next(context);
                return;
            }

            // 如果是公开页面，允许访问
            if (_publicPages.Contains(path))
            {
                await _next(context);
                return;
            }

            // 如果是受保护的页面，检查认证状态
            if (_protectedPages.Contains(path))
            {
                if (!IsAuthenticated(context))
                {
                    // 未认证，重定向到登录页面
                    context.Response.Redirect("/login.html");
                    return;
                }
            }

            // 处理根路径访问
            if (path == "/" || path == "")
            {
                if (!IsAuthenticated(context))
                {
                    // 未认证，重定向到登录页面
                    context.Response.Redirect("/login.html");
                    return;
                }
                else
                {
                    // 已认证，重定向到首页
                    context.Response.Redirect("/home.html");
                    return;
                }
            }

            await _next(context);
        }

        private bool IsAuthenticated(HttpContext context)
        {
            try
            {
                // 从Cookie中获取token
                var token = context.Request.Cookies["userToken"];
                
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                // 创建scope来获取JwtService
                using var scope = _serviceProvider.CreateScope();
                var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();

                // 验证token
                var principal = jwtService.ValidateToken(token);
                if (principal != null)
                {
                    context.User = principal;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        private bool IsStaticResource(string path)
        {
            var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
            return staticExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}