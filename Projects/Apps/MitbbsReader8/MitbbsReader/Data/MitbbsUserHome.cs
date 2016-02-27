using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Text;
using System.Windows.Shapes;
using System.Xml.Serialization;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Naboo.MitbbsReader
{
    public class MitbbsUserHome
    {
        //public static String MitbbsMobileUserHomeUrl = "/mobile/mkjjy.php";
        public static String MitbbsMobileUserHomeUrl = "/mwap/home/index.php";
        public static String MitbbsMobileUserHomeRootUrl = "/mwap/home/";

        public String Url { get; set; }
        public String MailboxUrl { get; set; }
        public ObservableCollection<MitbbsLink> MyBoards { get; set; }
        public ObservableCollection<MitbbsSimpleTopicLinkBase> MyArticles { get; set; }
        public MitbbsClubGroup MyClubGroup { get; set; }
        public bool IsLoaded { get; set; }
        public event EventHandler<DataLoadedEventArgs> UserHomeLoaded;

        [XmlIgnore]
        public ObservableCollection<MitbbsLink> MyClubs
        {
            get
            {
                return MyClubGroup.ClubLinks;
            }
        }

        private HtmlWeb _web;
        
        public MitbbsUserHome()
        {
            MyBoards = new ObservableCollection<MitbbsLink>();
            MyArticles = new ObservableCollection<MitbbsSimpleTopicLinkBase>();
            MyClubGroup = new MitbbsClubGroup();
            MyClubGroup.ClubGroupLoaded += OnMyClubLoaded;

            IsLoaded = false;
        }

        public void ClearContent()
        {
            MyBoards.Clear();
            MyArticles.Clear();
            MailboxUrl = null;
            MyClubGroup.ClearContent();

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

        private void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnUrlLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                if (LoadFromHtml(args.Document.DocumentNode, loadedArgs))
                {
                    IsLoaded = true;
                    MyClubGroup.LoadFromUrl(_web, App.Settings.BuildUrl(MitbbsClubGroup.UserClubGroupUrl));
                    return;
                }
            }
            else
            {
                loadedArgs.Error = args.Error;
            }

            if (UserHomeLoaded != null)
            {
                UserHomeLoaded(this, loadedArgs);
            }
        }

        private void OnMyClubLoaded(object sender, DataLoadedEventArgs args)
        {
            // Add local favorites to the board list
            // TODO: add an option to control the behavior
            //
            foreach (MitbbsLink boardLink in App.Settings.BoardBookmarks)
            {
                InsertMyBoard(boardLink);
            }

            //IsLoaded = MyClubGroup.IsLoaded;
            
            if (UserHomeLoaded != null)
            {
                UserHomeLoaded(this, args);
            }
        }

        public bool LoadFromHtml(HtmlNode rootNode, DataLoadedEventArgs loadedArgs)
        {
            bool isUserHomeLoaded = false;

            try
            {
                var divNodes = rootNode.Descendants("div");
                foreach (var divNode in divNodes)
                {
                    if (divNode.Attributes["class"].Value == "jy")
                    {
                        var ulNodes = divNode.Descendants("ul");
                        foreach (var ulNode in ulNodes)
                        {
                            if (ulNode.GetAttributeValue("class", "").Contains("jy_group"))
                            {
                                foreach (var liNode in ulNode.ChildNodes)
                                {
                                    if (liNode.Name == "li")
                                    {
                                        if (liNode.FirstChild.InnerText.Trim() == "我的讨论区")
                                        {
                                            var relativeBoardsURL = liNode.FirstChild.Attributes["href"].Value;
                                            LoadMyBoards(relativeBoardsURL);
                                        }

                                        IEnumerable<HtmlNode> linkNodes = liNode.Descendants("a");

                                        foreach (HtmlNode linkNode in linkNodes)
                                        {
                                            String linkText = HtmlUtilities.GetPlainHtmlText(linkNode.FirstChild.InnerText);
                                            Debug.WriteLine("link: " + linkText);

                                            if (linkText == "我的邮箱")
                                            {
                                                MailboxUrl = linkNode.Attributes["href"].Value;
                                                isUserHomeLoaded = true;
                                            }
                                            //else if (linkText == "我的讨论区")
                                            //{
                                            //    MitbbsBoardLinkMobile boardLink = new MitbbsBoardLinkMobile();
                                            //    boardLink.ParentUrl = Url;
                                            //    if (boardLink.LoadFromHtml(linkNode))
                                            //    {
                                            //        MyBoards.Add(MitbbsBoardLink.CreateFromMobileLink(boardLink));
                                            //    }
                                            //}
                                            else if (linkText == "我的文章")
                                            {
                                                MitbbsSimpleTopicLinkMobile topicLink = new MitbbsSimpleTopicLinkMobile();
                                                topicLink.ParentUrl = Url;
                                                if (topicLink.LoadFromHtml(linkNode))
                                                {
                                                    MyArticles.Add(MitbbsSimpleTopicLink.CreateFromMobileLink(topicLink));
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                loadedArgs.Error = e;
            }

            return isUserHomeLoaded;
        }

        private void LoadMyBoards(string relativeBoardsURL)
        {
            var url = App.Settings.BuildUrl(MitbbsUserHome.MitbbsMobileUserHomeRootUrl + relativeBoardsURL);
            //var web = new HtmlWeb();
            _web.LoadCompleted += OnBoardListLoaded;
            _web.LoadAsync(url);
        }

        private void OnBoardListLoaded(object sender, HtmlDocumentLoadCompleted e)
        {
            _web.LoadCompleted -= OnBoardListLoaded;
            if (e.Document != null)
            {
                foreach (var node in e.Document.DocumentNode.Descendants("ul"))
                {
                    if (node.GetAttributeValue("class", "").Contains("hot_list_wrap"))
                    {
                        foreach (var liNode in node.ChildNodes)
                        {
                            if (liNode.Name == "li")
                            {
                                var boardLink = new MitbbsBoardLinkMobile();
                                boardLink.ParentUrl = App.Settings.BuildUrl("");
                                foreach (var aNode in liNode.Descendants("a"))
                                {
                                    boardLink.LoadFromHtml(aNode);
                                    MyBoards.Add(MitbbsBoardLink.CreateFromMobileLink(boardLink));
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
        }

        private int CompareBoardNames(String name1, String name2)
        {
            String boardNameTemplate = "(?<1>.*)\\((?<2>.*)\\)";
            String enName1;
            String enName2;

            Match match = Regex.Match(name1, boardNameTemplate);
            if (!match.Success)
            {
                return 1;
            }

            enName1 = match.Groups[2].Value.ToUpper();

            match = Regex.Match(name2, boardNameTemplate);
            if (!match.Success)
            {
                return -1;
            }

            enName2 = match.Groups[2].Value.ToUpper();

            return enName1.CompareTo(enName2);
        }

        private void InsertLink(MitbbsLink link, ObservableCollection<MitbbsLink> links)
        {
            if (!link.Name.EndsWith("*"))
            {
                link.Name += " *";
            }

            for (int i = 0; i < links.Count; i++)
            {
                if (link.Name == links[i].Name || link.Url == links[i].Url)
                {
                    return;
                }

                if (CompareBoardNames(link.Name, links[i].Name) < 0)
                {
                    links.Insert(i, link);
                    return;
                }
            }

            links.Add(link);
        }

        public void InsertMyBoard(MitbbsLink boardLink)
        {
            if (boardLink is MitbbsBoardLinkBase)
            {
                InsertLink(boardLink, MyBoards);
            }
            else if (boardLink is MitbbsClubLink)
            {
                InsertLink(boardLink, MyClubs);
            }
        }
    }
}
