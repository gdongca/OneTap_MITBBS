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
    public class MitbbsSearchInBoard
    {
        public String BoardName { get; set; }

        public String Keyword1 { get; set; }
        public String Keyword2 { get; set; }
        public String ExcludedKeyword { get; set; }
        public String Author { get; set; }
        public uint DaysToSearch { get; set; }
        public bool ExcludeReplies { get; set; }
        public bool IsClub { get; set; }

        public ObservableCollection<MitbbsTopicSearchLink> TopicLinks { get; private set; }
        public event EventHandler<DataLoadedEventArgs> SearchCompleted;

        public bool IsSearchCompleted { get; private set; }

        private String _searchUrl;
        private String _resultUrl;
        private HtmlWeb _web;
        private bool _isSearchPageLoaded = false;
        
        private static String _searchUrlTemplate = "/mitbbs_bbsbfind.php?board={0}&opflag={1}";

        public MitbbsSearchInBoard()
        {
            TopicLinks = new ObservableCollection<MitbbsTopicSearchLink>();

            BoardName = null;
            Keyword1 = null;
            Keyword2 = null;
            ExcludedKeyword = null;
            Author = null;
            DaysToSearch = 365;
            ExcludeReplies = true;
        }

        public void StartSearch(HtmlWeb web)
        {
            LoadSearchPage(web);
        }

        private void ClearContent()
        {
            TopicLinks.Clear();

            _searchUrl = null;
            _resultUrl = null;
            
            _isSearchPageLoaded = false;
            IsSearchCompleted = false;
        }

        private void LoadSearchPage(HtmlWeb web)
        {
            ClearContent();
            _web = web;
            _searchUrl = String.Format(App.Settings.BuildUrl(_searchUrlTemplate), BoardName, IsClub ? 1 : 0);

            _web.LoadCompleted += OnSearchPageLoaded;
            _web.LoadAsync(_searchUrl);
        }

        private void OnSearchPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnSearchPageLoaded;

            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    IEnumerable<HtmlNode> formNodes = args.Document.DocumentNode.Descendants("form");
                    foreach (HtmlNode formNode in formNodes)
                    {
                        if ((formNode.Attributes["name"] != null) && (formNode.Attributes["name"].Value == "form_query"))
                        {
                            _web.FormElements = new FormElementCollection(formNode.ParentNode);
                            _resultUrl = HtmlUtilities.GetAbsoluteUrl(_searchUrl, formNode.Attributes["action"].Value);

                            _isSearchPageLoaded = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    loadedArgs.Error = e;
                }
            }
            else
            {
                loadedArgs.Error = args.Error;
            }

            if (_isSearchPageLoaded)
            {
                LoadResultPage();
            }
            else
            {
                if (SearchCompleted != null)
                {
                    SearchCompleted(this, loadedArgs);
                }
            }
        }

        private void LoadResultPage()
        {
            if (_isSearchPageLoaded && (_resultUrl != null))
            {
                if (BoardName != null)
                {
                    _web.FormElements["board"] = BoardName;
                }

                if (Keyword1 != null)
                {
                    _web.FormElements["title"] = Keyword1;
                }

                if (Keyword2 != null)
                {
                    _web.FormElements["title2"] = Keyword2;
                }

                if (ExcludedKeyword != null)
                {
                    _web.FormElements["title3"] = ExcludedKeyword;
                }

                if (Author != null)
                {
                    _web.FormElements["userid"] = Author;
                }

                if (ExcludeReplies)
                {
                    _web.FormElements["og"] = "on";
                }

                _web.FormElements["dt"] = DaysToSearch.ToString();

                _web.LoadCompleted += OnResultPageLoaded;
                _web.LoadAsync(_resultUrl, HtmlWeb.OpenMode.Post);
            }
        }

        private void OnResultPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnResultPageLoaded;
            DataLoadedEventArgs loadedArgs = new DataLoadedEventArgs();

            if (args.Document != null)
            {
                try
                {
                    IEnumerable<HtmlNode> trNodes = args.Document.DocumentNode.Descendants("tr");
                    foreach (HtmlNode trNode in trNodes)
                    {
                        MitbbsTopicSearchLink searchLink = new MitbbsTopicSearchLink();
                        searchLink.ParentUrl = _resultUrl;

                        if (searchLink.LoadFromHtml(trNode))
                        {
                            TopicLinks.Insert(0, searchLink);
                        }
                    }

                    IsSearchCompleted = true;
                }
                catch (Exception e)
                {
                    loadedArgs.Error = e;
                }
            }
            else
            {
                loadedArgs.Error = args.Error;
            }

            if (SearchCompleted != null)
            {
                SearchCompleted(this, loadedArgs);
            }
        }
    }
}
