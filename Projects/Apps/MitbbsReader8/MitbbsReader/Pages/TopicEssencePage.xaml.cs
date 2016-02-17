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
    public partial class TopicEssencePage : TopicPageBase
    {
        private bool _pageIsLoaded = false;
        
        public TopicEssencePage()
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

            _topic = new MitbbsTopicEssenceMobile();
            _topic.TopicLoaded += Topic_Loaded;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "精华区文章");

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
            OfflineTag.Visibility = System.Windows.Visibility.Collapsed;

            if (_url != null)
            {
                ClearContent();

                MitbbsTopicBase _savedTopic;
                if (_offline && App.Settings.OfflineContentManager.TryLoadOfflineContent(_offlineID, _url, out _savedTopic))
                {
                    _topic.TopicLoaded -= Topic_Loaded;
                    _topic = _savedTopic;
                    _savedTopic.TopicLoaded += Topic_Loaded;

                    Naboo.AppUtil.AsyncCallHelper.DelayCall(
                        () => Topic_Loaded(this, null)
                        );
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

                if (_offline)
                {
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

        private void ShowProgress(String progressText)
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            
            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = false; //board menu
            (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = false; //bookmark menu
            (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = false; //download menu
            (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = false; //setting menu
            
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

                if (_topic.BoardUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true; //open board menu
                    (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).Text = "进入" + _topic.BoardName + "版";
                }

                if (App.Settings.IsUrlBookmarked(_topic.Url))
                {
                    (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).Text = "删除书签"; //bookmark menu
                }
                else
                {
                    (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).Text = "加入书签"; //bookmark menu
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
            }

            if (!_offline)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
            }

            if (App.Settings.IsUrlBookmarked(_topic.Url) || _topic.IsLoaded)
            {
                (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = true; //bookmark menu
            }

            if (!_offline)
            {
                (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = true; //download menu
            }

            (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = true; //setting menu
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadTopic(false);
        }

        private void OpenBoardMenu_Click(object sender, EventArgs e)
        {
            if (_openFromBoard)
            {
                NavigationService.GoBack();
            }
            else
            {
                if (_topic.BoardUrl != null)
                {
                    String pageUrl = String.Format("/Pages/BoardPage.xaml?Url={0}", Uri.EscapeDataString(_topic.BoardUrl));
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));

                    App.Settings.AddReadingHistory(
                    new MitbbsBoardLinkMobile()
                    {
                        Name = _topic.BoardName,
                        Url = _topic.BoardUrl,
                        BoardName = _topic.BoardName
                    }
                    );

                    ClearContent();
                }
            }
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_topic.Url);
        }

        protected override void ReplyButton_Click(object sender, EventArgs e)
        {
        }

        protected override void ForwardButton_Click(object sender, EventArgs e)
        {
        }

        protected override void ModifyButton_Click(object sender, EventArgs e)
        {
        }

        protected override void DeleteButton_Click(object sender, EventArgs e)
        {
        }

        protected override void QuickReplyButton_Click(object sender, EventArgs e)
        {
        }

        private void SettingMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenSettingPage(NavigationService);
        }

        private void BookmarkMenu_Click(object sender, EventArgs e)
        {
            if (App.Settings.IsUrlBookmarked(_topic.Url))
            {
                App.Settings.RemoveReadingBookMark(_topic.Url);
                (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).Text = "加入书签"; //bookmark menu
            }
            else
            {
                MitbbsTopicEssenceLink topicLink = new MitbbsTopicEssenceLink()
                {
                    Name = _topic.Title,
                    Url = _topic.Url
                };

                if (_offline)
                {
                    topicLink.OfflineID = _offlineID.ToString();
                }

                App.Settings.ReadingBookmarks.Add(topicLink);
                (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).Text = "删除书签"; //bookmark menu
            }
        }

        private void DownloadStatusButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.NotficationCenter.OpenNotification(NavigationService);
        }

        private void DownloadMenu_Click(object sender, EventArgs e)
        {
            String downloadUrl = _topic.FirstPageUrl == null ? _topic.Url : _topic.FirstPageUrl;

            PageHelper.OpenDownloadPage(MitbbsOfflineContentType.TopicEssense, downloadUrl, _topic.Title, NavigationService);
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent(_originalUrl, _url);
        }
    }
}