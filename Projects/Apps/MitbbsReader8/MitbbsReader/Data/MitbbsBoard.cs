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
using System.Linq;
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    [XmlInclude(typeof(MitbbsBoard))]
    [XmlInclude(typeof(MitbbsClubBoard))]
    [XmlInclude(typeof(MitbbsBoardMobile))]
    public abstract class MitbbsBoardBase
    {
        public String Url { get; set; }
        public String BoardName { get; set; }
        public String EnBoardName { get; set; }

        public String FirstPageUrl { get; set; }
        public String LastPageUrl { get; set; }
        public String PrevPageUrl { get; set; }
        public String NextPageUrl { get; set; }

        public String BoardPageUrl { get; set; }
        public String CollectionPageUrl { get; set; }
        public String ReservePageUrl { get; set; }
        public String EssensePageUrl { get; set; }

        public String NewPostUrl { get; set; }

        public ObservableCollection<MitbbsTopicLinkBase> TopTopicLinks { get; set; }
        public ObservableCollection<MitbbsTopicLinkBase> TopicLinks { get; set; }

        public bool IsLoaded { get; set; }

        [XmlIgnore]
        public bool IgnoreReadHistory { get; set; }

        private bool _hideTopArticle = false;
        [XmlIgnore]
        public bool HideTopArticle
        {
            get
            {
                return _hideTopArticle;
            }

            set
            {
                if (_hideTopArticle != value)
                {
                    _hideTopArticle = value;
                    if (_hideTopArticle)
                    {
                        foreach (var topicLink in TopicLinks)
                        {
                            if (topicLink.IsOnTop)
                            {
                                TopTopicLinks.Add(topicLink);
                            }
                        }

                        foreach (var topicLink in TopTopicLinks)
                        {
                            TopicLinks.Remove(topicLink);
                        }
                    }
                    else
                    {
                        int index = 0;
                        foreach (var topicLink in TopTopicLinks)
                        {
                            TopicLinks.Insert(index, topicLink);
                            index++;
                        }

                        TopTopicLinks.Clear();
                    }
                }
            }
        }

        [XmlIgnore]
        public virtual String DisplayBoardName
        {
            get
            {
                if (BoardPageUrl == null)
                {
                }
                else if (CollectionPageUrl == null)
                {
                    return BoardName + " (文摘区)";
                }
                else if (ReservePageUrl == null)
                {
                    return BoardName + " (保留区)";
                }

                if (String.IsNullOrEmpty(EnBoardName))
                {
                    return BoardName;
                }
                else
                {
                    return BoardName + "-" + EnBoardName;
                }
            }
        }

        public event EventHandler<DataLoadedEventArgs> BoardLoaded;

        protected HtmlWeb _web;
        protected bool _goForward;
        protected String _url;

        public MitbbsBoardBase()
        {
            TopTopicLinks = new ObservableCollection<MitbbsTopicLinkBase>();
            TopicLinks = new ObservableCollection<MitbbsTopicLinkBase>();
            IgnoreReadHistory = false;
        }

        public virtual void LoadFromUrl(HtmlWeb web, String url, bool goForward)
        {
            ClearContent();
            Url = url;
            _url = url;
            _web = web;
            _goForward = goForward;

            _web.LoadCompleted += OnUrlLoaded;
            _web.LoadAsync(url);
        }

        protected virtual void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnUrlLoaded;
            DataLoadedEventArgs loadedEventArgs = new DataLoadedEventArgs();
            loadedEventArgs.Error = args.Error;

            if (args.Document != null)
            {
                LoadFromHtml(args.Document.DocumentNode, loadedEventArgs);
            }

            TriggerLoadedEvent(loadedEventArgs);
        }

        public abstract bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs);

        public virtual void ClearContent()
        {
            BoardName = "";
            FirstPageUrl = null;
            LastPageUrl = null;
            PrevPageUrl = null;
            NextPageUrl = null;

            BoardPageUrl = null;
            CollectionPageUrl = null;
            ReservePageUrl = null;
            EssensePageUrl = null;

            TopTopicLinks.Clear();
            TopicLinks.Clear();
            
            IsLoaded = false;
        }

        protected void TriggerLoadedEvent(DataLoadedEventArgs loadedEventArgs)
        {
            if (BoardLoaded != null)
            {
                BoardLoaded(this, loadedEventArgs);
            }
        }
    }

    public class MitbbsBoardMobile : MitbbsBoardBase
    {
        private uint _numberOfSubpagesLoaded;
        private String _firstPageUrl;
        private String _lastPageUrl;
        private String _prevPageUrl;
        private String _nextPageUrl;

        private const int _numOfSubpagesToLoadAtOnce = 3;

        private String _titleTemplate = "(?<1>.*)-(?<2>.*)";
        String _boardUrlTemplate = "(?<1>.*)?board=(?<2>.*)";

        public MitbbsBoardMobile() : base()
        {
            _numberOfSubpagesLoaded = 0;
        }

        public override void ClearContent()
        {
            base.ClearContent();
            _numberOfSubpagesLoaded = 0;
        }

        private void CalculateEnBoardName()
        {
            Match match = Regex.Match(Url, _boardUrlTemplate);
            if (match.Success)
            {
                EnBoardName = match.Groups[2].Value;

                int index = EnBoardName.IndexOf("&");
                if (index >= 0)
                {
                    EnBoardName = EnBoardName.Substring(0, index);
                }
            }
            else
            {
                EnBoardName = null;
            }
        }

        protected override void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            DataLoadedEventArgs loadedEventArgs = new DataLoadedEventArgs();
            loadedEventArgs.Error = args.Error;

            if ((args.Document != null) && LoadFromHtml(args.Document.DocumentNode, loadedEventArgs))
            {
                _numberOfSubpagesLoaded++;

                if (_numberOfSubpagesLoaded < _numOfSubpagesToLoadAtOnce)
                {
                    if (_goForward)
                    {
                        if (_numberOfSubpagesLoaded == 1)
                        {
                            FirstPageUrl = _firstPageUrl;
                            PrevPageUrl = _prevPageUrl;
                        }

                        if (_nextPageUrl != null)
                        {
                            _url = _nextPageUrl;
                            _web.LoadAsync(_nextPageUrl);
                        }
                        else
                        {
                            IsLoaded = true;
                        }
                    }
                    else
                    {
                        if (_numberOfSubpagesLoaded == 1)
                        {
                            LastPageUrl = _lastPageUrl;
                            NextPageUrl = _nextPageUrl;
                        }

                        if (_prevPageUrl != null)
                        {
                            Url = _prevPageUrl;
                            _url = _prevPageUrl;
                            _web.LoadAsync(_prevPageUrl);
                        }
                        else
                        {
                            IsLoaded = true;
                        }
                    }
                }
                else
                {
                    IsLoaded = true;
                }

                if (IsLoaded)
                {
                    if (_goForward)
                    {
                        LastPageUrl = _lastPageUrl;
                        NextPageUrl = _nextPageUrl;
                    }
                    else
                    {
                        FirstPageUrl = _firstPageUrl;
                        PrevPageUrl = _prevPageUrl;
                    }

                    _web.LoadCompleted -= OnUrlLoaded;

                    // Load the first subpage again
                    // so the site will remember the last location
                    // and we will not need to parse the result
                    //
                    _web.LoadCompleted += OnLoadBoardFinalized;
                    _web.LoadAsync(Url);
                }
            }
            else
            {
                _web.LoadCompleted -= OnUrlLoaded;
                TriggerLoadedEvent(loadedEventArgs);
            }
        }

        private void OnLoadBoardFinalized(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnLoadBoardFinalized;
            DataLoadedEventArgs loadedEventArgs = new DataLoadedEventArgs();

            TriggerLoadedEvent(loadedEventArgs);
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            bool loaded = false;
            int insertIndex = 0;
            
            _firstPageUrl = null;
            _lastPageUrl = null;
            _prevPageUrl = null;
            _nextPageUrl = null;

            CalculateEnBoardName();

            try
            {
                IEnumerable<HtmlNode> titleNodes = RootNode.Descendants("title");
                foreach (HtmlNode titleNode in titleNodes)
                {
                    String titleText = HtmlUtilities.GetPlainHtmlText(titleNode.FirstChild.InnerText);
                    Match match = Regex.Match(titleText, _titleTemplate);
                    if (match.Success)
                    {
                        BoardName = match.Groups[2].Value;
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }

                IEnumerable<HtmlNode> divNodes = RootNode.Descendants("div");

                foreach (HtmlNode divNode in divNodes)
                {
                    if (divNode.Attributes.Contains("id"))
                    {
                        if (divNode.Attributes["id"].Value.StartsWith("sy_biaoti"))
                        {
                            IEnumerable<HtmlNode> linkNodes = divNode.Descendants("a");

                            foreach (HtmlNode linkNode in linkNodes)
                            {
                                String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText);
                                if (linkText == "版面")
                                {
                                    BoardPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "文摘区")
                                {
                                    CollectionPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "保留区")
                                {
                                    ReservePageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "精华区")
                                {
                                    EssensePageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "发表话题")
                                {
                                    NewPostUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "[首页]")
                                {
                                    _firstPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "[上页]")
                                {
                                    _prevPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "[下页]")
                                {
                                    _nextPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                                else if (linkText == "[末页]")
                                {
                                    _lastPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, linkNode.Attributes["href"].Value);
                                }
                            }
                        }
                        else if (divNode.Attributes["id"].Value == "bmwz_1")
                        {
                            HtmlNodeCollection topicNodes = divNode.ChildNodes;

                            for (int i = 0; i < topicNodes.Count; i++)
                            {
                                HtmlNode topicNode = topicNodes[i];

                                if (topicNode.Name == "div")
                                {
                                    MitbbsTopicLinkMobile topicLink = new MitbbsTopicLinkMobile();

                                    topicLink.ParentUrl = _url;
                                    if (topicLink.LoadFromHtml(topicNode))
                                    {
                                        if (IgnoreReadHistory)
                                        {
                                            topicLink.IsRead = false;
                                        }

                                        if (_goForward)
                                        {
                                            TopicLinks.Add(topicLink);
                                        }
                                        else
                                        {
                                            TopicLinks.Insert(insertIndex, topicLink);
                                            insertIndex++;
                                        }

                                        loaded = true;
                                    }
                                }
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
                loaded = false;
            }

            return loaded;
        }

    }

    public class MitbbsBoard : MitbbsBoardBase
    {
        private String _titleTemplate = @"(?<1>.*)\((?<2>.*)\)(?<3>.*)\((?<4>.*)\)";
        String _boardUrlTemplate = @"(?<1>.*)bbsdoc/(?<2>.*).html";
        String _boardUrlTemplate2 = @"(?<1>.*)bbsdoc1/(?<2>.*)_(?<3>.*)_(?<4>.*).html";
        String _boardUrlTemplate3 = @"(?<1>.*)bbsdoc2/(?<2>.*)_(?<3>.*)_(?<4>.*).html";

        [XmlIgnore]
        public bool UserIsLoggedIn = false;

        [XmlIgnore]
        public override String DisplayBoardName
        {
            get
            {
                if (String.IsNullOrEmpty(EnBoardName))
                {
                    return BoardName;
                }
                else
                {
                    return BoardName + "-" + EnBoardName;
                }
            }
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            try
            {
                CalculateEnBoardName();

                IEnumerable<HtmlNode> titleNodes = RootNode.Descendants("title");
                foreach (HtmlNode titleNode in titleNodes)
                {
                    String titleText = HtmlUtilities.GetPlainHtmlText(titleNode.FirstChild.InnerText);
                    Match match = Regex.Match(titleText, _titleTemplate);
                    if (match.Success)
                    {
                        BoardName = match.Groups[1].Value.Trim();
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }
                
                bool headNodeFound = false;
                var headNodes = from tbNode in RootNode.Descendants("td")
                                where (tbNode.Attributes.Contains("class") && tbNode.Attributes["class"].Value == "jiahui-4")
                                select tbNode;

                foreach (HtmlNode headNode in headNodes)
                {
                    foreach (HtmlNode linkNode in headNode.Descendants("a"))
                    {
                        String linkText = linkNode.GetLinkText();
                        String linkUrl = linkNode.GetLinkUrl(Url);

                        if (linkText == "首页")
                        {
                            FirstPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "上页")
                        {
                            PrevPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "下页")
                        {
                            NextPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "末页")
                        {
                            LastPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkUrl.Contains("mitbbs_postdoc.php"))
                        {
                            NewPostUrl = linkUrl;
                            headNodeFound = true;
                        }
                    }

                    if (headNodeFound)
                    {
                        IsLoaded = true;
                        break;
                    }
                }

                headNodeFound = false;
                headNodes = from tbNode in RootNode.Descendants("td")
                            where (tbNode.Attributes.Contains("class") && tbNode.Attributes["class"].Value == "news-bg")
                            select tbNode;

                foreach (HtmlNode headNode in headNodes)
                {
                    foreach (HtmlNode linkNode in headNode.Descendants("a"))
                    {
                        String linkText = linkNode.GetLinkText();
                        String linkUrl = linkNode.GetLinkUrl(Url);

                        if (linkText == "俱乐部" || linkText == "版面")
                        {
                            BoardPageUrl = linkUrl;
                        }
                        else if (linkText == "文摘区")
                        {
                            CollectionPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "保留区")
                        {
                            ReservePageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "精华区" && !String.IsNullOrEmpty(EnBoardName))
                        {
                            String mobileLink;
                            if (MitbbsLinkConverter.FullBoardEssenceLinkToMobileLink(linkUrl, out mobileLink))
                            {
                                EssensePageUrl = mobileLink;
                            }

                            headNodeFound = true;
                        }
                    }

                    if (headNodeFound)
                    {
                        IsLoaded = true;
                        break;
                    }
                }

                foreach (HtmlNode linkNode in RootNode.Descendants("a"))
                {
                    MitbbsTopicLink topicLinkNode = new MitbbsTopicLink();
                    topicLinkNode.ParentUrl = Url;

                    if (topicLinkNode.LoadFromHtml(linkNode, UserIsLoggedIn))
                    {
                        if (IgnoreReadHistory)
                        {
                            topicLinkNode.IsRead = false;
                        }

                        if (HideTopArticle && topicLinkNode.IsOnTop)
                        {
                            TopTopicLinks.Add(topicLinkNode);
                        }
                        else
                        {
                            TopicLinks.Add(topicLinkNode);
                        }
                        IsLoaded = true;
                    }
                }
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
            }
            finally
            {
                if (IsLoaded)
                {
                    if (BoardPageUrl == null)
                    {
                    }
                    else if (CollectionPageUrl == null)
                    {
                        BoardName = BoardName + " (文摘区)";
                    }
                    else if (ReservePageUrl == null)
                    {
                        BoardName = BoardName + " (保留区)";
                    }
                }
            }

            return IsLoaded;
        }

        private void CalculateEnBoardName()
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
                EnBoardName = match.Groups[2].Value;
            }
            else
            {
                EnBoardName = null;
            }
        }
    }

    public class MitbbsClubBoard : MitbbsBoardBase
    {
        private String _titleTemplate = @"(?<1>.*)-(?<2>.*)";
        String _boardUrlTemplate = @"(?<1>.*)club_bbsdoc/(?<2>.*).html";
        String _boardUrlTemplate2 = @"(?<1>.*)club_bbsdoc2/(?<2>.*)_(?<3>.*).html";

        [XmlIgnore]
        public override String DisplayBoardName
        {
            get
            {
                return BoardName;
            }
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            try
            {
                CalculateEnBoardName();

                IEnumerable<HtmlNode> titleNodes = RootNode.Descendants("title");
                foreach (HtmlNode titleNode in titleNodes)
                {
                    String titleText = HtmlUtilities.GetPlainHtmlText(titleNode.FirstChild.InnerText);
                    Match match = Regex.Match(titleText, _titleTemplate);
                    if (match.Success)
                    {
                        BoardName = match.Groups[1].Value.Trim();
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }

                bool headNodeFound = false;
                var headNodes = from tbNode in RootNode.Descendants("table")
                                where (tbNode.Attributes.Contains("class") && tbNode.Attributes["class"].Value == "jiahui-4")
                                select tbNode;

                foreach (HtmlNode headNode in headNodes)
                {
                    foreach (HtmlNode linkNode in headNode.Descendants("a"))
                    {
                        String linkText = linkNode.GetLinkText();
                        String linkUrl = linkNode.GetLinkUrl(Url);

                        if (linkText == "首页")
                        {
                            FirstPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "上页")
                        {
                            PrevPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "下页")
                        {
                            NextPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "末页")
                        {
                            LastPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkUrl.Contains("mitbbs_postdoc.php"))
                        {
                            NewPostUrl = linkUrl;
                            headNodeFound = true;
                        }
                    }

                    if (headNodeFound)
                    {
                        IsLoaded = true;
                        break;
                    }
                }

                headNodeFound = false;
                headNodes = from tbNode in RootNode.Descendants("td")
                            where (tbNode.Attributes.Contains("class") && tbNode.Attributes["class"].Value == "news-bg")
                            select tbNode;

                foreach (HtmlNode headNode in headNodes)
                {
                    foreach (HtmlNode linkNode in headNode.Descendants("a"))
                    {
                        String linkText = linkNode.GetLinkText();
                        String linkUrl = linkNode.GetLinkUrl(Url);

                        if (linkText == "俱乐部" || linkText == "版面")
                        {
                            BoardPageUrl = linkUrl;
                        }
                        else if (linkText == "文摘区")
                        {
                            CollectionPageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        else if (linkText == "保留区")
                        {
                            ReservePageUrl = linkUrl;
                            headNodeFound = true;
                        }
                        //else if (linkText == "精华区")
                        //{
                        //    EssensePageUrl = linkUrl;
                        //    headNodeFound = true;
                        //}
                    }

                    if (headNodeFound)
                    {
                        IsLoaded = true;
                        break;
                    }
                }

                foreach (HtmlNode linkNode in RootNode.Descendants("a"))
                {
                    MitbbsClubTopicLink topicLink = new MitbbsClubTopicLink();
                    topicLink.ParentUrl = Url;

                    if (topicLink.LoadFromHtml(linkNode))
                    {
                        if (IgnoreReadHistory)
                        {
                            topicLink.IsRead = false;
                        }

                        if (HideTopArticle && topicLink.IsOnTop)
                        {
                            TopTopicLinks.Add(topicLink);
                        }
                        else
                        {
                            TopicLinks.Add(topicLink);
                        }
                        IsLoaded = true;
                    }
                }
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
            }
            finally
            {
                if (IsLoaded)
                {
                    if (BoardPageUrl == null)
                    {
                    }
                    else if (CollectionPageUrl == null)
                    {
                        BoardName = BoardName + " (文摘区)";
                    }
                    else if (ReservePageUrl == null)
                    {
                        BoardName = BoardName + " (保留区)";
                    }
                }
            }

            return IsLoaded;
        }

        private void CalculateEnBoardName()
        {
            Match match = Regex.Match(Url, _boardUrlTemplate);
            if (!match.Success)
            {
                match = Regex.Match(Url, _boardUrlTemplate2);
            }

            if (match.Success)
            {
                EnBoardName = match.Groups[2].Value;
            }
            else
            {
                EnBoardName = null;
            }
        }
    }
}

