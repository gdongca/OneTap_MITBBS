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
    public partial class TopicPage : TopicPageBase
    {
        private bool _pageIsLoaded = false;
        private bool _fullPage = false;
        
        private MitbbsPostDeleteMobile _postDeleteMobile = new MitbbsPostDeleteMobile();
        
        public TopicPage()
        {
            InitializeComponent();
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            _topicTitleTextBlock = TopicTitleTextBlock;
            _topicBodyPanel = TopicBodyPanel;
            _topicScrollViewer = TopicScrollViewer;
            _rootGrid = LayoutRoot;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            if (App.ForceRefreshContent)
            {
                _forceRefresh = true;
                App.ForceRefreshContent = false;
            }

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("FullPage"))
            {
                _fullPage = bool.Parse(parameters["FullPage"]);
            }
            else
            {
                _fullPage = false;
                _showQuickReply = true;
            }

            if (parameters.ContainsKey("Type"))
            {
                _club = parameters["Type"].ToLower() == "club";
            }
            else
            {
                _club = false;
            }

            if (_topic == null || !_topic.IsLoaded)
            {
                if (_topic != null)
                {
                    _topic.TopicLoaded -= Topic_Loaded;
                }

                if (_fullPage)
                {
                    _topic = new MitbbsTopic();
                }
                else
                {
                    _topic = new MitbbsTopicMobile();
                }

                _topic.TopicLoaded += Topic_Loaded;
                DataContext = _topic;
            }

            base.OnNavigatedTo(e);

            if (!_offline)
            {
                App.Settings.OfflineContentManager.CleanupOfflineContent(_offlineID);
                _offlineID = MitbbsOfflineContentManager.CreateNewRootID();
            }

            if (_pageIsLoaded && !_topic.IsLoaded)
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
            OfflineTag.Visibility = System.Windows.Visibility.Collapsed;
            
            if (_url != null)
            {
                _preloaded = false;

                ClearContent();
                
                MitbbsTopicBase _savedTopic;

                if ((_offline || App.Settings.Preload) && App.Settings.OfflineContentManager.TryLoadOfflineContent(_offlineID, _url, out _savedTopic))
                {
                    _topic.TopicLoaded -= Topic_Loaded;
                    _topic = _savedTopic;
                    _savedTopic.TopicLoaded += Topic_Loaded;

                    if (_offline)
                    {
                        //OfflineTag.Visibility = System.Windows.Visibility.Visible;
                    }

                    Naboo.AppUtil.AsyncCallHelper.DelayCall(
                        () => Topic_Loaded(this, null)
                        );

                    _preloaded = true;
                }
                else if (_offline)
                {
                    ShowProgress("无法读取离线内容");
                    LoadTopicProgressBar.Visibility = Visibility.Collapsed;

                    return;
                }
                
                if (String.IsNullOrEmpty(_topic.Title))
                {
                    ShowProgress("正在读取文章...");
                }
                else
                {
                    ShowProgress(_topic.Title);
                }

                if (_offline || _preloaded)
                {
                    return;
                }

                HtmlWeb web = App.WebSession.CreateWebClient();
                _topic.LoadFromUrl(web, _url, pageToLoad);
            }
            else
            {
                ShowProgress("参数错误!");
                LoadTopicProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void Topic_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadTopicProgressBar.Visibility = Visibility.Collapsed;
            TopicBodyPanel.Children.Clear();
            _imageLoader.ClearImages();

            if (_topic.IsLoaded)
            {
                if (!_offline && App.Settings.Preload && (_topic.PrevPageUrl != null || _topic.NextPageUrl != null))
                {
                    var downloader = CreateTopicDownloader();
                    downloader.AddTopicUrl("", _topic.PrevPageUrl);
                    downloader.AddTopicUrl("", _topic.NextPageUrl);

                    downloader.StartDownload(App.WebSession.CreateWebClient(), null);
                }

                _url = _topic.Url;

                RenderTopic();

                if (_topic.PrevPageUrl != null)
                {
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //prev page button
                }

                if (_topic.NextPageUrl != null)
                {
                    (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //next page button
                }

                if (_topic.ReplyUrl != null && !_offline)
                {
                    (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = true; //reply button

                    if (_showQuickReply)
                    {
                        (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = true; //quick reply menu
                    }
                }

                if (_topic.FirstPageUrl != null)
                {
                    (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true; //first page menu
                }

                if (_topic.LastPageUrl != null)
                {
                    (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = true; //last page menu
                }

                if (_openFromBoard || (_topic.BoardUrl != null && !_offline))
                {
                    (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = true; //open board menu

                    if (_club)
                    {
                        (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).Text = "进入" + _topic.BoardName + "俱乐部";
                    }
                    else
                    {
                        (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).Text = "进入" + _topic.BoardName + "版";
                    }
                }

                if (App.Settings.IsUrlBookmarked(_topic.Url))
                {
                    (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).Text = "删除书签"; //bookmark menu
                }
                else
                {
                    (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).Text = "加入书签"; //bookmark menu
                }

                if (App.Settings.IsUrlBeingWatched(_originalUrl))
                {
                    (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).Text = "停止关注"; //ad watch menu
                }
                else
                {
                    (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).Text = "关注"; //ad watch menu
                }

                if (_offline)
                {
                    OfflineTag.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                TopicTitleTextBlock.Text = "读取文章失败";
                TopicBodyPanel.Visibility = Visibility.Collapsed;

                if (_club)
                {
                    if (App.WebSession.IsLoggedIn)
                    {
                        MessageBox.Show("请确认你有访问此俱乐部的权限。请访问未名空间的网站管理你的俱乐部设置", "无法读取俱乐部文章", MessageBoxButton.OK);
                    }
                    else
                    {
                        MessageBox.Show("因为你尚未登录，所以你可能无法访问此俱乐部。请在设置页面中设置登录信息，并确认你的账户有访问此俱乐部的权限。请访问未名空间的网站管理你的俱乐部设置", "无法读取俱乐部文章", MessageBoxButton.OK);
                    }

                    if (!_offline)
                    {
                        App.Settings.OfflineContentManager.CleanupOfflineContent(_offlineID);
                    }

                    NavigationService.GoBack();
                }
            }

            if (!_offline)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
            }

            if (App.Settings.IsUrlBookmarked(_topic.Url) || _topic.IsLoaded)
            {
                (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).IsEnabled = true; //bookmark menu
            }

            if (!_offline && _fullPage)
            {
                (ApplicationBar.MenuItems[5] as ApplicationBarMenuItem).IsEnabled = true; //download menu
            }

            if (!_offline && _topic.IsLoaded)
            {
                (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).IsEnabled = true; //add watch menu
            }
        }

        private MitbbsTopicListOfflineDownloader CreateTopicDownloader()
        {
            MitbbsTopicListOfflineDownloader downloader;
            if (_fullPage)
            {
                downloader = new MitbbsTopicListOfflineDownloader(new MitbbsTopic());
            }
            else
            {
                downloader = new MitbbsTopicListOfflineDownloader(new MitbbsTopicMobile());
            }

            downloader.RootID = _offlineID;
            downloader.DownloadAllPages = false;
            downloader.DownloadImages = false;
            return downloader;
        }

        private void ShowProgress(String progressText)
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //prev page button
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //next page button
            (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false; //reply button

            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = false; //first page menu
            (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = false; //last page menu
            (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = false; //open board menu
            (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = false; //quick reply menu
            (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).IsEnabled = false; //bookmark menu
            (ApplicationBar.MenuItems[5] as ApplicationBarMenuItem).IsEnabled = false; //download menu
            (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).IsEnabled = false; //add watch menu

            LoadTopicProgressBar.Visibility = Visibility.Visible;
            TopicBodyPanel.Visibility = Visibility.Collapsed;

            TopicTitleTextBlock.Text = progressText;
            TopicTitleTextBlock.TextWrapping = TextWrapping.Wrap;
            TopicTitleTextBlock.Visibility = Visibility.Visible;
        }

        private void DeletePost(String deleteUrl)
        {
            ShowProgress("正在删除...");

            if (_fullPage)
            {
                MitbbsTopic fullTopic = (_topic as MitbbsTopic);
                if (fullTopic.PostDelete.IsInited)
                {
                    fullTopic.PostDelete.DeletePostCompleted += Post_Deleted;
                    fullTopic.PostDelete.DeletePost(deleteUrl);
                }
            }
            else
            {
                _postDeleteMobile.DeletePostCompleted += Post_Deleted;
                _postDeleteMobile.DeletePost(App.WebSession.CreateWebClient(), deleteUrl);
            }
        }

        private void Post_Deleted(object sender, DataLoadedEventArgs e)
        {
            bool success;
            if (_fullPage)
            {
                MitbbsTopic fullTopic = (_topic as MitbbsTopic);
                fullTopic.PostDelete.DeletePostCompleted -= Post_Deleted;
                success = fullTopic.PostDelete.IsPostDeleted;
            }
            else
            {
                _postDeleteMobile.DeletePostCompleted -= Post_Deleted;
                success = _postDeleteMobile.IsPostDeleted;
            }

            if (!success)
            {
                MessageBox.Show("删除操作失败！请确认你已经登录，或者尝试重新登录。");
            }
            else if((_topic.Posts.Count <= 1) && (_topic.PrevPageUrl == null) && (_topic.NextPageUrl == null))
            {
                App.ForceRefreshContent = true;

                if (!_offline)
                {
                    App.Settings.OfflineContentManager.CleanupOfflineContent(_offlineID);
                }

                NavigationService.GoBack();
                return;
            }

            LoadTopic(false);
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            App.Settings.OfflineContentManager.CleanupOfflineContent(_offlineID);
            _offlineID = MitbbsOfflineContentManager.CreateNewRootID();

            LoadTopic(false);
        }

        private void NextPageButton_Click(object sender, EventArgs e)
        {
            if (_topic.NextPageUrl != null)
            {
                _url = _topic.NextPageUrl;
                LoadTopic(true);
            }
        }

        private void PrevPageButton_Click(object sender, EventArgs e)
        {
            if (_topic.PrevPageUrl != null)
            {
                _url = _topic.PrevPageUrl;
                LoadTopic(true);
            }
        }

        private void FirstPageMenu_Click(object sender, EventArgs e)
        {
            if (_topic.FirstPageUrl != null)
            {
                _url = _topic.FirstPageUrl;
                LoadTopic(true);
            }
        }

        private void LastPageMenu_Click(object sender, EventArgs e)
        {
            if (_topic.LastPageUrl != null)
            {
                _url = _topic.LastPageUrl;
                LoadTopic(true);
            }
        }

        private void ReplyTopicButton_Click(object sender, EventArgs e)
        {
            if (_topic.ReplyUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/NewPostPage.xaml?Url={0}&PageTitle={1}&SendFrom=true&BlankText=true&FullPage={2}", Uri.EscapeDataString(_topic.ReplyUrl), Uri.EscapeUriString("回复文章"), _fullPage);
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法回复", MessageBoxButton.OK);
                }
            }
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_topic.Url);
        }

        private void OpenBoardMenu_Click(object sender, EventArgs e)
        {
            if (_openFromBoard || _topic.BoardUrl == null)
            {
                if (!_offline)
                {
                    App.Settings.OfflineContentManager.CleanupOfflineContent(_offlineID);
                }

                NavigationService.GoBack();
            }
            else
            {
                String type = _club ? "Club" : "Board";
                String pageUrl = String.Format("/Pages/BoardPage.xaml?Url={0}&FullPage={1}&Type={2}&Name={3}", Uri.EscapeDataString(_topic.BoardUrl), _fullPage, type, Uri.EscapeDataString(_topic.BoardName));
                NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));

                if (_fullPage)
                {
                    App.Settings.AddReadingHistory(
                        new MitbbsBoardLink()
                        {
                            Name = _topic.BoardName,
                            Url = _topic.BoardUrl,
                        }
                        );
                }
                else
                {
                    App.Settings.AddReadingHistory(
                        new MitbbsBoardLinkMobile()
                        {
                            Name = _topic.BoardName,
                            Url = _topic.BoardUrl
                        }
                        );
                }

                ClearContent();
            }
        }

        protected override void ReplyButton_Click(object sender, EventArgs e)
        {
            String replyUrl = null;
            if ((sender as Button).DataContext != null)
            {
                replyUrl = ((sender as Button).DataContext as MitbbsPostBase).ReplyPostUrl;
            }

            if (replyUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/NewPostPage.xaml?Url={0}&PageTitle={1}&SendFrom=true&FullPage={2}", Uri.EscapeDataString(replyUrl), Uri.EscapeUriString("回复文章"), _fullPage);
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法回复", MessageBoxButton.OK);
                }
            }
        }

        protected override void ForwardButton_Click(object sender, EventArgs e)
        {
            MitbbsPostBase post = null;
            if ((sender as Button).DataContext != null)
            {
                post = ((sender as Button).DataContext as MitbbsPostBase);
            }

            if (post != null && post.ForwardUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format(
                        "/Pages/ForwardPostPage.xaml?Url={0}&Title={1}&Author={2}",
                        Uri.EscapeDataString(post.ForwardUrl),
                        Uri.EscapeDataString(post.Title),
                        Uri.EscapeDataString(post.Author)
                        );
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法转帖", MessageBoxButton.OK);
                }
            }
        }

        protected override void ModifyButton_Click(object sender, EventArgs e)
        {
            String modifyUrl = null;
            if ((sender as Button).DataContext != null)
            {
                modifyUrl = ((sender as Button).DataContext as MitbbsPostBase).ModifyPostUrl;
            }

            if (modifyUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/NewPostPage.xaml?Url={0}&PageTitle={1}&FullPage={2}", Uri.EscapeDataString(modifyUrl), Uri.EscapeUriString("修改文章"), _fullPage);
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法修改文章", MessageBoxButton.OK);
                }
            }
        }

        protected override void DeleteButton_Click(object sender, EventArgs e)
        {
            String deleteUrl = null;
            if ((sender as Button).DataContext != null)
            {
                deleteUrl = ((sender as Button).DataContext as MitbbsPostBase).DeletePostUrl;
            }

            if (deleteUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    MessageBoxResult result = MessageBox.Show("确认删除此贴吗？", "", MessageBoxButton.OKCancel);

                    if (result == MessageBoxResult.OK)
                    {
                        DeletePost(deleteUrl);
                    }
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法删帖", MessageBoxButton.OK);
                }
            }
        }

        protected override void QuickReplyButton_Click(object sender, EventArgs e)
        {
            String replyUrl = null;
            if ((sender as Button).DataContext != null)
            {
                replyUrl = ((sender as Button).DataContext as MitbbsPostBase).ReplyPostUrl;
            }

            if (replyUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/QuickReplyPage.xaml?Url={0}&FullPage={1}", Uri.EscapeDataString(replyUrl), _fullPage);
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法一键回复", MessageBoxButton.OK);
                }
            }
        }

        private void QuickReplyMenu_Click(object sender, EventArgs e)
        {
            if (_topic.ReplyUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/QuickReplyPage.xaml?Url={0}&BlankText=true&FullPage={1}", Uri.EscapeDataString(_topic.ReplyUrl), _fullPage);
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法一键回复", MessageBoxButton.OK);
                }
            }
        }

        private void SettingMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenSettingPage(NavigationService, _offline);
        }

        private void BookmarkMenu_Click(object sender, EventArgs e)
        {
            if (App.Settings.IsUrlBookmarked(_topic.Url))
            {
                App.Settings.RemoveReadingBookMark(_topic.Url);
                (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).Text = "加入书签"; //bookmark menu
            }
            else
            {
                MitbbsLink topicLink;

                if (_fullPage)
                {
                    if (_club)
                    {
                        topicLink = new MitbbsClubTopicLink()
                        {
                            Name = _topic.Title,
                            Url = _topic.Url,
                        };
                    }
                    else
                    {
                        topicLink = new MitbbsTopicLink()
                        {
                            Name = _topic.Title,
                            Url = _topic.Url,
                        };
                    }
                }
                else
                {
                    topicLink = new MitbbsSimpleTopicLinkMobile()
                    {
                        Name = _topic.Title,
                        Url = _topic.Url,
                        BoardName = _topic.BoardName,
                    };
                }

                if (_offline)
                {
                    topicLink.OfflineID = _offlineID.ToString();
                }

                App.Settings.AddReadingBookMark(topicLink);
                (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).Text = "删除书签"; //bookmark menu
            }
        }

        private void DownloadMenu_Click(object sender, EventArgs e)
        {
            String downloadUrl = _topic.FirstPageUrl == null ? _topic.Url : _topic.FirstPageUrl;

            if (_club)
            {
                PageHelper.OpenDownloadPage(MitbbsOfflineContentType.ClubTopic, downloadUrl, _topic.Title, NavigationService);
            }
            else if (_fullPage)
            {
                PageHelper.OpenDownloadPage(MitbbsOfflineContentType.Topic, downloadUrl, _topic.Title, NavigationService);
            }
            else
            {
                PageHelper.OpenDownloadPage(MitbbsOfflineContentType.Unknown, downloadUrl, _topic.Title, NavigationService);
            }
        }

        private void AddWatchMenu_Click(object sender, EventArgs e)
        {
            if (App.Settings.IsUrlBeingWatched(_originalUrl))
            {
                App.Settings.RemoveWatchItem(_originalUrl);
                (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).Text = "关注"; //add watch menu
            }
            else
            {
                MitbbsLink topicLink = App.Settings.FindHistoryEntry(_originalUrl);

                if (topicLink == null)
                {
                    if (_fullPage)
                    {
                        topicLink = new MitbbsTopicLink()
                        {
                            Name = _topic.Title,
                            Url = _originalUrl,
                        };
                    }
                    else
                    {
                        topicLink = new MitbbsSimpleTopicLinkMobile()
                        {
                            Name = _topic.Title,
                            Url = _originalUrl,
                            BoardName = _topic.BoardName,
                        };
                    }
                }

                App.Track("Statistics", "NewWatch", null);
                App.Settings.AddWatchItem(topicLink);
                (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).Text = "停止关注"; //add watch menu

                MessageBox.Show("将会定期检查是否此文章有新帖子。请注意右上角出现的通知图标，或者直接打开'历史收藏'页面查看被关注文章的更新", "文章已加入关注列表", MessageBoxButton.OK);
            }
        }

        private void DownloadStatusButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.SetIgnoreTap();
            NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.NotficationCenter.OpenNotification(NavigationService);
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent(_originalUrl, _url);
        }
    }
}