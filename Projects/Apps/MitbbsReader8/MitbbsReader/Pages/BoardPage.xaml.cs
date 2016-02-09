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
    public partial class BoardPage : PhoneApplicationPage
    {
        private String _url;
        private bool _fullPage = false;
        private bool _club = false;
        private MitbbsBoardBase _board;
        private double _scrollOffset = -1;
        private bool _forceRefresh = false;
        private bool _offline = false;
        private Guid _offlineID;
        private bool? _hideTop;
        private String _originalUrl;
        
        public BoardPage()
        {
            InitializeComponent();
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot);
        }

        private void LoadBoard(bool goForward)
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //prev page button
            (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false; //next page button
            (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false; //add post button

            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = false; //search menu
            (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = false; //first page menu
            (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = false; //last page menu
            (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = false; //board page menu
            (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).IsEnabled = false; //collection page menu
            (ApplicationBar.MenuItems[5] as ApplicationBarMenuItem).IsEnabled = false; //reserved page menu
            (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).IsEnabled = false; //essense page menu
            (ApplicationBar.MenuItems[7] as ApplicationBarMenuItem).IsEnabled = false; //bookmark menu
            (ApplicationBar.MenuItems[8] as ApplicationBarMenuItem).IsEnabled = false; //download menu

            LoadBoardProgressBar.Visibility = Visibility.Visible;
            BoardLoadingText.Visibility = Visibility.Visible;
            BoardNameTextBlock.Visibility = Visibility.Collapsed;
            TopicLinksListBox.Visibility = Visibility.Collapsed;
            OfflineTag.Visibility = System.Windows.Visibility.Collapsed;

            if (_url != null)
            {
                if (_offline)
                {
                    MitbbsBoardBase _savedBoard;
                    if (App.Settings.OfflineContentManager.TryLoadOfflineContent(_offlineID, _url, out _savedBoard))
                    {
                        _board = _savedBoard;
                        _board.HideTopArticle = false;
                        _board.HideTopArticle = GetHideTopSetting();

                        DataContext = _board;
                        OfflineTag.Visibility = System.Windows.Visibility.Visible;

                        Naboo.AppUtil.AsyncCallHelper.DelayCall(
                            () => Board_Loaded(this, null)
                            );
                    }
                    else
                    {
                        BoardLoadingText.Text = "离线内容不存在";
                        LoadBoardProgressBar.Visibility = Visibility.Collapsed;
                        return;
                    }
                }

                if (String.IsNullOrEmpty(_board.BoardName))
                {
                    BoardLoadingText.Text = "正在读取版面...";
                }
                else
                {
                    BoardLoadingText.Text = "正在读取<" + _board.BoardName + ">...";
                }

                if (_offline)
                {
                    return;
                }

                if (_board is MitbbsBoard)
                {
                    (_board as MitbbsBoard).UserIsLoggedIn = App.WebSession.IsLoggedIn;
                }

                _board.LoadFromUrl(App.WebSession.CreateWebClient(), _url, goForward);
            }
            else
            {
                BoardLoadingText.Text = "参数错误!";
            }
        }

        private void Board_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadBoardProgressBar.Visibility = Visibility.Collapsed;

            if (_board.IsLoaded)
            {
                BoardLoadingText.Visibility = Visibility.Collapsed;
                BoardNameTextBlock.Visibility = Visibility.Visible;
                TopicLinksListBox.Visibility = Visibility.Visible;

                BoardNameTextBlock.Text = _board.DisplayBoardName;
                App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _board.DisplayBoardName, "版面");

                if (_board.PrevPageUrl != null && !_offline)
                {
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //prev page button
                }

                if (_board.NextPageUrl != null && !_offline)
                {
                    (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true; //next page button
                }

                if (_board.NewPostUrl != null && !_offline)
                {
                    (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = true; //new post button
                }

                if (_board.EnBoardName != null && !_offline)
                {
                    (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true; //search menu
                }

                if (_board.FirstPageUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = true; //first page menu
                }

                if (_board.LastPageUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = true; //last page menu
                }

                if (_board.BoardPageUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = true; //board page menu
                }

                if (_club)
                {
                    (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).Text = "俱乐部"; //board page menu
                }

                if (_board.CollectionPageUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[4] as ApplicationBarMenuItem).IsEnabled = true; //collection page menu
                }

                if (_board.ReservePageUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[5] as ApplicationBarMenuItem).IsEnabled = true; //reserved page menu
                }

                if (_board.EssensePageUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[6] as ApplicationBarMenuItem).IsEnabled = true; //essense page menu
                }

                if (App.Settings.IsUrlBookmarked(_board.Url) || _board.IsLoaded)
                {
                    (ApplicationBar.MenuItems[7] as ApplicationBarMenuItem).IsEnabled = true; //bookmark menu
                }

                if (App.Settings.IsUrlBookmarked(_board.Url))
                {
                    (ApplicationBar.MenuItems[7] as ApplicationBarMenuItem).Text = "删除书签"; //bookmark menu
                }
                else
                {
                    (ApplicationBar.MenuItems[7] as ApplicationBarMenuItem).Text = "加入书签"; //bookmark menu
                }

                if (_scrollOffset >= 0)
                {
                    //TopicLinkListPanel.UpdateLayout();
                    //TopicLinkListPanel.ScrollToVerticalOffset(_scrollOffset);
                    _scrollOffset = -1;
                }
                else
                {
                    //TopicLinkListPanel.UpdateLayout();
                    //TopicLinkListPanel.ScrollToVerticalOffset(0);
                }

                if (TopicLinksListBox.Items.Count > 0)
                {
                    TopicLinksListBox.ScrollIntoView(TopicLinksListBox.Items[0]);
                }
            }
            else
            {
                BoardLoadingText.Text = "读取版面失败";

                if (_club)
                {
                    if (App.WebSession.IsLoggedIn)
                    {
                        MessageBox.Show("请确认你有访问此俱乐部的权限。请访问未名空间的网站管理你的俱乐部设置", "无法读取俱乐部", MessageBoxButton.OK);
                    }
                    else
                    {
                        MessageBox.Show("因为你尚未登录，所以你可能无法访问此俱乐部。请在设置页面中设置登录信息，并确认你的账户有访问此俱乐部的权限。请访问未名空间的网站管理你的俱乐部设置", "无法读取俱乐部", MessageBoxButton.OK);
                    }
                    NavigationService.GoBack();
                }
            }

            if (!_offline)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button

                if (_fullPage)
                {
                    (ApplicationBar.MenuItems[8] as ApplicationBarMenuItem).IsEnabled = true; //download menu
                }
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            PageHelper.InitAdControl(AdGrid);

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
            }

            if (parameters.ContainsKey("Type"))
            {
                _club = parameters["Type"].ToLower() == "club";
            }
            else
            {
                _club = false;
            }

            if (parameters.ContainsKey("Url"))
            {
                _originalUrl = parameters["Url"];
                _url = parameters["Url"];
                if (!_fullPage && !_url.ToLower().Contains("sno="))
                {
                    _url += "&sno=1";
                }
            }
            else
            {
                _url = null;
            }

            if (parameters.ContainsKey("OfflineID"))
            {
                _offline = HtmlAgilityPack.HtmlUtilities.TryParseGuid(parameters["OfflineID"], out _offlineID);
            }

            if (State.ContainsKey("Url"))
            {
                _url = (String)State["Url"];
            }

            if (_board == null || !_board.IsLoaded)
            {
                if (_board != null)
                {
                    _board.BoardLoaded -= Board_Loaded;
                }

                if (_fullPage)
                {
                    if (_club)
                    {
                        _board = new MitbbsClubBoard();
                    }
                    else
                    {
                        _board = new MitbbsBoard();
                    }
                }
                else
                {
                    _board = new MitbbsBoardMobile();
                }

                _board.BoardLoaded += Board_Loaded;
                DataContext = _board;
            }

            if (_board != null)
            {
                _board.HideTopArticle = GetHideTopSetting();
            }

            SetHideTopSetting(GetHideTopSetting());

            if (parameters.ContainsKey("Name"))
            {
                if (String.IsNullOrEmpty(_board.BoardName))
                {
                    _board.BoardName = parameters["Name"];
                }
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _board.DisplayBoardName, "版面");

            if ((!_board.IsLoaded || _forceRefresh) && State.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = (double)State["ScrollOffset"];
            }
            else
            {
                _scrollOffset = -1;
            }

            base.OnNavigatedTo(e);

            if (!_board.IsLoaded || _forceRefresh)
            {
                _forceRefresh = false;

                LoadBoard(true);
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

            if (_board != null && _board.IsLoaded)
            {
                //State["ScrollOffset"] = TopicLinkListPanel.VerticalOffset;
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
            LoadBoard(true);
        }

        private void PrevPageButton_Click(object sender, EventArgs e)
        {
            if (_board.PrevPageUrl != null)
            {
                _url = _board.PrevPageUrl;
                LoadBoard(false);
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void NextPageButton_Click(object sender, EventArgs e)
        {
            if (_board.NextPageUrl != null)
            {
                _url = _board.NextPageUrl;
                LoadBoard(true);
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void FirstPageMenu_Click(object sender, EventArgs e)
        {
            if (_board.FirstPageUrl != null)
            {
                _url = _board.FirstPageUrl;
                LoadBoard(true);
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void LastPageMenu_Click(object sender, EventArgs e)
        {
            if (_board.LastPageUrl != null)
            {
                _url = _board.LastPageUrl;
                LoadBoard(false);
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void TopicLinksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsTopicLinkBase selected = ((sender as ListBox).SelectedItem as MitbbsTopicLinkBase);

            if (selected != null)
            {
                if (_offline)
                {
                    selected.OfflineID = _offlineID.ToString();
                }

                PageHelper.OpenMitbbsLink(selected, NavigationService);
            }

            (sender as ListBox).SelectedItem = null;
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_board.Url);
        }

        private void CollectionPageMenu_Click(object sender, EventArgs e)
        {
            if (_board.CollectionPageUrl != null)
            {
                _url = _board.CollectionPageUrl;
                LoadBoard(true);
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void BoardPageMenu_Click(object sender, EventArgs e)
        {
            if (_board.BoardPageUrl != null)
            {
                _url = _board.BoardPageUrl;
                LoadBoard(true);
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void ReservedPageMenu_Click(object sender, EventArgs e)
        {
            if (_board.ReservePageUrl != null)
            {
                _url = _board.ReservePageUrl;
                LoadBoard(true);
                PageHelper.RefreshAdControl(AdGrid);
            }
        }

        private void EssensePageMenu_Click(object sender, EventArgs e)
        {
            if (_board.EssensePageUrl != null)
            {
                MitbbsBoardEssenceLink link = new MitbbsBoardEssenceLink() { Name = "", ParentUrl = _url, Url = _board.EssensePageUrl };
                PageHelper.OpenMitbbsLink(link, NavigationService);
            }
        }

        private void SettingMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenSettingPage(NavigationService, _offline);
        }

        private void DownloadMenu_Click(object sender, EventArgs e)
        {
            if (_club)
            {
                PageHelper.OpenDownloadPage(MitbbsOfflineContentType.ClubBoard, _url, _board.BoardName, NavigationService);
            }
            else if (_fullPage)
            {
                PageHelper.OpenDownloadPage(MitbbsOfflineContentType.Board, _url, _board.BoardName, NavigationService);
            }
            else
            {
                PageHelper.OpenDownloadPage(MitbbsOfflineContentType.Unknown, _url, _board.BoardName, NavigationService);
            }
        }

        private void BoardNameTextBlock_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //TopicLinkListPanel.ScrollToVerticalOffset(0);

            if (TopicLinksListBox.Items.Count > 0)
            {
                TopicLinksListBox.ScrollIntoView(TopicLinksListBox.Items[0]);
            }
        }

        private bool GetHideTopSetting()
        {
            return _hideTop.HasValue ? _hideTop.Value : App.Settings.HideTop;
        }

        private void SetHideTopSetting(bool hideTop)
        {
            if (hideTop == App.Settings.HideTop)
            {
                _hideTop = null;
            }
            else
            {
                _hideTop = hideTop;
            }

            if (_board != null)
            {
                _board.HideTopArticle = GetHideTopSetting();
            }

            if (GetHideTopSetting())
            {
                ToggleTopArticleText.Text = "  + ";
            }
            else
            {
                ToggleTopArticleText.Text = "  - ";
            }
        }

        private void ToggleTopArticleText_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SetHideTopSetting(!GetHideTopSetting());
        }

        private void DownloadStatusButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.NotficationCenter.OpenNotification(NavigationService);
        }

        private void BookmarkMenu_Click(object sender, EventArgs e)
        {
            if (App.Settings.IsUrlBookmarked(_board.Url))
            {
                App.Settings.RemoveReadingBookMark(_board.Url);
                (ApplicationBar.MenuItems[7] as ApplicationBarMenuItem).Text = "加入书签"; //bookmark menu
            }
            else
            {
                MitbbsLink boardLink;
                String boardName = _board.BoardName;
                if (!String.IsNullOrEmpty(_board.EnBoardName))
                {
                    boardName += " (" + _board.EnBoardName + ")";
                }

                if (_fullPage)
                {
                    if (_club)
                    {
                        boardLink = new MitbbsClubLink()
                        {
                            Name = boardName,
                            Url = _originalUrl,
                        };
                    }
                    else
                    {
                        boardLink = new MitbbsBoardLink()
                        {
                            Name = boardName,
                            Url = _originalUrl,
                        };
                    }
                }
                else
                {
                    boardLink = new MitbbsBoardLinkMobile()
                    {
                        Name = boardName,
                        Url = _board.Url,
                        BoardName = boardName,
                    };
                }

                if (_offline)
                {
                    boardLink.OfflineID = _offlineID.ToString();
                }

                App.Settings.AddReadingBookMark(boardLink);
                (ApplicationBar.MenuItems[7] as ApplicationBarMenuItem).Text = "删除书签"; //bookmark menu

                if (_club)
                {
                    MessageBox.Show("此俱乐部已成功加入本地收藏，请在'历史收藏'中查看。如果你已经登录用户，此俱乐部也会出现在用户主页的俱乐部列表中。", "添加收藏", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show("此版面已成功加入本地收藏，请在'历史收藏'中查看。如果你已经登录用户，此版面也会出现在用户主页的版面列表中。", "添加收藏", MessageBoxButton.OK);
                }
            }
        }

        private void SearchButton_Click_1(object sender, EventArgs e)
        {
            if (_board.EnBoardName != null)
            {
                String pageUrl;
                if (_club)
                {
                    pageUrl = String.Format("/Pages/SearchInBoardPage.xaml?BoardName={0}&Type=Club", Uri.EscapeDataString(_board.EnBoardName));
                }
                else
                {
                    pageUrl = String.Format("/Pages/SearchInBoardPage.xaml?BoardName={0}", Uri.EscapeDataString(_board.EnBoardName));
                }

                NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
            }
        }

        private void NewPostMenu_Click(object sender, EventArgs e)
        {
            if (_board.NewPostUrl != null)
            {
                if (App.WebSession.IsLoggedIn)
                {
                    String pageUrl = String.Format("/Pages/NewPostPage.xaml?Url={0}&SendFrom=true&FullPage={1}", Uri.EscapeDataString(_board.NewPostUrl), _fullPage);
                    NavigationService.Navigate(new Uri(pageUrl, UriKind.Relative));
                }
                else
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。");
                }
            }
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent(_originalUrl, _url);
        }
    }
}