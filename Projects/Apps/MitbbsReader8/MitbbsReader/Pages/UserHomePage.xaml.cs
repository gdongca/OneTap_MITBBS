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
using HtmlAgilityPack;
using System.Collections.ObjectModel;

namespace Naboo.MitbbsReader.Pages
{
    public partial class UserHomePage : PhoneApplicationPage
    {
        private String _url;
        private MitbbsUserHome _userHome;
        private MitbbsMailbox _unreadMailbox = new MitbbsMailbox();
        private double _scrollOffset = -1;
        private int _selectedPivot = -1;
        private bool _showMailMessage = false;
        private bool _keepNavHistory = false;
        
        public UserHomePage()
        {
            InitializeComponent();
            _unreadMailbox.MailboxLoaded += Mailbox_Loaded;
            DataContext = _userHome;
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;
            
            _url = App.Settings.BuildUrl(MitbbsUserHome.MitbbsMobileUserHomeUrl);

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "UserHome");
        }

        private void LoadUserHome()
        {
            LoadHomeProgressBar.Visibility = Visibility.Visible;

            if (App.WebSession.IsConnecting)
            {
                App.WebSession.LogInCompleted += OnLogOnOrLogOutCompleted;
                return;
            }
            else if (!App.WebSession.IsLoggedIn)
            {
                MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。");
                NavigationService.GoBack();
                return;
            }

            if (_url != null)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
                (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //home button
                (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //history button
                (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false; //setting button

                _userHome.UserHomeLoaded += UserHome_Loaded;
                _userHome.LoadFromUrl(App.WebSession.CreateWebClient(), _url);

                _showMailMessage = true;
                _unreadMailbox.LoadFromUrl(App.WebSession.CreateWebClient(), App.Settings.BuildUrl(MitbbsMailbox.NewMailsUrl));
            }
        }

        private void OnLogOnOrLogOutCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            App.WebSession.LogInCompleted -= OnLogOnOrLogOutCompleted;
            PivotControl.Title = "  家页 " + "(" + App.WebSession.Username + ")";
            LoadUserHome();
        }
        private void Mailbox_Loaded(object sender, DataLoadedEventArgs e)
        {
            if (sender == _unreadMailbox)
            {
                if (_unreadMailbox.MailLinks.Count > 0)
                {
                    UnreadMailIndicator.Text = _unreadMailbox.MailLinks.Count.ToString();
                    UnreadMailIndicator.Visibility = System.Windows.Visibility.Visible;

                    if (_showMailMessage)
                    {
                        ShowNewMailMessage(_unreadMailbox.MailLinks.Count);
                    }

                    App.Settings.NotficationCenter.HasNewMail = true;
                }
                else
                {
                    UnreadMailIndicator.Visibility = System.Windows.Visibility.Collapsed;
                    App.Settings.NotficationCenter.HasNewMail = false;
                }
            }
        }

        DispatcherTimer _popTimer = new DispatcherTimer();
        private void ShowNewMailMessage(int mailCount)
        {
            if (PopUpMessageBox.Visibility == Visibility.Collapsed)
            {
                PopUpMessageText.Text = "你有" + mailCount + "封未读邮件";
                PopUpMessageBox.Visibility = Visibility.Visible;
                
                _popTimer.Interval = new TimeSpan(0, 0, 0, 5, 0);
                _popTimer.Tick += PopUp_Timer;

                _popTimer.Start();
            }
        }

        private void PopUp_Timer(object sender, EventArgs e)
        {
            _popTimer.Tick -= PopUp_Timer;
            _popTimer.Stop();

            PopUpMessageBox.Visibility = Visibility.Collapsed;
        }

        private void UserHome_Loaded(object sender, DataLoadedEventArgs e)
        {
            _userHome.UserHomeLoaded -= UserHome_Loaded;
            LoadHomeProgressBar.Visibility = Visibility.Collapsed;

            if (_userHome.IsLoaded)
            {
                if (PivotControl.SelectedItem != null)
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
            }
            else
            {
                MessageBox.Show("请确认你已经登录，或者尝试重新登录。重试请按刷新按钮。", "无法打开家页", MessageBoxButton.OK);
            }

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //home button
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //history button
            (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = true; //setting button
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
#if !NODO
            if (!_keepNavHistory && NavigationService.BackStack.Count() > 1)
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

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "用户家页");

            var parameters = NavigationContext.QueryString;
            _selectedPivot = -1;
            _scrollOffset = -1;

            if (parameters.ContainsKey("ShowPage") && !State.ContainsKey("SelectedIndex"))
            {
                _selectedPivot = int.Parse(parameters["ShowPage"]);
            }

            if (parameters.ContainsKey("KeepNavHistory"))
            {
                _keepNavHistory = bool.Parse(parameters["KeepNavHistory"]);
            }
            else
            {
                _keepNavHistory = false;
            }

            if (_userHome != App.UserHome)
            {
                _userHome = App.UserHome;
                DataContext = _userHome;
            }
            else if (!_userHome.IsLoaded)
            {
                _userHome.UserHomeLoaded -= UserHome_Loaded;

                App.UserHome = new MitbbsUserHome();
                _userHome = App.UserHome;
                DataContext = _userHome;
            }

            if (!_userHome.IsLoaded)
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

            base.OnNavigatedTo(e);

            if (!_userHome.IsLoaded)
            {
                LoadUserHome();
            }
            else
            {
                _showMailMessage = false;
                _unreadMailbox.LoadFromUrl(App.WebSession.CreateWebClient(), App.Settings.BuildUrl(MitbbsMailbox.NewMailsUrl));
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            State["SelectedIndex"] = PivotControl.SelectedIndex;

            if (PivotControl.SelectedItem != null)
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

            if (_selectedPivot >= 0)
            {
                PivotControl.SelectedIndex = _selectedPivot;
                _selectedPivot = -1;
            }
            
            PivotControl.Title = "  家页 " + "(" + App.WebSession.Username + ")";
        }

        private void MyBoardListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = (sender as ListBox);
            MitbbsLink link = (list.SelectedItem as MitbbsLink);

            if (link != null)
            {
                PageHelper.OpenMitbbsLink(link, NavigationService);

                if (list == MyBoardListBox)
                {
                    App.Track("Navigation", "EntryPoint", "UserHomeMyBoards");
                }
                else if (list == MyArticleListBox)
                {
                    App.Track("Navigation", "EntryPoint", "UserHomeMyArticles");
                }

                (sender as ListBox).SelectedItem = null;
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadUserHome();
        }

        private void HomeButton_Click(object sender, EventArgs e)
        {
#if NODO
            NavigationService.GoBack();
#else
            NavigationService.Navigate(new Uri("/Pages/MitbbsHomePage.xaml", UriKind.Relative));
#endif
        }

        private void AddMyBoardMenu_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("请用浏览器访问未名空间主页www.mitbbs.com，登录之后在用户家页中管理'我的讨论区'和'我的俱乐部'。你需要现在打开浏览器访问未名空间网站吗？", "管理我的版面和俱乐部", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                PageHelper.OpenLinkInBrowser(App.Settings.BuildUrl(""));
            }
        }

        private void MailboxListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = (sender as ListBox).SelectedIndex;
            String pageUrl = null;

            switch (selectedIndex)
            {
                case 0:
                    pageUrl = String.Format("/Pages/MailboxPage.xaml?Url={0}&MailboxName={1}", 
                        Uri.EscapeDataString(App.Settings.BuildUrl(MitbbsMailbox.NewMailsUrl)), 
                        Uri.EscapeDataString("未读邮件"));
                    App.Track("Navigation", "EntryPoint", "UserHomeUnreadMail");
                    App.Settings.NotficationCenter.HasNewMail = false;
                    break;
                case 1:
                    pageUrl = String.Format("/Pages/MailboxPage.xaml?Url={0}&MailboxName={1}", 
                        Uri.EscapeDataString(App.Settings.BuildUrl(MitbbsMailbox.InboxUrl)), 
                        Uri.EscapeDataString("收件箱"));
                    App.Track("Navigation", "EntryPoint", "UserHomeInbox");
                    break;
                case 2:
                    pageUrl = String.Format("/Pages/MailboxPage.xaml?Url={0}&MailboxName={1}", 
                        Uri.EscapeDataString(App.Settings.BuildUrl(MitbbsMailbox.OutboxUrl)), 
                        Uri.EscapeDataString("发件箱"));
                    App.Track("Navigation", "EntryPoint", "UserHomeOutbox");
                    break;
                case 3:
                    pageUrl = String.Format("/Pages/NewPostPage.xaml?Url={0}&PageTitle={1}&SendFrom=false", 
                        Uri.EscapeDataString(App.Settings.BuildUrl(MitbbsMailbox.CreateMailUrl)), 
                        Uri.EscapeUriString("发送邮件"));
                    App.Track("Navigation", "EntryPoint", "UserHomeNewMail");
                    App.Settings.NotficationCenter.HasNewMail = false;
                    break;
            }

            if (pageUrl != null)
            {
                NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
            }

            (sender as ListBox).SelectedIndex = -1;
        }

        private void HistoryButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/HistoryPage.xaml", UriKind.Relative));
        }

        private void SettingButton_Click(object sender, EventArgs e)
        {
            if (App.WebSession.IsConnecting)
            {
                MessageBox.Show("用户正在登录。请稍后再试。");
            }
            else
            {
                PageHelper.OpenSettingPageWithSiteSetting(NavigationService);
                App.Track("Navigation", "EntryPoint", "UserHomeMySettings");
            }
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
            PageHelper.OpenDownloadPage(MitbbsOfflineContentType.UserHome, 
                App.Settings.BuildUrl(MitbbsUserHome.MitbbsMobileUserHomeUrl),
                "用户家页", 
                NavigationService);
            App.Track("Navigation", "EntryPoint", "UserHomeDownload");
        }

        private void AboutMenu_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
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
}