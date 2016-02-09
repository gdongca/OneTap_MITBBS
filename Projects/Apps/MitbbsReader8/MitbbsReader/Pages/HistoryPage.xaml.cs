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
using System.Collections.ObjectModel;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Naboo.MitbbsReader.Pages
{
    public partial class HistoryPage : PhoneApplicationPage
    {
        private double _scrollOffset = -1;
        private int _selectedPivot = -1;

        private ObservableCollection<MitbbsLink> _readingHistory = new ObservableCollection<MitbbsLink>();
        
        public ObservableCollection<MitbbsLink> ReadingHistory
        {
            get
            {
                return App.Settings.ReadingHistory;
            }
        }

        public ObservableCollection<MitbbsLink> BoardHistory
        {
            get
            {
                return App.Settings.BoardHistory;
            }
        }

        public ObservableCollection<MitbbsLink> Bookmarks
        {
            get
            {
                return App.Settings.ReadingBookmarks;
            }
        }

        public ObservableCollection<MitbbsLink> BoardBookmarks
        {
            get
            {
                return App.Settings.BoardBookmarks;
            }
        }

        public ObservableCollection<MitbbsLink> WatchList
        {
            get
            {
                return App.Settings.WatchList;
            }
        }

        public HistoryPage()
        {
            InitializeComponent();
            DataContext = this;
            CheckWatchListProgressBar.DataContext = App.Settings.NotficationCenter;
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "History");
        }

        private void RefreshHistory()
        {
            int count = 0;
            _readingHistory.Clear();

            foreach (MitbbsLink link in App.Settings.ReadingHistory)
            {
                _readingHistory.Add(link);
                count++;

                if (count >= 50)
                {
                    break;
                }
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            bool cleanHistory = false;
            foreach (var item in NavigationService.BackStack)
            {
                if (item.Source.OriginalString.Contains("HistoryPage.xaml"))
                {
                    cleanHistory = true;
                    break;
                }
            }

            if (cleanHistory)
            {
                while (NavigationService.BackStack.Count() > 1)
                {
                    NavigationService.RemoveBackEntry();
                }

                App.Settings.CurrentSessionHistory.Reset();
            }

            if (PageHelper.SessionHistoryHandleNavigateTo(NavigationService))
            {
                return;
            }

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "关注、历史、收藏");

            SetEditable(false);

            //RefreshHistory();

            if (State.ContainsKey("SelectedIndex"))
            {
                _selectedPivot = (int)State["SelectedIndex"];
            }

            if (State.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = (double)State["ScrollOffset"];
            }

            App.Settings.CheckWatchList(false);

            base.OnNavigatedTo(e);
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

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (MitbbsLink.CanEdit)
            {
                SetEditable(false);
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
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
            else if (WatchList.Count <= 0)
            {
                if (Bookmarks.Count > 0)
                {
                    PivotControl.SelectedIndex = 1;
                }
                else if (BoardBookmarks.Count > 0)
                {
                    PivotControl.SelectedIndex = 2;
                }
                else if (ReadingHistory.Count > 0)
                {
                    PivotControl.SelectedIndex = 3;
                }
                else if (BoardHistory.Count > 0)
                {
                    PivotControl.SelectedIndex = 4;
                }
            }
            else
            {
                if (App.Settings.AutoCheckUpdate && App.Settings.NotficationCenter.HasOldWatchedItem)
                {
                    MessageBox.Show("如果你不想继续检查老文章的更新，请将其从关注列表里删除。及时删除不再关注的文章有助于节省您手机电池的电力。", "你关注的文章中有些已经很久没有更新", MessageBoxButton.OK);
                }
            }

        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsLink link = ((sender as ListBox).SelectedItem as MitbbsLink);

            if (link != null)
            {
                Guid rootID;
                if (HtmlAgilityPack.HtmlUtilities.TryParseGuid(link.OfflineID, out rootID))
                {
                    if (!App.Settings.OfflineContentManager.OfflineContentExists(rootID))
                    {
                        link.OfflineID = "";
                    }
                }

                PageHelper.OpenMitbbsLink(link, NavigationService, true);

                (sender as ListBox).SelectedItem = null;
            }
        }

        private void ClearHistoryMenu_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("你确认清楚所有历史记录吗？", "清除历史记录", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                App.Settings.ClearHistory();
                //RefreshHistory();
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void DeleteFavButton_Click(object sender, RoutedEventArgs e)
        {
            MitbbsLink link = (sender as Button).Tag as MitbbsLink;

            if (link != null)
            {
                App.Settings.RemoveReadingBookMark(link.Url);
            }
        }

        private void SetEditable(bool editable)
        {
            App.Settings.SetHistoryEditable(editable);

            if (MitbbsLink.CanEdit)
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).Text = "结束编辑";
            }
            else
            {
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).Text = "编辑";
            }
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            SetEditable(!MitbbsLink.CanEdit);
        }

        private void DeleteWatchButton_Click(object sender, RoutedEventArgs e)
        {
            MitbbsLink link = (sender as Button).Tag as MitbbsLink;

            if (link != null)
            {
                App.Settings.RemoveWatchItem(link.Url);
            }
        }

        private void CheckWatchButton_Click(object sender, EventArgs e)
        {
            PivotControl.SelectedIndex = 0;

            if (WatchList.Count > 0)
            {
                App.Settings.CheckWatchList(true);
            }
            else
            {
                MessageBox.Show("你没有添加任何关注文章。请打开文章后点击'关注'菜单添加文章到关注文章列表。");
            }
        }

        private void WatchListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsLink link = ((sender as ListBox).SelectedItem as MitbbsLink);

            if (link != null)
            {
                Guid rootID;
                if (HtmlAgilityPack.HtmlUtilities.TryParseGuid(link.OfflineID, out rootID))
                {
                    if (!App.Settings.OfflineContentManager.OfflineContentExists(rootID))
                    {
                        link.OfflineID = "";
                    }
                }

                link.HasNewContent = false;
                PageHelper.OpenMitbbsLink(link, NavigationService, true);

                (sender as ListBox).SelectedItem = null;

                App.Settings.NotficationCenter.UpdateStatus();
            }
        }

        private void ListBoxItem_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SetEditable(!MitbbsLink.CanEdit);
        }

        private void DownloadStatusButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            //App.Settings.NotficationCenter.OpenNotification(NavigationService);
            PivotControl.SelectedIndex = 0;
        }
    }
}