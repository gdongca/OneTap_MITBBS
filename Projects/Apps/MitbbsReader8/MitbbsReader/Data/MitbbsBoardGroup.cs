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
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class MitbbsBoardGroup
    {
        public static readonly String MobileBoardGroupHome = "/mobile/mbbssec.php";

        public String Url { get; set; }
        public String BoardGroupName { get; set; }

        public ObservableCollection<MitbbsLink> BoardLinks { get; set; }

        public bool IsLoaded { get; set; }

        public event EventHandler<DataLoadedEventArgs> BoardGroupLoaded;

        private HtmlWeb _web;

        private String _titleTemplate = "(?<1>.*)-(?<2>.*)";

        public MitbbsBoardGroup()
        {
            BoardLinks = new ObservableCollection<MitbbsLink>();
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
            BoardGroupName = "";

            BoardLinks.Clear();

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

            if (BoardGroupLoaded != null)
            {
                BoardGroupLoaded(this, loadedEventArgs);
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
                        BoardGroupName = match.Groups[2].Value;
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
                    if (divNode.Attributes["id"].Value == "bmwz")
                    {
                        HtmlNodeCollection boardLinkNodes = divNode.FirstChild.ChildNodes;
                        for (int i = 0; i < boardLinkNodes.Count; i++)
                        {
                            HtmlNode boardLinkNode = boardLinkNodes[i];
                            MitbbsLink boardLink = MitbbsLink.CreateLinkInstance(boardLinkNode, Url);
                            
                            if (boardLink != null)
                            {
                                BoardLinks.Add(boardLink);
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
    }

}
