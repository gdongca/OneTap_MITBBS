using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public partial class SearchInBoardPage : PhoneApplicationPage
    {
        private String _boardName;
        private bool _club;
        private MitbbsSearchInBoard _search = new MitbbsSearchInBoard();

        public SearchInBoardPage()
        {
            InitializeComponent();
            _search.SearchCompleted += OnSearchCompleted;
            DataContext = _search;

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "SearchInBoard");
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("BoardName"))
            {
                _boardName = parameters["BoardName"];
                App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "版面搜索: " + _boardName);
            }
            else
            {
                _boardName = null;
                App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, null);
            }

            if (parameters.ContainsKey("Type"))
            {
                _club = parameters["Type"].ToLower() == "club";
            }
            else
            {
                _club = false;
            }

            if (State.ContainsKey("Title"))
            {
                TitleTextBox.Text = (String)State["Title"];
            }

            if (State.ContainsKey("Author"))
            {
                AuthorTextBox.Text = (String)State["Author"];
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            State["Title"] = TitleTextBox.Text;
            State["Author"] = AuthorTextBox.Text;

            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (SearchPanel.Visibility == System.Windows.Visibility.Collapsed)
            {
                e.Cancel = true;
                ShowSearchPanel(true);
            }

            base.OnBackKeyPress(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PageHelper.SessionHistoryHandlePageLoaded(NavigationService))
            {
                return;
            }

            if (_search.IsSearchCompleted)
            {
                ShowSearchPanel(false);
            }
            else
            {
                TitleText.Text = _boardName + "版内查询";
                ShowSearchPanel(true);
            }
        }

        private void ShowSearchPanel(bool toShow)
        {
            if (toShow)
            {
                SearchResultListBox.IsEnabled = false;
                SearchPanel.Visibility = Visibility.Visible;
            }
            else
            {
                SearchResultListBox.IsEnabled = true;
                SearchPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void StartSearch()
        {
            if ((_boardName == null) || (_boardName == ""))
            {
                return;
            }

            _search.Keyword1 = TitleTextBox.Text.Trim();
            _search.Author = AuthorTextBox.Text.Trim();

            if ((_search.Keyword1 == "") && (_search.Author == ""))
            {
                return;
            }

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //search button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //close button

            TitleText.Text = "正在搜索...";
            SearchProgress.Visibility = Visibility.Visible;
            ShowSearchPanel(false);
            SearchResultListBox.IsEnabled = false;
            
            _search.BoardName = _boardName;
            _search.IsClub = _club;
            _search.StartSearch(App.WebSession.CreateWebClient());
        }

        private void OnSearchCompleted(object sender, DataLoadedEventArgs args)
        {
            if (_search.IsSearchCompleted)
            {
                TitleText.Text = _boardName + "版内查询（" + _search.TopicLinks.Count + "条结果）";
                ShowSearchPanel(false);
            }
            else
            {
                TitleText.Text = _boardName + "版内查询";
                MessageBox.Show("搜索失败。请稍后再试一次。");
                ShowSearchPanel(true);
            }

            SearchProgress.Visibility = Visibility.Collapsed;
            
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //search button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //close button
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            if (SearchPanel.Visibility == Visibility.Collapsed)
            {
                ShowSearchPanel(true);
            }
            else
            {
                ShowSearchPanel(false);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void StartSearchButton_Click(object sender, RoutedEventArgs e)
        {
            StartSearch();
        }

        private void SearchResultListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsTopicSearchLink selected = ((sender as ListBox).SelectedItem as MitbbsTopicSearchLink);

            if (selected != null)
            {
                PageHelper.OpenMitbbsLink(selected, NavigationService);
            }

            (sender as ListBox).SelectedItem = null;
        }

        private void HideSearchButton_Click(object sender, RoutedEventArgs e)
        {
            ShowSearchPanel(false);
        }
    }
}