using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;
using Microsoft.Phone.Shell;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class BackgroundDownloader : INotifyPropertyChanged
    {
        private bool _isDownloading = false;
        public bool IsDownloading
        {
            get
            {
                return _isDownloading;
            }

            private set
            {
                _isDownloading = value;
                NotifyPropertyChanged("IsDownloading");
            }
        }

        private OfflineDownloadQueue _downloadQueue;
        private Timer _timer;
        private object _lock = new object();
        private volatile bool _stopped = false;

        TimeSpan _interval = new TimeSpan(0, 15, 00);
        TimeSpan _initInterval = new TimeSpan(0, 0, 30);

        public BackgroundDownloader()
        {
            _timer = new Timer(Download_Timer);

            IsDownloading = false;
        }

        public void StartTimer(bool startNow = true)
        {
            TimeSpan interval = _initInterval;

            if (startNow)
            {
                interval = TimeSpan.FromSeconds(0);
            }

            _stopped = false;
            _timer.Change((long)interval.TotalMilliseconds, Timeout.Infinite);
        }

        public void StopTimer()
        {
            _stopped = true;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            Stop();
        }

        private void Download_Timer(object state)
        {
            lock (_lock)
            {
                if (!_stopped)
                {
                    TimeSpan interval = _initInterval;
                    try
                    {
                        if (AppUtil.AppInfo.IsNetworkConnected() && !App.WebSession.IsConnecting)
                        {
                            interval = _interval;
                            Start();
                        }
                    }
                    finally
                    {
                        if (!_stopped)
                        {
                            _timer.Change((long)interval.TotalMilliseconds, Timeout.Infinite);
                        }
                    }
                }
            }
        }

        public void Start()
        {
            lock (_lock)
            {
                if (IsDownloading)
                {
                    return;
                }

                if (_downloadQueue == null)
                {
                    _downloadQueue = new OfflineDownloadQueue();
                    _downloadQueue.DownloadCompleted += OfflineDownload_Completed;
                    _downloadQueue.ContentDownloadCompleted += OfflineContentDownload_Completed;
                }

                if (App.Settings.OfflineContentManager.DownloadQueue.Count > 0)
                {
                    DisableIdleDetection();

                    IsDownloading = true;

                    _downloadQueue.SetDownloadContents(App.Settings.OfflineContentManager.DownloadQueue);
                    _downloadQueue.StartDownload(App.WebSession.CreateWebClient(), null);
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_downloadQueue != null)
                {
                    _downloadQueue.Cancel();
                }

                IsDownloading = false;

                RestoreIdleDetection();
            }
        }

        private void OfflineContentDownload_Completed(object sender, OfflineDownloadQueue.DownloadQueueContentCompletedEventArgs e)
        {
            if (e.Success)
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                    () =>
                    {
                        App.Settings.OfflineContentManager.MoveQueuedContentToIndex(e.Content);
                        App.Settings.SaveToStorage();
                    }
                    );
            }
            else
            {
                if (AppUtil.AppInfo.IsNetworkConnected())
                {
                    e.Content.Failed = true;

                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                        () =>
                        {
                            App.Settings.OfflineContentManager.MoveQueuedContentToIndex(e.Content);
                            App.Settings.SaveToStorage();
                        }
                        );

                }
                else
                {
                    e.Content.Failed = false;
                }
            }
        }

        private void OfflineDownload_Completed(object sender, DataLoadedEventArgs e)
        {
            lock (_lock)
            {
                IsDownloading = false;

                RestoreIdleDetection();

                StartTimer(true);
            }
        }

        private void DisableIdleDetection()
        {
            if (Naboo.AppUtil.AppInfo.IsWifiConnected())
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }

#if !NODO
            if (Naboo.AppUtil.AppInfo.IsNetworkConnected())
            {
                PhoneApplicationService.Current.ApplicationIdleDetectionMode = IdleDetectionMode.Disabled;
            }
#endif
        }

        private void RestoreIdleDetection()
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                () =>
                {
                    PropertyChangedEventHandler handler = PropertyChanged;
                    if (null != handler)
                    {
                        handler(this, new PropertyChangedEventArgs(propertyName));
                    }
                }
                );
        }
    }
}
