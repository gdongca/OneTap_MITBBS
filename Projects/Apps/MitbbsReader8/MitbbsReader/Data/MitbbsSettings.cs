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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Naboo.AppUtil;

namespace Naboo.MitbbsReader
{
    public class MitbbsUserInfo : INotifyPropertyChanged
    {
        public String DisplayName
        {
            get
            {
                if (IsDefault)
                {
                    return Username + " (缺省账号)";
                }
                else
                {
                    return Username;
                }
            }
        }

        private String _username;
        public String Username
        {
            get
            {
                return _username;
            }

            set
            {
                _username = value;

                NotifyPropertyChanged("Username");
                NotifyPropertyChanged("DisplayName");
            }
        }

        private String _password;
        public String Password
        {
            get
            {
                return _password;
            }

            set
            {
                _password = value;

                NotifyPropertyChanged("Password");
            }
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get
            {
                return _isDefault;
            }

            set
            {
                _isDefault = value;

                NotifyPropertyChanged("IsDefault");
                NotifyPropertyChanged("DisplayName");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class MitbbsSite
    {
        public String Name { get; set; }
        public String Description { get; set; }
        public String Url { get; set; }
    }

    public class MitbbsSettings
    {
#if DEBUG
        public static TimeSpan OldContentExpirePeriod = TimeSpan.FromDays(1);
#else
        public static TimeSpan OldContentExpirePeriod = TimeSpan.FromDays(30);
#endif

        public AppReminder Reminder = new AppReminder(App.License, App.TheAppInfo)
        {
            RatingMessage = "如果你喜欢这个软件，请到Marketplace里给它评级。谢谢！",
            RatingMessageTitle = "请给我评级"
        };

        public bool LogOn = false;
        public bool ShowSystemTray = false;
        public SupportedPageOrientation OrientationMode = SupportedPageOrientation.PortraitOrLandscape;
        public MitbbsCustomTheme Theme = new MitbbsCustomTheme();
        public bool HideFullQuote = true;
        public bool UseLocationForAds = true;
        public bool Preload = true;
        public bool MiniAppbar = false;
        public bool ShareInfo = true;
        public bool RestoreLastVisit = true;
        public bool HideTop = false;
        public bool DownloadUnderWifiOnly = false;
        public bool AutoStartDownload = true;
        public bool AutoCheckUpdate = true;
        public bool AppendSentFrom = true;

        public DateTime LastAdTapTime = DateTime.MinValue;

        public long AppMemoryLimit = 110000000;
        public long MaxUploadSize = 1048576;

        [XmlIgnore]
        public ObservableCollection<MitbbsSite> Sites = new ObservableCollection<MitbbsSite>()
        {
            new MitbbsSite()
            {
                Name = "MITBBS.COM",
                Description = "主站点",
                Url = "http://www.mitbbs.com"
            },
            new MitbbsSite()
            {
                Name = "MITBBS.ORG",
                Description = "中国大陆可以访问",
                Url = "http://www.mitbbs.org"
            },
            new MitbbsSite()
            {
                Name = "UNKNOWNSPACE.ORG",
                Description = "备用站点",
                Url = "http://www.unknownspace.org"
            }
        };

        [XmlIgnore]
        public MitbbsSite Site
        {
            get
            {
                if (SiteIndex < 0)
                {
                    SiteIndex = 0;
                }

                if (SiteIndex > Sites.Count - 1)
                {
                    SiteIndex = 0;
                }

                return Sites[SiteIndex];
            }
        }

#if CHINA
        //public int SiteIndex = 1;
        public int SiteIndex = 0;
#else
        public int SiteIndex = 0;
#endif

        public MitbbsOfflineContentManager OfflineContentManager = new MitbbsOfflineContentManager();
        public ObservableCollection<MitbbsUserInfo> Users = new ObservableCollection<MitbbsUserInfo>();
        
        public SessionHistory CurrentSessionHistory = new SessionHistory();

        public SessionHistory PreviousSessionHistory = new SessionHistory();

        public bool ResetSessionHistory = false;

        [XmlIgnore]
        public MitbbsUserInfo DefaultUser
        {
            get
            {
                if (!LogOn)
                {
                    return null;
                }

                foreach (var user in Users)
                {
                    if (user.IsDefault)
                    {
                        return user;
                    }
                }

                return null;
            }
        }

        private String _legacyUsername = null;
        private String _legacyPassword = null;

        public String Username
        {
            get
            {
                MitbbsUserInfo defaultUser = DefaultUser;
                if (defaultUser == null)
                {
                    return "";
                }
                else
                {
                    return defaultUser.Username;
                }
            }

            set
            {
                _legacyUsername = value;
            }
        }

        public String Password
        {
            get
            {
                MitbbsUserInfo defaultUser = DefaultUser;
                if (defaultUser == null)
                {
                    return "";
                }
                else
                {
                    return defaultUser.Password;
                }
            }

            set
            {
                _legacyPassword = value;
            }
        }

        //public List<MitbbsLink> BoardSearchLinks
        //{
        //    get
        //    {
        //        return MitbbsBoardSearch.Instance.AllBoardLinks;
        //    }

        //    set
        //    {
        //        MitbbsBoardSearch.Instance.AllBoardLinks = value;
        //    }
        //}

        public void SetDefaultUser(MitbbsUserInfo defaultUser)
        {
            foreach (var user in Users)
            {
                if (user == defaultUser)
                {
                    user.IsDefault = true;
                }
                else
                {
                    user.IsDefault = false;
                }
            }
        }

        private void RestoreLegacyUserInfo()
        {
            if (Users.Count <= 0 && !String.IsNullOrEmpty(_legacyUsername) && !String.IsNullOrEmpty(_legacyPassword))
            {
                Users.Add(
                    new MitbbsUserInfo()
                    {
                        Username = _legacyUsername,
                        Password = _legacyPassword,
                        IsDefault = true
                    }
                    );
            }
        }

        private bool _keepHistory = true;
        public bool KeepHistory
        {
            get
            {
                return _keepHistory;
            }
            set
            {
                _keepHistory = value;
                if (!_keepHistory)
                {
                    ClearHistory();
                }
            }
        }

        private bool? _downloadStarted;
        private BackgroundDownloader _bgDownloader = new BackgroundDownloader();

        [XmlIgnore]
        public BackgroundDownloader BGDownloader
        {
            get
            {
                return _bgDownloader;
            }
        }

        private NotificationCenter _notificationCenter = new NotificationCenter();
        [XmlIgnore]
        public NotificationCenter NotficationCenter
        {
            get
            {
                return _notificationCenter;
            }
        }

        public ObservableCollection<MitbbsLink> ReadingHistory = new ObservableCollection<MitbbsLink>();

        public ObservableCollection<MitbbsLink> BoardHistory = new ObservableCollection<MitbbsLink>();

        public ObservableCollection<MitbbsLink> ReadingBookmarks = new ObservableCollection<MitbbsLink>();
        public ObservableCollection<MitbbsLink> BoardBookmarks = new ObservableCollection<MitbbsLink>();
        public ObservableCollection<MitbbsLink> WatchList = new ObservableCollection<MitbbsLink>();
        
        public Guid BoardGroupPreloadOfflineID = MitbbsOfflineContentManager.CreateNewRootID();

        private GenericLink _selectedLink = null;
        [XmlIgnore]
        public GenericLink SelectedLink
        {
            get
            {
                return _selectedLink;
            }

            set
            {
                GenericLink originalLink = _selectedLink;

                if (value != _selectedLink)
                {
                    _selectedLink = value;

                    if (originalLink != null)
                    {
                        originalLink.UpdateLinkState();
                    }

                    if (_selectedLink != null)
                    {
                        _selectedLink.UpdateLinkState();
                    }
                }
            }
        }

        private DateTime _lastTapTime = DateTime.Now;

        [XmlIgnore]
        public Dictionary<String, MitbbsLink> QuickHistory = new Dictionary<String, MitbbsLink>();

        private int _maxReadingHistoryItems = 200;
        private int _maxBoardHistoryItems = 50;
        private Object _lock1 = new Object();

        public void SetIgnoreTap()
        {
            _lastTapTime = DateTime.Now;
        }

        [XmlIgnore]
        public bool IsTapIgnored
        {
            get
            {
                if (DateTime.Now >= _lastTapTime)
                {
                    TimeSpan timeElpased = DateTime.Now - _lastTapTime;
                    return timeElpased < new TimeSpan(0, 0, 1);
                }
                else
                {
                    return false;
                }
            }
        }

        public MitbbsSettings()
        {
            System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += NetworkAddressChanged;

            WatchList.ToList<MitbbsLink>();
        }

        private void NetworkAddressChanged(object sender, EventArgs e)
        {
            if (CanDownloadStarts())
            {
                RestoreDownload();
            }
            else
            {
                PauseDownload();
            }
        }

        private void AddQuickHistory(MitbbsLink link)
        {
            lock (_lock1)
            {
                if (KeepHistory)
                {
                    if (QuickHistory.ContainsKey(link.Url))
                    {
                        QuickHistory.Remove(link.Url);
                    }

                    QuickHistory.Add(link.Url, link);

                    link.UpdateLinkState();
                }
            }
        }

        public bool IsUrlInReadingHistory(String url)
        {
            if (url == null)
            {
                return false;
            }

            return QuickHistory.ContainsKey(url);
        }

        private void IndexReadingHistory()
        {
            lock (_lock1)
            {
                QuickHistory.Clear();
                foreach (MitbbsLink link in ReadingHistory)
                {
                    AddQuickHistory(link);
                }
            }
        }

        public void AddWatchItem(MitbbsLink link)
        {
            for (int i = 0; i < WatchList.Count; i++)
            {
                MitbbsLink oldLink = WatchList[i];
                if (oldLink.Url == link.Url)
                {
                    WatchList.Remove(oldLink);
                    break;
                }
            }

            link.AccessDate = DateTime.Now;
            WatchList.Insert(0, link);

            SaveToStorage();
        }

        public void RemoveWatchItem(String url)
        {
            for (int i = 0; i < WatchList.Count; i++)
            {
                MitbbsLink oldLink = WatchList[i];
                if (oldLink.Url == url)
                {
                    WatchList.Remove(oldLink);
                    break;
                }
            }

            SaveToStorage();
        }

        public void CheckWatchList(bool forceCheck = false)
        {
            _notificationCenter.StartCheckNow(forceCheck);
        }

        public bool IsUrlBeingWatched(String url)
        {
            foreach (var link in WatchList)
            {
                if (link.Url == url)
                {
                    return true;
                }
            }

            return false;
        }

        public MitbbsLink FindHistoryEntry(string url)
        {
            MitbbsLink link;

            foreach (MitbbsLink watchLink in WatchList)
            {
                if (watchLink.Url == url)
                {
                    return watchLink;
                }
            }

            if (QuickHistory.TryGetValue(url, out link))
            {
                return link;
            }

            return null;
        }

        public void AddReadingHistory(MitbbsLink newLink)
        {
            if (KeepHistory)
            {
                lock (_lock1)
                {
                    bool isReadingHistory = false;
                    newLink.AccessDate = DateTime.Now;
                    int maxItems;

                    ObservableCollection<MitbbsLink> history;
                    if ((newLink is MitbbsBoardLinkBase) ||
                        (newLink is MitbbsBoardGroupLink) ||
                        (newLink is MitbbsBoardEssenceLink) ||
                        (newLink is MitbbsClubLink) ||
                        (newLink is MitbbsClubGroupLink)
                        )
                    {
                        maxItems = _maxBoardHistoryItems;
                        history = BoardHistory;
                    }
                    else
                    {
                        maxItems = _maxReadingHistoryItems;
                        isReadingHistory = true;
                        history = ReadingHistory;
                    }

                    for (int i = 0; i < history.Count; i++)
                    {
                        MitbbsLink oldLink = history[i];
                        if (oldLink.Url == newLink.Url)
                        {
                            history.Remove(oldLink);

                            if (isReadingHistory)
                            {
                                QuickHistory.Remove(oldLink.Url);
                            }

                            newLink.LastVisitedPage = oldLink.LastVisitedPage;
                            newLink.LastVisitedScreenPos = oldLink.LastVisitedScreenPos;
                            newLink.LastVisitedUrl = oldLink.LastVisitedUrl;
                            newLink.LastVisitedPageContentCount = oldLink.LastVisitedPageContentCount;
                            break;
                        }
                    }

                    while (history.Count >= maxItems)
                    {
                        history.RemoveAt(history.Count - 1);
                    }

                    history.Insert(0, newLink);

                    if (isReadingHistory)
                    {
                        AddQuickHistory(newLink);
                    }
                }
            }
        }

        public void ClearHistory()
        {
            lock (_lock1)
            {
                ReadingHistory.Clear();
                BoardHistory.Clear();
                QuickHistory.Clear();
            }
        }

        public void RemoveReadingBookMark(String url)
        {
            try
            {
                for (int i = 0; i < ReadingBookmarks.Count; i++)
                {
                    MitbbsLink link = ReadingBookmarks[i];
                    if (link.Url == url)
                    {
                        ReadingBookmarks.RemoveAt(i);
                        return;
                    }
                }

                for (int i = 0; i < BoardBookmarks.Count; i++)
                {
                    MitbbsLink link = BoardBookmarks[i];
                    if (link.Url == url)
                    {
                        BoardBookmarks.RemoveAt(i);
                        return;
                    }
                }
            }
            finally
            {
                SaveToStorage();
            }
        }

        public void AddReadingBookMark(MitbbsLink newLink)
        {
            if (
                (newLink is MitbbsBoardGroupLink) ||
                (newLink is MitbbsBoardEssenceLink) ||
                (newLink is MitbbsClubGroupLink)
                )
            {
                return;
            }

            try
            {
                ObservableCollection<MitbbsLink> bookmarks;
                if ((newLink is MitbbsBoardLinkBase) ||
                    (newLink is MitbbsClubLink)
                    )
                {
                    bookmarks = BoardBookmarks;
                }
                else
                {
                    bookmarks = ReadingBookmarks;
                }


                for (int i = 0; i < bookmarks.Count; i++)
                {
                    MitbbsLink oldLink = bookmarks[i];
                    if (oldLink.Url == newLink.Url)
                    {
                        return;
                    }
                }

                newLink.AccessDate = DateTime.Now;
                bookmarks.Insert(0, newLink);

                //TODO: should we sort the list?

                if (bookmarks == BoardBookmarks && App.UserHome != null && App.UserHome.IsLoaded)
                {
                    App.UserHome.InsertMyBoard(newLink);
                }
            }
            finally
            {
                SaveToStorage();
            }
        }

        public bool IsUrlBookmarked(String url)
        {
            for (int i = 0; i < ReadingBookmarks.Count; i++)
            {
                MitbbsLink oldLink = ReadingBookmarks[i];
                if (oldLink.Url == url)
                {
                    return true;
                }
            }

            for (int i = 0; i < BoardBookmarks.Count; i++)
            {
                MitbbsLink oldLink = BoardBookmarks[i];
                if (oldLink.Url == url)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetHistoryEditable(bool editable)
        {
            MitbbsLink.CanEdit = editable;

            lock (_lock1)
            {
                UpdateLinkStates(ReadingHistory);
            }

            UpdateLinkStates(BoardHistory);
            UpdateLinkStates(ReadingBookmarks);
            UpdateLinkStates(BoardBookmarks);
            UpdateLinkStates(WatchList);
        }

        private void UpdateLinkStates(ObservableCollection<MitbbsLink> links)
        {
            foreach (var link in links)
            {
                link.UpdateLinkState();
            }
        }

        private void ApplyAfterLoadPageSettings(Object pageObj, Panel layoutRoot)
        {
            PhoneApplicationPage page = pageObj as PhoneApplicationPage;

            if (page != null)
            {
                if (layoutRoot != null)
                {
                    Theme.ApplyThemeToPage(page, layoutRoot);

                    Microsoft.Phone.Shell.SystemTray.IsVisible = ShowSystemTray;
                    page.SupportedOrientations = OrientationMode;
                }
            }
        }

        private void AdjustPageSize(Object pageObj, Panel layoutRoot)
        {
            PhoneApplicationPage page = pageObj as PhoneApplicationPage;

#if !NODO
            if (page != null)
            {
                if (layoutRoot != null)
                {
                    if (page.ApplicationBar != null && page.ApplicationBar.Mode == ApplicationBarMode.Minimized)
                    {
                        double dw = 10;

                        if (page.Orientation == PageOrientation.LandscapeLeft && page.ActualWidth > 480)
                        {
                            layoutRoot.MaxWidth = page.ActualWidth - page.ApplicationBar.MiniSize - dw;
                            layoutRoot.Width = page.ActualWidth - page.ApplicationBar.MiniSize - dw;
                            layoutRoot.HorizontalAlignment = HorizontalAlignment.Left;
                        }
                        else if (page.Orientation == PageOrientation.LandscapeRight && page.ActualWidth > 480)
                        {
                            layoutRoot.MaxWidth = page.ActualWidth - page.ApplicationBar.MiniSize - dw;
                            layoutRoot.Width = page.ActualWidth - page.ApplicationBar.MiniSize - dw;
                            layoutRoot.HorizontalAlignment = HorizontalAlignment.Right;
                        }
                        else
                        {
                            layoutRoot.Width = double.NaN;
                            layoutRoot.MaxWidth = double.PositiveInfinity;
                            if (layoutRoot.HorizontalAlignment != HorizontalAlignment.Stretch)
                            {
                                layoutRoot.HorizontalAlignment = HorizontalAlignment.Stretch;
                            }
                        }
                    }
                }
            }
#endif
        }

        public void ApplyPageSettings(PhoneApplicationPage page, Panel layoutRoot, bool applyAppBarSetting = true)
        {
            if (layoutRoot != null)
            {
                App.Settings.Theme.ApplyThemeToPage(page, layoutRoot);
            }

            page.OrientationChanged += 
                (s, e) => AdjustPageSize(s, layoutRoot);

            page.SizeChanged += 
                (s, e) => AdjustPageSize(s, layoutRoot);

            page.Loaded += 
                (s, e) => ApplyAfterLoadPageSettings(s, layoutRoot);

#if !NODO
            if (applyAppBarSetting && page.ApplicationBar != null)
            {
                if (App.Settings.MiniAppbar)
                {
                    page.ApplicationBar.Mode = ApplicationBarMode.Minimized;
                }
                else
                {
                    page.ApplicationBar.Mode = ApplicationBarMode.Default;
                }
            }
#endif
        }

        //private bool _historyLoaded = false;
        //private bool _historyMerged = false;
        //public void LoadReadingHistory()
        //{
        //    lock (_lock1)
        //    {
        //        if (_historyLoaded)
        //        {
        //            return;
        //        }

        //        _historyLoaded = true;
        //    }

        //    Microsoft.Phone.Reactive.IScheduler scheduler = Microsoft.Phone.Reactive.Scheduler.ThreadPool;
        //    scheduler.Schedule(new Action(
        //            () =>
        //            {
        //                ObservableCollection<MitbbsLink> oldReadingHistory;
        //                if (StorageHelper.TryLoadObject("reading_history.xml", out oldReadingHistory))
        //                {
        //                    System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
        //                        () =>
        //                        {
        //                            lock (_lock1)
        //                            {
        //                                foreach (var item in oldReadingHistory)
        //                                {
        //                                    ReadingHistory.Add(item);
        //                                }

        //                                IndexReadingHistory();
        //                                _historyMerged = true;
        //                            }
        //                        }
        //                        );
        //                }
        //            }
        //            ),
        //            TimeSpan.FromSeconds(1)
        //            );
        //}

        public void ResetHistory()
        {
            if (ResetSessionHistory && KeepHistory)
            {
                PreviousSessionHistory = CurrentSessionHistory;
            }

            CurrentSessionHistory = new SessionHistory();

            ResetSessionHistory = false;
        }

        public void SaveToStorage()
        {
            TextWriter writer = null;

            try
            {
                String fileName = "maindata.xml";
                String oldFile = fileName + ".old";
                String newFile = fileName + ".new";

                IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream file = isoStorage.OpenFile(newFile, FileMode.Create);
                writer = new StreamWriter(file);
                XmlSerializer xs = new XmlSerializer(typeof(MitbbsSettings));
                xs.Serialize(writer, this);
                writer.Close();

                if (isoStorage.FileExists(fileName))
                {
                    if (isoStorage.FileExists(oldFile))
                    {
                        isoStorage.DeleteFile(oldFile);
                    }

                    isoStorage.MoveFile(fileName, oldFile);
                }

                isoStorage.MoveFile(newFile, fileName);

                //if (_historyMerged)
                //{
                //    StorageHelper.SaveObject("reading_history.xml", ReadingHistory);
                //}
            }
            catch (Exception)
            {

            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }
        }

        public static MitbbsSettings LoadFromStorage()
        {
            MitbbsSettings mainData = null;
            TextReader reader = null;

            try
            {
                String fileName = "maindata.xml";

                IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication();

                if (!isoStorage.FileExists(fileName))
                {
                    fileName = fileName + ".old";
                }

                IsolatedStorageFileStream file = isoStorage.OpenFile(fileName, FileMode.OpenOrCreate);
                reader = new StreamReader(file);
                XmlSerializer xs = new XmlSerializer(typeof(MitbbsSettings));

                mainData = (MitbbsSettings)xs.Deserialize(reader);
                reader.Close();

                mainData.Reminder.License = App.License;
                mainData.Reminder.RatingMessage = "如果你喜欢这个软件，请到Marketplace里给它评级。谢谢！";
                mainData.Reminder.RatingMessageTitle = "请给我评级";

                mainData.RestoreLegacyUserInfo();

#if !NODO
                long memoryLimit = Microsoft.Phone.Info.DeviceStatus.ApplicationMemoryUsageLimit;
                if (memoryLimit > 0)
                {
                    mainData.AppMemoryLimit = memoryLimit;
                }
#endif

                //mainData.LoadReadingHistory();
                mainData.IndexReadingHistory();

                mainData.OfflineContentManager.CompactContentList();
            }
            catch (Exception)
            {

            }

            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
            }

            return mainData;
        }

        public bool CanDownloadStarts(bool showMessage = false, bool addNew = false)
        {
            String msgTitle = "无法开始下载";

            if (addNew)
            {
                msgTitle = "此内容已加入下载队列";
            }

            if (!AppInfo.IsNetworkConnected())
            {
                if (showMessage)
                {
                    MessageBox.Show("网络连接不存在，无法开始下载。", msgTitle, MessageBoxButton.OK);
                }

                return false;
            }

            if (DownloadUnderWifiOnly && !AppInfo.IsWifiConnected())
            {
                if (showMessage)
                {
                    MessageBox.Show("你设置了只在WiFi网络连接时下载。现在没有WiFi网络连接，无法开始下载。", msgTitle, MessageBoxButton.OK);
                }

                return false;
            }

            if (_downloadStarted.HasValue)
            {
                if (!_downloadStarted.Value && showMessage)
                {
                    MessageBox.Show("你已经手动停止了自动下载。请在'离线内容'页面恢复开始下载", msgTitle, MessageBoxButton.OK);
                }

                return _downloadStarted.Value;
            }

            if (!AutoStartDownload)
            {
                if (showMessage)
                {
                    MessageBox.Show("你没有设置自动下载。请在'离线内容'页面手动开始下载", msgTitle, MessageBoxButton.OK);
                }
            }

            return AutoStartDownload;
        }

        public void RestoreDownload(bool startNow = true)
        {
            if (CanDownloadStarts())
            {
                _bgDownloader.StartTimer(startNow);
            }
        }

        public void PauseDownload()
        {
            _bgDownloader.StopTimer();
        }

        public void StartDownload()
        {
            _downloadStarted = true;
            RestoreDownload();

            if (_downloadStarted.Value == AutoStartDownload)
            {
                _downloadStarted = null;
            }
        }

        public void StopDownload()
        {
            _downloadStarted = false;
            PauseDownload();

            if (_downloadStarted.Value == AutoStartDownload)
            {
                _downloadStarted = null;
            }
        }

        public String BuildUrl(String url)
        {
            url = url.Trim();
            if (!url.ToLower().StartsWith("http:") && !url.ToLower().StartsWith("https:"))
            {
                if (!url.StartsWith("/"))
                {
                    url = "/" + url;
                }

                url = Site.Url + url;
            }

            return url;
        }
    }
}
