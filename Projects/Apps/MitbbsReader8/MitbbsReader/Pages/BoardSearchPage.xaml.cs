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

namespace Naboo.MitbbsReader.Pages
{
    public partial class BoardSearchPage : PhoneApplicationPage
    {
        private MitbbsBoardSearch _boardSearch;
        private String _sharedPicFieldId;

        public BoardSearchPage()
        {
            InitializeComponent();
            _boardSearch = MitbbsBoardSearch.Instance;
            DataContext = _boardSearch;
            
            _boardSearch.StartUpdateResults();

            App.Settings.ApplyPageSettings(this, LayoutRoot);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "搜索版面和俱乐部");

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("FileId"))
            {
                //This is a picture being shared
                _sharedPicFieldId = parameters["FileId"];
                PageTitle.Text = "分享图片: 请输入发帖版面";
            }

            if (State.ContainsKey("ExitWhenShown"))
            {
                Boolean exitWhenShown = (Boolean)State["ExitWhenShown"];
                if (exitWhenShown)
                {
                    App.Quit();
                }
            }

            if (State.ContainsKey("SearchKey"))
            {
                _boardSearch.SearchKeyword = (String)State["SearchKey"];
            }
            else
            {
                _boardSearch.SearchKeyword = "";
            }

            SearchTextBox.Text = _boardSearch.SearchKeyword;
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            State["SearchKey"] = _boardSearch.SearchKeyword;

            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            _boardSearch.SearchKeyword = "";
            base.OnBackKeyPress(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(_sharedPicFieldId))
            {
                App.Track("Navigation", "OpenPage", "BoardSearch");
            }
            else
            {
                App.Track("Navigation", "OpenPage", "SharePictureFromApp");
            }

            if (PageHelper.SessionHistoryHandlePageLoaded(NavigationService))
            {
                return;
            }

            if (!String.IsNullOrEmpty(_sharedPicFieldId) && !App.Settings.LogOn)
            {
                MessageBox.Show("用户未登录！", "无法分享图片", MessageBoxButton.OK);
                App.Quit();
                return;
            }

            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _boardSearch.SearchKeyword = SearchTextBox.Text;
        }

        private void BoardLinkListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsLink selected = ((sender as ListBox).SelectedItem as MitbbsLink);

            if (selected != null)
            {
                if (_sharedPicFieldId != null)
                {
                    if (selected is MitbbsBoardLink)
                    {
                        MitbbsBoardLink boardLink = selected as MitbbsBoardLink;
                        String url = String.Format(App.Settings.BuildUrl("/mitbbs_postdoc.php?board={0}&ftype=0"), boardLink.EnBoardName);
                        String pageUrl = String.Format(
                            "/Pages/NewPostPage.xaml?Url={0}&SendFrom=true&FullPage={1}&PicFileId={2}",
                            Uri.EscapeDataString(url), true, _sharedPicFieldId);
                        NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));

                        State["ExitWhenShown"] = true;
                    }
                    else if (selected is MitbbsClubLink)
                    {
                        MitbbsClubLink clubLink = selected as MitbbsClubLink;
                        String url = String.Format(
                            App.Settings.BuildUrl("/mitbbs_postdoc.php?board={0}&ftype=0&opflag=1"), 
                            clubLink.EnName
                            );
                        String pageUrl = String.Format(
                            "/Pages/NewPostPage.xaml?Url={0}&SendFrom=true&FullPage={1}&PicFileId={2}",
                            Uri.EscapeDataString(url), true, _sharedPicFieldId);
                        NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));

                        State["ExitWhenShown"] = true;
                    }
                }
                else
                {
                    PageHelper.OpenMitbbsLink(selected, NavigationService);
                }
            }

            (sender as ListBox).SelectedItem = null;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            _boardSearch.SearchKeyword = "";
            NavigationService.GoBack();
        }
    }
}