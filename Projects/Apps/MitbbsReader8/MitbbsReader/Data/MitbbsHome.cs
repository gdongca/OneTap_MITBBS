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
using System.Xml.Serialization;
using System.Linq;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    [XmlInclude(typeof(MitbbsHome))]
    [XmlInclude(typeof(MitbbsHomeMobile))]
    public abstract class MitbbsHomeBase
    {
        public String Url { get; set; }
        public ObservableCollection<MitbbsLink> TopArticles { get; set; }
        public ObservableCollection<MitbbsLink> HotArticles { get; set; }
        public ObservableCollection<MitbbsLink> RecommendedArticles { get; set; }
        public ObservableCollection<MitbbsBoardLinkBase> HotBoards { get; set; }
        public MitbbsClubHome ClubHome { get; set; }
        public bool LoadClubHome { get; set; }

        [XmlIgnore]
        public ObservableCollection<MitbbsLink> ClubGroups
        {
            get
            {
                return ClubHome.ClubGroupLinks;
            }
        }

        public bool IsLoaded { get; set; }
        public event EventHandler<DataLoadedEventArgs> HomeLoaded;

        protected HtmlWeb _web;

        public MitbbsHomeBase()
        {
            TopArticles = new ObservableCollection<MitbbsLink>();
            HotArticles = new ObservableCollection<MitbbsLink>();
            RecommendedArticles = new ObservableCollection<MitbbsLink>();
            HotBoards = new ObservableCollection<MitbbsBoardLinkBase>();

            ClubHome = new MitbbsClubHome();
            ClubHome.ClubHomeLoaded += OnClubHomeLoad;

            LoadClubHome = false;
            IsLoaded = false;
        }

        public void LoadFromUrl(HtmlWeb web, String url)
        {
            ClearContent();
            Url = url;
            _web = web;

            _web.LoadCompleted += OnUrlLoaded;
            _web.LoadAsync(Url);
        }

        public void ClearContent()
        {
            TopArticles.Clear();
            HotArticles.Clear();
            RecommendedArticles.Clear();
            HotBoards.Clear();

            ClubHome.ClearContent();

            IsLoaded = false;
        }

        protected void TriggerHomeLoaded(DataLoadedEventArgs args)
        {
            if (HomeLoaded != null)
            {
                HomeLoaded(this, args);
            }
        }

        protected void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            bool isHomeLoaded = false;
            _web.LoadCompleted -= OnUrlLoaded;

            DataLoadedEventArgs loadedEventArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                isHomeLoaded = LoadFromHtml(args.Document.DocumentNode, loadedEventArgs);
            }
            else
            {
                loadedEventArgs.Error = args.Error;
            }

            if (isHomeLoaded && LoadClubHome)
            {
                ClubHome.LoadFromUrl(_web, App.Settings.BuildUrl(MitbbsClubHome.ClubHomeUrl));
                return;
            }
            else
            {
                IsLoaded = isHomeLoaded;
                TriggerHomeLoaded(loadedEventArgs);
            }
        }

        protected void OnClubHomeLoad(object sender, DataLoadedEventArgs args)
        {
            IsLoaded = ClubHome.IsLoaded;

            TriggerHomeLoaded(args);
        }

        public void ForceUpdateHistoryStatus()
        {
            foreach (var link in TopArticles)
            {
                link.UpdateLinkState();
            }

            foreach (var link in HotArticles)
            {
                link.UpdateLinkState();
            }

            foreach (var link in RecommendedArticles)
            {
                link.UpdateLinkState();
            }
        }

        public abstract bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs);
    }

    public class MitbbsHomeMobile : MitbbsHomeBase
    {
        public static string MitbbsMobileHomeUrl = "/mobile/index.php";

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            bool isHomeLoaded = false;

            try
            {
                IEnumerable<HtmlNode> divNodes = RootNode.Descendants("div");
                int counter = 0;
                foreach (HtmlNode divNode in divNodes)
                {
                    if (divNode.Attributes.Contains("id") && divNode.Attributes["id"].Value == "mnpage_first")
                    {
                        if (counter == 3)
                        {
                            // because the mitbbs mobile home page is mal-formated
                            // we have to do some special handling for the hot boards
                            // section
                            //
                            ExtractHotBoardEntries(divNode);
                        }
                        else
                        {
                            HtmlNodeCollection listNodes = divNode.ChildNodes[1].ChildNodes;
                            for (int i = 0; i < listNodes.Count; i++)
                            {
                                HtmlNode listNode = listNodes[i];

                                if (listNode.Name == "li")
                                {
                                    MitbbsSimpleTopicLinkMobile topicLink = new MitbbsSimpleTopicLinkMobile();
                                    topicLink.ParentUrl = Url;

                                    if (topicLink.LoadFromHtml(listNode))
                                    {
                                        switch (counter)
                                        {
                                            case 0:
                                                TopArticles.Add(topicLink);
                                                break;
                                            case 1:
                                                HotArticles.Add(topicLink);
                                                break;
                                            case 2:
                                                RecommendedArticles.Add(topicLink);
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        counter++;

                        if (counter >= 4)
                        {
                            isHomeLoaded = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
            }

            return isHomeLoaded;
        }

        protected void ExtractHotBoardEntries(HtmlNode rootNode)
        {
            IEnumerable<HtmlNode> linkNodes = rootNode.Descendants("a");

            foreach (HtmlNode linkNode in linkNodes)
            {
                MitbbsBoardLinkMobile boardLink = new MitbbsBoardLinkMobile();
                boardLink.ParentUrl = Url;

                if (boardLink.LoadFromHtml(linkNode))
                {
                    HotBoards.Add(MitbbsBoardLink.CreateFromMobileLink(boardLink));
                }
            }
        }
    }

    public class MitbbsHome : MitbbsHomeBase
    {
        public static string MitbbsHomeUrl = @"";

        public override bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            bool isHomeLoaded = false;

            try
            {
                if (loadArticleList(TopArticles, RootNode, "index_other_news"))
                {
                    isHomeLoaded = true;
                }

                if (loadArticleList(HotArticles, RootNode, "index_ten_big_left"))
                {
                    isHomeLoaded = true;
                }

                if (loadArticleList(RecommendedArticles, RootNode, "index_ten_big_right"))
                {
                    isHomeLoaded = true;
                }

                //var topArticleNodes = from tdNode in RootNode.Descendants("td")
                //                      where tdNode.Attributes.Contains("class") &&
                //                      tdNode.Attributes["class"].Value == "huibian_3"
                //                      select tdNode;

                //int index = 0;
                //foreach (HtmlNode topArticleNode in topArticleNodes)
                //{
                //    ObservableCollection<MitbbsLink> articleList = null;
                //    switch (index)
                //    {
                //        case 0:
                //            articleList = TopArticles;
                //            break;
                //        case 1:
                //            articleList = HotArticles;
                //            break;
                //        case 2:
                //            articleList = RecommendedArticles;
                //            break;
                //    }

                //    if (articleList == null)
                //    {
                //        break;
                //    }

                //    foreach (HtmlNode linkNode in topArticleNode.Descendants("a"))
                //    {
                //        MitbbsSimpleTopicLink topicLink = new MitbbsSimpleTopicLink();
                //        topicLink.ParentUrl = Url;

                //        if (topicLink.LoadFromHtml(linkNode))
                //        {
                //            articleList.Add(topicLink);
                //            isHomeLoaded = true;
                //        }
                //    }

                //    index++;
                //}
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
            }

            return isHomeLoaded;
        }

        private bool loadArticleList(ObservableCollection<MitbbsLink> articleList, HtmlNode RootNode, String className)
        {
            bool isListLoaded = false;
            var topArticleNodes = from tdNode in RootNode.Descendants("div")
                                  where tdNode.Attributes.Contains("class") &&
                                  tdNode.Attributes["class"].Value == className
                                  select tdNode;


            foreach (HtmlNode topArticleNode in topArticleNodes)
            {
                foreach (HtmlNode linkNode in topArticleNode.Descendants("a"))
                {
                    MitbbsSimpleTopicLink topicLink = new MitbbsSimpleTopicLink();
                    topicLink.ParentUrl = Url;

                    if (topicLink.LoadFromHtml(linkNode))
                    {
                        articleList.Add(topicLink);
                        isListLoaded = true;
                    }
                }
            }

            return isListLoaded;
        }

    }
}
