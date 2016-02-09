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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
using Microsoft.Phone.Reactive;
using Naboo.AppUtil;

namespace Naboo.MitbbsReader
{
    public class MitbbsBoardSearch : INotifyPropertyChanged
    {
        static private int _maxSearchResultCount = 100;

        public Guid OfflineID { get; set; }
        public List<MitbbsLink> AllBoardLinks { get; set; }
        public ObservableCollection<MitbbsLink> ResultBoardLinks { get; private set; }

        private volatile String _processedKeyword = "";
        private volatile String _searchKeyword = "";
        public String SearchKeyword
        {
            get
            {
                return _searchKeyword;
            }

            set
            {
                if (_searchKeyword != value)
                {
                    lock (_lock)
                    {
                        _searchKeyword = value;

                        if (!Updating)
                        {
                            StartUpdateResults();
                        }
                    }

                    NotifyPropertyChanged("SearchKeyword");
                    NotifyPropertyChanged("ShowUpdateProgress");
                }
            }
        }

        private object _lock = new object();
        private object _lock2 = new object();
        private object _lock3 = new object();
        private volatile bool _updating = false;
        private List<MitbbsLink> _allBoardLinks2;

        private static object _gLock = new object();
        private static MitbbsBoardSearch _instance;
        public static MitbbsBoardSearch Instance
        {
            get
            {
                lock (_gLock)
                {
                    if (_instance == null)
                    {
                        _instance = new MitbbsBoardSearch(App.Settings.BoardGroupPreloadOfflineID);
                    }

                    return _instance;
                }
            }
        }

        public bool Updating
        {
            get
            {
                return _updating;
            }

            set
            {
                _updating = value;
                NotifyPropertyChanged("Updating");
                NotifyPropertyChanged("ShowUpdateProgress");
            }
        }

        public bool ShowUpdateProgress
        {
            get
            {
                return (Updating || AllBoardLinks.Count <= 0) && !String.IsNullOrEmpty(SearchKeyword);
            }
        }

        public bool HasResults
        {
            get
            {
                return ResultBoardLinks.Count > 0;
            }
        }

        public MitbbsBoardSearch(Guid offlineID)
        {
            OfflineID = offlineID;
            AllBoardLinks = new List<MitbbsLink>();
            ResultBoardLinks = new ObservableCollection<MitbbsLink>();
        }

        public void PopulateBoardList()
        {
            lock (_lock2)
            {
                _allBoardLinks2 = new List<MitbbsLink>();

                foreach (MitbbsLink link in GetAllBoardLinksRecursively(App.Settings.BuildUrl(MitbbsBoardGroup.MobileBoardGroupHome)))
                {
                    _allBoardLinks2.Add(link);
                }

                foreach (MitbbsLink link in GetAllClubinks(App.Settings.BuildUrl(MitbbsClubHome.ClubHomeUrl)))
                {
                    link.Name = "俱乐部-" + link.Name;
                    _allBoardLinks2.Add(link);
                }

                if (_allBoardLinks2.Count > 0)
                {
                    AllBoardLinks = _allBoardLinks2;
                }
            }
        }

        public IEnumerable<MitbbsLink> SearchForBoard(String name)
        {
            if (String.IsNullOrEmpty(name))
            {
                yield break;
            }

            name = name.ToLower().Trim();

            foreach (MitbbsLink link in AllBoardLinks)
            {
                if (link.Name.ToLower().Contains(name))
                {
                    yield return link;
                }
            }
        }

        public MitbbsLink GetRandomBoard(bool noClub = false)
        {
            List<MitbbsLink> allBoardLinks = AllBoardLinks;

            if (allBoardLinks.Count <= 0)
            {
                return null;
            }

            Random random = new Random();

            int maxIndex = allBoardLinks.Count;
            for (int i = 0; i< 1000; i++)
            {
                int index = random.Next(0, allBoardLinks.Count - 1);
                MitbbsLink link = allBoardLinks[index];

                if (!noClub || !(link is MitbbsClubLink))
                {
                    return link;
                }
                else
                {
                    maxIndex = index - 1;
                }
            }

            return null;
        }

        public void StartUpdateResults()
        {
            String srcKey = _searchKeyword;
            IScheduler scheduler = Scheduler.ThreadPool;
            scheduler.Schedule(new Action(
                () =>
                {
                    if (srcKey == _searchKeyword)
                    {
                        UpdateResultsThread();
                    }
                }
                ),
                (AllBoardLinks.Count <= 0) ? TimeSpan.FromSeconds(0) : TimeSpan.FromSeconds(1)
                );
        }

        public void StartPopulateBoardList()
        {
            String srcKey = _searchKeyword;
            IScheduler scheduler = Scheduler.ThreadPool;
            scheduler.Schedule(new Action(
                () =>
                {
                    UpdateResultsThread(true);
                }
                ),
                TimeSpan.FromSeconds(0)
                );
        }

        public void StartRestoreBoardList()
        {
            String srcKey = _searchKeyword;
            IScheduler scheduler = Scheduler.ThreadPool;
            scheduler.Schedule(new Action(
                () =>
                {
                    RestoreFromStorage();
                }
                ),
                TimeSpan.FromSeconds(0)
                );
        }

        private void UpdateResultsThread(bool forcePopulate = false)
        {
            lock (_lock3)
            {
                lock (_lock)
                {
                    if (!forcePopulate && _processedKeyword == _searchKeyword && AllBoardLinks.Count > 0)
                    {
                        return;
                    }
                }

                try
                {
                    Updating = true;

                    if (forcePopulate || AllBoardLinks.Count <= 0)
                    {
                        PopulateBoardList();
                    }

                    List<MitbbsLink> results = new List<MitbbsLink>();
                    while (true)
                    {
                        _processedKeyword = _searchKeyword;

                        results.Clear();
                        foreach (MitbbsLink link in SearchForBoard(_processedKeyword))
                        {
                            results.Add(link);

                            if (results.Count >= _maxSearchResultCount)
                            {
                                break;
                            }
                        }

                        lock (_lock)
                        {
                            if (_searchKeyword == _processedKeyword)
                            {
                                String srcKey = _searchKeyword;
                                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                                    () =>
                                    {
                                        if (srcKey == _searchKeyword)
                                        {
                                            ResultBoardLinks.Clear();
                                            foreach (MitbbsLink link in results)
                                            {
                                                ResultBoardLinks.Add(link);
                                            };

                                            NotifyPropertyChanged("HasResults");
                                        }
                                    }
                                    );

                                break;
                            }
                        }
                    }
                }
                finally
                {
                    Updating = false;
                }
            }
        }

        private IEnumerable<MitbbsLink> GetAllBoardLinksRecursively(String url)
        {
            MitbbsBoardGroup _boardGroup;
            if (App.Settings.OfflineContentManager.TryLoadOfflineContent(OfflineID, url, out _boardGroup))
            {
                foreach (MitbbsLink link in _boardGroup.BoardLinks)
                {
                    if (link is MitbbsBoardLinkBase)
                    {
                        yield return link;
                    }
                    else
                    {
                        foreach (MitbbsLink link2 in GetAllBoardLinksRecursively(link.Url))
                        {
                            yield return link2;
                        }
                    }
                }
            }
        }

        private IEnumerable<MitbbsLink> GetAllClubinks(String url)
        {
            MitbbsClubHome _clubHome;
            if (App.Settings.OfflineContentManager.TryLoadOfflineContent(OfflineID, "clubhome-" + url, out _clubHome))
            {
                foreach (MitbbsLink link in _clubHome.ClubGroupLinks)
                {
                    MitbbsClubGroupAllPages _clubGroup;
                    if (App.Settings.OfflineContentManager.TryLoadOfflineContent(OfflineID, link.Url, out _clubGroup))
                    {
                        foreach (MitbbsLink clubLink in _clubGroup.ClubLinks)
                        {
                            yield return clubLink;
                        }
                    }
                }
            }
            
        }

        public void SaveToStorage()
        {
            lock (_lock2)
            {
                if (AllBoardLinks.Count > 0)
                {
                    StorageHelper.SaveObject("boardlist.xml", AllBoardLinks);
                }
            }
        }

        public void RestoreFromStorage()
        {
            lock (_lock3)
            {
                lock (_lock2)
                {
                    if (AllBoardLinks.Count <= 0)
                    {
                        List<MitbbsLink> restoredBoardLinks;
                        if (StorageHelper.TryLoadObject("boardlist.xml", out restoredBoardLinks))
                        {
                            AllBoardLinks = restoredBoardLinks;
                        }
                    }
                }
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
