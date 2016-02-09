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
using System.Xml.Serialization;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    [XmlInclude(typeof(MitbbsClubGroup))]
    [XmlInclude(typeof(MitbbsClubHomeGroup))]
    [XmlInclude(typeof(MitbbsClubGroupAllPages))]
    public abstract class MitbbsClubGroupBase
    {
        public String Url { get; set; }
        public String ClubGroupName { get; set; }

        public ObservableCollection<MitbbsLink> ClubLinks { get; set; }

        public bool IsLoaded { get; set; }
        public event EventHandler<DataLoadedEventArgs> ClubGroupLoaded;

        protected HtmlWeb _web;

        public MitbbsClubGroupBase()
        {
            ClubLinks = new ObservableCollection<MitbbsLink>();
        }

        public virtual void LoadFromUrl(HtmlWeb web, String url)
        {
            ClearContent();
            Url = url;
            _web = web;

            _web.LoadCompleted += OnUrlLoaded;
            _web.LoadAsync(Url);
        }

        public virtual void ClearContent()
        {
            ClubGroupName = "";
            ClubLinks.Clear();
            IsLoaded = false;
        }

        protected void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnUrlLoaded;

            DataLoadedEventArgs loadedEventArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                LoadFromHtml(args.Document.DocumentNode, loadedEventArgs);
            }
            else
            {
                loadedEventArgs.Error = args.Error;
            }

            TriggerClubGroupLoaded(loadedEventArgs);
        }

        protected void TriggerClubGroupLoaded(DataLoadedEventArgs loadedEventArgs)
        {
            if (ClubGroupLoaded != null)
            {
                ClubGroupLoaded(this, loadedEventArgs);
            }
        }

        public abstract bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs);
    }

    public class MitbbsClubGroupAllPages : MitbbsClubGroupBase
    {
        private MitbbsClubGroup _clubGroup;

        public MitbbsClubGroupAllPages()
        {
            _clubGroup = new MitbbsClubGroup();
            _clubGroup.ClubGroupLoaded += OnClubGroupPageLoaded;
        }

        public override void LoadFromUrl(HtmlWeb web, String url)
        {
            ClearContent();
            Url = url;
            _web = web;

            _clubGroup.LoadFromUrl(web, Url);
        }

        private void OnClubGroupPageLoaded(object sender, DataLoadedEventArgs args)
        {
            if (_clubGroup.IsLoaded)
            {
                if (string.IsNullOrEmpty(ClubGroupName))
                {
                    ClubGroupName = _clubGroup.ClubGroupName;
                }

                foreach (MitbbsClubLink clubLink in _clubGroup.ClubLinks)
                {
                    ClubLinks.Add(clubLink);
                }

                if (_clubGroup.NextPageUrl != null)
                {
                    // Continue to load the remaining pages
                    //
                    _clubGroup.LoadFromUrl(_web, _clubGroup.NextPageUrl);
                    return;
                }
                else
                {
                    // Loading all pages is completed
                    //
                    IsLoaded = true;
                }
            }

            TriggerClubGroupLoaded(args);
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            throw new NotImplementedException();
        }
    }

    public class MitbbsClubGroup : MitbbsClubGroupBase
    {
        public static String UserClubGroupUrl = @"/club/mitbbs_club_myclub.php";

        public String FirstPageUrl { get; set; }
        public String LastPageUrl { get; set; }
        public String PrevPageUrl { get; set; }
        public String NextPageUrl { get; set; }

        private String _titleTemplate = @"(?<1>.*)-(?<2>.*)";
        private String _pageNavUrlPrefix = @"javascript:botpagehref";
        private String _jsUrlTemplate = @"javascript:botpagehref\('(?<1>.*)'\)";

        public override void ClearContent()
        {
            base.ClearContent();

            FirstPageUrl = null;
            LastPageUrl = null;
            PrevPageUrl = null;
            NextPageUrl = null;
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            try
            {
                IEnumerable<HtmlNode> titleNodes = RootNode.Descendants("title");
                foreach (HtmlNode titleNode in titleNodes)
                {
                    String titleText = HtmlUtilities.GetPlainHtmlText(titleNode.FirstChild.InnerText);
                    Match match = Regex.Match(titleText, _titleTemplate);
                    if (match.Success)
                    {
                        ClubGroupName = match.Groups[1].Value;
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }

                IEnumerable<HtmlNode> linkNodes = RootNode.Descendants("a");
                foreach (HtmlNode linkNode in linkNodes)
                {
                    MitbbsLink link = new MitbbsLink();
                    if (link.LoadFromHtml(linkNode))
                    {
                        if (link.Url.StartsWith(_pageNavUrlPrefix))
                        {
                            if (link.Name == "首页")
                            {
                                FirstPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, ParseJsUrl(link.Url));
                            }
                            else if (link.Name == "上页")
                            {
                                PrevPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, ParseJsUrl(link.Url));
                            }
                            else if (link.Name == "下页")
                            {
                                NextPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, ParseJsUrl(link.Url));
                            }
                            else if (link.Name == "末页")
                            {
                                LastPageUrl = HtmlUtilities.GetAbsoluteUrl(Url, ParseJsUrl(link.Url));
                            }

                            IsLoaded = true;
                        }
                        else
                        {
                            MitbbsClubLink clubLink = new MitbbsClubLink();
                            clubLink.ParentUrl = Url;

                            if (clubLink.LoadFromHtml(linkNode))
                            {
                                ClubLinks.Add(clubLink);
                                IsLoaded = true;
                            }
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                IsLoaded = false;
                loadedEventArgs.Error = e;
            }

            return IsLoaded;
        }

        private String ParseJsUrl(String jsUrl)
        {
            jsUrl = jsUrl.Trim();
            Match match = Regex.Match(jsUrl, _jsUrlTemplate);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return jsUrl;
        }
    }

    public class MitbbsClubHomeGroup : MitbbsClubGroupBase
    {
        private MitbbsClubHome _clubHome;

        public MitbbsClubHomeGroup()
        {
            _clubHome = new MitbbsClubHome();
            _clubHome.ClubHomeLoaded += OnClubHomeLoaded;
        }

        public override void LoadFromUrl(HtmlWeb web, String url)
        {
            ClearContent();
            ClubGroupName = "俱乐部";
            Url = url;
            _web = web;

            _clubHome.LoadFromUrl(web, Url);
        }

        private void OnClubHomeLoaded(object sender, DataLoadedEventArgs args)
        {
            IsLoaded = _clubHome.IsLoaded;

            if (_clubHome.IsLoaded)
            {
                ClubLinks.Clear();
                foreach (MitbbsLink link in _clubHome.ClubGroupLinks)
                {
                    ClubLinks.Add(link);
                }
            }

            TriggerClubGroupLoaded(args);
        }

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            throw new NotImplementedException();
        }
    }
}
