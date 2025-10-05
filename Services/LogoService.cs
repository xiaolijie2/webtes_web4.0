using MobileECommerceAPI.Models;

namespace MobileECommerceAPI.Services
{
    public class LogoService
    {
        private readonly DataService _dataService;
        private readonly ILogger<LogoService> _logger;
        private const string LogosFileName = "logos";
        private const string FontsFileName = "fonts";

        public LogoService(DataService dataService, ILogger<LogoService> logger)
        {
            _dataService = dataService;
            _logger = logger;
        }

        public async Task<LogoResponse> GetCurrentLogoAsync()
        {
            try
            {
                var logos = await _dataService.LoadDataAsync<Logo>(LogosFileName);
                var currentLogo = logos.FirstOrDefault(l => l.IsActive);

                if (currentLogo == null)
                {
                    // 返回默认LOGO
                    currentLogo = new Logo
                    {
                        Id = 1,
                        Type = "text",
                        Text = "SheIn",
                        FontFamily = "Arial",
                        FontSize = 24,
                        Color = "#007AFF",
                        FontWeight = 700,
                        Width = 150,
                        Height = 50,
                        IsActive = true
                    };
                }

                return new LogoResponse
                {
                    Success = true,
                    Message = "获取LOGO成功",
                    Logo = currentLogo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current logo");
                return new LogoResponse { Success = false, Message = "获取LOGO失败" };
            }
        }

        public async Task<LogoResponse> UpdateLogoAsync(LogoRequest request)
        {
            try
            {
                var logos = await _dataService.LoadDataAsync<Logo>(LogosFileName);
                
                // 将所有现有LOGO设为非活跃状态
                foreach (var logo in logos)
                {
                    logo.IsActive = false;
                }

                var logoId = await _dataService.GetNextIdAsync<Logo>(LogosFileName, l => l.Id);
                
                var newLogo = new Logo
                {
                    Id = logoId,
                    Type = request.Type,
                    Text = request.Text,
                    ImageUrl = request.ImageUrl,
                    FontFamily = request.FontFamily,
                    FontSize = request.FontSize,
                    Color = request.Color,
                    FontWeight = request.FontWeight,
                    TextEffect = request.TextEffect,
                    Layout = request.Layout,
                    Spacing = request.Spacing,
                    Alignment = request.Alignment,
                    Width = request.Width,
                    Height = request.Height,
                    IsActive = true,
                    CreatedTime = DateTime.Now,
                    UpdatedTime = DateTime.Now
                };

                logos.Add(newLogo);
                await _dataService.SaveDataAsync(logos, LogosFileName);

                return new LogoResponse
                {
                    Success = true,
                    Message = "LOGO更新成功",
                    Logo = newLogo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating logo");
                return new LogoResponse { Success = false, Message = "LOGO更新失败" };
            }
        }

        public async Task<List<FontInfo>> GetAvailableFontsAsync()
        {
            try
            {
                var fonts = await _dataService.LoadDataAsync<FontInfo>(FontsFileName);
                
                if (!fonts.Any())
                {
                    // 初始化默认字体库
                    fonts = GetDefaultFonts();
                    await _dataService.SaveDataAsync(fonts, FontsFileName);
                }

                return fonts.Where(f => f.IsAvailable).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available fonts");
                return GetDefaultFonts();
            }
        }

        public async Task<List<Logo>> GetLogoHistoryAsync()
        {
            try
            {
                var logos = await _dataService.LoadDataAsync<Logo>(LogosFileName);
                return logos.OrderByDescending(l => l.UpdatedTime).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logo history");
                return new List<Logo>();
            }
        }

        private List<FontInfo> GetDefaultFonts()
        {
            return new List<FontInfo>
            {
                // 中文字体
                new FontInfo { Name = "SimSun", DisplayName = "宋体", Category = "chinese", IsAvailable = true },
                new FontInfo { Name = "SimHei", DisplayName = "黑体", Category = "chinese", IsAvailable = true },
                new FontInfo { Name = "Microsoft YaHei", DisplayName = "微软雅黑", Category = "chinese", IsAvailable = true },
                new FontInfo { Name = "KaiTi", DisplayName = "楷体", Category = "chinese", IsAvailable = true },
                new FontInfo { Name = "FangSong", DisplayName = "仿宋", Category = "chinese", IsAvailable = true },
                new FontInfo { Name = "LiSu", DisplayName = "隶书", Category = "chinese", IsAvailable = true },
                new FontInfo { Name = "YouYuan", DisplayName = "幼圆", Category = "chinese", IsAvailable = true },
                new FontInfo { Name = "STXihei", DisplayName = "华文细黑", Category = "chinese", IsAvailable = true },
                
                // 英文字体
                new FontInfo { Name = "Arial", DisplayName = "Arial", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Helvetica", DisplayName = "Helvetica", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Times New Roman", DisplayName = "Times New Roman", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Georgia", DisplayName = "Georgia", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Verdana", DisplayName = "Verdana", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Trebuchet MS", DisplayName = "Trebuchet MS", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Courier New", DisplayName = "Courier New", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Impact", DisplayName = "Impact", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Comic Sans MS", DisplayName = "Comic Sans MS", Category = "english", IsAvailable = true },
                new FontInfo { Name = "Tahoma", DisplayName = "Tahoma", Category = "english", IsAvailable = true },
                
                // 艺术字体（中英文兼容）
                new FontInfo { Name = "Brush Script MT", DisplayName = "毛笔字体", Category = "artistic", IsAvailable = true },
                new FontInfo { Name = "Lucida Handwriting", DisplayName = "手写体", Category = "artistic", IsAvailable = true },
                new FontInfo { Name = "Chiller", DisplayName = "恐怖字体", Category = "artistic", IsAvailable = true },
                new FontInfo { Name = "Jokerman", DisplayName = "小丑字体", Category = "artistic", IsAvailable = true }
            };
        }
    }
}