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
using System.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Reactive;

namespace Naboo.MitbbsReader.Pages
{
    public partial class OfflineContentPage : PhoneApplicationPage
    {
        private double _scrollOffset = -1;
        private int _selectedPivot = -1;
        private bool _loaded = false;

        public OfflineContentPage()
        {
            InitializeComponent();
            DataContext = App.Settings.OfflineContentManager;
            DownloadStatusButton.DataContext = App.Settings.BGDownloader;
            NotificationButton.DataContext = App.Settings.NotficationCenter;

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "OfflineContent");
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            bool cleanHistory = false;
            foreach (var item in NavigationService.BackStack)
            {
                if (item.Source.OriginalString.Contains("OfflineContentPage.xaml"))
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

            App.Settings.CurrentSessionHistory.SetLastPageName(NavigationService, "离线内容");

            SetEditable(false);

            _selectedPivot = -1;
            _scrollOffset = -1;
            
            if (!_loaded)
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

            UpdateDownloadButtons();

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

            SetEditable(false);

            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (MitbbsOfflineContentManager.IsEditable)
            {
                SetEditable(!MitbbsOfflineContentManager.IsEditable);
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
        }

        private void HomeContentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsOfflineContentIndex contentIndex = ((sender as ListBox).SelectedItem as MitbbsOfflineContentIndex);

            if (contentIndex != null)
            {
                bool open = true;

                if (!contentIndex.IsDownloaded)
                {
                    MessageBoxResult result = MessageBox.Show("有的文章可能无法阅读。你确信想打开此离线内容吗？", "此离线内容尚未完全下载", MessageBoxButton.OKCancel);

                    if (result != MessageBoxResult.OK)
                    {
                        open = false;
                    }
                }

                if (open)
                {
                    PageHelper.OpenMitbbsLink(contentIndex.Link, NavigationService, true);
                }

                (sender as ListBox).SelectedItem = null;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (PageHelper.SessionHistoryHandlePageLoaded(NavigationService))
            {
                return;
            }

            if (!_loaded)
            {
                if (_selectedPivot >= 0)
                {
                    PivotControl.SelectedIndex = _selectedPivot;
                    _selectedPivot = -1;

                    //ScrollViewer scrollPanel = (PivotControl.SelectedItem as PivotItem).Content as ScrollViewer;
                    //if (_scrollOffset >= 0)
                    //{
                    //    scrollPanel.UpdateLayout();
                    //    scrollPanel.ScrollToVerticalOffset(_scrollOffset);
                    //    _scrollOffset = -1;
                    //}
                }
                else if (App.Settings.OfflineContentManager.DownloadQueue.Count > 0)
                {
                    //MessageBox.Show("请点击'开始下载'按钮继续下载队列中的离线内容", "下载队列中有未完成的内容", MessageBoxButton.OK);
                    PivotControl.SelectedIndex = 1;
                }
                else if (App.Settings.OfflineContentManager.AllContents.Count <= 0)
                {
                    if (App.Settings.OfflineContentManager.DownloadQueue.Count > 0)
                    {
                        PivotControl.SelectedIndex = 1;
                    }
                    else
                    {
                        MessageBox.Show("请在主页、版面、或者文章的页面里选择'下载'菜单下载离线内容", "你目前没有任何离线内容", MessageBoxButton.OK);
                    }
                }
            }

            _loaded = true;
        }

        private void CleanUpMenu_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("你确定要删除所有离线内容吗？", "删除离线内容", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
#if DEBUG
                Guid[] excludes = null;
#else
                Guid[] excludes  = { App.Settings.BoardGroupPreloadOfflineID };
#endif

                App.Settings.PauseDownload();

                App.Settings.OfflineContentManager.CleanupAllContentIndex();

                IScheduler scheduler = Scheduler.ThreadPool;
                    scheduler.Schedule(new Action(
                        () =>
                        {
                            App.Settings.OfflineContentManager.CleanupAllOfflineContent(excludes);
                        }
                        ),
                        TimeSpan.FromMilliseconds(0)
                        );
            }
        }

        private void DeleteContentButton_Click(object sender, RoutedEventArgs e)
        {
            MitbbsOfflineContentIndex contentIndex = (sender as Button).Tag as MitbbsOfflineContentIndex;

            if (contentIndex != null)
            {
                MessageBoxResult result = MessageBox.Show("你想要删除[" + contentIndex.Link.Name + "]已下载的内容吗？", "删除离线内容", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    App.Settings.OfflineContentManager.CleanupOfflineContent(contentIndex);
                }
            }
        }

        private void SetEditable(bool editable)
        {
            App.Settings.OfflineContentManager.SetEditable(editable);

            if (MitbbsOfflineContentManager.IsEditable)
            {
                App.Settings.PauseDownload();
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).Text = "结束编辑";
            }
            else
            {
                App.Settings.RestoreDownload();
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).Text = "编辑";
            }

            UpdateDownloadButtons();
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            SetEditable(!MitbbsOfflineContentManager.IsEditable);
        }

        private void InfoButton_Click(object sender, EventArgs e)
        {
            long size = App.Settings.OfflineContentManager.TotalSize;
            long available = App.Settings.OfflineContentManager.GetAvailableFreeSpace();

            try
            {
                MessageBox.Show(
                    "可下载的离线内容类型: 主页，版面，文章\n" +
                    "已用空间: " + size.ToString("N0") + " Bytes\n" +
                    "可用空间: " + available.ToString("N0") + " Bytes\n",
                    "信息",
                    MessageBoxButton.OK
                    );
            }
            catch (Exception ex)
            {
            }
        }

        private void DownloadQueueListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MitbbsOfflineContentIndex contentIndex = ((sender as ListBox).SelectedItem as MitbbsOfflineContentIndex);

            if (contentIndex != null)
            {
                MessageBoxResult result = MessageBox.Show("有的文章可能无法阅读。你确信想打开此离线内容吗？", "此离线内容尚未完全下载", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    PageHelper.OpenMitbbsLink(contentIndex.Link, NavigationService, true);
                }

                (sender as ListBox).SelectedItem = null;
            }
        }

        private void ReDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            MitbbsOfflineContentIndex contentIndex = (sender as Button).Tag as MitbbsOfflineContentIndex;

            if (contentIndex != null)
            {
                MessageBoxResult result = MessageBoxResult.OK ;

                if (contentIndex.IsDownloaded)
                {
                    result = MessageBox.Show("你想要删除[" + contentIndex.Link.Name + "]已下载的内容吗？如果你选择不删除，新的下载内容将和老的内容共存。", "重新下载离线内容", MessageBoxButton.OKCancel);
                }

                if (result == MessageBoxResult.OK)
                {
                    App.Settings.OfflineContentManager.RedownloadContent(contentIndex);
                }
                else
                {
                    App.Settings.OfflineContentManager.AddContentToQueue(contentIndex.Key, contentIndex.Name, contentIndex.ContentType);
                }

                //App.Settings.RestoreDownload();

                if (App.Settings.CanDownloadStarts(true, true))
                {
                    MessageBox.Show("将在本程序运行的同时自动下载。如果你不想中断离线内容的下载，请不要切换到其它程序或者锁屏。如果下载被中断，将会在下次程序运行的时候自动恢复。", "此内容已加入下载队列", MessageBoxButton.OK);
                }
                //else
                //{
                //    MessageBox.Show("自动下载已被关闭，请手动开始下载", "此内容已加入下载队列", MessageBoxButton.OK);
                //}
            }
        }

        private void DownloadButton_Click(object sender, EventArgs e)
        {
            //if (App.Settings.OfflineContentManager.DownloadQueue.Count > 0)
            //{
            //    PageHelper.OpenDownloadPage(NavigationService);
            //}
            //else
            //{
            //    MessageBox.Show("下载队列为空。请点击主页、版面、俱乐部、文章页面里的'下载'菜单添加离线内容到下载队列。", "无法开始下载", MessageBoxButton.OK);
            //}

            App.Settings.StartDownload();

            App.Settings.CanDownloadStarts(true);

            UpdateDownloadButtons();
        }

        private void StopDownloadButton_Click(object sender, EventArgs e)
        {
            App.Settings.StopDownload();

            UpdateDownloadButtons();
        }

        private void UpdateDownloadButtons()
        {
            if (MitbbsOfflineContentManager.IsEditable)
            {
                DownloadStatusText.Text = "状态：下载已停止";
                (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false;
                (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false;
            }
            else if (App.Settings.CanDownloadStarts())
            {
                DownloadStatusText.Text = "状态：自动下载";
                (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = false;
                (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = true;
            }
            else
            {
                DownloadStatusText.Text = "状态：下载已停止";
                (ApplicationBar.Buttons[2] as ApplicationBarIconButton).IsEnabled = true;
                (ApplicationBar.Buttons[3] as ApplicationBarIconButton).IsEnabled = false;
            }
        }

        private void ListBoxItem_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            SetEditable(!MitbbsOfflineContentManager.IsEditable);
        }

        private void DownloadStatusButton_Click(object sender, RoutedEventArgs e)
        {
            //NavigationService.Navigate(new Uri("/Pages/OfflineContentPage.xaml", UriKind.Relative));
            PivotControl.SelectedIndex = 3;
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.NotficationCenter.OpenNotification(NavigationService);
        }
    }
}