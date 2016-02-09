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
    public partial class BoardEssencePage : PhoneApplicationPage
    {
        private String _url;
        private MitbbsBoardEssence _board;
        private double _scrollOffset = -1;
        private UrlHistory _urlHistory = new UrlHistory();
        private bool _offline = false;
        private Guid _offlineID;
        private String _folderName = "根目录";

        public BoardEssencePage()
        {
            InitializeComponent();
            _board = new MitbbsBoardEssence();
            _board.BoardLoaded += Board_Loaded;
            DataContext = _board;
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(ApplicationBar.MenuItems.Count - 1); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot);
        }

        private void LoadBoard()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //refresh button
            
            (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = false; //board page menu
            (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = false; //download menu
            (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = false; //setting menu

            //(ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = false; //collection page menu
            //(ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = false; //reserved page menu
            //(ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = false; //essense page menu

            LoadBoardProgressBar.Visibility = Visibility.Visible;
            BoardLoadingText.Visibility = Visibility.Visible;
            BoardNameTextBlock.Visibility = Visibility.Collapsed;
            EssenceLinkListBox.Visibility = Visibility.Collapsed;
            OfflineTag.Visibility = System.Windows.Visibility.Collapsed;

            if (_url != null)
            {
                if (_offline)
                {
                    MitbbsBoardEssence _savedBoard;
                    if (App.Settings.OfflineContentManager.TryLoadOfflineContent(_offlineID, _url, out _savedBoard))
                    {
                        _board = _savedBoard;
                        
                        DataContext = _board;
                        
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
                    BoardLoadingText.Text = "正在读取精华区...";
                }
                else
                {
                    BoardLoadingText.Text = "正在读取<" + _board.BoardName + ">...";
                }

                if (_offline)
                {
                    return;
                }

                _board.LoadFromUrl(App.WebSession.CreateWebClient(), _url);
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
                EssenceLinkListBox.Visibility = Visibility.Visible;

                BoardNameTextBlock.Text = _board.BoardName;
                App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _board.BoardName, "精华区");

                if (_board.BoardPageUrl != null && !_offline)
                {
                    (ApplicationBar.MenuItems[0] as ApplicationBarMenuItem).IsEnabled = true; //board page menu
                }

                //if (_board.CollectionPageUrl != null)
                //{
                //    (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = true; //collection page menu
                //}

                //if (_board.ReservePageUrl != null)
                //{
                //    (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).IsEnabled = true; //reserved page menu
                //}

                //if (_board.EssensePageUrl != null)
                //{
                //    (ApplicationBar.MenuItems[3] as ApplicationBarMenuItem).IsEnabled = true; //essense page menu
                //}

                if (_scrollOffset >= 0)
                {
                    //EssenceLinkListPanel.UpdateLayout();
                    //EssenceLinkListPanel.ScrollToVerticalOffset(_scrollOffset);
                    _scrollOffset = -1;
                }
                else
                {
                    //EssenceLinkListPanel.UpdateLayout();
                    //EssenceLinkListPanel.ScrollToVerticalOffset(0);
                }

                if (_offline)
                {
                    OfflineTag.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                BoardLoadingText.Text = "读取精华区失败";
            }

            if (!_offline)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //refresh button
                (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).IsEnabled = true; //download menu
            }

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

            if (parameters.ContainsKey("Name"))
            {
                if (String.IsNullOrEmpty(_board.BoardName))
                {
                    _board.BoardName = parameters["Name"];
                }
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, _board.BoardName, "精华区");

            if (State.ContainsKey("Url"))
            {
                _url = (String)State["Url"];
            }

            if (parameters.ContainsKey("OfflineID"))
            {
                _offline = HtmlAgilityPack.HtmlUtilities.TryParseGuid(parameters["OfflineID"], out _offlineID);
            }

            if (!_board.IsLoaded)
            {
                _urlHistory.LoadState(State, "UrlHistory");
            }

            if (!_board.IsLoaded && State.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = (double)State["ScrollOffset"];
            }
            else
            {
                _scrollOffset = -1;
            }

            base.OnNavigatedTo(e);

            if (!_board.IsLoaded)
            {
                LoadBoard();
            }
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            if (PageHelper.SessionHistoryHandleNavigateFrom(NavigationService))
            {
                return;
            }

            PageHelper.CleanupAdControl(AdGrid);

            State.Clear();

            State["Url"] = _url;

            _urlHistory.SaveState(State, "UrlHistory");

            if (_board.IsLoaded)
            {
                //State["ScrollOffset"] = EssenceLinkListPanel.VerticalOffset;
            }

            base.OnNavigatedFrom(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PageHelper.SessionHistoryHandlePageLoaded(NavigationService))
            {
                return;
            }

            if (_board.IsLoaded)
            {
                return;
            }

            LoadBoard();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadBoard();
        }

        private bool TryGoToUpperLevel()
        {
            if (_urlHistory.Count > 0)
            {
                UrlHistoryItem lastUrl = _urlHistory.Pop();
                if (lastUrl != null)
                {
                    MitbbsBoardEssence _lastBoard = (lastUrl.Data as MitbbsBoardEssence);
                    _url = lastUrl.Url;
                    _folderName = lastUrl.Name;

                    if (_lastBoard != null)
                    {
                        _board = _lastBoard;
                        DataContext = _board;

                        if (!_board.IsLoaded)
                        {
                            _scrollOffset = lastUrl.ScrollOffset;
                            LoadBoard();
                            PageHelper.RefreshAdControl(AdGrid);
                        }
                        else
                        {
                            Board_Loaded(this, null);
                            //EssenceLinkListPanel.UpdateLayout();
                            //EssenceLinkListPanel.ScrollToVerticalOffset(lastUrl.ScrollOffset);
                        }
                    }
                    else
                    {
                        _scrollOffset = lastUrl.ScrollOffset;
                        LoadBoard();
                        PageHelper.RefreshAdControl(AdGrid);
                    }

                    return true;
                }
            }

            return false;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (TryGoToUpperLevel())
            {
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        private void EssenceLinkListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsLink selected = ((sender as ListBox).SelectedItem as MitbbsLink);

            //if (selected is MitbbsBoardEssenceLink)
            //{
            //    selected.Select();
            //    String name = _board.BoardName;
            //    _urlHistory.Push(new UrlHistoryItem() { Url = _url, ScrollOffset = 0 /*EssenceLinkListPanel.VerticalOffset*/ , Data = _board, Name = _folderName });
                    
            //    _board = new MitbbsBoardEssence();
            //    _board.BoardName = name;
            //    _board.BoardLoaded += Board_Loaded;
            //    DataContext = _board;

            //    _url = selected.Url;
            //    _folderName = selected.Name;
            //    LoadBoard();
            //    PageHelper.RefreshAdControl(AdGrid);
            //}
            //else
            //{
            //    if (_offline)
            //    {
            //        selected.OfflineID = _offlineID.ToString();
            //    }

            //    PageHelper.OpenMitbbsLink(selected, NavigationService);
            //}

            if (selected != null)
            {
                if (_offline)
                {
                    selected.OfflineID = _offlineID.ToString();
                }
            }

            PageHelper.OpenMitbbsLink(selected, NavigationService);

            (sender as ListBox).SelectedItem = null;
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_board.Url);
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            if (!TryGoToUpperLevel())
            {
                NavigationService.GoBack();
            }
        }

        private void BoardPageMenu_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void SettingMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenSettingPage(NavigationService);
        }

        //private void CollectionPageMenu_Click(object sender, EventArgs e)
        //{
        //    if (_board.CollectionPageUrl != null)
        //    {
        //        _url = _board.CollectionPageUrl;
        //        LoadBoard();
        //    }
        //}

        //private void BoardPageMenu_Click(object sender, EventArgs e)
        //{
        //    if (_board.BoardPageUrl != null)
        //    {
        //        _url = _board.BoardPageUrl;
        //        LoadBoard();
        //    }
        //}

        //private void ReservedPageMenu_Click(object sender, EventArgs e)
        //{
        //    if (_board.ReservePageUrl != null)
        //    {
        //        _url = _board.ReservePageUrl;
        //        LoadBoard();
        //    }
        //}

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
            PageHelper.OpenDownloadPage(MitbbsOfflineContentType.BoardEssense, _board.Url, _board.BoardName + " - " + PageHelper.GetFilteredEssenseItemName(_folderName), NavigationService);
        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent("", _url);
        }
    }
}