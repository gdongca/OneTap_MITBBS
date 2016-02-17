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
using System.Windows.Media.Imaging;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public partial class MailPage : TopicPageBase
    {
        private bool _pageIsLoaded = false;
        private MitbbsPostDeleteMobile _postDelete = new MitbbsPostDeleteMobile();

        public MailPage()
        {
            InitializeComponent();

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            _topicTitleTextBlock = TopicTitleTextBlock;
            _topicBodyPanel = TopicBodyPanel;
            _topicScrollViewer = TopicScrollViewer;
            _rootGrid = LayoutRoot;

            _topic = new MitbbsMail();
            _topic.TopicLoaded += Topic_Loaded;

            _showQuickReply = false;
            _showReplyToUser = false;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "个人邮件");

            if (App.ForceRefreshContent)
            {
                _forceRefresh = true;
                App.ForceRefreshContent = false;
            }

            base.OnNavigatedTo(e);

            if (_pageIsLoaded && (!_topic.IsLoaded || _forceRefresh))
            {
                _forceRefresh = false;

                LoadTopic(true);
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            base.OnNavigatedFrom(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PageHelper.SessionHistoryHandlePageLoaded(NavigationService))
            {
                return;
            }

            if ((!_pageIsLoaded && !_topic.IsLoaded) || _forceRefresh)
            {
                _forceRefresh = false;

                LoadTopic(true);
            }

            _pageIsLoaded = true;
        }

        protected override void LoadTopic(bool resetScrollPos, int pageToLoad = -1)
        {
            _resetScrollPos = resetScrollPos;

            if (_url != null)
            {
                if (String.IsNullOrEmpty(_topic.Title))
                {
                    ShowProgress("正在读取邮件...");
                }
                else
                {
                    ShowProgress(_topic.Title);
                }

                ClearContent();

                if (App.WebSession.IsConnecting)
                {
                    TopicTitleTextBlock.Text = "正在登录...";
                    App.WebSession.LogInCompleted += OnLogOnOrLogOutCompleted;
                    return;
                }
                else if (!App.WebSession.IsLoggedIn)
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。");
                    NavigationService.GoBack();
                    return;
                }

                HtmlWeb web = App.WebSession.CreateWebClient();
                _topic.LoadFromUrl(web, _url);
            }
            else
            {
                ShowProgress("参数错误!");
            }
        }

        private void OnLogOnOrLogOutCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            App.WebSession.LogInCompleted -= OnLogOnOrLogOutCompleted;
            LoadTopic(_resetScrollPos);
        }

        private void ShowProgress(String progressText)
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //reply button
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //delete button
            (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false; //share button

            LoadTopicProgressBar.Visibility = Visibility.Visible;
            TopicBodyPanel.Visibility = Visibility.Collapsed;

            TopicTitleTextBlock.Text = progressText;
            TopicTitleTextBlock.TextWrapping = TextWrapping.Wrap;
            TopicTitleTextBlock.Visibility = Visibility.Visible;
        }

        private void Topic_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadTopicProgressBar.Visibility = Visibility.Collapsed;
            TopicBodyPanel.Children.Clear();
            _imageLoader.ClearImages();

            if (_topic.IsLoaded)
            {
                _url = _topic.Url;

                RenderTopic();

                if (_topic.ReplyUrl != null)
                {
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //reply button
                }

                if ((_topic as MitbbsMail).DeleteUrl != null)
                {
                    (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //delete button
                }

                (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = true; //share button
            }
            else
            {
                TopicTitleTextBlock.Text = "读取邮件失败";
                TopicBodyPanel.Visibility = Visibility.Collapsed;

                MessageBox.Show("读取邮件失败！如果你多次遇到此错误，请尝试重新登录。");
            }

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadTopic(false);
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_topic.Url);
        }

        protected override void ReplyButton_Click(object sender, EventArgs e)
        {
            if (_topic.ReplyUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/NewPostPage.xaml?Url={0}&PageTitle={1}&SendFrom=false", Uri.EscapeDataString(_topic.ReplyUrl), Uri.EscapeUriString("回复邮件"));
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。");
                }
            }
        }

        protected override void ModifyButton_Click(object sender, EventArgs e)
        {
        }

        protected override void DeleteButton_Click(object sender, EventArgs e)
        {
            String deleteUrl = (_topic as MitbbsMail).DeleteUrl;

            if (deleteUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    MessageBoxResult result = MessageBox.Show("确认删除此邮件吗？", "", MessageBoxButton.OKCancel);

                    if (result == MessageBoxResult.OK)
                    {
                        DeletePost(deleteUrl);
                        App.RefreshMailbox = true;
                    }
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。");
                }
            }
        }

        private void DeletePost(String deleteUrl)
        {
            ShowProgress("正在删除邮件...");

            _postDelete.DeletePostCompleted += Post_Deleted;
            _postDelete.DeletePost(App.WebSession.CreateWebClient(), deleteUrl);
        }

        private void Post_Deleted(object sender, DataLoadedEventArgs e)
        {
            _postDelete.DeletePostCompleted -= Post_Deleted;

            if (!_postDelete.IsPostDeleted)
            {
                MessageBox.Show("删除操作失败！请确认你已经登录，或者尝试重新登录。");

                LoadTopic(false);
            }
            else
            {
                NavigationService.GoBack();
            }
        }

        protected override void QuickReplyButton_Click(object sender, EventArgs e)
        {
        }

        protected override void ForwardButton_Click(object sender, EventArgs e)
        {
        }

        private void SharePost(MitbbsPostBase post)
        {
            if (post != null)
            {
                String postUrl;
                if (post.PostUrl != null)
                {
                    postUrl = post.PostUrl;
                }
                else
                {
                    postUrl = _topic.Url;
                }

                String text = "\n\n" + post.GetText();

                EmailComposeTask emailTask = new EmailComposeTask();

                emailTask.Subject = "FW: " + post.Title;
                emailTask.Body = text;
                emailTask.Show();
            }
        }

        protected override void ShareButton_Click(object sender, EventArgs e)
        {
            MitbbsPostBase post = (sender as Button).DataContext as MitbbsPostBase;

            SharePost(post);
        }

        private void ShareAppBarButton_Click(object sender, EventArgs e)
        {
            if (_topic.Posts.Count > 0)
            {
                SharePost(_topic.Posts[0]);
            }
        }

        private void SettingMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenSettingPage(NavigationService);
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent(_originalUrl, _url);
        }
    }
}