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
    public partial class ClubGroupPage : PhoneApplicationPage
    {
        private String _url;
        private MitbbsClubGroupBase _clubGroup = null;
        private double _scrollOffset;
        bool _preload = false;
        bool _clubHome = false;

        public ClubGroupPage()
        {
            InitializeComponent();
            InitializeComponent();
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot);
        }

        private void LoadClubGroup()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = false; //setting menu

            LoadClubGroupProgressBar.Visibility = Visibility.Visible;
            ClubGroupLoadingText.Visibility = Visibility.Visible;
            ClubGroupNameTextBlock.Visibility = Visibility.Collapsed;
            ClubLinkListBox.Visibility = Visibility.Collapsed;

            if (_url != null)
            {
                _preload = false;
                MitbbsClubGroupBase _saveClubGroup;
                if (App.Settings.Preload & App.Settings.OfflineContentManager.TryLoadOfflineContent(App.Settings.BoardGroupPreloadOfflineID, _url, MitbbsSettings.OldContentExpirePeriod, out _saveClubGroup))
                {
                    _clubGroup.ClubGroupLoaded -= ClubGroup_Loaded;
                    _clubGroup = _saveClubGroup;
                    _clubGroup.ClubGroupLoaded += ClubGroup_Loaded;
                    DataContext = _clubGroup;

                    Naboo.AppUtil.AsyncCallHelper.DelayCall(
                        () => ClubGroup_Loaded(this, null)
                        );

                    _preload = true;
                }

                if (String.IsNullOrEmpty(_clubGroup.ClubGroupName))
                {
                    ClubGroupLoadingText.Text = "正在读取俱乐部...";
                }
                else
                {
                    ClubGroupLoadingText.Text = "正在读取<" + _clubGroup.ClubGroupName + ">...";
                }

                if (_preload)
                {
                    return;
                }

                HtmlWeb web = App.WebSession.CreateWebClient();
                
                _clubGroup.LoadFromUrl(web, _url);
            }
            else
            {
                ClubGroupLoadingText.Text = "参数错误!";
            }
        }

        private void ClubGroup_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadClubGroupProgressBar.Visibility = Visibility.Collapsed;

            if (_clubGroup.IsLoaded)
            {
                ClubGroupLoadingText.Visibility = Visibility.Collapsed;
                ClubGroupNameTextBlock.Visibility = Visibility.Visible;
                ClubLinkListBox.Visibility = Visibility.Visible;

                ClubGroupNameTextBlock.Text = _clubGroup.ClubGroupName;

                if (_scrollOffset >= 0)
                {
                    //ClubLinkListPanel.UpdateLayout();
                    //ClubLinkListPanel.ScrollToVerticalOffset(_scrollOffset);
                    _scrollOffset = -1;
                }
                else
                {
                    //ClubLinkListPanel.UpdateLayout();
                    //ClubLinkListPanel.ScrollToVerticalOffset(0);
                }

                if (!_preload)
                {
                    App.Settings.OfflineContentManager.SaveOfflineContent(App.Settings.BoardGroupPreloadOfflineID, _url, _clubGroup);
                }
            }
            else
            {
                ClubGroupLoadingText.Text = "读取俱乐部失败";
            }

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true; //setting menu
        }

        private MitbbsClubGroupBase CreateClubGroup()
        {
            if (_clubHome)
            {
                return new MitbbsClubHomeGroup();
            }
            else
            {
                return new MitbbsClubGroupAllPages();
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            PageHelper.InitAdControl(AdGrid);

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("Url"))
            {
                _url = parameters["Url"];
            }
            else
            {
                _url = null;
            }

            if (parameters.ContainsKey("ClubHome"))
            {
                _clubHome = bool.Parse(parameters["ClubHome"]);
            }
            else
            {
                _clubHome = false;
            }

            if (State.ContainsKey("Url"))
            {
                _url = (String)State["Url"];
            }

            if (_clubGroup == null || !_clubGroup.IsLoaded)
            {
                if (_clubGroup != null)
                {
                    _clubGroup.ClubGroupLoaded -= ClubGroup_Loaded;
                }

                _clubGroup = CreateClubGroup();
                _clubGroup.ClubGroupLoaded += ClubGroup_Loaded;
                DataContext = _clubGroup;
            }

            if (parameters.ContainsKey("Name"))
            {
                if (String.IsNullOrEmpty(_clubGroup.ClubGroupName))
                {
                    _clubGroup.ClubGroupName = parameters["Name"];
                }
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _clubGroup.ClubGroupName, "俱乐部分类");

            if (!_clubGroup.IsLoaded && State.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = (double)State["ScrollOffset"];
            }
            else
            {
                _scrollOffset = -1;
            }

            base.OnNavigatedTo(e);

            if (!_clubGroup.IsLoaded)
            {
                LoadClubGroup();
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            PageHelper.CleanupAdControl(AdGrid);

            State["Url"] = _url;

            if (_clubGroup.IsLoaded)
            {
                //State["ScrollOffset"] = ClubLinkListPanel.VerticalOffset;
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

        private void ClubLinkListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsLink link = ((sender as ListBox).SelectedItem as MitbbsLink);

            if (link != null)
            {
                PageHelper.OpenMitbbsLink(link, NavigationService);
                
                (sender as ListBox).SelectedItem = null;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            App.Settings.OfflineContentManager.CleanupOfflineContent(App.Settings.BoardGroupPreloadOfflineID, _url);
            LoadClubGroup();
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_clubGroup.Url);
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