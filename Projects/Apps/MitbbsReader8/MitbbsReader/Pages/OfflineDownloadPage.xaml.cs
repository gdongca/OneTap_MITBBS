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
using Naboo.MitbbsReader;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public partial class OfflineDownloadPage : PhoneApplicationPage
    {
        private bool _downloadCompleted;
        private OfflineDownloadQueue _downloader = null;

        public OfflineDownloadPage()
        {
            InitializeComponent();

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "OfflineDownload");
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            PageHelper.InitAdControl(AdGrid);

            var parameters = NavigationContext.QueryString;

            String _url = null;
            String _type = null;
            String _name = null;

            if (parameters.ContainsKey("Type"))
            {
                _type = parameters["Type"].ToLower();
            }

            if (parameters.ContainsKey("Url"))
            {
                _url = parameters["Url"].ToLower();
            }

            if (parameters.ContainsKey("Name"))
            {
                _name = parameters["Name"].ToLower();
            }
            else
            {
                _name = "离线内容";
            }

            if (State.ContainsKey("DownloadCompleted"))
            {
                _downloadCompleted = (bool)State["DownloadCompleted"];
            }

            MitbbsOfflineContentType contentType = MitbbsOfflineContentType.Unknown;

            if (_downloader == null)
            {
                if (_type == "home")
                {
                    contentType = MitbbsOfflineContentType.Home;
                }
                else if (_type == "userhome")
                {
                    contentType = MitbbsOfflineContentType.UserHome;
                }
                else if (_type == "board")
                {
                    contentType = MitbbsOfflineContentType.Board;
                }
                else if (_type == "clubboard")
                {
                    contentType = MitbbsOfflineContentType.ClubBoard;
                }
                else if (_type == "topic")
                {
                    contentType = MitbbsOfflineContentType.Topic;
                }
                else if (_type == "clubtopic")
                {
                    contentType = MitbbsOfflineContentType.ClubTopic;
                }

                if (_url != null && !App.Settings.OfflineContentManager.AddContentToQueue(_url, _name, contentType))
                {
                    MessageBox.Show("无法识别离线内容的类型", "无法添加离线内容到下载队列", MessageBoxButton.OK);
                    NavigationService.GoBack();
                    return;
                }
                else
                {
                    if (_url != null)
                    {
                        App.Settings.SaveToStorage();

                        MessageBoxResult result = MessageBox.Show("现在开始下载下载队列中的所有内容吗？如果选择关闭，你可以之后在离线内容页面里选择继续下载队列中的项目。", "离线内容已成功添加至队列", MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.Cancel)
                        {
                            _downloadCompleted = true;
                            NavigationService.GoBack();
                        }
                    }

                    _downloader = new OfflineDownloadQueue();
                }
            }
            else if (_downloader.IsDownloading)
            {
                DisableIdleDetection();
            }

            if (_downloader != null && !_downloadCompleted && !_downloader.IsDownloading)
            {
                StartDownload();
            }

            UpdateButtonStates();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            RestoreIdleDetection();
            State["DownloadCompleted"] = _downloadCompleted;

            if (_downloader != null)
            {
                State["OfflineID"] = _downloader.RootID;
            }

            if (_downloader != null && _downloader.IsDownloading)
            {
                _downloader.DownloadCompleted -= OfflineDownload_Completed;
                _downloader.DownloadProgressed -= OfflineDownload_Progress;
                _downloader.ContentDownloadCompleted -= OfflineContentDownload_Completed;
                _downloader.Cancel();
            }

            base.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmCancel())
            {
                e.Cancel = true;
                return;
            }

            _downloader.DownloadCompleted -= OfflineDownload_Completed;
            _downloader.DownloadProgressed -= OfflineDownload_Progress;
            _downloader.ContentDownloadCompleted -= OfflineContentDownload_Completed;
            _downloader.Cancel();

            base.OnBackKeyPress(e);
        }

        private void DisableIdleDetection()
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;

#if !NODO
            if (Naboo.AppUtil.AppInfo.IsNetworkConnected() &&
                Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType != Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.Wireless80211)
            {
                PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
            }
#endif
        }

        private void RestoreIdleDetection()
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
        }

        private bool ConfirmCancel()
        {
            if (_downloader == null || !_downloader.IsDownloading || _downloadCompleted)
            {
                return true;
            }

            MessageBoxResult result = MessageBox.Show("你确认中断下载下载队列中的离线内容吗？如果选择中断，你可以随后在离线内容页面里点击'开始下载'按钮继续下载队列中的项目。", "中断下载", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_downloadCompleted && _downloader == null)
            {
                MessageBox.Show("未知错误", "无法下载离线内容", MessageBoxButton.OK);
                NavigationService.GoBack();
            }
        }

        private void StartDownload()
        {
            DisableIdleDetection();

            ProgressTextPanel.Children.Clear();

            DownloadProgressBar.Minimum = 0;
            DownloadProgressBar.Maximum = 100;
            DownloadProgressBar.Value = 0;
            DownloadProgressBar2.Visibility = System.Windows.Visibility.Visible;

            _downloader.SetDownloadContents(App.Settings.OfflineContentManager.DownloadQueue);
            _downloader.DownloadCompleted += OfflineDownload_Completed;
            _downloader.DownloadProgressed += OfflineDownload_Progress;
            _downloader.ContentDownloadCompleted += OfflineContentDownload_Completed;
            _downloader.StartDownload(App.WebSession.CreateWebClient(), null);
        }

        private void OfflineContentDownload_Completed(object sender, OfflineDownloadQueue.DownloadQueueContentCompletedEventArgs e)
        {
            if (e.Success)
            {
                App.Settings.OfflineContentManager.MoveQueuedContentToIndex(e.Content);
                App.Settings.SaveToStorage();
            }
        }

        private void OfflineDownload_Completed(object sender, DataLoadedEventArgs e)
        {
            _downloader.DownloadCompleted -= OfflineDownload_Completed;
            _downloader.DownloadProgressed -= OfflineDownload_Progress;
            _downloader.ContentDownloadCompleted -= OfflineContentDownload_Completed;
            DownloadProgressBar.Value = 100;
            DownloadProgressBar2.Visibility = System.Windows.Visibility.Collapsed;

            _downloadCompleted = _downloader.IsCompleted;

            if (!_downloader.IsCompleted || App.Settings.OfflineContentManager.DownloadQueue.Count > 0)
            {
                MessageBoxResult result = MessageBox.Show("是否重试？", "队列中还有未完成下载的离线内容", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    StartDownload();
                    return;
                }
                else
                {
                    App.Settings.OfflineContentManager.CleanupOfflineContent(_downloader.RootID);
                }
            }
            else
            {
                _downloadCompleted = true;
                App.Settings.SaveToStorage();
            }

            UpdateButtonStates();
        }

        private void OfflineDownload_Progress(object sender, OfflineDownloader.DownloadProgressEventArgs e)
        {
            TextBlock progressText = new TextBlock()
            {
                Text = e.Step,
                TextWrapping = TextWrapping.Wrap
            };

            DownloadProgressBar.Value = e.Progress;
            PageTitle.Text = "正在下载 " + e.Progress + "%";

            ProgressTextPanel.Children.Add(progressText);
            ProgressTextPanel.UpdateLayout();
            ProgressScrollViewer.UpdateLayout();
            ProgressScrollViewer.ScrollToVerticalOffset(ProgressTextPanel.ActualHeight);
        }

        private void UpdateButtonStates()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = _downloadCompleted; //ok button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = !_downloadCompleted; //cancel button

            if (_downloadCompleted)
            {
                PageTitle.Text = "下载完成";
                Subtitle.Text = "请在'离线内容'页面里查看已下载的内容";
                Subtitle.Visibility = System.Windows.Visibility.Visible;
                RestoreIdleDetection();
            }
            else if (_downloader != null && _downloader.IsDownloading)
            {
                PageTitle.Text = "正在下载...";
                Subtitle.Text = "正在下载离线内容，请不要切换到其它程序或者锁屏";
                Subtitle.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                PageTitle.Text = "下载失败";
                Subtitle.Visibility = System.Windows.Visibility.Collapsed;
                RestoreIdleDetection();
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            if (ConfirmCancel())
            {
                _downloader.Cancel();
                NavigationService.GoBack();
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (_downloadCompleted)
            {
                NavigationService.GoBack();
            }
        }
    }
}