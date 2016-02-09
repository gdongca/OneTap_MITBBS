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
    public class MitbbsClubHome
    {
        public static String ClubHomeUrl = @"/club/clubindex_t_g.html";

        public String Url { get; set; }
        public ObservableCollection<MitbbsLink> ClubGroupLinks { get; set; }

        public bool IsLoaded { get; set; }
        public event EventHandler<DataLoadedEventArgs> ClubHomeLoaded;

        private HtmlWeb _web;

        public MitbbsClubHome()
        {
            ClubGroupLinks = new ObservableCollection<MitbbsLink>();
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
            ClubGroupLinks.Clear();
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

            if (ClubHomeLoaded != null)
            {
                ClubHomeLoaded(this, loadedEventArgs);
            }
        }

        public bool LoadFromHtml(HtmlNode RootNode, DataLoadedEventArgs loadedEventArgs)
        {
            try
            {
                IEnumerable<HtmlNode> linkNodes = RootNode.Descendants("a");
                foreach (HtmlNode linkNode in linkNodes)
                {
                    MitbbsClubGroupLink clubGroupLink = new MitbbsClubGroupLink();
                    clubGroupLink.ParentUrl = Url;

                    if (clubGroupLink.LoadFromHtml(linkNode))
                    {
                        ClubGroupLinks.Add(clubGroupLink);
                        IsLoaded = true;
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
