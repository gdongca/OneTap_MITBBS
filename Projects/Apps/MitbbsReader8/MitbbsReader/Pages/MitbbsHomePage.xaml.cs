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
using System.Windows.Threading;
using System.Collections.ObjectModel;
using HtmlAgilityPack;
using Naboo.AppUtil;

namespace Naboo.MitbbsReader.Pages
{
    public partial class MitbbsHomePage : PhoneApplicationPage
    {
        public ObservableCollection<GenericLink> BoardMenuLinks;

        private String _url;
        private MitbbsHomeBase _mitbbsHome;
        private double _scrollOffset = -1;
        private int _selectedPivot = -1;
        private DispatcherTimer _popTimer = new DispatcherTimer();
        private MitbbsBoardGroupOfflineDownloader _boardGroupDownloader = new MitbbsBoardGroupOfflineDownloader();
        private MitbbsClubHomeOfflineDownloader _clubHomeDownloader = new MitbbsClubHomeOfflineDownloader();
        private bool _offline = false;
        private Guid _offlineID;
        private bool _pageDeleted = false;

#if NODO
        private bool _redirected = false;
#endif
        
        public MitbbsHomePage()
        {
            InitializeComponent();
            DataContext = _mitbbsHome;
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

            _url = App.Settings.BuildUrl(MitbbsHome.MitbbsHomeUrl);
            App.WebSession.LogInCompleted += OnLogOnOrLogOutCompleted;
            App.WebSession.LogOutCompleted += OnLogOnOrLogOutCompleted;

            if (!App.IsTrial)
            {
                ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //Remove Ad menu
            }

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "MitbbsHome");

            BoardMenuLinks = new ObservableCollection<GenericLink>();
            
            BoardMenuLinks.Add(
                new MitbbsBoardGroupLink()
                {
                    Name = "所有版面",
                    Image = "/Images/lines_appbar.png",
                    Url = App.Settings.BuildUrl(MitbbsBoardGroup.MobileBoardGroupHome)
                }
                );

            BoardMenuLinks.Add(
                new MitbbsClubGroupLink()
                {
                    Name = "所有俱乐部",
                    Image = "/Images/lines_appbar.png",
                    Url = App.Settings.BuildUrl(MitbbsClubHome.ClubHomeUrl),
                    IsClubHome = true
                }
                );

            BoardMenuLinks.Add(
                new AppMenuLink()
                {
                    Name = "用户版面",
                    Image = "/Images/person_appbar.png",
                    Url = "/Pages/UserHomePage.xaml?KeepNavHistory=True",
                    RequiresLogIn = true
                }
                );

            BoardMenuLinks.Add(
                new AppMenuLink()
                {
                    Name = "用户俱乐部",
                    Image = "/Images/person_appbar.png",
                    Url = "/Pages/UserHomePage.xaml?ShowPage=1&KeepNavHistory=True",
                    RequiresLogIn = true
                }
                );

            BoardMenuLinks.Add(
                new AppMenuLink()
                {
                    Name = "快速搜索",
                    Image = "/Images/search_appbar.png",
                    Url = "/Pages/BoardSearchPage.xaml",
                }
                );

            BoardMenuLinks.Add(
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

            BoardListBox.ItemsSource = BoardMenuLinks;
        }

        private void LoadHome()
        {
            LoadHomeProgressBar.Visibility = Visibility.Visible;

            if (_url != null)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
                (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //user home button
                (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //history button
                (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false; //setting button
                OfflineTag.Visibility = System.Windows.Visibility.Collapsed;

                if (_offline)
                {
                    MitbbsHomeBase _savedHome;
                    if (App.Settings.OfflineContentManager.TryLoadOfflineContent(_offlineID, _url, out _savedHome))
                    {
                        _mitbbsHome = _savedHome;
                        DataContext = _mitbbsHome;
                        OfflineTag.Visibility = System.Windows.Visibility.Visible;

                        AsyncCallHelper.DelayCall(
                            () => MitbbsHome_Loaded(this, null)
                            );
                    }
                    else
                    {
                        MessageBox.Show("离线内容不存在");
                        LoadHomeProgressBar.Visibility = Visibility.Collapsed;
                        NavigationService.GoBack();
                    }

                    return;
                }

                if (!AppInfo.IsNetworkConnected())
                {
                    MessageBox.Show("没有可用的网络连接", "无法读取未名空间主页", MessageBoxButton.OK);
                    LoadHomeProgressBar.Visibility = Visibility.Collapsed;
                    (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
                    return;
                }

                _mitbbsHome.HomeLoaded += MitbbsHome_Loaded;
                _mitbbsHome.LoadFromUrl(App.WebSession.CreateWebClient(), _url);

                PivotControl.Title = "  " + App.License.AppTitle + " (" + App.WebSession.Username + ")";
            }
        }

        private void MitbbsHome_Loaded(object sender, DataLoadedEventArgs e)
        {
            _mitbbsHome.HomeLoaded -= MitbbsHome_Loaded;
            LoadHomeProgressBar.Visibility = Visibility.Collapsed;

            if (_mitbbsHome.IsLoaded)
            {
                if(PivotControl.SelectedItem != null)
                {
                    //ScrollViewer scrollPanel = (PivotControl.SelectedItem as PivotItem).Content as ScrollViewer;
                    //if (_scrollOffset >= 0)
                    //{
                    //    scrollPanel.UpdateLayout();
                    //    scrollPanel.ScrollToVerticalOffset(_scrollOffset);
                    //    _scrollOffset = -1;
                    //}
                    //else
                    //{
                    //    scrollPanel.UpdateLayout();
                    //    scrollPanel.ScrollToVerticalOffset(0);
                    //}
                }

                if (!_offline)
                {
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //user home button
                }

                //if (App.Settings.Preload && !_offline && !_boardGroupDownloader.IsCompleted && !_boardGroupDownloader.IsDownloading)
                //{
                //    HtmlWeb web1 = App.WebSession.CreateWebClient();
                //    HtmlWeb web2 = App.WebSession.CreateWebClient();

                //    _boardGroupDownloader.RootID = App.Settings.BoardGroupPreloadOfflineID;
                //    _boardGroupDownloader.StartDownload(web1, MitbbsBoardGroup.MobileBoardGroupHome);

                //    _clubHomeDownloader.RootID = App.Settings.BoardGroupPreloadOfflineID;
                //    _clubHomeDownloader.StartDownload(web2, MitbbsClubHome.ClubHomeUrl);
                //}
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("你需要尝试连接其它的服务器吗？", "连接服务器失败!", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    PageHelper.OpenSettingPageWithSiteSetting(NavigationService);
                }
                else
                {
                    MessageBox.Show("请点刷新键重试。如果你重复看到此错误，有可能是以下问题之一：1)未名空间服务器故障。2)此网络连接无法访问未名空间网站。3)网络连接网页改版。", "连接服务器失败!", MessageBoxButton.OK);
                }
            }

            if (!_offline)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
                (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //history button
            }

            (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = true; //setting button
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("OfflineID"))
            {
                _offline = HtmlAgilityPack.HtmlUtilities.TryParseGuid(parameters["OfflineID"], out _offlineID);
            }

#if !NODO
            if (!_offline && NavigationService.BackStack.Count() > 1)
            {
                while (NavigationService.BackStack.Count() > 1)
                {
                    NavigationService.RemoveBackEntry();
                }
                App.Settings.CurrentSessionHistory.Reset();
            }
#endif

            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "未名主页");
            
            _selectedPivot = -1;
            _scrollOffset = -1;
            
            if (parameters.ContainsKey("ShowPage") && !State.ContainsKey("SelectedIndex"))
            {
                _selectedPivot = int.Parse(parameters["ShowPage"]);
            }

            if (_offline && !_pageDeleted && PivotControl.Items.Count >= 4)
            {
                PivotControl.Items.RemoveAt(3);
                
                ApplicationBar.MenuItems.RemoveAt(0);
                ApplicationBar.MenuItems.RemoveAt(0);
                ApplicationBar.MenuItems.RemoveAt(0);
            }

            if (_offline)
            {
                HeaderImage.Visibility = System.Windows.Visibility.Collapsed;
                HomeImage.Visibility = System.Windows.Visibility.Collapsed;
                PivotControl.Foreground = (Brush)App.Current.Resources["PhoneAccentBrush"];

                if (_mitbbsHome == null)
                {
                    _mitbbsHome = new MitbbsHome();
                }
            }
            else
            {
                if (_mitbbsHome != App.MitbbsHome)
                {
                    _mitbbsHome = App.MitbbsHome;
                    DataContext = _mitbbsHome;
                }
                else if (!_mitbbsHome.IsLoaded)
                {
                    _mitbbsHome.HomeLoaded -= MitbbsHome_Loaded;

                    App.MitbbsHome = new MitbbsHome();
                    _mitbbsHome = App.MitbbsHome;
                    DataContext = _mitbbsHome;
                }
            }

            if (!_mitbbsHome.IsLoaded)
            {
                if (State.ContainsKey("SelectedIndex"))
                {
                    _selectedPivot = (int)State["SelectedIndex"];
                }

                if (State.ContainsKey("ScrollOffset"))
                {
                    _scrollOffset = (double)State["ScrollOffset"];
                }
            }
            else
            {
                _mitbbsHome.ForceUpdateHistoryStatus();
            }

            base.OnNavigatedTo(e);

#if NODO
            if (!_offline && !AppInfo.IsNetworkConnected() && !_redirected)
            {
                _redirected = true;
                MessageBoxResult result = MessageBox.Show("你想直接查看离线内容吗？", "网络连接不存在", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    OfflineMenu_Click(this, null);
                    return;
                }
            }
#endif

            if (!_mitbbsHome.IsLoaded)
            {
                LoadHome();
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            State["SelectedIndex"] = PivotControl.SelectedIndex;
            
            if(PivotControl.SelectedItem != null)
            {
                //ScrollViewer scrollPanel = (PivotControl.SelectedItem as PivotItem).Content as ScrollViewer;
                //State["ScrollOffset"] = scrollPanel.VerticalOffset;
            }
            base.OnNavigatedFrom(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PageHelper.SessionHistoryHandlePageLoaded(NavigationService))
            {
                return;
            }

            if (App.NeedToExit)
            {
                NavigationService.GoBack();
            }

            if (_selectedPivot >= 0)
            {
                PivotControl.SelectedIndex = _selectedPivot;
                _selectedPivot = -1;
            }

            if (_offline)
            {
                PivotControl.Title = "  " + App.License.AppTitle;
            }
            else
            {
                PivotControl.Title = "  " + App.License.AppTitle + " (" + App.WebSession.Username + ")";
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
#if NODO
            if (!_offline && PopUpMessageBox.Visibility == Visibility.Collapsed)
            {
                PopUpMessageBox.Visibility = Visibility.Visible;
                e.Cancel = true;

                _popTimer.Interval = new TimeSpan(0, 0, 0, 2, 0);
                _popTimer.Tick += PopUp_Timer;

                _popTimer.Start();
            }
#endif

            base.OnBackKeyPress(e);
        }

        private void PopUp_Timer(object sender, EventArgs e)
        {
            _popTimer.Tick -= PopUp_Timer;
            _popTimer.Stop();

            PopUpMessageBox.Visibility = Visibility.Collapsed;
        }

        private void OnLogOnOrLogOutCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            PivotControl.Title = "  " + App.License.AppTitle + " (" + App.WebSession.Username + ")";
        }

        private void TopArticleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = (sender as ListBox);
            MitbbsLink link = (list.SelectedItem as MitbbsLink);

            if (link != null)
            {
                if (_offline)
                {
                    link.OfflineID = _offlineID.ToString();
                }

                PageHelper.OpenMitbbsLink(link, NavigationService);

                App.Track("Navigation", "EntryPoint", "HomeArticles");
                
                (sender as ListBox).SelectedItem = null;
            }
        }

        private void BoardListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = (sender as ListBox);
            GenericLink link = (list.SelectedItem as GenericLink);

            if (link != null)
            {
                PageHelper.OpenMitbbsLink(link, NavigationService);

                App.Track("Navigation", "EntryPoint", "HomeBoardGroup");

                (sender as ListBox).SelectedItem = null;
            }
        }

        private void AboutMenu_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (App.Settings.LogOn && !App.WebSession.IsLoggedIn && !App.WebSession.IsConnecting && AppInfo.IsNetworkConnected())
            {
                App.WebSession.StartLogIn(App.Settings.Username, App.Settings.Password);
            }

            LoadHome();
        }

        private void SettingButton_Click(object sender, EventArgs e)
        {
            if (App.WebSession.IsConnecting)
            {
                MessageBox.Show("用户正在登录。请稍后再试。");
            }
            else
            {
                if (_offline)
                {
                    PageHelper.OpenSettingPage(NavigationService, _offline);
                }
                else
                {
                    PageHelper.OpenSettingPageWithSiteSetting(NavigationService);
                }
                
                App.Track("Navigation", "EntryPoint", "HomeSettings");
            }
        }

        private void UserHomeButton_Click(object sender, EventArgs e)
        {
            if (App.WebSession.IsLoggedIn)
            {
                NavigationService.Navigate(new Uri("/Pages/UserHomePage.xaml", UriKind.Relative));
            }
            else if (App.WebSession.IsConnecting)
            {
                MessageBox.Show("用户正在登录。请稍后再试。");
            }
            else
            {
                MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。");
            }
        }

        private void RemoveAdMenu_Click(object sender, EventArgs e)
        {
            App.License.ShowTurnOffAdMessage();
        }

        private void HistoryButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/HistoryPage.xaml", UriKind.Relative));
        }

        private void HelpMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenGeneralLink(@"http://charmingco2.com/2011/12/01/tips-for-onetap-mitbbs-reader/", NavigationService);
        }

        private void OfflineMenu_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
        }

        private void DownloadMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenDownloadPage(MitbbsOfflineContentType.Home, App.Settings.BuildUrl(MitbbsHome.MitbbsHomeUrl), "未名空间主页", NavigationService);
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

        private void MenuItemButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = (sender as Button);
            GenericLink link = button.Tag as GenericLink;

            PageHelper.OpenMitbbsLink(link, NavigationService);
        }
    }
}