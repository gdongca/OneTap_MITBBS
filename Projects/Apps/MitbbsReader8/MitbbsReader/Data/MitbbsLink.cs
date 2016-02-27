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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public enum MitbbsLinkState
    {
        Default,
        Selected,
        InHistory
    }

    [XmlInclude(typeof(MitbbsHomeLink))]
    [XmlInclude(typeof(MitbbsUserHomeLink))]
    [XmlInclude(typeof(MitbbsBoardLink))]
    [XmlInclude(typeof(MitbbsBoardLinkMobile))]
    [XmlInclude(typeof(MitbbsBoardGroupLink))]
    [XmlInclude(typeof(MitbbsSimpleTopicLink))]
    [XmlInclude(typeof(MitbbsSimpleTopicLinkMobile))]
    [XmlInclude(typeof(MitbbsTopicLinkMobile))]
    [XmlInclude(typeof(MitbbsTopicLink))]
    [XmlInclude(typeof(MitbbsTopicSearchLink))]
    [XmlInclude(typeof(MitbbsBoardEssenceLink))]
    [XmlInclude(typeof(MitbbsTopicEssenceLink))]
    [XmlInclude(typeof(MitbbsClubGroupLink))]
    [XmlInclude(typeof(MitbbsClubLink))]
    [XmlInclude(typeof(MitbbsClubTopicLink))]
    [XmlInclude(typeof(AppMenuLink))]
    public abstract class GenericLink : INotifyPropertyChanged
    {
        public String Name { get; set; }
        public String Url { get; set; }
        public String Image { get; set; }
        
        public void UpdateLinkState()
        {
            NotifyPropertyChanged("IsInHistory");
            NotifyPropertyChanged("LinkState");
            NotifyPropertyChanged("LinkStateNoHistory");
            NotifyPropertyChanged("IsEditable");
        }

        [XmlIgnore]
        public virtual bool IsInHistory
        {
            get
            {
                return App.Settings.IsUrlInReadingHistory(Url);
            }
        }

        [XmlIgnore]
        public MitbbsLinkState LinkState
        {
            get
            {
                if (App.Settings.SelectedLink == this)
                {
                    return MitbbsLinkState.Selected;
                }

                if (IsInHistory)
                {
                    return MitbbsLinkState.InHistory;
                }

                return MitbbsLinkState.Default;
            }
        }

        [XmlIgnore]
        public MitbbsLinkState LinkStateNoHistory
        {
            get
            {
                if (App.Settings.SelectedLink == this)
                {
                    return MitbbsLinkState.Selected;
                }

                return MitbbsLinkState.Default;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Select()
        {
            App.Settings.SelectedLink = this;
        }
    }

    public class AppMenuLink : GenericLink
    {
        public bool RequiresLogIn { get; set; }
        public bool BlockIfLoggingIn { get; set; }
        public String Subtitle { get; set; }
        
        [XmlIgnore]
        public Action AppAction { get; set; }

        
        public bool HasSubtitle
        {
            get
            {
                return !String.IsNullOrEmpty(Subtitle);
            }
        }

        public AppMenuLink()
        {
            RequiresLogIn = false;
            BlockIfLoggingIn = false;
            AppAction = null;
        }
    }

    public class MitbbsLink : GenericLink
    {
        public String ParentUrl { get; set; }
        public DateTime AccessDate { get; set; }
        public String OfflineID { get; set; }
        
        public String LastVisitedUrl { get; set; }
        public int LastVisitedPage { get; set; }
        public double LastVisitedScreenPos { get; set; }
        public int LastPage { get; set; }
        public int LastVisitedPageContentCount { get; set; }

        [XmlIgnore]
        public String AccessDateText
        {
            get
            {
                return AccessDate.ToShortDateString();
            }
        }

        public static bool CanEdit = false;

        [XmlIgnore]
        public bool IsEditable
        {
            get
            {
                return CanEdit;
            }
        }

        private bool _hasNewContent = false;
        public bool HasNewContent
        {
            get
            {
                return _hasNewContent;
            }

            set
            {
                _hasNewContent = value;
                NotifyPropertyChanged("HasNewContent");
            }
        }

        public MitbbsLink()
        {
            AccessDate = DateTime.Now;
            OfflineID = "";
            
            LastVisitedUrl = null;
            LastVisitedPage = -1;
            LastVisitedScreenPos = -1;
            LastVisitedPageContentCount = -1;
        }

        protected virtual bool LoadFromLinkNode(HtmlNode linkNode)
        {
            String url = linkNode.Attributes["href"].Value;
            
            if (ParentUrl != null)
            {
                Url = HtmlUtilities.GetAbsoluteUrl(ParentUrl, url);
            }
            else
            {
                Url = url;
            }

            Name = HtmlUtilities.GetPlainHtmlText(linkNode.InnerText);

            return true;
        }

        public virtual bool LoadFromHtml(HtmlNode rootNode)
        {
            if (rootNode.Name == "a")
            {
                return LoadFromLinkNode(rootNode);
            }

            bool found = false;
            IEnumerable<HtmlNode> linkNodes = rootNode.Descendants("a");

            foreach (HtmlNode linkNode in linkNodes)
            {
                if (LoadFromLinkNode(linkNode))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        private static String _boardEssenceUrlPrefix = "mbbsdoc.php?path=";
        private static String _topicEssenceUrlPrefix = "mbbsann2.php?path=";
        private static String _boardGroupUrlPrefix = "mbbsboa";
        private static String _boardUrlPrefix = "mbbsdoc";
        private static String _topicUrlPrefix = "marticle";

        public static MitbbsLink CreateLinkInstance(HtmlNode rootNode, String ParentUrl)
        {
            MitbbsLink generalLink = new MitbbsLink();
            MitbbsLink linkInstance = null;

            if (!generalLink.LoadFromHtml(rootNode))
            {
                return null;
            }

            String url = generalLink.Url;

            if (url.StartsWith(_boardEssenceUrlPrefix))
            {
                linkInstance = new MitbbsBoardEssenceLink();
            }
            else if (url.StartsWith(_topicEssenceUrlPrefix))
            {
                linkInstance = new MitbbsTopicEssenceLink();
            }
            else if (url.StartsWith(_boardGroupUrlPrefix))
            {
                linkInstance = new MitbbsBoardGroupLink();
            }
            else if (url.StartsWith(_boardUrlPrefix))
            {
                linkInstance = new MitbbsBoardLinkMobile();
            }
            else if (url.StartsWith(_topicUrlPrefix))
            {
                linkInstance = new MitbbsSimpleTopicLinkMobile();
            }
            else
            {
                return null;
            }

            linkInstance.ParentUrl = ParentUrl;
            if (!linkInstance.LoadFromHtml(rootNode))
            {
                return null;
            }

            if (linkInstance is MitbbsBoardLinkMobile)
            {
                linkInstance = MitbbsBoardLink.CreateFromMobileLink(linkInstance as MitbbsBoardLinkMobile);
            }

            return linkInstance;
        }
    }

    public class MitbbsHomeLink : MitbbsLink
    {
    }

    public class MitbbsUserHomeLink : MitbbsLink
    {
    }

    public abstract class MitbbsBoardLinkBase: MitbbsLink
    {
        public String BoardName { get; set; }
        public abstract String EnBoardName { get; }
    }

    public class MitbbsBoardLinkMobile : MitbbsBoardLinkBase
    {
        public override string EnBoardName
        {
            get { throw new NotImplementedException(); }
        }

        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            bool result = base.LoadFromHtml(rootNode);

            if (result)
            {
                String site;
                Dictionary<String, String> values;

                HtmlUtilities.ParseQueryStringFromUrl(Url, out site, out values);

                if (values.ContainsKey("board"))
                {
                    String boardName;

                    if (values.TryGetValue("board", out boardName))
                    {
                        BoardName = boardName;
                    }
                }
            }

            return result;
        }

        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            String url = linkNode.Attributes["href"].Value;

            if (ParentUrl != null)
            {
                Url = HtmlUtilities.GetAbsoluteUrl(ParentUrl, url);
            }
            else
            {
                Url = url;
            }

            foreach (var text in linkNode.Descendants("h3"))
            {
                if (text.GetAttributeValue("class", "") == "hot_name")
                {
                    Name += text.InnerText;
                    break;
                }
            }

            return true;
        }
    }

    public class MitbbsBoardLink : MitbbsBoardLinkBase
    {
        private static String fullBoardLinkTemplate = @"/bbsdoc1/{0}_1_0.html";
        private String _boardUrlTemplate = @"(?<1>.*)bbsdoc/(?<2>.*).html";
        private String _boardUrlTemplate2 = @"(?<1>.*)bbsdoc1/(?<2>.*)_(?<3>.*)_(?<4>.*).html";
        private String _boardUrlTemplate3 = @"(?<1>.*)bbsdoc2/(?<2>.*)_(?<3>.*)_(?<4>.*).html";

        public override string EnBoardName
        {
            get
            {
                Match match = Regex.Match(Url, _boardUrlTemplate);
                if (!match.Success)
                {
                    match = Regex.Match(Url, _boardUrlTemplate2);
                }

                if (!match.Success)
                {
                    match = Regex.Match(Url, _boardUrlTemplate3);
                }

                if (match.Success)
                {
                    return match.Groups[2].Value;
                }
                else
                {
                    return null;
                }
            }
        }

        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            bool result = base.LoadFromHtml(rootNode);

            if (result)
            {
                String[] tokens = Url.Split('/');
                if (tokens.Length >= 3)
                {
                    String[] tokens2 = tokens[tokens.Length - 1].Split('.');
                    if (tokens2.Length == 2)
                    {
                        BoardName = tokens2[0];
                    }
                }
            }

            return result;
        }

        public static MitbbsBoardLinkBase CreateFromMobileLink(MitbbsBoardLinkMobile mobileLink)
        {
            //return mobileLink;

            if (mobileLink.BoardName != null)
            {
                String fullUrl = String.Format(App.Settings.BuildUrl(fullBoardLinkTemplate), mobileLink.BoardName);

                return new MitbbsBoardLink()
                {
                    Name = mobileLink.Name,
                    Url = fullUrl,
                    BoardName = mobileLink.BoardName
                };
            }

            return mobileLink;
        }
    }

    public class MitbbsBoardGroupLink : MitbbsLink
    {
        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            if (!base.LoadFromLinkNode(linkNode))
            {
                return false;
            }

            if (Name == "回到上一级")
            {
                return false;
            }

            if (Name == "俱乐部")
            {
                return false;
            }

            return true;
        }
    }

    public abstract class MitbbsSimpleTopicLinkBase: MitbbsLink
    {
        public String BoardName { get; set; }
    }

    public class MitbbsSimpleTopicLinkMobile : MitbbsSimpleTopicLinkBase
    {
        private String _topicUrlTemplate = "(?<1>.*)board=(?<2>.*)&(?<3>.*)";

        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            if (!base.LoadFromLinkNode(linkNode))
            {
                return false;
            }

            Match match = Regex.Match(Url, _topicUrlTemplate);
            if (match.Success)
            {
                BoardName = match.Groups[2].Value;
                int sep = BoardName.IndexOf('&');
                if (sep >= 0)
                {
                    BoardName = BoardName.Substring(0, sep);
                }

                return true;
            }

            return false;
        }
    }

    public class MitbbsSimpleTopicLink : MitbbsSimpleTopicLinkBase
    {
        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            if (!base.LoadFromLinkNode(linkNode))
            {
                return false;
            }

            String rawUrl = linkNode.GetLinkUrl(null);

            if (rawUrl.Contains("/article_t/"))
            {
                String[] tokens = rawUrl.Split('/');

                if (tokens.Length >= 4)
                {
                    BoardName = tokens[2];
                    return true;
                }
            }

            return false;
        }

        public static MitbbsSimpleTopicLinkBase CreateFromMobileLink(MitbbsSimpleTopicLinkBase mobileLink)
        {
            String fullUrl;
            if (MitbbsLinkConverter.MobileTopicLinkToFullLink(mobileLink.Url, out fullUrl))
            {
                return new MitbbsSimpleTopicLink()
                {
                    Url = fullUrl,
                    BoardName = mobileLink.BoardName,
                    AccessDate = mobileLink.AccessDate,
                    ParentUrl = mobileLink.ParentUrl,
                    Name = mobileLink.Name
                };
            }

            return mobileLink;
        }
    }

    public class MitbbsTopicLinkBase : MitbbsLink
    {
        public String Author { get; set; }
        public String IssueDate { get; set; }
        public String Author2 { get; set; }
        public String IssueDate2 { get; set; }
        public String ReplyCount { get; set; }
        public long Number { get; set; }
        public bool IsOnTop { get; set; }
        public bool HasImage { get; set; }
        public bool MFlag { get; set; }
        public bool BFlag { get; set; }
        public bool GFlag { get; set; }
        public bool IsRead { get; set; }
        
        public bool HasAuthor2
        {
            get
            {
                return !String.IsNullOrEmpty(Author2) && !String.IsNullOrEmpty(IssueDate2);
            }
        }

        public override bool IsInHistory
        {
            get
            {
                if (IsRead)
                {
                    return true;
                }
                else
                {
                    return base.IsInHistory;
                }
            }
        }

        public String DisplayTitle
        {
            get
            {
                String displayTitle = Name;
                bool hasPrefix = false;

                if (HasImage)
                {
                    displayTitle = "  " + displayTitle;
                    hasPrefix = true;
                }

                if (MFlag)
                {
                    displayTitle = "  " + displayTitle;
                    hasPrefix = true;
                }

                if (BFlag)
                {
                    displayTitle = "  " + displayTitle;
                    hasPrefix = true;
                }

                if (GFlag)
                {
                    displayTitle = "  " + displayTitle;
                    hasPrefix = true;
                }

                if (IsOnTop)
                {
                    displayTitle = "      " + displayTitle;
                    hasPrefix = true;
                }

                if (hasPrefix)
                {
                    displayTitle = " " + displayTitle;
                }

                return displayTitle;
            }
        }

        public String Prefix
        {
            get
            {
                String prefix = "";

                if (HasImage)
                {
                    prefix = "@" + prefix;
                }

                if (MFlag)
                {
                    prefix = "m" + prefix;
                }

                if (BFlag)
                {
                    prefix = "b" + prefix;
                }

                if (GFlag)
                {
                    prefix = "g" + prefix;
                }

                if (IsOnTop)
                {
                    prefix = "[置顶]" + prefix;
                }

                return prefix;
            }
        }

    }

    public class MitbbsTopicLinkMobile : MitbbsTopicLinkBase
    {
        private String _dateTemplate = "时间:(?<1>.*)\\)";

        public override bool LoadFromHtml(HtmlNode RootNode)
        {
            IEnumerable<HtmlNode> divNodes = RootNode.Descendants("div");

            foreach (HtmlNode divNode in divNodes)
            {
                if (divNode.Attributes.Contains("id"))
                {
                    if (divNode.Attributes["id"].Value == "zhiding")
                    {
                        IsOnTop = true;
                        Number = 0;
                    }
                    else if (divNode.Attributes["id"].Value == "shuzi")
                    {
                        IsOnTop = false;
                        String numberText = HtmlUtilities.GetPlainHtmlText(divNode.InnerText);

                        try
                        {
                            Number = long.Parse(numberText);
                        }
                        catch (Exception)
                        {
                            Number = 0;
                        }
                    }
                    else if (divNode.Attributes["id"].Value == "att")
                    {
                        String numberText = HtmlUtilities.GetPlainHtmlText(divNode.InnerText);

                        if (!numberText.Contains("*"))
                        {
                            IsRead = true;
                        }
                        else
                        {
                            IsRead = false;
                        }

                        if (numberText.Contains("@"))
                        {
                            HasImage = true;
                        }
                        else
                        {
                            HasImage = false;
                        }

                        if (numberText.Contains("m"))
                        {
                            MFlag = true;
                            IsRead = true;
                        }
                        else if (numberText.Contains("M"))
                        {
                            MFlag = true;
                            IsRead = false;
                        }
                        else
                        {
                            MFlag = false;
                        }

                        if (numberText.Contains("b"))
                        {
                            BFlag = true;
                            IsRead = true;
                        }
                        else if (numberText.Contains("B"))
                        {
                            BFlag = true;
                            IsRead = false;
                        }
                        else
                        {
                            BFlag = false;
                        }

                        if (numberText.Contains("g"))
                        {
                            GFlag = true;
                            IsRead = true;
                        }
                        else if (numberText.Contains("G"))
                        {
                            GFlag = true;
                            IsRead = false;
                        }
                        else
                        {
                            GFlag = false;
                        }

                        if (!App.Settings.KeepHistory || !App.WebSession.IsLoggedIn)
                        {
                            IsRead = false;
                        }
                    }
                }
                else if (divNode.Attributes.Contains("class"))
                {
                    if (divNode.Attributes["class"].Value == "wenzi")
                    {
                        int startOffset = 0;
                        HtmlNode linkNode = divNode.ChildNodes[startOffset + 0];
                        if (linkNode.Name == "a")
                        {
                            Name = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText).Trim('●').Trim();
                            Url = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);
                        }
                        else
                        {
                            return false;
                        }

                        HtmlNode authorNode = divNode.ChildNodes[startOffset + 2];
                        if (authorNode.Name == "a")
                        {
                            Author = authorNode.InnerText;
                        }
                        else
                        {
                            return false;
                        }

                        HtmlNode dateNode = divNode.ChildNodes[startOffset + 3].FirstChild;
                        Match match = Regex.Match(HtmlUtilities.GetPlainHtmlText(dateNode.InnerText), _dateTemplate);
                        if (match.Success)
                        {
                            IssueDate = match.Groups[1].ToString();
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }

    public class MitbbsTopicLink : MitbbsTopicLinkBase
    {
        public override bool LoadFromHtml(HtmlNode rootNode)
        {
            return LoadFromHtml(rootNode, false);
        }

        public bool LoadFromHtml(HtmlNode rootNode, bool loggedIn)
        {
            if (rootNode.Name != "a")
            {
                return false;
            }

            if (!base.LoadFromLinkNode(rootNode))
            {
                return false;
            }

            if (!IsTopicUrl(Url))
            {
                return false;
            }

            HtmlNode pTD = rootNode.GetParentOfType("td");
            HtmlNode prevTD = pTD.GetPrevSiblingOfType("td");
            HtmlNode prevTD2 = prevTD.GetPrevSiblingOfType("td");
            HtmlNode nextTD = pTD.GetNextSiblingOfType("td");
            
            String prevText = HtmlUtilities.GetPlainHtmlText(prevTD.InnerText).Trim();
            IsOnTop = (prevText == "提示");

            if (!prevText.Contains("*"))
            {
                IsRead = true;
            }
            else
            {
                IsRead = false;
            }

            if (prevText.Contains("@"))
            {
                HasImage = true;
            }
            else
            {
                HasImage = false;
            }

            if (prevText.Contains("m"))
            {
                MFlag = true;
                IsRead = true;
            }
            else if (prevText.Contains("M"))
            {
                MFlag = true;
                IsRead = false;
            }
            else
            {
                MFlag = false;
            }

            if (prevText.Contains("b"))
            {
                BFlag = true;
                IsRead = true;
            }
            else if (prevText.Contains("B"))
            {
                BFlag = true;
                IsRead = false;
            }
            else
            {
                BFlag = false;
            }

            if (prevText.Contains("g"))
            {
                GFlag = true;
                IsRead = true;
            }
            else if (prevText.Contains("G"))
            {
                GFlag = true;
                IsRead = false;
            }
            else
            {
                GFlag = false;
            }

            if (IsOnTop || !App.Settings.KeepHistory || !App.WebSession.IsLoggedIn || !loggedIn)
            {
                IsRead = false;
            }

            ReplyCount = null;
            Author = null;
            foreach (HtmlNode authorNode in nextTD.Descendants("a"))
            {
                Author = HtmlUtilities.GetPlainHtmlText(authorNode.InnerText).Trim();
                break;
            }

            if (Author == null)
            {
                ReplyCount = nextTD.GetPlainInnerText().Trim();
                if (!ReplyCount.Contains("/"))
                {
                    ReplyCount = "";
                }

                nextTD = nextTD.GetNextSiblingOfType("td");
                foreach (HtmlNode authorNode in nextTD.Descendants("a"))
                {
                    Author = HtmlUtilities.GetPlainHtmlText(authorNode.InnerText).Trim();
                    break;
                }
            }

            IssueDate = null;
            foreach (HtmlNode dateNode in nextTD.Descendants("span"))
            {
                IssueDate = dateNode.GetPlainInnerText().Trim();
                break;
            }

            if (IssueDate == null)
            {
                nextTD = nextTD.GetNextSiblingOfType("td");
                IssueDate = nextTD.GetPlainInnerText().Trim();
            }

            Author2 = null;
            IssueDate2 = null;

            HtmlNode nextTD2 = nextTD.GetNextSiblingOfType("td");
            if (nextTD2 != null)
            {
                
                foreach (HtmlNode authorNode in nextTD2.Descendants("a"))
                {
                    Author2 = HtmlUtilities.GetPlainHtmlText(authorNode.InnerText).Trim();
                    break;
                }

                foreach (HtmlNode dateNode in nextTD2.Descendants("span"))
                {
                    IssueDate2 = dateNode.GetPlainInnerText().Trim();
                    break;
                }
            }

            if (Author2 != null)
            {
                Author = Author + " / " + Author2;
            }

            if (IssueDate2 != null)
            {
                IssueDate = IssueDate2;
            }

            if ((Author == null) || (IssueDate == null))
            {
                return false;
            }

            if ((prevTD2 != null) && !IsOnTop)
            {
                try
                {
                    Number = long.Parse(prevTD2.InnerText);
                }
                catch (Exception)
                {
                    Number = 0;
                }
            }

            Name = Name.Trim('●').Trim();

            return true;
        }

        private bool IsTopicUrl(String url)
        {
            url = url.Trim();

            if (url.Contains("/clubarticle"))
            {
                return true;
            }

            if (url.Contains("/article"))
            {
                return true;
            }

            return false;
        }
    }

    public class MitbbsClubTopicLink : MitbbsTopicLink
    {
    }

    public class MitbbsTopicSearchLink : MitbbsLink
    {
        public String Author { get; set; }
        public String IssueDate { get; set; }
        public bool IsMobile { get; set; }

        private String _userUrlPrefix = "/user_info";
        private String _topicUrlPrefix = "/article";
        private String _topicUrlPrefix2 = "/clubarticle";

        public override bool LoadFromHtml(HtmlNode RootNode)
        {
            IEnumerable<HtmlNode> trNodes = RootNode.Descendants("tr");
            foreach (HtmlNode trNode in trNodes)
            {
                return false;
            }

            Author = null;
            Name = null;
            Url = null;

            IEnumerable<HtmlNode> linkNodes = RootNode.Descendants("a");
            foreach (HtmlNode linkNode in linkNodes)
            {
                String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText);
                String link = linkNode.Attributes["href"].Value;

                if (link.ToLower().StartsWith(_userUrlPrefix))
                {
                    Author = linkText;
                }
                else if (link.ToLower().StartsWith(_topicUrlPrefix) || link.ToLower().StartsWith(_topicUrlPrefix2))
                {
                    Name = linkText;
                    
                    String fullUrl;
                    
                    fullUrl = HtmlUtilities.GetAbsoluteUrl(ParentUrl, link);
                    IsMobile = false;
                    Url = fullUrl;
                }
            }

            if ((Author == null) || (Name == null) || (Url == null))
            {
                return false;
            }

            IEnumerable<HtmlNode> tdNodes = RootNode.Descendants("td");
            foreach (HtmlNode tdNode in tdNodes)
            {
                if ((tdNode.Attributes["class"] != null) && (tdNode.Attributes["class"].Value == "black4"))
                {
                    IssueDate = HtmlUtilities.GetPlainHtmlText(tdNode.FirstChild.InnerText);
                    return true;
                }
            }

            return false;
        }
    }

    public class MitbbsBoardEssenceLink : MitbbsLink
    {
        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            if (!base.LoadFromLinkNode(linkNode))
            {
                return false;
            }

            Name = "[目录] " + Name;
            return true;
        }
    }

    public class MitbbsTopicEssenceLink : MitbbsLink
    {
        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            if (!base.LoadFromLinkNode(linkNode))
            {
                return false;
            }

            Name = "[文件] " + Name;
            return true;
        }
    }

    public class MitbbsMailLink : MitbbsLink
    {
        public String DeleteLink { get; private set; }
        public String MoveLink { get; private set; }
        public bool IsReplied { get; private set; }
        public String Author { get; private set; }
        public String Recipient { get; private set; }
        public String Date { get; private set; }
        
        private String _authorTemplate = "发信者：(?<1>[^,]*)";
        private String _authorTemplate2 = "发信人：(?<1>[^,]*)";
        private String _recipientTemplate = "收信者：(?<1>[^,]*)";
        
        public MitbbsMailLink()
        {
            IsNew = false;
            IsReplied = false;
        }

        public override bool LoadFromHtml(HtmlNode RootNode)
        {
            Name = null;
            Url = null;

            IEnumerable<HtmlNode> linkNodes = RootNode.Descendants("a");
            foreach (HtmlNode linkNode in linkNodes)
            {
                String url;

                if (ParentUrl != null)
                {
                    url = HtmlUtilities.GetAbsoluteUrl(ParentUrl, linkNode.Attributes["href"].Value);
                }
                else
                {
                    url = linkNode.Attributes["href"].Value;
                }

                String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText);

                if (linkText == "删除")
                {
                    DeleteLink = url;
                }
                else if (linkText == "移动")
                {
                    MoveLink = url;
                }
                else
                {
                    Url = url;
                    Name = linkText;
                }
            }

            IsNew = false;
            IsReplied = false;

            IEnumerable<HtmlNode> imgNodes = RootNode.Descendants("img");
            foreach (HtmlNode imgNode in imgNodes)
            {
                String imgUrl = imgNode.Attributes["src"].Value;
                if (imgUrl != null)
                {
                    if (imgUrl.Contains("receive1.gif"))
                    {
                        IsNew = true;
                    }
                    else if (imgUrl.Contains("receive2.gif"))
                    {
                        IsReplied = true;
                    }
                }
            }

            IEnumerable<HtmlNode> textNodes = RootNode.Descendants("#text");
            foreach (HtmlNode textNode in textNodes)
            {
                String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                int index = text.IndexOf("  ");

                if (index > 0)
                {
                    String authorText = text.Substring(0, index);
                    String dateText = text.Substring(index + 2, text.Length - index - 2);

                    Match match = Regex.Match(authorText, _authorTemplate);
                    if (!match.Success)
                    {
                        match = Regex.Match(authorText, _authorTemplate2);
                        Date = dateText;
                    }

                    if (match.Success)
                    {
                        Author = match.Groups[1].Value;
                        Date = dateText;
                    }
                    else
                    {
                        match = Regex.Match(authorText, _recipientTemplate);
                        if (match.Success)
                        {
                            Recipient = match.Groups[1].Value;
                            Date = dateText;
                        }
                    }
                }
            }

            return (Name != null) && (Url != null);
        }

        private bool _new = false;
        public bool IsNew
        {
            get
            {
                return _new;
            }

            set
            {
                _new = value;
                NotifyPropertyChanged("DisplayTitle");
                NotifyPropertyChanged("Prefix");
            }
        }

        public String DisplayTitle
        {
            get
            {
                String displayTitle = Name;
                bool hasPrefix = false;

                if (IsNew)
                {
                    displayTitle = "  " + displayTitle;
                    hasPrefix = true;
                }

                if (hasPrefix)
                {
                    displayTitle = " " + displayTitle;
                }

                return displayTitle;
            }
        }

        public String Prefix
        {
            get
            {
                String prefix = "";

                if (IsNew)
                {
                    prefix = "新" + prefix;
                }

                return prefix;
            }
        }

        public String AuthorOrRecipient
        {
            get
            {
                if (Author != null)
                {
                    return "发信人： " + Author;
                }
                else if (Recipient != null)
                {
                    return "收信人： " + Recipient;
                }
                else
                {
                    return "无邮件信息";
                }
            }
        }


    }

    public class MitbbsClubGroupLink : MitbbsLink
    {
        public bool IsClubHome { get; set; }
        private String _clubGroupUrlPrefix = @"/club/clubindex_t_g";
        private String _clubHomeUrlPrefix  = @"/club/clubindex_t_g.";

        public MitbbsClubGroupLink()
        {
            IsClubHome = false;
        }

        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            bool result = base.LoadFromLinkNode(linkNode);

            if (result)
            {
                if (!Url.Contains(_clubGroupUrlPrefix) || Url.Contains(_clubHomeUrlPrefix))
                {
                    result = false;
                }
            }

            return result;
        }
    }

    public class MitbbsClubLink : MitbbsLink
    {
        private String _clubUrlPrefix = @"/club_bbsdoc/";

        private String _boardUrlTemplate = @"(?<1>.*)bbsdoc/(?<2>.*).html";
        private String _boardUrlTemplate2 = @"(?<1>.*)bbsdoc1/(?<2>.*)_(?<3>.*)_(?<4>.*).html";
        private String _boardUrlTemplate3 = @"(?<1>.*)bbsdoc2/(?<2>.*)_(?<3>.*)_(?<4>.*).html";

        public string EnName
        {
            get
            {
                Match match = Regex.Match(Url, _boardUrlTemplate);
                if (!match.Success)
                {
                    match = Regex.Match(Url, _boardUrlTemplate2);
                }

                if (!match.Success)
                {
                    match = Regex.Match(Url, _boardUrlTemplate3);
                }

                if (match.Success)
                {
                    return match.Groups[2].Value;
                }
                else
                {
                    return null;
                }
            }
        }

        protected override bool LoadFromLinkNode(HtmlNode linkNode)
        {
            bool result = base.LoadFromLinkNode(linkNode);

            if (result)
            {
                if (!Url.Contains(_clubUrlPrefix))
                {
                    result = false;
                }
            }

            return result;
        }
    }
}
