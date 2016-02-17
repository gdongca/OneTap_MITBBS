using System;
using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.ComponentModel;
using Naboo.AppUtil;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class OfflineContentManager
    {
        protected object _lock = new object();
        protected String _offlineCacheFolder;

        public OfflineContentManager(String offlineCacheFolder)
        {
            _offlineCacheFolder = offlineCacheFolder;
        }

        public bool SaveOfflineContent<T>(Guid rootID, String key, T content)
        {
            TextWriter writer = null;
            String filename = GenerateOfflineFileName(rootID, key);

            try
            {
                IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication();

                lock (_lock)
                {
                    if (!isoStorage.DirectoryExists(Path.GetDirectoryName(filename)))
                    {
                        isoStorage.CreateDirectory(Path.GetDirectoryName(filename));
                    }

                    IsolatedStorageFileStream file = isoStorage.OpenFile(filename, FileMode.Create);
                    writer = new StreamWriter(file);
                    XmlSerializer xs = new XmlSerializer(typeof(T));
                    xs.Serialize(writer, content);
                    writer.Close();
                }

                return true;
            }
            catch(Exception)
            {

            }
            finally
            {
                if (writer != null)
                {
                    writer.Dispose();
                }
            }

            return false;
        }

        public bool TryLoadOfflineContent<T>(Guid rootID, String key, out T content)
        {
            return TryLoadOfflineContent<T>(rootID, key, TimeSpan.MaxValue, out content);
        }

        public bool TryLoadOfflineContent<T>(Guid rootID, String key, TimeSpan maxAge, out T content)
        {
            content = default(T);
            TextReader reader = null;
            String filename = GenerateOfflineFileName(rootID, key);

            try
            {
                IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication();

                lock (_lock)
                {
                    if (isoStorage.FileExists(filename))
                    {
                        DateTime lastWriteTime = isoStorage.GetLastWriteTime(filename).LocalDateTime;
                        if ((DateTime.Now - lastWriteTime) > maxAge)
                        {
                            return false;
                        }

                        IsolatedStorageFileStream file = isoStorage.OpenFile(filename, FileMode.OpenOrCreate);
                        reader = new StreamReader(file);
                        XmlSerializer xs = new XmlSerializer(typeof(T));

                        content = (T)xs.Deserialize(reader);
                        reader.Close();

                        return true;
                    }
                }
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

            return false;
        }

        public bool SaveOfflineContent(Guid rootID, String key, Stream stream)
        {
            String filename = GenerateOfflineFileName(rootID, key);

            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        if (!isoStorage.DirectoryExists(Path.GetDirectoryName(filename)))
                        {
                            isoStorage.CreateDirectory(Path.GetDirectoryName(filename));
                        }

                        using (var outputStream = isoStorage.OpenFile(filename, FileMode.Create))
                        {
                            stream.CopyTo(outputStream);
                        }
                    }
                }

                return true;
            }
            catch (Exception)
            {
            }

            return false;
        }

        public bool TryLoadOfflineContent(Guid rootID, String key, out Stream outputStream)
        {
            String filename = GenerateOfflineFileName(rootID, key);

            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        if (isoStorage.FileExists(filename))
                        {
                            outputStream = isoStorage.OpenFile(filename, FileMode.OpenOrCreate);
                            return true;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            outputStream = null;
            return false;
        }

        public bool OfflineContentExists(Guid rootID, String key)
        {
            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    String filename = GenerateOfflineFileName(rootID, key);
                    return isoStorage.FileExists(filename);
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        public bool OfflineContentExists(Guid rootID)
        {
            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    return isoStorage.DirectoryExists(GenerateOfflineRootFolderName(rootID));
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        public long GetOfflineContentSize(Guid rootID)
        {
            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        String dirName = GenerateOfflineRootFolderName(rootID);

                        if (isoStorage.DirectoryExists(dirName))
                        {
                            return GetDirectorySize(isoStorage, dirName);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return 0;
        }

        public void CleanupOfflineContent(Guid rootID)
        {
            String folderName = GenerateOfflineRootFolderName(rootID);

            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        if (isoStorage.DirectoryExists(folderName))
                        {
                            DeleteDirectory(isoStorage, folderName);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public void CleanupOfflineContent(Guid rootID, String key)
        {
            String filename = GenerateOfflineFileName(rootID, key);

            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        if (isoStorage.FileExists(filename))
                        {
                            isoStorage.DeleteFile(filename);
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public void CleanupOfflineContent(Guid rootID, TimeSpan maxAge)
        {
            String folderName = GenerateOfflineRootFolderName(rootID);
            DateTime cutoffTime = DateTime.Now - maxAge;

            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        if (isoStorage.DirectoryExists(folderName))
                        {
                            String pattern = folderName + @"\*";
                            String[] files = isoStorage.GetFileNames(pattern);
                            foreach (String fName in files)
                            {
                                String fullPath = Path.Combine(folderName, fName);
                                DateTime lastWriteTime = isoStorage.GetLastWriteTime(fullPath).LocalDateTime;

                                if (lastWriteTime < cutoffTime)
                                {
                                    isoStorage.DeleteFile(fullPath);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public virtual void CleanupAllOfflineContent(Guid[] excludedRootIDs = null)
        {
            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        if (isoStorage.DirectoryExists(_offlineCacheFolder))
                        {
                            if (excludedRootIDs == null || excludedRootIDs.Length <= 0)
                            {
                                DeleteDirectory(isoStorage, _offlineCacheFolder);
                            }
                            else
                            {
                                String pattern = _offlineCacheFolder + @"\*";
                                String[] dirs = isoStorage.GetDirectoryNames(pattern);
                                foreach (String dName in dirs)
                                {
                                    bool excluded = false;
                                    foreach (Guid rootID in excludedRootIDs)
                                    {
                                        if (rootID.ToString().ToLower() == dName)
                                        {
                                            excluded = true;
                                            break;
                                        }
                                    }

                                    if (!excluded)
                                    {
                                        DeleteDirectory(isoStorage, Path.Combine(_offlineCacheFolder, dName));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }

        public static Guid CreateNewRootID()
        {
            return Guid.NewGuid();
        }

        public long GetTotalSpaceUsed()
        {
            try
            {
                using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    lock (_lock)
                    {
                        if (isoStorage.DirectoryExists(_offlineCacheFolder))
                        {
                            return GetDirectorySize(isoStorage, _offlineCacheFolder);
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }

            return 0;
        }

        public long GetAvailableFreeSpace()
        {
            using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return isoStorage.AvailableFreeSpace;
            }
        }

        protected String GenerateOfflineRootFolderName(Guid rootID)
        {
            return _offlineCacheFolder + @"\" + rootID.ToString();
        }

        public String GenerateOfflineFileName(Guid rootID, String key)
        {
            int pathLimit = 180;
            String dir = GenerateOfflineRootFolderName(rootID);
            String filename = EncodeKeyIntoFilename(key);

            if (filename.Length + dir.Length > pathLimit)
            {
                filename = filename.Substring((filename.Length + dir.Length) - pathLimit);
            }

            return Path.Combine(dir,filename);
        }

        protected static String EncodeKeyIntoFilename(String key)
        {
            StringBuilder sb = new StringBuilder(key.Length);
            foreach (char c in key)
            {
                char c2 = c;
                if (c2 == '/' || c2 == ':' || c2 == '?' || c2 == '=' || c2 == '&')
                {
                    c2 = '_';
                }

                sb.Append(c2);
            }

            return sb.ToString();
        }

        protected static void DeleteDirectory(IsolatedStorageFile fs, String dirName)
        {
            String pattern = dirName + @"\*";
            String[] files = fs.GetFileNames(pattern);
            foreach (String fName in files)
            {
                fs.DeleteFile(Path.Combine(dirName, fName));
            }
            String[] dirs = fs.GetDirectoryNames(pattern);
            foreach (String dName in dirs)
            {
                DeleteDirectory(fs, Path.Combine(dirName, dName));
            }
            fs.DeleteDirectory(dirName);
        }

        protected static long GetFileSize(IsolatedStorageFile store, string filename)
        {
            try
            {
                using (FileStream fs = store.OpenFile(filename, FileMode.Open))
                {
                    return fs.Length;
                }
            }
            catch (Exception e)
            {
            }

            return 0;
        }

        protected static long GetDirectorySize(IsolatedStorageFile fs, String dirName)
        {
            long size = 0;

            try
            {
                String pattern = dirName + @"\*";
                String[] files = fs.GetFileNames(pattern);
                foreach (String fName in files)
                {
                    size += GetFileSize(fs, Path.Combine(dirName, fName));
                }
                String[] dirs = fs.GetDirectoryNames(pattern);
                foreach (String dName in dirs)
                {
                    size += GetDirectorySize(fs, Path.Combine(dirName, dName));
                }
            }
            catch (Exception e)
            {
            }

            return size;
        }
    }

    public enum MitbbsOfflineContentType
    {
        Unknown,
        Home,
        UserHome,
        Board,
        ClubBoard,
        Topic,
        ClubTopic,
        BoardEssense,
        TopicEssense,
    }

    public class MitbbsOfflineContentIndex : INotifyPropertyChanged
    {
        public Guid RootID { get; set; }
        public String Key { get; set; }
        public DateTime DownloadDate { get; set; }
        public DateTime ExpireDate { get; set; }
        public MitbbsOfflineContentType ContentType { get; set; }
        public int EstimatedItemCount { get; set; }
        
        private String _name;
        public String Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
                NotifyPropertyChanged("Name");
                NotifyPropertyChanged("DisplayName");
            }
        }
        
        private long _size;
        public long Size
        {
            get
            {
                return _size;
            }

            set
            {
                _size = value;
                NotifyPropertyChanged("Size");
                NotifyPropertyChanged("DisplayName");
            }
        }

        private bool _isDownloaded;
        public bool IsDownloaded
        {
            get
            {
                return _isDownloaded;
            }

            set
            {
                _isDownloaded = value;
                NotifyPropertyChanged("IsDownloaded");
                NotifyPropertyChanged("DisplayName");
            }
        }

        private bool _failed;
        public bool Failed
        {
            get
            {
                return _failed;
            }

            set
            {
                _failed = value;
                NotifyPropertyChanged("Failed");
                NotifyPropertyChanged("DisplayName");
            }
        }

        private int _progress;
        public int DownloadProgress
        {
            get
            {
                return _progress;
            }

            set
            {
                _progress = value;
                NotifyPropertyChanged("DownloadProgress");
                NotifyPropertyChanged("DisplayName");
            }
        }

        [XmlIgnore]
        public String DownloadDateText
        {
            get
            {
                return DownloadDate.ToString("yyyy年MM月dd日 HH:mm:ss");
            }
        }

        [XmlIgnore]
        public bool IsExpired
        {
            get
            {
                return DateTime.Now >= ExpireDate;
            }
        }
        public MitbbsLink Link { get; set; }

        [XmlIgnore]
        public bool IsEditable
        {
            get
            {
                return MitbbsOfflineContentManager.IsEditable;
            }
        }

        [XmlIgnore]
        public String DisplayName
        {
            get
            {
                if (IsDownloaded)
                {
                    return Name + " (" + MitbbsOfflineContentManager.GetSizeString(Size) + ")";
                }
                else if (Failed)
                {
                    return Name + " (下载失败)";
                }
                else
                {
                    return Name + " (" + DownloadProgress + "%)";
                }
            }
        }

        public void UpdateChange()
        {
            NotifyPropertyChanged("IsEditable");
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

    public class MitbbsOfflineContentManager : OfflineContentManager
    {
        protected object _cLock = new object();

        public ObservableCollection<MitbbsOfflineContentIndex> AllContents { get; protected set; }

        // These three content lists are deprecated
        // all new contents go into AllContents
        //
        public ObservableCollection<MitbbsOfflineContentIndex> HomeContents { get; protected set; }
        public ObservableCollection<MitbbsOfflineContentIndex> BoardContents { get; protected set; }
        public ObservableCollection<MitbbsOfflineContentIndex> TopicContents { get; protected set; }

        public ObservableCollection<MitbbsOfflineContentIndex> DownloadQueue { get; protected set; }

        //public ObservableCollection<MitbbsOfflineContentIndex> MailboxContents = { get; protected set; }
        //public ObservableCollection<MitbbsOfflineContentIndex> MailContents = new { get; protected set; }

        public static bool IsEditable = false;
        
        public MitbbsOfflineContentManager() : base(@"MitbbsOfflineCache")
        {
            AllContents = new ObservableCollection<MitbbsOfflineContentIndex>();
            HomeContents = new ObservableCollection<MitbbsOfflineContentIndex>();
            BoardContents = new ObservableCollection<MitbbsOfflineContentIndex>();
            TopicContents = new ObservableCollection<MitbbsOfflineContentIndex>();
            DownloadQueue = new ObservableCollection<MitbbsOfflineContentIndex>();
        }

        [XmlIgnore]
        public long TotalSize
        {
            get
            {
                long size = 0;

                foreach (var index in AllContents)
                {
                    size += index.Size;
                }

                //foreach (var index in HomeContents)
                //{
                //    size += index.Size;
                //}

                //foreach (var index in BoardContents)
                //{
                //    size += index.Size;
                //}

                //foreach (var index in TopicContents)
                //{
                //    size += index.Size;
                //}

                return size;
            }
        }

        [XmlIgnore]
        public int TotalCount
        {
            get
            {
                return AllContents.Count /* + HomeContents.Count + BoardContents.Count + TopicContents.Count + DownloadQueue.Count */;
            }
        }

        public void CompactContentList()
        {
            try
            {
                foreach (var index in HomeContents)
                {
                    AllContents.Add(index);
                }

                foreach (var index in BoardContents)
                {
                    AllContents.Add(index);
                }

                foreach (var index in TopicContents)
                {
                    AllContents.Add(index);
                }
            }
            finally
            {
                HomeContents.Clear();
                BoardContents.Clear();
                TopicContents.Clear();
            }
        }

        public static String GetSizeString(long size)
        {
            long sizeInKB = size / 1024;
            long sizeInMB = sizeInKB / 1024;

            if (sizeInMB > 0)
            {
                return sizeInMB.ToString("N0") + "MB";
            }

            if (sizeInKB > 0)
            {
                return sizeInKB.ToString("N0") + "KB";
            }

            return size.ToString("N0") + "B";
        }

        public bool AddContentToQueue(
            String url,
            String name,
            MitbbsOfflineContentType contentType
            )
        {
            lock (_cLock)
            {
                foreach (var content in DownloadQueue)
                {
                    if (content.Key == url && content.ContentType == contentType)
                    {
                        return true;
                    }
                }

                MitbbsOfflineContentIndex contentIndex = new MitbbsOfflineContentIndex()
                {
                    RootID = CreateNewRootID(),
                    Key = url,
                    Name = name,
                    ContentType = contentType,
                    DownloadDate = DateTime.Now,
                    ExpireDate = DateTime.MaxValue,
                    IsDownloaded = false,
                    Size = 0
                };

                contentIndex.Link = CreateMitbbsLink(contentIndex);
                contentIndex.EstimatedItemCount = EstimateItemCount(contentIndex);

                DownloadQueue.Add(contentIndex);

                App.Track("Statistics", "NewDownload", null);

                return true;
            }
        }

        public void RedownloadContent(
            MitbbsOfflineContentIndex contentIndex
            )
        {
            lock (_cLock)
            {
                CleanupOfflineContent(contentIndex);

                foreach (var content in DownloadQueue)
                {
                    if (content.Key == contentIndex.Key && content.ContentType == contentIndex.ContentType)
                    {
                        return;
                    }
                }

                //contentIndex.Name = contentIndex.Link.Name;
                contentIndex.DownloadProgress = 0;
                contentIndex.DownloadDate = DateTime.Now;
                contentIndex.IsDownloaded = false;
                contentIndex.Failed = false;
                contentIndex.Size = 0;

                contentIndex.Link = CreateMitbbsLink(contentIndex);

                DownloadQueue.Add(contentIndex);

                App.Track("Statistics", "NewDownload", null);
            }
        }

        public void MoveQueuedContentToIndex(MitbbsOfflineContentIndex contentIndex)
        {
            lock (_cLock)
            {
                contentIndex.DownloadDate = DateTime.Now;
                MitbbsLink link = contentIndex.Link;

                contentIndex.Size = GetOfflineContentSize(contentIndex.RootID);
                //contentIndex.Name = contentIndex.Name + " (" + GetSizeString(contentIndex.Size) + ")";

                AllContents.Remove(contentIndex);
                AllContents.Insert(0, contentIndex);

                //if (link is MitbbsHomeLink || link is MitbbsUserHomeLink)
                //{
                //    HomeContents.Remove(contentIndex);
                //    HomeContents.Insert(0, contentIndex);
                //}
                //else if (link is MitbbsBoardLinkBase || link is MitbbsClubLink || link is MitbbsBoardEssenceLink)
                //{
                //    BoardContents.Remove(contentIndex);
                //    BoardContents.Insert(0, contentIndex);
                //}
                //else if (link is MitbbsTopicLinkBase || link is MitbbsTopicEssenceLink)
                //{
                //    TopicContents.Remove(contentIndex);
                //    TopicContents.Insert(0, contentIndex);
                //}

                DownloadQueue.Remove(contentIndex);

                App.Track("Statistics", "DownloadCompleted", null);
            }
        }

        public void CleanupOfflineContent(MitbbsOfflineContentIndex index)
        {
            lock (_cLock)
            {
                AllContents.Remove(index);

                //HomeContents.Remove(index);
                //BoardContents.Remove(index);
                //TopicContents.Remove(index);

                DownloadQueue.Remove(index);

                CleanupOfflineContent(index.RootID);
            }
        }

        public void SetEditable(bool editable)
        {
            IsEditable = editable;

            UpdateChange(AllContents);

            //UpdateChange(HomeContents);
            //UpdateChange(BoardContents);
            //UpdateChange(TopicContents);

            UpdateChange(DownloadQueue);
        }

        private void UpdateChange(ObservableCollection<MitbbsOfflineContentIndex> contents)
        {
            foreach (var index in contents)
            {
                index.UpdateChange();
            }
        }

        public void CleanupAllContentIndex()
        {
            lock (_cLock)
            {
                AllContents.Clear();

                //HomeContents.Clear();
                //BoardContents.Clear();
                //TopicContents.Clear();

                DownloadQueue.Clear();
            }
        }

        public void CleanupUnindexedFiles()
        {
            lock (_cLock)
            {
                try
                {
                    using (IsolatedStorageFile isoStorage = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        CleanupEmptyIndices(isoStorage, HomeContents);
                        CleanupEmptyIndices(isoStorage, BoardContents);
                        CleanupEmptyIndices(isoStorage, TopicContents);

                        String pattern = _offlineCacheFolder + @"\*";
                        String[] dirs = isoStorage.GetDirectoryNames(pattern);

                        foreach (String dName in dirs)
                        {
                            bool foundIndex = false;
                            Guid rootID = new Guid(dName);

                            foreach (var index in AllContents)
                            {
                                if (rootID == index.RootID)
                                {
                                    foundIndex = true;
                                    break;
                                }
                            }

                            //foreach (var index in HomeContents)
                            //{
                            //    if (rootID == index.RootID)
                            //    {
                            //        foundIndex = true;
                            //        break;
                            //    }
                            //}

                            //if (!foundIndex)
                            //{
                            //    foreach (var index in BoardContents)
                            //    {
                            //        if (rootID == index.RootID)
                            //        {
                            //            foundIndex = true;
                            //            break;
                            //        }
                            //    }
                            //}

                            //if (!foundIndex)
                            //{
                            //    foreach (var index in TopicContents)
                            //    {
                            //        if (rootID == index.RootID)
                            //        {
                            //            foundIndex = true;
                            //            break;
                            //        }
                            //    }
                            //}

                            if (!foundIndex)
                            {
                                foreach (var index in DownloadQueue)
                                {
                                    if (rootID == index.RootID)
                                    {
                                        foundIndex = true;
                                        break;
                                    }
                                }
                            }

                            if (!foundIndex)
                            {
                                if (rootID == App.Settings.BoardGroupPreloadOfflineID)
                                {
                                    foundIndex = true;
                                }
                            }

                            if (!foundIndex)
                            {
                                DeleteDirectory(isoStorage, Path.Combine(_offlineCacheFolder, dName));
                            }
                        }


                    }
                }
                catch (Exception e)
                {
                }
            }
        }

        private void CleanupEmptyIndices(IsolatedStorageFile isoStorage, ObservableCollection<MitbbsOfflineContentIndex> indices)
        {
            lock (_cLock)
            {
                List<MitbbsOfflineContentIndex> toDelete = new List<MitbbsOfflineContentIndex>();

                foreach (var index in indices)
                {
                    String rootDir = GenerateOfflineRootFolderName(index.RootID);
                    if (!isoStorage.DirectoryExists(rootDir))
                    {
                        toDelete.Add(index);
                    }
                }

                foreach (var index in toDelete)
                {
                    indices.Remove(index);
                }
            }
        }

        private bool FindAndRemove(
            String url,
            String name,
            MitbbsOfflineContentType contentType,
            ObservableCollection<MitbbsOfflineContentIndex> contentList
            )
        {
            lock (_cLock)
            {
                MitbbsOfflineContentIndex found = null;
                foreach (var content in contentList)
                {
                    if (content.Key == url && content.ContentType == contentType)
                    {
                        found = content;
                        break;
                    }
                }

                if (found != null)
                {
                    contentList.Remove(found);
                    return true;
                }

                return false;
            }
        }

        public static MitbbsLink CreateMitbbsLink(MitbbsOfflineContentIndex contentIndex)
        {
            switch (contentIndex.ContentType)
            {
                case MitbbsOfflineContentType.Home:
                    return new MitbbsHomeLink()
                    {
                        Url = contentIndex.Key,
                        Name = "未名空间主页",
                        OfflineID = contentIndex.RootID.ToString()
                    };
                case MitbbsOfflineContentType.UserHome:
                    return new MitbbsUserHomeLink()
                    {
                        Url = contentIndex.Key,
                        Name = "用户" + App.WebSession.Username + "家页",
                        OfflineID = contentIndex.RootID.ToString()
                    };
                case MitbbsOfflineContentType.Board:
                    return new MitbbsBoardLink()
                    {
                        Url = contentIndex.Key,
                        Name = contentIndex.Name,
                        BoardName = contentIndex.Name,
                        OfflineID = contentIndex.RootID.ToString()
                    };
                case MitbbsOfflineContentType.ClubBoard:
                    return new MitbbsClubLink()
                    {
                        Url = contentIndex.Key,
                        Name = contentIndex.Name,
                        OfflineID = contentIndex.RootID.ToString()
                    };
                case MitbbsOfflineContentType.Topic:
                    return new MitbbsTopicLink()
                    {
                        Url = contentIndex.Key,
                        Name = contentIndex.Name,
                        OfflineID = contentIndex.RootID.ToString()
                    };
                case MitbbsOfflineContentType.ClubTopic:
                    return new MitbbsClubTopicLink()
                    {
                        Url = contentIndex.Key,
                        Name = contentIndex.Name,
                        OfflineID = contentIndex.RootID.ToString()
                    };
                case MitbbsOfflineContentType.TopicEssense:
                    return new MitbbsTopicEssenceLink()
                    {
                        Url = contentIndex.Key,
                        Name = contentIndex.Name,
                        OfflineID = contentIndex.RootID.ToString()
                    };
                case MitbbsOfflineContentType.BoardEssense:
                    return new MitbbsBoardEssenceLink()
                    {
                        Url = contentIndex.Key,
                        Name = contentIndex.Name,
                        OfflineID = contentIndex.RootID.ToString()
                    };
                default:
                    return null;
            }
        }

        public static int EstimateItemCount(MitbbsOfflineContentIndex contentIndex)
        {
            switch (contentIndex.ContentType)
            {
                case MitbbsOfflineContentType.Home:
                    return 60;
                case MitbbsOfflineContentType.Board:
                case MitbbsOfflineContentType.ClubBoard:
                    return 100;
                default:
                    return 1;
            }
        }
    }
}
