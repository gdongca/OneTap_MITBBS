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
    public class MitbbsBoardEssence
    {
        public String Url { get; set; }
        public String BoardName { get; set; }

        public String BoardPageUrl { get; set; }
        public String CollectionPageUrl { get; set; }
        public String ReservePageUrl { get; set; }
        public String EssensePageUrl { get; set; }

        public ObservableCollection<MitbbsLink> EssenceLinks { get; set; }
        public bool IsLoaded { get; set; }

        public event EventHandler<DataLoadedEventArgs> BoardLoaded;

        private HtmlWeb _web;
        private String _titleTemplate = "(?<1>.*)-(?<2>.*)";
        
        public MitbbsBoardEssence()
        {
            EssenceLinks = new ObservableCollection<MitbbsLink>();
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
            BoardName = "";
            
            BoardPageUrl = null;
            CollectionPageUrl = null;
            ReservePageUrl = null;
            EssensePageUrl = null;

            EssenceLinks.Clear();

            IsLoaded = false;
        }

        private void OnUrlLoaded(object sender, HtmlDocumentLoadCompleted args)
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

            if (BoardLoaded != null)
            {
                BoardLoaded(this, loadedEventArgs);
            }
        }

        public bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
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
                        BoardName = match.Groups[2].Value + " (精华区)";
                        break;
                    }
                    else
                    {
                        return false;
                    }
                }

                bool firstHeaderRead = false;
                bool secondHeaderRead = false;

                IEnumerable<HtmlNode> divNodes = RootNode.Descendants("div");

                foreach (HtmlNode divNode in divNodes)
                {
                    if (divNode.Attributes["id"].Value == "sy_biaoti2")
                    {
                        if (!firstHeaderRead)
                        {
                            // First header
                            //
                            HtmlNodeCollection headNodes = divNode.ChildNodes[1].ChildNodes;

                            for (int i = 0; i < headNodes.Count; i++)
                            {
                                HtmlNode headNode = headNodes[i];
                                HtmlNode linkNode = null;
                                if (headNode.Name == "a")
                                {
                                    linkNode = headNode;
                                }
                                else if (headNode.Name == "span")
                                {
                                    HtmlNodeCollection spanNodes = headNode.ChildNodes;
                                    for (int j = 0; j < spanNodes.Count; j++)
                                    {
                                        HtmlNode spanNode = spanNodes[j];
                                        if (spanNode.Name == "a")
                                        {
                                            linkNode = spanNode;
                                            break;
                                        }
                                    }
                                }

                                if (linkNode != null)
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
                                }
                            }
                            firstHeaderRead = true;
                        }
                        else if (!secondHeaderRead)
                        {
                            // Second header
                            //
                            secondHeaderRead = true;
                        }
                    }
                    else if (divNode.Attributes["id"].Value == "bmwz")
                    {
                        // because the mitbbs mobile board essence page is mal-formated
                        // we have to do some special handling
                        //
                        IEnumerable<HtmlNode> linkNodes = divNode.Descendants("a");

                        foreach (HtmlNode linkNode in linkNodes)
                        {
                            MitbbsLink essenceLink = MitbbsLink.CreateLinkInstance(linkNode, Url);

                            if (essenceLink != null)
                            {
                                EssenceLinks.Add(essenceLink);

                                IsLoaded = true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                loadedEventArgs.Error = e;
            }

            return IsLoaded;
        }
    }
}
