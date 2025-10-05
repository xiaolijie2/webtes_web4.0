using Microsoft.AspNetCore.Mvc;
using MobileECommerceAPI.Models;
using MobileECommerceAPI.Services;

namespace TaskPlatform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogoController : ControllerBase
    {
        private readonly LogoService _logoService;
        private readonly ILogger<LogoController> _logger;

        public LogoController(LogoService logoService, ILogger<LogoController> logger)
        {
            _logoService = logoService;
            _logger = logger;
        }

        [HttpGet("current")]
        public async Task<ActionResult<LogoResponse>> GetCurrentLogo()
        {
            try
            {
                var result = await _logoService.GetCurrentLogoAsync();
                
                if (result.Success)
                {
                    return Ok(result.Logo);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current logo");
                return StatusCode(500, new LogoResponse { Success = false, Message = "服务器内部错误" });
            }
        }

        [HttpPost("update")]
        public async Task<ActionResult<LogoResponse>> UpdateLogo([FromBody] LogoRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Type))
                {
                    return BadRequest(new LogoResponse { Success = false, Message = "LOGO类型不能为空" });
                }

                if (request.Type == "text" && string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest(new LogoResponse { Success = false, Message = "文字LOGO的文本内容不能为空" });
                }

                if (request.Type == "image" && string.IsNullOrEmpty(request.ImageUrl))
                {
                    return BadRequest(new LogoResponse { Success = false, Message = "图片LOGO的图片地址不能为空" });
                }

                if (request.Type == "combined" && (string.IsNullOrEmpty(request.Text) || string.IsNullOrEmpty(request.ImageUrl)))
                {
                    return BadRequest(new LogoResponse { Success = false, Message = "组合LOGO的文本和图片都不能为空" });
                }

                // 验证尺寸范围
                if (request.Width < 50 || request.Width > 300 || request.Height < 20 || request.Height > 100)
                {
                    return BadRequest(new LogoResponse { Success = false, Message = "LOGO尺寸超出允许范围" });
                }

                // 验证字体大小范围
                if (request.FontSize < 12 || request.FontSize > 48)
                {
                    return BadRequest(new LogoResponse { Success = false, Message = "字体大小必须在12-48px之间" });
                }

                // 验证字体粗细范围
                if (request.FontWeight < 100 || request.FontWeight > 900)
                {
                    return BadRequest(new LogoResponse { Success = false, Message = "字体粗细必须在100-900之间" });
                }

                var result = await _logoService.UpdateLogoAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating logo");
                return StatusCode(500, new LogoResponse { Success = false, Message = "服务器内部错误" });
            }
        }

        [HttpGet("fonts")]
        public async Task<ActionResult<List<FontInfo>>> GetAvailableFonts()
        {
            try
            {
                var fonts = await _logoService.GetAvailableFontsAsync();
                return Ok(fonts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available fonts");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<Logo>>> GetLogoHistory()
        {
            try
            {
                var history = await _logoService.GetLogoHistoryAsync();
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logo history");
                return StatusCode(500, "服务器内部错误");
            }
        }

        [HttpGet("preview")]
        public async Task<ActionResult> PreviewLogo([FromQuery] string type, [FromQuery] string? text, 
            [FromQuery] string? imageUrl, [FromQuery] string fontFamily = "Arial", 
            [FromQuery] int fontSize = 24, [FromQuery] string color = "#007AFF", 
            [FromQuery] int fontWeight = 700, [FromQuery] string layout = "left-right")
        {
            try
            {
                var previewData = new
                {
                    type,
                    text,
                    imageUrl,
                    fontFamily,
                    fontSize,
                    color,
                    fontWeight,
                    layout
                };

                return Ok(previewData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating logo preview");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}