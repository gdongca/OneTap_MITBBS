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
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml.Serialization;
using HtmlAgilityPack;
using System.Diagnostics;

namespace Naboo.MitbbsReader
{
    public class MitbbsPageLink
    {
        public int PageIndex { get; set; }
        public String PageUrl { get; set; }
    }

    [XmlInclude(typeof(MitbbsTopicMobile))]
    [XmlInclude(typeof(MitbbsTopic))]
    [XmlInclude(typeof(MitbbsTopicEssenceMobile))]
    [XmlInclude(typeof(MitbbsMail))]
    public abstract class MitbbsTopicBase
    {
        public String Url { get; set; }
        public String Title { get; set; }
        public String BoardName { get; set; }
        public String FirstAuthor { get; set; }
        public String BoardUrl { get; set; }
        public String ReplyUrl { get; set; }

        public String FirstPageUrl { get; set; }
        public String LastPageUrl { get; set; }
        public String PrevPageUrl { get; set; }
        public String NextPageUrl { get; set; }

        public int PageIndex { get; set; }
        public int LastPageIndex { get; set; }
        
        public bool IsLoaded { get; set; }

        [XmlIgnore]
        public Dictionary<int, String> PageLinks { get; set; }
        public ObservableCollection<MitbbsPostBase> Posts { get; set; }

        public event EventHandler<DataLoadedEventArgs> TopicLoaded;

        protected HtmlWeb _web;
        protected String _alternateUrl;
        protected int _pageToLoad;

        public List<KeyValuePair<int, String>> PageLinkList
        {
            get
            {
                return PageLinks.ToList();
            }

            set
            {
                PageLinks.Clear();
                foreach (var entry in value)
                {
                    PageLinks.Add(entry.Key, entry.Value);
                }
            }
        }

        public MitbbsTopicBase()
        {
            Posts = new ObservableCollection<MitbbsPostBase>();
            PageLinks = new Dictionary<int,string>();
            ClearContent();
        }

        public virtual void LoadFromUrl(HtmlWeb web, String url, int pageToLoad = -1)
        {
            Url = url;
            IsLoaded = false;
            ClearContent();

            _pageToLoad = pageToLoad;
            _web = web;
            _web.LoadCompleted += OnUrlLoaded;
            _web.LoadAsync(url);
        }

        protected virtual void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            DataLoadedEventArgs loadedEventArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    if (!LoadFromHtml(args.Document.DocumentNode, loadedEventArgs))
                    {
                        if (_alternateUrl != null)
                        {
                            Url = _alternateUrl;
                            _web.LoadAsync(Url);
                            return;
                        }
                    }
                    else if (_pageToLoad >= 1 && PageIndex >= 1 && _pageToLoad != PageIndex)
                    {
                        int pageNum = _pageToLoad;
                        if (_pageToLoad == int.MaxValue)
                        {
                            pageNum = LastPageIndex;
                        }

                        String newUrl;
                        if (PageLinks.TryGetValue(pageNum, out newUrl))
                        {
                            Url = newUrl;
                            ClearContent();
                            _web.LoadAsync(Url);
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    loadedEventArgs.Error = e;
                }
            }
            else
            {
                loadedEventArgs.Error = args.Error;
            }

            _web.LoadCompleted -= OnUrlLoaded;

            if (_alternateUrl == null)
            {
                TriggerLoadedEvent(loadedEventArgs);
            }
        }

        protected void TriggerLoadedEvent(DataLoadedEventArgs loadedEventArgs)
        {
            if (TopicLoaded != null)
            {
                TopicLoaded(this, loadedEventArgs);
            }
        }

        public virtual void ClearContent()
        {
            BoardName = "";
            FirstAuthor = "";
            BoardUrl = null;

            FirstPageUrl = null;
            PrevPageUrl = null;
            NextPageUrl = null;
            LastPageUrl = null;
            PageIndex = 0;
            LastPageIndex = 0;

            PageLinks.Clear();
            Posts.Clear();
            _alternateUrl = null;
            _pageToLoad = -1;

            IsLoaded = false;
        }

        public abstract bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs);
    }

    public class MitbbsTopicMobile : MitbbsTopicBase
    {
        private String _topicUrlTemplate = "(?<1>.*)board=(?<2>.*)&(?<3>.*)";

        private void CalculateBoardUrl()
        {
            Match match = Regex.Match(Url, _topicUrlTemplate);
            if (match.Success)
            {
                String boardId = match.Groups[2].Value;
                String boardUrl = "mbbsdoc.php?board=" + boardId;

                BoardUrl = HtmlUtilities.GetAbsoluteUrl(Url, boardUrl);
            }
            else
            {
                BoardUrl = null;
            }
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            try
            {
                ClearContent();

                CalculateBoardUrl();

                IEnumerable<HtmlNode> divNodes = RootNode.Descendants("div");

                foreach (HtmlNode divNode in divNodes)
                {
                    if (divNode.Attributes.Contains("id"))
                    {
                        if (divNode.Attributes["id"].Value == "wenzhangye1")
                        {
                            HtmlNodeCollection headNodes = divNode.ChildNodes[0].ChildNodes;

                            for (int i = 0; i < headNodes.Count; i++)
                            {
                                HtmlNode headNode = headNodes[i];
                                if (headNode.Name == "a")
                                {
                                    HtmlNode linkNode = headNode.FirstChild;
                                    if (linkNode != null)
                                    {
                                        if (linkNode.InnerText == "首页")
                                        {
                                            FirstPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, headNode.Attributes["href"].Value);
                                        }
                                        else if (linkNode.InnerText == "上页")
                                        {
                                            PrevPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, headNode.Attributes["href"].Value);
                                        }
                                        else if (linkNode.InnerText == "下页")
                                        {
                                            NextPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, headNode.Attributes["href"].Value);
                                        }
                                        else if (linkNode.InnerText == "末页")
                                        {
                                            LastPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, headNode.Attributes["href"].Value);
                                        }
                                    }
                                }
                            }
                        }
                        else if (divNode.Attributes["id"].Value == "wenzhangyudu")
                        {
                            HtmlNodeCollection postNodes = divNode.ChildNodes[0].ChildNodes;

                            for (int i = 0; i < postNodes.Count; i++)
                            {
                                HtmlNode postNode = postNodes[i];
                                if (postNode.Name == "li")
                                {
                                    MitbbsPostBase post = new MitbbsPostMobile();
                                    post.ParentUrl = Url;
                                    if (post.LoadFromHtml(postNode))
                                    {
                                        if (Posts.Count <= 0)
                                        {
                                            Title = post.Title;
                                            BoardName = post.BoardName;
                                            FirstAuthor = post.Author;
                                            ReplyUrl = post.ReplyPostUrl;

                                            IsLoaded = true;
                                        }

                                        Posts.Add(post);
                                    }
                                }
                                else if (postNode.Name == "a")
                                {
                                    if (postNode.FirstChild.InnerText == "同主题阅读")
                                    {
                                        _alternateUrl = HtmlUtilities.GetAbsoluteUrl(Url, postNode.Attributes["href"].Value);
                                        return false;
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
                IsLoaded = false;
            }

            return IsLoaded;
        }
    }

    public class MitbbsTopic : MitbbsTopicBase
    {
        private String _clubTopicUrlTemplate = "(?<1>.*)/clubarticle_(?<4>.*)/(?<2>.*)/(?<3>.*).html";
        private String _clubTopicUrlTemplate2 = "(?<1>.*)/clubarticle/(?<2>.*)/(?<3>.*).html";

        private String _topicUrlTemplate = "(?<1>.*)/article_t/(?<2>.*)/(?<3>.*).html";
        private String _topicUrlTemplate2 = "(?<1>.*)/article/(?<2>.*)/(?<3>.*).html";
        private String _topicUrlTemplate3 = "(?<1>.*)/article_(?<4>.*)/(?<2>.*)/(?<3>.*).html";

        [XmlIgnore]
        public MitbbsPostDelete PostDelete { get; private set; }

        private void CalculatePageIndex()
        {
            int i = 1;
            while (true)
            {
                if (!PageLinks.ContainsKey(i))
                {
                    PageIndex = i;
                    break;
                }

                i++;
            }

            LastPageIndex = PageLinks.Count + 1;
        }

        private void CalculateBoardUrl()
        {
            Match match = Regex.Match(Url, _clubTopicUrlTemplate);
            if (!match.Success)
            {
                match = Regex.Match(Url, _clubTopicUrlTemplate2);
            }

            if (match.Success)
            {
                String boardId = match.Groups[2].Value;
                String boardUrl = "/club_bbsdoc1/" + boardId + "_1_0.html";

                BoardUrl = HtmlUtilities.GetAbsoluteUrl(Url, boardUrl);

                return;
            }

            match = Regex.Match(Url, _topicUrlTemplate);
            if (!match.Success)
            {
                match = Regex.Match(Url, _topicUrlTemplate2);
            }

            if (!match.Success)
            {
                match = Regex.Match(Url, _topicUrlTemplate3);
            }

            if (match.Success)
            {
                String boardId = match.Groups[2].Value;
                String boardUrl = "/bbsdoc1/" + boardId + "_1_0.html";

                BoardUrl = HtmlUtilities.GetAbsoluteUrl(Url, boardUrl);

                return;
            }

            BoardUrl = null;
        }

        public MitbbsTopic()
        {
            PostDelete = new MitbbsPostDelete();
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            try
            {
                _alternateUrl = null;

                bool headNodeFound = false;
                var headNodes = from tdNode in RootNode.Descendants("td")
                                where (tdNode.Attributes.Contains("class") && tdNode.Attributes["class"].Value == "news-bg")
                                select tdNode;

                foreach (HtmlNode headNode in headNodes)
                {
                    foreach (HtmlNode linkNode in headNode.Descendants("a"))
                    {
                        try
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
                            else
                            {
                                int pageNum;
                                if (int.TryParse(linkText, out pageNum))
                                {
                                    if (!PageLinks.ContainsKey(pageNum))
                                    {
                                        PageLinks.Add(pageNum, linkUrl);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());
                        }
                    }

                    if (headNodeFound)
                    {
                        break;
                    }
                }

                headNodes = from tdNode in RootNode.Descendants("td")
                            where (tdNode.Attributes.Contains("class") && tdNode.Attributes["class"].Value == "logo-bg")
                            select tdNode;
                foreach (HtmlNode headNode in headNodes)
                {
                    foreach (HtmlNode linkNode in headNode.Descendants("a"))
                    {
                        String linkText = linkNode.GetLinkText();
                        String linkUrl = linkNode.GetLinkUrl(Url);

                        if (linkText == "同主题阅读")
                        {
                            _alternateUrl = linkUrl;
                            return false;
                        }
                    }
                }

                var postNodes = from tdNode in RootNode.Descendants("td")
                                where (tdNode.Attributes.Contains("class") && tdNode.Attributes["class"].Value == "jiawenzhang-type")
                                select tdNode;

                foreach (HtmlNode postNode in postNodes)
                {
                    MitbbsPost post = new MitbbsPost();
                    post.ParentUrl = Url;
                    if (post.LoadFromHtml(postNode))
                    {
                        if (Posts.Count <= 0)
                        {
                            Title = post.Title;
                            BoardName = post.BoardName;
                            FirstAuthor = post.Author;
                            ReplyUrl = post.ReplyPostUrl;

                            IsLoaded = true;
                        }

                        Posts.Add(post);
                    }
                }

                var delFormNodes = from formNode in RootNode.Descendants("form")
                                   where formNode.Attributes.Contains("name") && formNode.Attributes["name"].Value == "delform"
                                   select formNode;

                foreach (HtmlNode delFormNode in delFormNodes)
                {
                    PostDelete.Initialize(App.WebSession.CreateWebClient(), delFormNode, Url);
                    break;
                }

                CalculateBoardUrl();
                CalculatePageIndex();
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
                IsLoaded = false;
            }

            return IsLoaded;
        }
    }
}
