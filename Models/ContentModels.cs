using System.ComponentModel.DataAnnotations;

namespace MobileECommerceAPI.Models
{
    // 幻灯片模型
    public class Slideshow
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Image { get; set; } = string.Empty;
        public string? Link { get; set; }
        public int Order { get; set; } = 1;
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime UploadTime { get; set; } = DateTime.Now;
    }



    // LOGO模型
    public class LogoItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "text"; // text, image
        public string? Text { get; set; }
        public string? ImageUrl { get; set; }
        public string? FontFamily { get; set; }
        public int FontSize { get; set; } = 16;
        public string Color { get; set; } = "#000000";
        public string Name { get; set; } = string.Empty;
        public int Width { get; set; } = 150;
        public int Height { get; set; } = 50;
        public DateTime CreateTime { get; set; } = DateTime.Now;
    }

    // 请求模型
    public class SlideshowRequest
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Image { get; set; } = string.Empty;
        public string? Link { get; set; }
        public int Order { get; set; } = 1;
    }

    public class SlideshowUpdateRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Link { get; set; }
    }

}