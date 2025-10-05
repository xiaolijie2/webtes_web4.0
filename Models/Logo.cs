namespace MobileECommerceAPI.Models
{
    public class Logo
    {
        public int Id { get; set; }
        public string Type { get; set; } = "text"; // text, image, combined
        public string Text { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string FontFamily { get; set; } = "Arial";
        public int FontSize { get; set; } = 24;
        public string Color { get; set; } = "#007AFF";
        public int FontWeight { get; set; } = 700;
        public string TextEffect { get; set; } = "none"; // none, shadow, stroke, gradient
        public string Layout { get; set; } = "left-right"; // left-right, top-bottom, right-left, bottom-top
        public int Spacing { get; set; } = 8;
        public string Alignment { get; set; } = "center"; // left, center, right
        public int Width { get; set; } = 150;
        public int Height { get; set; } = 50;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime UpdatedTime { get; set; } = DateTime.Now;
    }

    public class LogoRequest
    {
        public string Type { get; set; } = "text";
        public string Text { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string FontFamily { get; set; } = "Arial";
        public int FontSize { get; set; } = 24;
        public string Color { get; set; } = "#007AFF";
        public int FontWeight { get; set; } = 700;
        public string TextEffect { get; set; } = "none";
        public string Layout { get; set; } = "left-right";
        public int Spacing { get; set; } = 8;
        public string Alignment { get; set; } = "center";
        public int Width { get; set; } = 150;
        public int Height { get; set; } = 50;
        public string Name { get; set; } = string.Empty;
    }

    public class LogoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Logo? Logo { get; set; }
    }

    public class FontInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // chinese, english, artistic
        public bool IsAvailable { get; set; } = true;
    }
}