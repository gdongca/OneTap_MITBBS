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
    public partial class MailboxPage : PhoneApplicationPage
    {
        private String _url;
        private MitbbsMailbox _mailbox = new MitbbsMailbox();
        private double _scrollOffset = -1;

        public MailboxPage()
        {
            InitializeComponent();
            _mailbox.MailboxLoaded += Mailbox_Loaded;
            DataContext = _mailbox;

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot);
        }

        private void LoadMailbox()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //prev page button
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //next page button
            (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false; //new mail button

            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = false; //first page menu
            (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = false; //last page menu
            (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = false; //setting menu
            
            LoadMailboxProgressBar.Visibility = Visibility.Visible;
            MailboxLoadingText.Visibility = Visibility.Visible;
            MailboxNameTextBlock.Visibility = Visibility.Collapsed;
            MailLinksListBox.Visibility = Visibility.Collapsed;

            if (App.WebSession.IsConnecting)
            {
                MailboxLoadingText.Text = "正在登录...";
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
                if (!String.IsNullOrEmpty(_mailbox.MailboxName))
                {
                    MailboxLoadingText.Text = "正在读取<" + _mailbox.MailboxName + ">...";
                }
                else
                {
                    MailboxLoadingText.Text = "正在读取邮箱...";
                }

                _mailbox.LoadFromUrl(App.WebSession.CreateWebClient(), _url);
            }
            else
            {
                MailboxLoadingText.Text = "参数错误!";
            }
        }

        private void OnLogOnOrLogOutCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            App.WebSession.LogInCompleted -= OnLogOnOrLogOutCompleted;
            LoadMailbox();
        }

        private void Mailbox_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadMailboxProgressBar.Visibility = Visibility.Collapsed;

            if (_mailbox.IsLoaded)
            {
                MailboxLoadingText.Visibility = Visibility.Collapsed;
                MailboxNameTextBlock.Visibility = Visibility.Visible;
                MailLinksListBox.Visibility = Visibility.Visible;

                if (_mailbox.MailLinks.Count > 0)
                {
                    MailboxNameTextBlock.Text = _mailbox.MailboxName;
                }
                else
                {
                    MailboxNameTextBlock.Text = _mailbox.MailboxName + "（空）";
                }

                if (_mailbox.PrevPageUrl != null)
                {
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //prev page button
                }

                if (_mailbox.NextPageUrl != null)
                {
                    (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //next page button
                }

                if (_mailbox.NewMailUrl != null)
                {
                    (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = true; //new post button
                }

                if (_mailbox.FirstPageUrl != null)
                {
                    (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true; //first page menu
                }

                if (_mailbox.LastPageUrl != null)
                {
                    (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = true; //last page menu
                }

                if (_scrollOffset >= 0)
                {
                    //MailLinkListPanel.UpdateLayout();
                    //MailLinkListPanel.ScrollToVerticalOffset(_scrollOffset);
                    _scrollOffset = -1;
                }
                else
                {
                    //MailLinkListPanel.UpdateLayout();
                    //MailLinkListPanel.ScrollToVerticalOffset(0);
                }

            }
            else
            {
                MailboxLoadingText.Text = "读取邮箱失败";

                MessageBox.Show("读取邮箱件失败！如果你多次遇到此错误，请尝试重新登录。");
            }

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
            (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = true; //setting menu
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

            if (parameters.ContainsKey("MailboxName"))
            {
                _mailbox.MailboxName = Uri.UnescapeDataString(parameters["MailboxName"]);
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _mailbox.MailboxName, "个人邮箱");

            if (State.ContainsKey("Url"))
            {
                _url = (String)State["Url"];
            }

            if (!_mailbox.IsLoaded && State.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = (double)State["ScrollOffset"];
            }
            else
            {
                _scrollOffset = -1;
            }

            base.OnNavigatedTo(e);

            if (!_mailbox.IsLoaded || App.RefreshMailbox)
            {
                App.RefreshMailbox = false;

                LoadMailbox();
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

            if (_mailbox.IsLoaded)
            {
                //State["ScrollOffset"] = MailLinkListPanel.VerticalOffset;
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

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadMailbox();
        }

        private void PrevPageButton_Click(object sender, EventArgs e)
        {
            if (_mailbox.PrevPageUrl != null)
            {
                _url = _mailbox.PrevPageUrl;
                LoadMailbox();
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void NextPageButton_Click(object sender, EventArgs e)
        {
            if (_mailbox.NextPageUrl != null)
            {
                _url = _mailbox.NextPageUrl;
                LoadMailbox();
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void FirstPageMenu_Click(object sender, EventArgs e)
        {
            if (_mailbox.FirstPageUrl != null)
            {
                _url = _mailbox.FirstPageUrl;
                LoadMailbox();
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void LastPageMenu_Click(object sender, EventArgs e)
        {
            if (_mailbox.LastPageUrl != null)
            {
                _url = _mailbox.LastPageUrl;
                LoadMailbox();
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void MailLinksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsMailLink selected = ((sender as ListBox).SelectedItem as MitbbsMailLink);

            if (selected != null)
            {
                PageHelper.OpenMitbbsLink(selected, NavigationService);
                selected.IsNew = false;
            }

            (sender as ListBox).SelectedItem = null;
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_mailbox.Url);
        }

        private void NewMailButton_Click(object sender, EventArgs e)
        {
            if (_mailbox.NewMailUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/NewPostPage.xaml?Url={0}&PageTitle={1}&SendFrom=false", Uri.EscapeDataString(_mailbox.NewMailUrl), Uri.EscapeUriString("发送邮件"));
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。");
                }
            }
        }

        private void SettingMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenSettingPage(NavigationService);
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent("", _url);
        }
    }
}