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
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public partial class BoardGroupPage : PhoneApplicationPage
    {
        private String _url;
        private MitbbsBoardGroup _boardGroup = new MitbbsBoardGroup();
        private double _scrollOffset;
        private bool _preload = false;

        public BoardGroupPage()
        {
            InitializeComponent();
            _boardGroup.BoardGroupLoaded += BoardGroup_Loaded;
            DataContext = _boardGroup;
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot);
        }

        private void LoadBoardGroup()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = false; //setting menu

            LoadBoardGroupProgressBar.Visibility = Visibility.Visible;
            BoardGroupLoadingText.Visibility = Visibility.Visible;
            BoardGroupNameTextBlock.Visibility = Visibility.Collapsed;
            BoardLinkListBox.Visibility = Visibility.Collapsed;

            if (_url != null)
            {
                _preload = false;
                MitbbsBoardGroup _saveBoardGroup;

                if (App.Settings.Preload & App.Settings.OfflineContentManager.TryLoadOfflineContent(App.Settings.BoardGroupPreloadOfflineID, _url, MitbbsSettings.OldContentExpirePeriod, out _saveBoardGroup))
                {
                    _boardGroup.BoardGroupLoaded -= BoardGroup_Loaded;
                    _boardGroup = _saveBoardGroup;
                    _boardGroup.BoardGroupLoaded += BoardGroup_Loaded;
                    DataContext = _boardGroup;

                    Naboo.AppUtil.AsyncCallHelper.DelayCall(
                        () => BoardGroup_Loaded(this, null)
                        );

                    _preload = true;
                }

                if (String.IsNullOrEmpty(_boardGroup.BoardGroupName))
                {
                    BoardGroupLoadingText.Text = "正在读取分类讨论区...";
                }
                else
                {
                    BoardGroupLoadingText.Text = "正在读取<" + _boardGroup.BoardGroupName + ">...";
                }

                if (_preload)
                {
                    return;
                }

                HtmlWeb web = App.WebSession.CreateWebClient();
                
                _boardGroup.LoadFromUrl(web, _url);
            }
            else
            {
                BoardGroupLoadingText.Text = "参数错误!";
            }
        }

        private void BoardGroup_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadBoardGroupProgressBar.Visibility = Visibility.Collapsed;

            if (_boardGroup.IsLoaded)
            {
                BoardGroupLoadingText.Visibility = Visibility.Collapsed;
                BoardGroupNameTextBlock.Visibility = Visibility.Visible;
                BoardLinkListBox.Visibility = Visibility.Visible;

                BoardGroupNameTextBlock.Text = _boardGroup.BoardGroupName;
                App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _boardGroup.BoardGroupName, "分类讨论区");

                if (_scrollOffset >= 0)
                {
                    //BoardLinkListPanel.UpdateLayout();
                    //BoardLinkListPanel.ScrollToVerticalOffset(_scrollOffset);
                    _scrollOffset = -1;
                }
                else
                {
                    //BoardLinkListPanel.UpdateLayout();
                    //BoardLinkListPanel.ScrollToVerticalOffset(0);
                }

                if (!_preload)
                {
                    App.Settings.OfflineContentManager.SaveOfflineContent(App.Settings.BoardGroupPreloadOfflineID, _url, _boardGroup);
                }
            }
            else
            {
                BoardGroupLoadingText.Text = "读取分类讨论区失败";
            }

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true; //setting menu
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("Url"))
            {
                _url = parameters["Url"];
            }
            else
            {
                _url = null;
            }

            if (State.ContainsKey("Url"))
            {
                _url = (String)State["Url"];
            }

            if (parameters.ContainsKey("Name"))
            {
                if (String.IsNullOrEmpty(_boardGroup.BoardGroupName))
                {
                    _boardGroup.BoardGroupName = parameters["Name"];
                }
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _boardGroup.BoardGroupName, "分类讨论区");

            if (!_boardGroup.IsLoaded && State.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = (double)State["ScrollOffset"];
            }
            else
            {
                _scrollOffset = -1;
            }

            base.OnNavigatedTo(e);

            if (!_boardGroup.IsLoaded)
            {
                LoadBoardGroup();
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            State["Url"] = _url;

            if (_boardGroup.IsLoaded)
            {
                //State["ScrollOffset"] = BoardLinkListPanel.VerticalOffset;
            }

            base.OnNavigatedFrom(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PageHelper.SessionHistoryHandlePageLoaded(NavigationService))
            {
                return;
            }
        }

        private void BoardLinkListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsLink link = ((sender as ListBox).SelectedItem as MitbbsLink);

            if (link != null)
            {
                if (link.Name == "俱乐部")
                {
                    MessageBox.Show("本软件暂时不支持俱乐部。请耐心等待后续版本更新。");
                }
                else
                {
                    PageHelper.OpenMitbbsLink(link, NavigationService);
                }

                (sender as ListBox).SelectedItem = null;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            App.Settings.OfflineContentManager.CleanupOfflineContent(App.Settings.BoardGroupPreloadOfflineID, _url);
            LoadBoardGroup();
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_boardGroup.Url);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void SettingMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenSettingPage(NavigationService);
        }

        private void DownloadStatusButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.NotficationCenter.OpenNotification(NavigationService);
        }

        private void SearchButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/BoardSearchPage.xaml", UriKind.Relative));
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent("", _url);
        }
    }
}