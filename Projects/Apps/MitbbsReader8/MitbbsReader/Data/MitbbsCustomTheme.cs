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
using Naboo.AppUtil;

namespace Naboo.MitbbsReader
{
    public class MitbbsCustomTheme : CustomTheme
    {
        public MitbbsCustomTheme() : base(App.Current.Resources)
        {
        }

        protected override void ApplySmallFontSize()
        {
            base.ApplySmallFontSize();

            SetResourceValue("MitbbsFontSizeSmall", (double)14);
            SetResourceValue("MitbbsFontSizeText", (double)18);
            SetResourceValue("MitbbsFontSizeNormal", (double)16);
            SetResourceValue("MitbbsFontSizeMedium", (double)18.667);
            SetResourceValue("MitbbsFontSizeMediumLarge", (double)20);
            SetResourceValue("MitbbsFontSizeLarge", (double)23);
            SetResourceValue("MitbbsFontSizeExtraLarge", (double)32);
            SetResourceValue("MitbbsFontSizeExtraExtraLarge", (double)55);
        }

        protected override void ApplyMediumFontSize()
        {
            base.ApplyMediumFontSize();

            SetResourceValue("MitbbsFontSizeSmall", (double)16);
            SetResourceValue("MitbbsFontSizeText", (double)24);
            SetResourceValue("MitbbsFontSizeNormal", (double)19);
            SetResourceValue("MitbbsFontSizeMedium", (double)22.667);
            SetResourceValue("MitbbsFontSizeMediumLarge", (double)25.333);
            SetResourceValue("MitbbsFontSizeLarge", (double)28);
            SetResourceValue("MitbbsFontSizeExtraLarge", (double)38);
            SetResourceValue("MitbbsFontSizeExtraExtraLarge", (double)60);
        }

        protected override void ApplyLargeFontSize()
        {
            base.ApplyLargeFontSize();

            SetResourceValue("MitbbsFontSizeSmall", (double)20);
            SetResourceValue("MitbbsFontSizeText", (double)30);
            SetResourceValue("MitbbsFontSizeNormal", (double)25);
            SetResourceValue("MitbbsFontSizeMedium", (double)27);
            SetResourceValue("MitbbsFontSizeMediumLarge", (double)30);
            SetResourceValue("MitbbsFontSizeLarge", (double)32);
            SetResourceValue("MitbbsFontSizeExtraLarge", (double)42.667);
            SetResourceValue("MitbbsFontSizeExtraExtraLarge", (double)72);
        }
    }
}
