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
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using Naboo.AppUtil;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public partial class MainPage : PhoneApplicationPage
    {
        private bool _loaded = false;
        private MainPageData pageData = new MainPageData();
        private AppAlert _appAlert;
        private MitbbsBoardGroupOfflineDownloader _boardGroupDownloader = new MitbbsBoardGroupOfflineDownloader();
        private MitbbsClubHomeOfflineDownloader _clubHomeDownloader = new MitbbsClubHomeOfflineDownloader();
        private String lasVisitMenu = "上次访问";
        private Boolean _triedLogIn = false;

        public MainPage()
        {
            InitializeComponent();

            pageData.MenuItems = new ObservableCollection<AppMenuLink>();
            DataContext = pageData;

            DowloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

            if (App.Settings.PreviousSessionHistory.Count > 0 && App.Settings.KeepHistory)
            {
                pageData.MenuItems.Add(
                    new AppMenuLink()
                    {
                        Name = lasVisitMenu,
                        Image = "/Images/repeat_appbar.png",
                        AppAction =
                            () =>
                            {
                                App.Track("Navigation", "OpenPreviousPages", null);
                                App.Settings.PreviousSessionHistory.StartRestoreFromHistory(NavigationService);
                            },
                        Subtitle = App.Settings.PreviousSessionHistory.LastPageName
                    }
                    );
            }

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "未名热点",
                    Image = "/Images/home_appbar.png",
                    Url = "/Pages/MitbbsHomePage.xaml"
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "用户家页",
                    Image = "/Images/person_appbar.png",
                    Url = "/Pages/UserHomePage.xaml",
                    RequiresLogIn = true
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "未名版面",
                    Image = "/Images/lines_appbar.png",
                    Url = "/Pages/MitbbsHomePage.xaml?ShowPage=3"
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "历史收藏",
                    Image = "/Images/fav_appbar.png",
                    Url = "/Pages/HistoryPage.xaml"
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "离线内容",
                    Image = "/Images/download_appbar.png",
                    Url = "/Pages/OfflineContentPage.xaml"
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "试试手气",
                    Image = "/Images/dice_appbar.png",
                    AppAction =
                        () =>
                        {
                            PageHelper.TryMyLuck(NavigationService);
                        }
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "参数设置",
                    Image = "/Images/setting_appbar.png",
                    Url = "/Pages/SettingPage.xaml?SiteSettingOn=true",
                    BlockIfLoggingIn = true
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "使用帮助",
                    Image = "/Images/help_appbar.png",
                    Url = @"http://charmingco2.com/2011/12/01/tips-for-onetap-mitbbs-reader/"
                }
                );

            pageData.MenuItems.Add(
                new AppMenuLink()
                {
                    Name = "软件信息",
                    Image = "/Images/copyright_appbar.png",
                    Url = "/Pages/AboutPage.xaml"
                }
                );

            App.WebSession.LogInCompleted += OnLogOnOrLogOutCompleted;
            App.WebSession.LogOutCompleted += OnLogOnOrLogOutCompleted;

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "Main");
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (!_triedLogIn && App.Settings.LogOn && !App.WebSession.IsConnecting && !App.WebSession.IsLoggedIn)
            {
                _triedLogIn = true;
                while (!AppInfo.IsNetworkConnected())
                {
                    MessageBoxResult result = MessageBox.Show("目前没有可用的网络连接。需要重试吗？", "无法登录未名空间", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                //WebSession.LogInCompleted += OnLogOnCompleted;
                App.WebSession.StartLogIn(App.Settings.Username, App.Settings.Password);
            }

            App.Settings.CurrentSessionHistory.Reset();
            
            ApplicationTitle.Text = App.License.AppTitle + " (" + App.WebSession.Username + ")";

            if (App.Settings.PreviousSessionHistory.Count <= 0 && pageData.MenuItems[0].Name == lasVisitMenu)
            {
                pageData.MenuItems.RemoveAt(0);
            }

            base.OnNavigatedTo(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.NeedToExit)
            {
                NavigationService.GoBack();
            }
            else
            {
                if (!_loaded)
                {
                    _loaded = true;

                    _appAlert = new AppAlert(App.License, App.TheAppInfo);
                    _appAlert.FetchAlertPage(@"http://dl.dropbox.com/u/16498469/ProjectNaboo/MitbbsReaderAppAlert.xml", true);

                    //if (App.Settings.OfflineContentManager.DownloadQueue.Count > 0)
                    //{
                    //    AsyncCallHelper.DelayCall(
                    //        () =>
                    //        {
                    //            MessageBox.Show("请在离线内容页面中点击'开始下载'按钮继续下载队列中的离线内容", "离线下载队列中有未完成的内容", MessageBoxButton.OK);
                    //        },
                    //        500
                    //        );
                    //}

                    _boardGroupDownloader.DownloadCompleted +=
                        (s, da) =>
                        {
                            HtmlWeb web2 = App.WebSession.CreateWebClient();

                            _clubHomeDownloader.RootID = App.Settings.BoardGroupPreloadOfflineID;
                            _clubHomeDownloader.OldContentMaxAge = MitbbsSettings.OldContentExpirePeriod;
                            _clubHomeDownloader.StartDownload(web2, App.Settings.BuildUrl(MitbbsClubHome.ClubHomeUrl));
                        };

                    _clubHomeDownloader.DownloadCompleted +=
                        (s, da) =>
                        {
                            if (_boardGroupDownloader.NewContentDownloaded || _clubHomeDownloader.NewContentDownloaded || (!MitbbsBoardSearch.Instance.Updating && MitbbsBoardSearch.Instance.AllBoardLinks.Count < 1000))
                            {
                                MitbbsBoardSearch.Instance.StartPopulateBoardList();
                            }
                        };

                    IScheduler scheduler = Scheduler.ThreadPool;
                    scheduler.Schedule(new Action(
                        () =>
                        {
                            if (!_boardGroupDownloader.IsCompleted && !_boardGroupDownloader.IsDownloading)
                            {
                                HtmlWeb web1 = App.WebSession.CreateWebClient();
                                
                                _boardGroupDownloader.RootID = App.Settings.BoardGroupPreloadOfflineID;
                                _boardGroupDownloader.OldContentMaxAge = MitbbsSettings.OldContentExpirePeriod;
                                _boardGroupDownloader.StartDownload(web1, App.Settings.BuildUrl(MitbbsBoardGroup.MobileBoardGroupHome));

                                
                            }
                        }
                        ),
                        TimeSpan.FromSeconds(1)
                        );
                }
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (PopUpMessageBox.Visibility == Visibility.Collapsed)
            {
                PopUpMessageBox.Visibility = Visibility.Visible;
                e.Cancel = true;

                DispatcherTimer popTimer = new DispatcherTimer();
                popTimer.Interval = new TimeSpan(0, 0, 0, 2, 0);

                popTimer.Tick +=
                    (s, a) =>
                    {
                        popTimer.Stop();
                        PopUpMessageBox.Visibility = Visibility.Collapsed;
                    };

                popTimer.Start();
            }

            base.OnBackKeyPress(e);
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = (sender as ListBox);
            AppMenuLink AppMenuLink = (list.SelectedItem as AppMenuLink);

            App.Settings.ResetSessionHistory = true;
            PageHelper.OpenMitbbsLink(AppMenuLink, NavigationService);

            list.SelectedItem = null;
        }

        private void MenuItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            AppMenuLink AppMenuLink = button.Tag as AppMenuLink;

            App.Settings.ResetSessionHistory = true;
            PageHelper.OpenMitbbsLink(AppMenuLink, NavigationService);
        }

        private void OnLogOnOrLogOutCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            ApplicationTitle.Text = App.License.AppTitle + " (" + App.WebSession.Username + ")";
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
        }

        private void DownloadStatusButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.NotficationCenter.OpenNotification(NavigationService);
        }
    }

    public class MainPageData
    {
        public ObservableCollection<AppMenuLink> MenuItems { get; set; }
    }
}