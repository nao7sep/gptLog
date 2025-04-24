using System;

namespace gptLog.App.Model
{
    public class AppSettings
    {
        public string FontFamily { get; set; } = FontDefaults.DefaultFontFamily;
        public int FontSize { get; set; } = FontDefaults.DefaultFontSize;
        public int TitleFontSize { get; set; } = FontDefaults.DefaultTitleFontSize;
    }
}