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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class MitbbsLinkConverter
    {
        public  static bool IsMitbbsFullTopicLink(String url)
        {
            if (url.Contains(App.Settings.Site.Url + "/article") && url.Trim().ToLower().EndsWith(".html"))
            {
                return true;
            }

            String fullClubLinkTemplate1 = @"(?<1>.*)/clubarticle/(?<2>.*)/(?<3>.*).html";
            String fullClubLinkTemplate2 = @"(?<1>.*)/clubarticle_t/(?<2>.*)/(?<3>.*).html";
            String fullClubLinkTemplate3 = @"(?<1>.*)/mitbbs_article.php?(?<2>.*)opflag=1";
            Match match = Regex.Match(url, fullClubLinkTemplate1);
            
            if (!match.Success)
            {
                match = Regex.Match(url, fullClubLinkTemplate2);
            }

            if (!match.Success)
            {
                match = Regex.Match(url, fullClubLinkTemplate3);
            }

            return match.Success;
        }

        public static bool FullBoardEssenceLinkToMobileLink(String fullLink, out String mobileLink)
        {
            String[] tokens = fullLink.Split('/');

            if (tokens.Length >= 3)
            {
                String section = tokens[tokens.Length - 3];
                String board = tokens[tokens.Length - 2];

                mobileLink = String.Format("{2}/mobile/mbbsdoc.php?path={0}/{1}&ftype=5", section, board, App.Settings.Site.Url);
                return true;
            }

            mobileLink = null;
            return false;
        }

        public static bool MobileTopicLinkToFullLink(String mobileLink, out String fullLink)
        {
            String mobileLinkTemplate1 = @"(?<1>.*)/mobile/marticle.php\?board=(?<2>.*)&id=(?<3>.*)&ftype=(?<4>.*)";
            String fullLinkTemplate1 = App.Settings.Site.Url + "/article/{0}/{1}_0.html";

            Match match = Regex.Match(mobileLink, mobileLinkTemplate1);
            if (match.Success)
            {
                fullLink = String.Format(fullLinkTemplate1, match.Groups[2].Value, match.Groups[3].Value);
                return true;
            }

            fullLink = null;
            return false;
        }

        public static bool FullTopicLinkToMobileLink(String fullLink, out String mobileLink)
        {
            String fullPageLinkTemplate = "(?<1>.*)/article_t1/(?<2>.*)/(?<3>.*)_0_(?<4>.*).html";
            String fullLinkTemplate1 = "(?<1>.*)/article_t/(?<2>.*)/(?<3>.*).html";
            String fullLinkTemplate2 = "(?<1>.*)/article(?<4>.*)/(?<2>.*)/(?<3>.*)_(?<5>.*).html";
            String mobileLinkTemplate1 = App.Settings.Site.Url + "/mobile/marticle_t.php?board={0}&gid={1}";
            String mobileLinkTemplate2 = App.Settings.Site.Url + "/mobile/marticle.php?board={0}&id={1}&ftype=0";
            String mobilePageLinkTemplate = App.Settings.Site.Url + "/mobile/marticle_t.php?board={0}&gid={1}&start=0&pno={2}";
            String mobileLinkPrefix = App.Settings.Site.Url + "/mobile/marticle";

            Match match = Regex.Match(fullLink, fullPageLinkTemplate);
            if (match.Success)
            {
                mobileLink = String.Format(mobilePageLinkTemplate, match.Groups[2].Value, match.Groups[3].Value, match.Groups[4].Value);
                return true;
            }

            match = Regex.Match(fullLink, fullLinkTemplate1);
            if (match.Success)
            {
                mobileLink = String.Format(mobileLinkTemplate1, match.Groups[2].Value, match.Groups[3].Value);
                return true; 
            }

            match = Regex.Match(fullLink, fullLinkTemplate2);
            if (match.Success)
            {
                mobileLink = String.Format(mobileLinkTemplate2, match.Groups[2].Value, match.Groups[3].Value);
                return true;
            }

            if (fullLink.ToLower().StartsWith(mobileLinkPrefix))
            {
                mobileLink = fullLink;
                return true;
            }

            mobileLink = "";
            return false;
        }

        public static bool MobileLinkToFullLink(String mobileLink, out String fullLink)
        {
            String site;
            Dictionary<String, String> values;
            HtmlUtilities.ParseQueryStringFromUrl(mobileLink, out site, out values);

            if (site.Contains("mobile/marticle_t") || (values.ContainsKey("board") && values.ContainsKey("gid")))
            {
                fullLink = String.Format(@"{2}/article_t/{0}/{1}.html", values.GetValue("board"), values.GetValue("gid"), App.Settings.Site.Url);
                return true;
            }

            if (site.Contains("mobile/mbbsdoc") || values.ContainsKey("board"))
            {
                fullLink = String.Format(@"{1}/bbsdoc1/{0}_1_0.html", values.GetValue("board"), App.Settings.Site.Url);
                return true;
            }

            fullLink = null;
            return false;
        }
    }
}
