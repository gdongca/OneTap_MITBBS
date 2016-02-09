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
using System.ComponentModel;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Phone.Shell;

namespace Naboo.MitbbsReader
{
    public class NotificationCenter : INotifyPropertyChanged
    {
        private bool _hasNewMail = false;
        public bool HasNewMail
        {
            get
            {
                return _hasNewMail;
            }

            set
            {
                _hasNewMail = value;
                NotifyPropertyChanged("HasNewMail");
                NotifyPropertyChanged("HasNotification");
            }
        }

        private bool _hasNewUpdate = false;
        public bool HasNewUpdate
        {
            get
            {
                return _hasNewUpdate;
            }

            set
            {
                _hasNewUpdate = value;
                NotifyPropertyChanged("HasNewUpdate");
                NotifyPropertyChanged("HasNotification");
            }
        }

        private bool _hasOldWatchedItem = false;
        public bool HasOldWatchedItem
        {
            get
            {
                bool result = _hasOldWatchedItem;
                _hasOldWatchedItem = false;
                return result;
            }
        }

        public bool HasNotification
        {
            get
            {
                return _hasNewMail || _hasNewUpdate;
            }
        }

        public bool Checking
        {
            get
            {
                return _checking;
            }

            set
            {
                _checking = value;
                NotifyPropertyChanged("Checking");
            }
        }

        private Timer _timer;
        TimeSpan _initInterval = new TimeSpan(0, 0, 5);
        TimeSpan _interval = new TimeSpan(0, 15, 0);
        private bool _checking = false;
        private DateTime _lastCheckTime = DateTime.MinValue;
        private bool _firstCheck = true;
        private volatile bool _stopped = false;
        private object _lock = new object();
        private volatile bool _forceCheck = false;

        public NotificationCenter()
        {
            _timer = new Timer(Check_Timer);
        }

        public void StartTimer()
        {
            UpdateStatus();

            if (!App.Settings.AutoCheckUpdate)
            {
                return;
            }

            _firstCheck = true;

            lock (_lock)
            {
                _stopped = false;
                _timer.Change((long)_initInterval.TotalMilliseconds, Timeout.Infinite);
            }
        }

        public void StartCheckNow(bool forceCheck = false)
        {
            lock (_lock)
            {
                _stopped = false;
                _forceCheck = forceCheck;

                _timer.Change(0, Timeout.Infinite);
            }
        }

        public void StopTimer()
        {
            lock (_lock)
            {
                _stopped = true;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void Check_Timer(object state)
        {
            if (App.WebSession.IsConnecting || _stopped)
            {
                if (!_stopped && App.Settings.AutoCheckUpdate)
                {
                    _timer.Change((long)_initInterval.TotalMilliseconds, Timeout.Infinite);
                }
                return;
            }

            _initInterval = _interval;
            
            StartChecking();

            if (!_stopped && App.Settings.AutoCheckUpdate)
            {
                _timer.Change((long)_interval.TotalMilliseconds, Timeout.Infinite);
            }
        }

        public void StartChecking()
        {
            if (!_forceCheck)
            {
                if (!App.Settings.AutoCheckUpdate || (!_firstCheck && PhoneApplicationService.Current.ApplicationIdleDetectionMode == IdleDetectionMode.Disabled))
                {
                    _stopped = true;
                    return;
                }

                if (!_firstCheck && PhoneApplicationService.Current.UserIdleDetectionMode == IdleDetectionMode.Disabled)
                {
                    return;
                }
            }

            lock (_lock)
            {
                if (Checking)
                {
                    return;
                }

                Checking = true;
            }

            if (_forceCheck || (DateTime.Now - _lastCheckTime) >= new TimeSpan(0, 15, 0))
            {
                _firstCheck = false;
                _lastCheckTime = DateTime.Now;

                if (App.WebSession.IsLoggedIn || App.Settings.WatchList.Count > 0)
                {
                    MitbbsMailbox unreadMailbox = new MitbbsMailbox();
                    unreadMailbox.MailboxLoaded +=
                        (s1, e1) =>
                        {
                            bool hasNewMail;
                            if (unreadMailbox.MailLinks.Count > 0)
                            {
                                hasNewMail = true;
                            }
                            else
                            {
                                hasNewMail = false;
                            }

                            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                                () =>
                                {
                                    HasNewMail = hasNewMail;
                                }
                                );

                            NewTopicContentChecker newContentChecker = new NewTopicContentChecker();
                            newContentChecker.CheckCompleted +=
                                (s, e) =>
                                {
                                    lock (_lock)
                                    {
                                        System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                                            () =>
                                            {
                                                CheckWatchListUpdate();
                                            }
                                            );

                                        _hasOldWatchedItem = newContentChecker.OldContentDetected;
                                        _forceCheck = false;
                                        Checking = false;
                                    }
                                };

                            newContentChecker.StartCheck(App.WebSession.CreateWebClient(), App.Settings.WatchList.ToList<MitbbsLink>());
                        };

                    unreadMailbox.LoadFromUrl(App.WebSession.CreateWebClient(), App.Settings.BuildUrl(MitbbsMailbox.NewMailsUrl));

                    return;
                }
            }

            lock (_lock)
            {
                _forceCheck = false;
                Checking = false;
            }
        }

        public void UpdateStatus()
        {
            CheckWatchListUpdate();
        }

        private void CheckWatchListUpdate()
        {
            bool hasUpdate = false;

            foreach (var entry in App.Settings.WatchList)
            {
                if (entry.HasNewContent)
                {
                    hasUpdate = true;
                    break;
                }
            }

            HasNewUpdate = hasUpdate;
        }

        public void OpenNotification(NavigationService nav)
        {
            if (_hasNewMail)
            {
                String pageUrl = String.Format("/Pages/MailboxPage.xaml?Url={0}&MailboxName={1}", 
                    Uri.EscapeDataString(App.Settings.BuildUrl(MitbbsMailbox.InboxUrl)), 
                    Uri.EscapeDataString("收件箱"));
                App.Track("Navigation", "EntryPoint", "NottificationCenterNewMail");
                nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                HasNewMail = false;
            }
            else if (_hasNewUpdate)
            {
                nav.Navigate(new Uri("/Pages/HistoryPage.xaml", UriKind.Relative));
                App.Track("Navigation", "EntryPoint", "NottificationCenterNewUpdate");
            }
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
