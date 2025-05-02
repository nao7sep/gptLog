﻿using System;

namespace gptLogApp.Model
{
    public class AppSettings
    {
        public string FontFamily { get; set; } = ApplicationDefaults.DefaultFontFamily;
        public int FontSize { get; set; } = ApplicationDefaults.DefaultFontSize;
        public int TitleFontSize { get; set; } = ApplicationDefaults.DefaultTitleFontSize;
    }
}