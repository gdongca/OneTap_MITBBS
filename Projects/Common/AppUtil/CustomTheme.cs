using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Xml.Serialization;

namespace Naboo.AppUtil
{
    public class CustomTheme
    {
        public enum CustomThemeType
        {
            DefaultTheme,
            DarkTheme,
            LightTheme,
        }

        public enum CustomFontSize
        {
            Small,
            Medium,
            Large,
        }

        [XmlIgnore]
        public CustomThemeType SystemThemeType = CustomThemeType.DarkTheme;

        public CustomThemeType ThemeType
        {
            get
            {
                return _themeType;
            }

            set
            {
                if (value != _themeType)
                {
                    _themeType = value;

                    ApplyDefaultTheme();

                    switch (_themeType)
                    {
                        case CustomThemeType.DefaultTheme:
                            break;

                        case CustomThemeType.DarkTheme:
                            ApplyDarkTheme();
                            break;

                        case CustomThemeType.LightTheme:
                            ApplyLightTheme();
                            break;
                    }
                }
            }
        }

        public CustomFontSize FontSize
        {
            get
            {
                return _fontSize;
            }

            set
            {
                if (value != _fontSize)
                {
                    _fontSize = value;

                    switch (_fontSize)
                    {
                        case CustomFontSize.Small:
                            ApplySmallFontSize();
                            break;
                        case CustomFontSize.Medium:
                            ApplyMediumFontSize();
                            break;
                        case CustomFontSize.Large:
                            ApplyLargeFontSize();
                            break;
                    }
                }
            }
        }
        
        protected CustomThemeType _themeType = CustomThemeType.DefaultTheme;
        protected CustomFontSize _fontSize = CustomFontSize.Medium;
        protected bool _themeApplied = false;
        protected ResourceDictionary _appResource;

        public CustomTheme(ResourceDictionary appResource)
        {
            _appResource = appResource;
            SaveDefaultTheme();
        }

        public void ApplyThemeToPage(PhoneApplicationPage page, Panel layoutRoot)
        {
            switch (ThemeType)
            {
                case CustomThemeType.DefaultTheme:
                    page.Background = _appResource["TransparentBrush"] as SolidColorBrush;
                    layoutRoot.Background = _appResource["TransparentBrush"] as SolidColorBrush;
                    break;

                case CustomThemeType.DarkTheme:
                case CustomThemeType.LightTheme:
                    page.Background = _appResource["PhoneBackgroundBrush"] as SolidColorBrush;
                    layoutRoot.Background = _appResource["PhoneBackgroundBrush"] as SolidColorBrush;
                    break;
            }

            if (page.ApplicationBar != null && (_themeApplied || (ThemeType != CustomThemeType.DefaultTheme)))
            {
                page.ApplicationBar.BackgroundColor = (_appResource["PhoneChromeBrush"] as SolidColorBrush).Color;
                page.ApplicationBar.ForegroundColor = (_appResource["PhoneForegroundBrush"] as SolidColorBrush).Color;
            }
        }

        protected virtual void ApplyDefaultTheme()
        {
            if (_themeApplied)
            {
                (_appResource["PhoneForegroundBrush"] as SolidColorBrush).Color = _defaultForegroundColor;
                (_appResource["PhoneBackgroundBrush"] as SolidColorBrush).Color = _defaultBackgroundColor;

                (_appResource["PhoneContrastForegroundBrush"] as SolidColorBrush).Color = _defaultContrastForegroundColor;
                (_appResource["PhoneContrastBackgroundBrush"] as SolidColorBrush).Color = _defaultContrastBackgroundColor;

                (_appResource["PhoneInactiveBrush"] as SolidColorBrush).Color = _defaultInactiveColor;
                (_appResource["PhoneBorderBrush"] as SolidColorBrush).Color = _defaultBorderColor;
                (_appResource["PhoneDisabledBrush"] as SolidColorBrush).Color = _defaultDisabledColor;
                (_appResource["PhoneSubtleBrush"] as SolidColorBrush).Color = _defaultSubtleColor;
                (_appResource["PhoneSemitransparentBrush"] as SolidColorBrush).Color = _defaultSemitransparentColor;
                (_appResource["PhoneInverseInactiveBrush"] as SolidColorBrush).Color = _defaultInverseInactiveColor;
                (_appResource["PhoneInverseBackgroundBrush"] as SolidColorBrush).Color = _defaultInverseBackgroundColor;
                (_appResource["PhoneChromeBrush"] as SolidColorBrush).Color = _defaultChromeColor;

                (_appResource["PhoneTextCaretBrush"] as SolidColorBrush).Color = _defaultTextCaretColor;
                (_appResource["PhoneTextBoxBrush"] as SolidColorBrush).Color = _defaultTextBoxColor;
                (_appResource["PhoneTextBoxForegroundBrush"] as SolidColorBrush).Color = _defaultTextBoxForegroundColor;
                (_appResource["PhoneTextBoxEditBackgroundBrush"] as SolidColorBrush).Color = _defaultTextBoxEditBackgroundColor;
                (_appResource["PhoneTextBoxEditBorderBrush"] as SolidColorBrush).Color = _defaultTextBoxEditBorderColor;
                (_appResource["PhoneTextBoxReadOnlyBrush"] as SolidColorBrush).Color = _defaultTextBoxReadOnlyColor;
                (_appResource["PhoneTextBoxSelectionForegroundBrush"] as SolidColorBrush).Color = _defaultTextBoxSelectionForegroundColor;
            }
        }

        protected virtual void ApplyLightTheme()
        {
            if (SystemThemeType == CustomThemeType.LightTheme)
            {
                return;
            }

            (_appResource["PhoneForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xDE, 0x00, 0x00, 0x00);
            (_appResource["PhoneBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            (_appResource["PhoneContrastForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneContrastBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xDE, 0x00, 0x00, 0x00);

            (_appResource["PhoneInactiveBrush"] as SolidColorBrush).Color = Color.FromArgb(0x33, 0x00, 0x00, 0x00);
            (_appResource["PhoneBorderBrush"] as SolidColorBrush).Color = Color.FromArgb(0x99, 0x00, 0x00, 0x00);
            (_appResource["PhoneDisabledBrush"] as SolidColorBrush).Color = Color.FromArgb(0x4D, 0x00, 0x00, 0x00);
            (_appResource["PhoneSubtleBrush"] as SolidColorBrush).Color = Color.FromArgb(0x66, 0x00, 0x00, 0x00);
            (_appResource["PhoneSemitransparentBrush"] as SolidColorBrush).Color = Color.FromArgb(0xAA, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneInverseInactiveBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xE5, 0xE5, 0xE5);
            (_appResource["PhoneInverseBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD);
            (_appResource["PhoneChromeBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD);

            (_appResource["PhoneTextCaretBrush"] as SolidColorBrush).Color = Color.FromArgb(0xDE, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxBrush"] as SolidColorBrush).Color = Color.FromArgb(0x26, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xDE, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxEditBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0x00, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxEditBorderBrush"] as SolidColorBrush).Color = Color.FromArgb(0xDE, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxReadOnlyBrush"] as SolidColorBrush).Color = Color.FromArgb(0x2E, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxSelectionForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            _themeApplied = true;
        }

        protected virtual void ApplyDarkTheme()
        {
            if (SystemThemeType == CustomThemeType.DarkTheme)
            {
                return;
            }

            (_appResource["PhoneForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

            (_appResource["PhoneContrastForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            (_appResource["PhoneContrastBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            (_appResource["PhoneInactiveBrush"] as SolidColorBrush).Color = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneBorderBrush"] as SolidColorBrush).Color = Color.FromArgb(0xBF, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneDisabledBrush"] as SolidColorBrush).Color = Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneSubtleBrush"] as SolidColorBrush).Color = Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneSemitransparentBrush"] as SolidColorBrush).Color = Color.FromArgb(0xAA, 0x00, 0x00, 0x00);
            (_appResource["PhoneInverseInactiveBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xCC, 0xCC, 0xCC);
            (_appResource["PhoneInverseBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneChromeBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0x1F, 0x1F, 0x1F);

            (_appResource["PhoneTextCaretBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxBrush"] as SolidColorBrush).Color = Color.FromArgb(0xBF, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneTextBoxForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxEditBackgroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneTextBoxEditBorderBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            (_appResource["PhoneTextBoxReadOnlyBrush"] as SolidColorBrush).Color = Color.FromArgb(0x77, 0x00, 0x00, 0x00);
            (_appResource["PhoneTextBoxSelectionForegroundBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            //(_appResource["PhoneRadioCheckBoxBrush"] as SolidColorBrush).Color = Color.FromArgb(0xBF, 0xFF, 0xFF, 0xFF);
            //(_appResource["PhoneRadioCheckBoxDisabledBrush"] as SolidColorBrush).Color = Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF);
            //(_appResource["PhoneRadioCheckBoxCheckBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
            //(_appResource["PhoneRadioCheckBoxCheckDisabledBrush"] as SolidColorBrush).Color = Color.FromArgb(0x66, 0x00, 0x00, 0x00);
            //(_appResource["PhoneRadioCheckBoxPressedBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
            //(_appResource["PhoneRadioCheckBoxPressedBorderBrush"] as SolidColorBrush).Color = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

            _themeApplied = true;
        }

        protected Color _defaultForegroundColor;
        protected Color _defaultBackgroundColor;
        protected Color _defaultContrastForegroundColor;
        protected Color _defaultContrastBackgroundColor;
        protected Color _defaultInactiveColor;
        protected Color _defaultBorderColor;
        protected Color _defaultDisabledColor;
        protected Color _defaultSubtleColor;
        protected Color _defaultSemitransparentColor;
        protected Color _defaultInverseInactiveColor;
        protected Color _defaultInverseBackgroundColor;
        protected Color _defaultChromeColor;
        protected Color _defaultTextCaretColor;
        protected Color _defaultTextBoxColor;
        protected Color _defaultTextBoxForegroundColor;
        protected Color _defaultTextBoxEditBackgroundColor;
        protected Color _defaultTextBoxEditBorderColor;
        protected Color _defaultTextBoxReadOnlyColor;
        protected Color _defaultTextBoxSelectionForegroundColor;

        protected void SaveDefaultTheme()
        {
            _defaultForegroundColor = (_appResource["PhoneForegroundBrush"] as SolidColorBrush).Color;
            _defaultBackgroundColor = (_appResource["PhoneBackgroundBrush"] as SolidColorBrush).Color;

            _defaultContrastForegroundColor = (_appResource["PhoneContrastForegroundBrush"] as SolidColorBrush).Color;
            _defaultContrastBackgroundColor = (_appResource["PhoneContrastBackgroundBrush"] as SolidColorBrush).Color;

            _defaultInactiveColor = (_appResource["PhoneInactiveBrush"] as SolidColorBrush).Color;
            _defaultBorderColor = (_appResource["PhoneBorderBrush"] as SolidColorBrush).Color;
            _defaultDisabledColor = (_appResource["PhoneDisabledBrush"] as SolidColorBrush).Color;
            _defaultSubtleColor = (_appResource["PhoneSubtleBrush"] as SolidColorBrush).Color;
            _defaultSemitransparentColor = (_appResource["PhoneSemitransparentBrush"] as SolidColorBrush).Color;
            _defaultInverseInactiveColor = (_appResource["PhoneInverseInactiveBrush"] as SolidColorBrush).Color;
            _defaultInverseBackgroundColor = (_appResource["PhoneInverseBackgroundBrush"] as SolidColorBrush).Color;
            _defaultChromeColor = (_appResource["PhoneChromeBrush"] as SolidColorBrush).Color;

            _defaultTextCaretColor = (_appResource["PhoneTextCaretBrush"] as SolidColorBrush).Color;
            _defaultTextBoxColor = (_appResource["PhoneTextBoxBrush"] as SolidColorBrush).Color;
            _defaultTextBoxForegroundColor = (_appResource["PhoneTextBoxForegroundBrush"] as SolidColorBrush).Color;
            _defaultTextBoxEditBackgroundColor = (_appResource["PhoneTextBoxEditBackgroundBrush"] as SolidColorBrush).Color;
            _defaultTextBoxEditBorderColor = (_appResource["PhoneTextBoxEditBorderBrush"] as SolidColorBrush).Color;
            _defaultTextBoxReadOnlyColor = (_appResource["PhoneTextBoxReadOnlyBrush"] as SolidColorBrush).Color;
            _defaultTextBoxSelectionForegroundColor = (_appResource["PhoneTextBoxSelectionForegroundBrush"] as SolidColorBrush).Color;

            if (_defaultBackgroundColor == Colors.Black)
            {
                SystemThemeType = CustomThemeType.DarkTheme;
            }
            else
            {
                SystemThemeType = CustomThemeType.LightTheme;
            }
        }

        protected void SetResourceValue(string key, object value)
        {
            _appResource.Remove(key);
            _appResource.Add(key, value);
        }

        protected virtual void ApplySmallFontSize()
        {
            SetResourceValue("PhoneFontSizeNormal", (double)18.667);
            SetResourceValue("PhoneFontSizeMedium", (double)18.667);
            SetResourceValue("PhoneFontSizeMediumLarge", (double)20);
            SetResourceValue("PhoneFontSizeLarge", (double)25);
            SetResourceValue("PhoneFontSizeExtraLarge", (double)35);
            SetResourceValue("PhoneFontSizeExtraExtraLarge", (double)60);
        }

        protected virtual void ApplyMediumFontSize()
        {
            SetResourceValue("PhoneFontSizeNormal", (double)20);
            SetResourceValue("PhoneFontSizeMedium", (double)22.667);
            SetResourceValue("PhoneFontSizeMediumLarge", (double)25.333);
            SetResourceValue("PhoneFontSizeLarge", (double)32);
            SetResourceValue("PhoneFontSizeExtraLarge", (double)42.667);
            SetResourceValue("PhoneFontSizeExtraExtraLarge", (double)72);

            //_appResource["PhoneFontSizeSmall"] = (double)18.667;
            //_appResource["PhoneFontSizeHuge"] = (double)186.667;
        }

        protected virtual void ApplyLargeFontSize()
        {
            SetResourceValue("PhoneFontSizeNormal", (double)25);
            SetResourceValue("PhoneFontSizeMedium", (double)27);
            SetResourceValue("PhoneFontSizeMediumLarge", (double)30);
            SetResourceValue("PhoneFontSizeLarge", (double)32);
            SetResourceValue("PhoneFontSizeExtraLarge", (double)42.667);
            SetResourceValue("PhoneFontSizeExtraExtraLarge", (double)72);
        }
    }
}
