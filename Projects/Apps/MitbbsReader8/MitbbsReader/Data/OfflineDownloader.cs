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
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public abstract class OfflineDownloader
    {
        public class DownloadProgressEventArgs : EventArgs
        {
            public String Step;
            public int Progress;
        }

        public Guid RootID = OfflineContentManager.CreateNewRootID();
        public String Url { get; protected set; }
        public event EventHandler<DataLoadedEventArgs> DownloadCompleted;
        public event EventHandler<DownloadProgressEventArgs> DownloadProgressed;

        public bool IsDownloading { get; protected set; }
        public bool IsCompleted { get; protected set; }

        protected HtmlWeb _web;
        protected volatile bool _isCanceled = false;
        protected OfflineContentManager _offlineContentManager = null;
        public int _totalProgress = 1;
        public int _currentProgress = 0;

        protected OfflineDownloader(OfflineContentManager offlineContentManager)
        {
            _offlineContentManager = offlineContentManager;
            IsCompleted = false;
            IsDownloading = false;
        }

        public virtual void StartDownload(HtmlWeb web, string url)
        {
            _web = web;
            Url = url;
            IsCompleted = false;
            _isCanceled = false;
            IsDownloading = true;

            StartDownloadInternal();
        }

        public virtual void Cancel()
        {
            _isCanceled = true;
            IsDownloading = false;
        }

        protected virtual void HandleDownloadComplete(Exception e)
        {
            IsDownloading = false;

            _currentProgress = _totalProgress;
            HandleDownloadProgress(_isCanceled ? "下载被取消" : (IsCompleted ? "下载完成" : "下载失败"));

            if (!_isCanceled && DownloadCompleted != null)
            {
                DataLoadedEventArgs args = new DataLoadedEventArgs();
                args.Error = e;

                DownloadCompleted(this, args);
            }
        }

        protected virtual void HandleDownloadProgress(String step)
        {
            if (!_isCanceled && DownloadProgressed != null)
            {
                DownloadProgressEventArgs args = new DownloadProgressEventArgs()
                {
                    Step = step,
                    Progress = _currentProgress * 100 / _totalProgress
                };

                DownloadProgressed(this, args);
            }
        }

        protected abstract void StartDownloadInternal();
    }

    public abstract class MitbbsOfflineDownloader : OfflineDownloader
    {
        protected MitbbsOfflineContentManager _mitbbsOfflineContentManager;

        protected MitbbsOfflineDownloader()
            : base(App.Settings.OfflineContentManager)
        {
            _mitbbsOfflineContentManager = App.Settings.OfflineContentManager;
        }

        public abstract MitbbsLink CreateMitbbsLink();
    }

    public class MitbbsTopicOfflineDownloader : MitbbsOfflineDownloader
    {
        public bool DownloadAllPages { get; set; }
        public bool DownloadImages { get; set; }

        protected MitbbsTopicBase _topic;
        protected String _title;
        protected String _url;
        protected int _totalPages = 0;
        protected List<String> topicPageUrls = new List<String>();
        protected MitbbsImagesOfflineDownloader _imagesDownloader = null;

        public MitbbsTopicOfflineDownloader(MitbbsTopicBase topic)
        {
            _topic = topic;
            DownloadAllPages = true;
            DownloadImages = true;
        }

        protected override void StartDownloadInternal()
        {
            _title = null;
            _url = null;
            _totalPages = 0;
            topicPageUrls.Clear();

            _imagesDownloader = new MitbbsImagesOfflineDownloader();
            _imagesDownloader.RootID = RootID;

            AddUrlToDownloadList(Url);
            DownloadNextPage();
        }

        protected void AddUrlToDownloadList(String url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                _totalProgress++;
                topicPageUrls.Add(url);
            }
        }

        protected void Topic_Loaded(object sender, DataLoadedEventArgs e)
        {
            _topic.TopicLoaded -= Topic_Loaded;

            if (_isCanceled)
            {
                return;
            }

            if (_topic.IsLoaded)
            {
                String urlToSave = _topic.Url;

                if (_title == null)
                {
                    _title = _topic.Title;
                    _url = Url;
                    urlToSave = Url;
                }

                _mitbbsOfflineContentManager.SaveOfflineContent(RootID, urlToSave, _topic);

                // Images
                //
                if (DownloadImages)
                {
                    foreach (var post in _topic.Posts)
                    {
                        foreach (var contentBlock in post.Contents)
                        {
                            if (contentBlock is ImageBlock)
                            {
                                _imagesDownloader.AddImageUrl((contentBlock as ImageBlock).ParentUrl, (contentBlock as ImageBlock).ImageUrl);
                            }
                        }
                    }
                }
            }

            if (DownloadAllPages && _topic.IsLoaded)
            {
                AddUrlToDownloadList(_topic.NextPageUrl);
                AddUrlToDownloadList(_topic.PrevPageUrl);
            }

            if (_topic.IsLoaded)
            {
                DownloadNextPage();
            }
            else
            {
                HandleDownloadComplete(e.Error);
            }
        }

        protected void DownloadNextPage()
        {
            String pageUrlToDownload = null;

            if (_isCanceled)
            {
                return;
            }

            while (topicPageUrls.Count > 0)
            {
                String tempUrl = topicPageUrls[0];
                topicPageUrls.RemoveAt(0);
                _currentProgress++;
                _totalPages++;

                if (!_mitbbsOfflineContentManager.OfflineContentExists(RootID, tempUrl))
                {
                    pageUrlToDownload = tempUrl;
                    break;
                }
            }

            if (pageUrlToDownload != null)
            {
                HandleDownloadProgress("下载文章页面");
                _topic.TopicLoaded += Topic_Loaded;
                _topic.LoadFromUrl(_web, pageUrlToDownload);
            }
            else
            {
                _imagesDownloader.DownloadCompleted += Images_Downloaded;
                _imagesDownloader.DownloadProgressed += ImagesDownload_Progress;

                _imagesDownloader.StartDownload(_web, null);
            }
        }

        protected void Images_Downloaded(object sender, DataLoadedEventArgs e)
        {
            _imagesDownloader.DownloadCompleted -= Images_Downloaded;
            _imagesDownloader.DownloadProgressed -= ImagesDownload_Progress;

            IsCompleted = true;
            HandleDownloadComplete(null);
        }

        private void ImagesDownload_Progress(object sender, OfflineDownloader.DownloadProgressEventArgs e)
        {
            _totalProgress = _totalPages + _imagesDownloader._totalProgress + 1;
            _currentProgress = _totalPages + _imagesDownloader._currentProgress + 1;

            if (_currentProgress == _totalProgress)
            {
                return;
            }

            HandleDownloadProgress(e.Step);
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_imagesDownloader != null)
            {
                _imagesDownloader.Cancel();
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            if (_topic is MitbbsTopicMobile)
            {
                return new MitbbsTopicLinkMobile()
                {
                    Url = _url,
                    Name = _title,
                };
            }
            else if (_topic is MitbbsTopic)
            {
                return new MitbbsTopicLink()
                {
                    Url = _url,
                    Name = _title,
                };
            }
            else if (_topic is MitbbsTopicEssenceMobile)
            {
                return new MitbbsTopicEssenceLink()
                {
                    Url = _url,
                    Name = _title,
                };
            }

            return null;
        }
    }

    public class MitbbsTopicListOfflineDownloader : MitbbsOfflineDownloader
    {
        public class TopicInfo
        {
            public String Title;
            public String Url;
        }

        public bool DownloadAllPages
        {
            get
            {
                return _topicDownloader.DownloadAllPages;
            }

            set
            {
                _topicDownloader.DownloadAllPages = value;
            }
        }

        public bool DownloadImages
        {
            get
            {
                return _topicDownloader.DownloadImages;
            }

            set
            {
                _topicDownloader.DownloadImages = value;
            }
        }

        protected MitbbsTopicOfflineDownloader _topicDownloader;
        protected List<TopicInfo> _topicUrls = new List<TopicInfo>();
        protected TopicInfo _currentTopic;
        protected bool _retried = false;

        public uint ErrorCount { get; protected set; }
        public uint SuccessCount { get; protected set; }
        public MitbbsTopicListOfflineDownloader(MitbbsTopicBase topic)
        {
            _topicDownloader = new MitbbsTopicOfflineDownloader(topic);
        }

        public void AddTopicUrl(String title, String topicUrl)
        {
            if (!string.IsNullOrEmpty(topicUrl))
            {
                _totalProgress++;
                _topicUrls.Add(new TopicInfo()
                    {
                        Title = title,
                        Url = topicUrl
                    }
                    );
            }
        }

        public void AddTopicUrls(Collection<MitbbsLink> links)
        {
            foreach (MitbbsLink link in links)
            {
                AddTopicUrl(link.Name, link.Url);
            }
        }

        protected override void StartDownloadInternal()
        {
            SuccessCount = 0;
            ErrorCount = 0;
            _currentTopic = null;
            _retried = false;
            _topicDownloader.RootID = RootID;
            DownloadNextTopic();
        }

        protected void DownloadNextTopic()
        {
            if (_isCanceled)
            {
                return;
            }

            if (_topicUrls.Count > 0)
            {
                _currentTopic = _topicUrls[0];
                String topicTitle = _topicUrls[0].Title;
                String topicUrl = _topicUrls[0].Url;
                _topicUrls.RemoveAt(0);

                _currentProgress++;
                HandleDownloadProgress("下载: " + topicTitle);

                _topicDownloader.DownloadCompleted += TopicDownload_Completed;
                _topicDownloader.StartDownload(_web, topicUrl);
            }
            else
            {
                IsCompleted = SuccessCount > 0 || ErrorCount <= 0;
                HandleDownloadComplete(null);
            }
        }

        protected void TopicDownload_Completed(object sender, DataLoadedEventArgs e)
        {
            _topicDownloader.DownloadCompleted -= TopicDownload_Completed;

            if (_topicDownloader.IsCompleted)
            {
                _retried = false;
                SuccessCount++;
            }
            else
            {
                HandleDownloadProgress("下载文章失败");
                
                if (!AppUtil.AppInfo.IsNetworkConnected())
                {
                    ErrorCount++;

                    _retried = false;
                    _topicUrls.Clear();
                    IsCompleted = false;
                    HandleDownloadComplete(e.Error);
                    return;
                }
                else if (!_retried && _currentTopic != null)
                {
                    _retried = true;
                    _currentProgress--;
                    _topicUrls.Insert(0, _currentTopic);
                }
                else
                {
                    ErrorCount++;

                    _retried = false;
                }
            }

            DownloadNextTopic();
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_topicDownloader != null)
            {
                _topicDownloader.Cancel();
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            throw new NotImplementedException();
        }
    }

    public class MitbbsBoardDownloader : MitbbsOfflineDownloader
    {
        protected MitbbsBoardEssence _boardEssense = null;
        protected MitbbsBoardBase _board = null;
        protected MitbbsTopicBase _topic = null;
        protected String _name = null;

        protected MitbbsTopicListOfflineDownloader _topicListDownloader = null;

        public MitbbsBoardDownloader(MitbbsBoardBase board, MitbbsTopicBase topic)
        {
            _board = board;
            _boardEssense = null;
            _topic = topic;

            _board.IgnoreReadHistory = true;
        }

        public MitbbsBoardDownloader(MitbbsBoardEssence boardEssense, MitbbsTopicBase topic, String name)
        {
            _board = null;
            _boardEssense = boardEssense;
            _topic = topic;
            _name = name;
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_topicListDownloader != null)
            {
                _topicListDownloader.Cancel();
            }
        }

        protected override void StartDownloadInternal()
        {
            if (_board != null)
            {
                _board.BoardLoaded += Board_Loaded;
                _board.LoadFromUrl(_web, Url, true);
            }
            else
            {
                _boardEssense.BoardLoaded += Board_Loaded;
                _boardEssense.LoadFromUrl(_web, Url);
            }
        }

        protected void Board_Loaded(object sender, DataLoadedEventArgs e)
        {
            bool loaded = false;
            if (_board != null)
            {
                _board.BoardLoaded -= Board_Loaded;
                loaded = _board.IsLoaded;
            }
            else
            {
                _boardEssense.BoardLoaded -= Board_Loaded;
                loaded = _boardEssense.IsLoaded;
            }

            if (_isCanceled)
            {
                return;
            }

            if (loaded)
            {
                if (_board != null)
                {
                    HandleDownloadProgress("下载: " + _board.BoardName);
                }
                else
                {
                    HandleDownloadProgress("下载: " + _boardEssense.BoardName);
                }

                bool saved = false;

                if (_board != null)
                {
                    saved = _mitbbsOfflineContentManager.SaveOfflineContent(RootID, Url, _board);
                }
                else
                {
                    saved = _mitbbsOfflineContentManager.SaveOfflineContent(RootID, Url, _boardEssense);
                }

                if (saved)
                {
                    _topicListDownloader = new MitbbsTopicListOfflineDownloader(_topic);
                    _topicListDownloader.RootID = RootID;
                    _topicListDownloader.DownloadCompleted += TopicList_Downloaded;
                    _topicListDownloader.DownloadProgressed += TopicListDownload_Progress;

                    if (_board != null)
                    {
                        foreach (var link in _board.TopicLinks)
                        {
                            _topicListDownloader.AddTopicUrl(link.Name, link.Url);
                        }
                    }
                    else
                    {
                        foreach (var link in _boardEssense.EssenceLinks)
                        {
                            _topicListDownloader.AddTopicUrl(link.Name, link.Url);
                        }
                    }

                    _topicListDownloader.StartDownload(_web, null);

                    return;
                }
            }
            
            HandleDownloadComplete(e.Error);
        }

        protected void TopicList_Downloaded(object sender, DataLoadedEventArgs e)
        {
            _topicListDownloader.DownloadCompleted -= TopicList_Downloaded;
            _topicListDownloader.DownloadProgressed -= TopicListDownload_Progress;

            if (_isCanceled)
            {
                return;
            }

            IsCompleted = _topicListDownloader.IsCompleted;

            HandleDownloadComplete(e.Error);
        }

        private void TopicListDownload_Progress(object sender, OfflineDownloader.DownloadProgressEventArgs e)
        {
            _totalProgress = _topicListDownloader._totalProgress + 1;
            _currentProgress = _topicListDownloader._currentProgress + 1;

            if (_currentProgress == _totalProgress)
            {
                return;
            }

            HandleDownloadProgress(e.Step); 
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            if (_board != null)
            {
                if (_board is MitbbsBoardMobile)
                {
                    return new MitbbsBoardLinkMobile()
                    {
                        Url = Url,
                        Name = _board.BoardName,
                        BoardName = _board.BoardName
                    };
                }
                else if (_board is MitbbsClubBoard)
                {
                    return new MitbbsClubLink()
                    {
                        Url = Url,
                        Name = _board.BoardName,
                    };
                }
                else if (_board is MitbbsBoard)
                {
                    return new MitbbsBoardLink()
                    {
                        Url = Url,
                        Name = _board.BoardName,
                        BoardName = _board.BoardName
                    };
                }
            }
            else
            {
                if (_boardEssense is MitbbsBoardEssence)
                {
                    return new MitbbsBoardEssenceLink()
                    {
                        Url = Url,
                        Name = _name == null ? _boardEssense.BoardName : _name,
                    };
                }
            }

            return null;
        }
    }

    public class MitbbsHomeOfflineDownloader : MitbbsOfflineDownloader
    {
        protected MitbbsHomeBase _mitbbsHome = null;
        protected MitbbsTopicListOfflineDownloader _topicListDownloader = null;

        protected override void StartDownloadInternal()
        {
            _mitbbsHome = new MitbbsHome();
            _mitbbsHome.HomeLoaded += MitbbsHome_Loaded;
            _mitbbsHome.LoadFromUrl(_web, Url);
        }

        protected void MitbbsHome_Loaded(object sender, DataLoadedEventArgs e)
        {
            _mitbbsHome.HomeLoaded -= MitbbsHome_Loaded;

            if (_isCanceled)
            {
                return;
            }

            if (_mitbbsHome.IsLoaded)
            {
                HandleDownloadProgress("下载: 主页");
                if (_mitbbsOfflineContentManager.SaveOfflineContent(RootID, Url, _mitbbsHome))
                {
                    if (_mitbbsHome is MitbbsHomeMobile)
                    {
                        _topicListDownloader = new MitbbsTopicListOfflineDownloader(new MitbbsTopicMobile());
                    }
                    else
                    {
                        _topicListDownloader = new MitbbsTopicListOfflineDownloader(new MitbbsTopic());
                    }

                    _topicListDownloader.RootID = RootID;
                    _topicListDownloader.DownloadCompleted += TopicList_Downloaded;
                    _topicListDownloader.DownloadProgressed += TopicListDownload_Progress;

                    foreach (var link in _mitbbsHome.TopArticles)
                    {
                        _topicListDownloader.AddTopicUrl(link.Name, link.Url);
                    }

                    foreach (var link in _mitbbsHome.HotArticles)
                    {
                        _topicListDownloader.AddTopicUrl(link.Name, link.Url);
                    }

                    foreach (var link in _mitbbsHome.RecommendedArticles)
                    {
                        _topicListDownloader.AddTopicUrl(link.Name, link.Url);
                    }

                    _topicListDownloader.StartDownload(_web, null);

                    return;
                }
            }
            
            HandleDownloadComplete(e.Error);
        }

        protected void TopicList_Downloaded(object sender, DataLoadedEventArgs e)
        {
            _topicListDownloader.DownloadCompleted -= TopicList_Downloaded;
            _topicListDownloader.DownloadProgressed -= TopicListDownload_Progress;

            if (_isCanceled)
            {
                return;
            }

            IsCompleted = _topicListDownloader.IsCompleted;
            HandleDownloadComplete(e.Error);
        }

        private void TopicListDownload_Progress(object sender, OfflineDownloader.DownloadProgressEventArgs e)
        {
            _totalProgress = _topicListDownloader._totalProgress + 1;
            _currentProgress = _topicListDownloader._currentProgress + 1;

            if (_currentProgress == _totalProgress)
            {
                return;
            }

            HandleDownloadProgress(e.Step);
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_topicListDownloader != null)
            {
                _topicListDownloader.Cancel();
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            return new MitbbsHomeLink()
            {
                Url = Url,
                Name = "未名空间主页"
            };
        }
    }

    public class MitbbsUserHomeOfflineDownloader : MitbbsOfflineDownloader
    {
        protected MitbbsUserHome _userHome = null;
        protected MitbbsTopicListOfflineDownloader _topicListDownloader = null;

        protected override void StartDownloadInternal()
        {
            _userHome = new MitbbsUserHome();
            _userHome.UserHomeLoaded += UserHome_Loaded;
            _userHome.LoadFromUrl(_web, Url);
        }

        protected void UserHome_Loaded(object sender, DataLoadedEventArgs e)
        {
            _userHome.UserHomeLoaded -= UserHome_Loaded;

            if (_isCanceled)
            {
                return;
            }

            if (_userHome.IsLoaded)
            {
                HandleDownloadProgress("下载: 用户家页");
                if (_mitbbsOfflineContentManager.SaveOfflineContent(RootID, Url, _userHome))
                {
                    _topicListDownloader = new MitbbsTopicListOfflineDownloader(new MitbbsTopicMobile());
                    _topicListDownloader.RootID = RootID;
                    _topicListDownloader.DownloadCompleted += TopicList_Downloaded;
                    _topicListDownloader.DownloadProgressed += TopicListDownload_Progress;

                    foreach (var link in _userHome.MyArticles)
                    {
                        _topicListDownloader.AddTopicUrl(link.Name, link.Url);
                    }

                    _topicListDownloader.StartDownload(_web, null);

                    return;
                }
            }
            
            HandleDownloadComplete(e.Error);
        }

        protected void TopicList_Downloaded(object sender, DataLoadedEventArgs e)
        {
            _topicListDownloader.DownloadCompleted -= TopicList_Downloaded;
            _topicListDownloader.DownloadProgressed -= TopicListDownload_Progress;
            IsCompleted = _topicListDownloader.IsCompleted;
            HandleDownloadComplete(e.Error);
        }

        private void TopicListDownload_Progress(object sender, OfflineDownloader.DownloadProgressEventArgs e)
        {
            _totalProgress = _topicListDownloader._totalProgress + 1;
            _currentProgress = _topicListDownloader._currentProgress + 1;

            if (_currentProgress == _totalProgress)
            {
                return;
            }

            HandleDownloadProgress(e.Step);
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_topicListDownloader != null)
            {
                _topicListDownloader.Cancel();
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            return new MitbbsUserHomeLink()
            {
                Url = Url,
                Name = "用户" + App.WebSession.Username + "家页"
            };
        }
    }

    public class MitbbsBoardGroupOfflineDownloader : MitbbsOfflineDownloader
    {
        protected MitbbsBoardGroup _boardGroup = null;
        protected List<String> _subBoardGroupUrls = new List<String>();
        protected MitbbsBoardGroupOfflineDownloader _subBoardGroupDownloader = null;

        public TimeSpan OldContentMaxAge = TimeSpan.MaxValue;
        public bool NewContentDownloaded { get; private set; }

        protected override void StartDownloadInternal()
        {
            NewContentDownloaded = false;
            _subBoardGroupUrls.Clear();

            if (_mitbbsOfflineContentManager.TryLoadOfflineContent(RootID, Url, OldContentMaxAge, out _boardGroup))
            {
                BoardGroup_Loaded(this, new DataLoadedEventArgs());
            }
            else
            {
                NewContentDownloaded = true;
                _mitbbsOfflineContentManager.CleanupOfflineContent(RootID, Url);

                _boardGroup = new MitbbsBoardGroup();
                _boardGroup.BoardGroupLoaded += BoardGroup_Loaded;
                _boardGroup.LoadFromUrl(_web, Url);
            }
        }

        protected void BoardGroup_Loaded(object sender, DataLoadedEventArgs e)
        {
            _boardGroup.BoardGroupLoaded -= BoardGroup_Loaded;

            if (_isCanceled)
            {
                return;
            }

            if (_boardGroup.IsLoaded)
            {
                if (_mitbbsOfflineContentManager.OfflineContentExists(RootID, Url) ||
                    _mitbbsOfflineContentManager.SaveOfflineContent(RootID, Url, _boardGroup))
                {
                    foreach (var link in _boardGroup.BoardLinks)
                    {
                        if (link is MitbbsBoardGroupLink)
                        {
                            _subBoardGroupUrls.Add(link.Url);
                        }
                    }
                }

                _subBoardGroupDownloader = new MitbbsBoardGroupOfflineDownloader();
                _subBoardGroupDownloader.RootID = RootID;
                
                DownloadNextBoardGroup();

                return;
            }
            
            HandleDownloadComplete(e.Error);
        }

        protected void DownloadNextBoardGroup()
        {
            if (_isCanceled)
            {
                return;
            }

            if (_subBoardGroupUrls.Count > 0)
            {
                String boardGroupUrl = _subBoardGroupUrls[0];
                _subBoardGroupUrls.RemoveAt(0);

                _subBoardGroupDownloader.DownloadCompleted += SubBoardGroup_Downloaded;
                _subBoardGroupDownloader.StartDownload(_web, boardGroupUrl);
            }
            else
            {
                IsCompleted = true;
                HandleDownloadComplete(null);
            }
        }

        protected void SubBoardGroup_Downloaded(object sender, DataLoadedEventArgs e)
        {
            _subBoardGroupDownloader.DownloadCompleted -= SubBoardGroup_Downloaded;

            if (_isCanceled)
            {
                return;
            }

            if (_subBoardGroupDownloader.IsCompleted)
            {
                NewContentDownloaded = NewContentDownloaded || _subBoardGroupDownloader.NewContentDownloaded;
                DownloadNextBoardGroup();
            }
            else
            {
                HandleDownloadComplete(e.Error);
            }
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_subBoardGroupDownloader != null)
            {
                _subBoardGroupDownloader.Cancel();
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            throw new NotImplementedException();
        }
    }

    public class MitbbsClubHomeOfflineDownloader : MitbbsOfflineDownloader
    {
        protected MitbbsClubHome _clubHome = null;
        protected List<String> _clubGroupUrls = new List<String>();
        protected MitbbsClubGroupBase _clubGroup = null;

        public TimeSpan OldContentMaxAge = TimeSpan.MaxValue;
        public bool NewContentDownloaded { get; private set; }

        protected override void StartDownloadInternal()
        {
            NewContentDownloaded = false;
            _clubGroupUrls.Clear();

            if (_mitbbsOfflineContentManager.TryLoadOfflineContent(RootID, "clubhome-" + Url, OldContentMaxAge, out _clubHome))
            {
                ClubHome_Loaded(this, new DataLoadedEventArgs());
            }
            else
            {
                NewContentDownloaded = true;
                _mitbbsOfflineContentManager.CleanupOfflineContent(RootID, "clubhome-" + Url);

                _clubHome = new MitbbsClubHome();
                _clubHome.ClubHomeLoaded += ClubHome_Loaded;
                _clubHome.LoadFromUrl(_web, Url);
            }
        }

        protected void ClubHome_Loaded(object sender, DataLoadedEventArgs e)
        {
            _clubHome.ClubHomeLoaded -= ClubHome_Loaded;

            if (_isCanceled)
            {
                return;
            }

            if (_clubHome.IsLoaded)
            {
                if (_mitbbsOfflineContentManager.OfflineContentExists(RootID, "clubhome-" + Url) ||
                    _mitbbsOfflineContentManager.SaveOfflineContent(RootID, "clubhome-" + Url, _clubHome))
                {
                    foreach (var link in _clubHome.ClubGroupLinks)
                    {
                        _clubGroupUrls.Add(link.Url);
                    }

                    DownloadNextClubGroup();

                    return;
                }
            }
            
            HandleDownloadComplete(e.Error);
        }

        protected void DownloadNextClubGroup()
        {
            if (_isCanceled)
            {
                return;
            }

            if (_clubGroupUrls.Count > 0)
            {
                String clubGroupUrl = _clubGroupUrls[0];
                _clubGroupUrls.RemoveAt(0);

                if (_mitbbsOfflineContentManager.TryLoadOfflineContent(RootID, clubGroupUrl, OldContentMaxAge, out _clubGroup))
                {
                    ClubGroup_Downloaded(this, new DataLoadedEventArgs());
                }
                else
                {
                    NewContentDownloaded = true;
                    _mitbbsOfflineContentManager.CleanupOfflineContent(RootID, clubGroupUrl);

                    _clubGroup = new MitbbsClubGroupAllPages();
                    _clubGroup.ClubGroupLoaded += ClubGroup_Downloaded;
                    _clubGroup.LoadFromUrl(_web, clubGroupUrl);
                }
            }
            else
            {
                IsCompleted = true;
                HandleDownloadComplete(null);
            }
        }

        protected void ClubGroup_Downloaded(object sender, DataLoadedEventArgs e)
        {
            _clubGroup.ClubGroupLoaded -= ClubGroup_Downloaded;

            if (_isCanceled)
            {
                return;
            }

            if (_clubGroup.IsLoaded)
            {
                if (!_mitbbsOfflineContentManager.OfflineContentExists(RootID, _clubGroup.Url))
                {
                    _mitbbsOfflineContentManager.SaveOfflineContent(RootID, _clubGroup.Url, _clubGroup);
                }

                DownloadNextClubGroup();
            }
            else
            {
                HandleDownloadComplete(e.Error);
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            throw new NotImplementedException();
        }
    }

    public class MitbbsImagesOfflineDownloader : MitbbsOfflineDownloader
    {
        protected class ImageInfo
        {
            public String PageUrl;
            public String ImageUrl;
        }

        protected WebClient _webClient;
        protected List<ImageInfo> imageUrls = new List<ImageInfo>();
        protected String _urlToSave = null;

        protected override void StartDownloadInternal()
        {
            CookieAwareClient webClient = new CookieAwareClient();
            webClient.Cookies = App.WebSession.Cookies;

            _webClient = webClient;

            _totalProgress = imageUrls.Count + 1;

            DownloadNextImage();
        }

        protected void DownloadNextImage()
        {
            String pageUrl = null;
            String imageUrlToDownload = null;

            if (_isCanceled)
            {
                return;
            }

            while (imageUrls.Count > 0)
            {
                String tempPageUrl = imageUrls[0].PageUrl;
                String tempImageUrl = imageUrls[0].ImageUrl;
                
                imageUrls.RemoveAt(0);
                _currentProgress++;

                if (!_mitbbsOfflineContentManager.OfflineContentExists(RootID, tempImageUrl))
                {
                    pageUrl = tempPageUrl;
                    imageUrlToDownload = tempImageUrl;
                    break;
                }
            }

            if (imageUrlToDownload != null)
            {
                HandleDownloadProgress("下载图片: " + imageUrlToDownload);

                _urlToSave = imageUrlToDownload;

                Version osVer = System.Environment.OSVersion.Version;

                if ((osVer.Major > 7) || ((osVer.Major == 7) && (osVer.Minor >= 1)))
                {
                    _webClient.Headers[HttpRequestHeader.Referer] = pageUrl;
                }

                _webClient.OpenReadCompleted += OnImageLoaded;

                try
                {
                    _webClient.OpenReadAsync(new Uri(imageUrlToDownload, UriKind.Absolute));
                }
                catch (Exception e)
                {
                    OnImageLoaded(this, null);
                }
            }
            else
            {
                IsCompleted = true;
                HandleDownloadComplete(null);
            }
        }

        protected void OnImageLoaded(object sender, OpenReadCompletedEventArgs args)
        {
            _webClient.OpenReadCompleted -= OnImageLoaded;

            if (_isCanceled)
            {
                return;
            }

            if ((args != null && args.Error == null) && _urlToSave != null)
            {
                _mitbbsOfflineContentManager.SaveOfflineContent(RootID, _urlToSave, args.Result);
            }
            else
            {
                HandleDownloadProgress("下载图片失败");

                if (!AppUtil.AppInfo.IsNetworkConnected())
                {
                    imageUrls.Clear();
                    IsCompleted = false;
                    HandleDownloadComplete(args.Error);
                    return;
                }
            }

            DownloadNextImage();
        }

        public void AddImageUrl(String pageUrl, String imageUrl)
        {
            if (!string.IsNullOrEmpty(pageUrl))
            {
                if (pageUrl == null)
                {
                    pageUrl = imageUrl;
                }

                imageUrls.Add(
                    new ImageInfo()
                    {
                        PageUrl = pageUrl,
                        ImageUrl = imageUrl
                    }
                    );
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            throw new NotImplementedException();
        }
    }

    public class OfflineDownloadQueue : MitbbsOfflineDownloader
    {
        public class DownloadQueueContentCompletedEventArgs : EventArgs
        {
            public MitbbsOfflineContentIndex Content;
            public bool Success;
        }

        public event EventHandler<DownloadQueueContentCompletedEventArgs> ContentDownloadCompleted;

        protected List<MitbbsOfflineContentIndex> _contents = new List<MitbbsOfflineContentIndex>();
        protected MitbbsOfflineDownloader _currentDownloader = null;
        protected int _previousTotalProgress;
        protected int _remainingTotalProgress;

        protected override void StartDownloadInternal()
        {
            _previousTotalProgress = 0;

            _remainingTotalProgress = 0;
            foreach (var content in _contents)
            {
                _remainingTotalProgress += content.EstimatedItemCount;
            }

            _totalProgress = _previousTotalProgress + _remainingTotalProgress;

            if (_totalProgress <= 0)
            {
                _totalProgress = 1;
            }

            DownloadNextContent();
        }

        public override void Cancel()
        {
            base.Cancel();

            if (_currentDownloader != null)
            {
                _currentDownloader.Cancel();
            }
        }

        protected void DownloadNextContent()
        {
            if (_isCanceled)
            {
                HandleDownloadComplete(null);
                return;
            }

            if (_contents.Count > 0)
            {
                MitbbsOfflineContentIndex content = _contents[0];
                content.Failed = false;

                content.DownloadDate = DateTime.Now;

                MitbbsOfflineDownloader downloader = CreateDownloader(content);

                _currentDownloader = downloader;
                _currentDownloader.DownloadCompleted += Content_Downloaded;
                _currentDownloader.DownloadProgressed += ContentDownload_Progress;

                HandleDownloadProgress("开始下载: " + content.Name);
                _totalProgress = _previousTotalProgress + _remainingTotalProgress;
                _remainingTotalProgress -= content.EstimatedItemCount;

                _currentDownloader.StartDownload(_web, content.Key);
            }
            else
            {
                IsCompleted = true;
                _currentDownloader = null;
                HandleDownloadComplete(null);
            }
        }

        protected void Content_Downloaded(object sender, DataLoadedEventArgs e)
        {
            _currentDownloader.DownloadCompleted -= Content_Downloaded;
            _currentDownloader.DownloadProgressed -= ContentDownload_Progress;

            MitbbsOfflineContentIndex contentIndex = _contents[0];
            contentIndex.Link = _currentDownloader.CreateMitbbsLink();

            if (contentIndex.Link != null)
            {
                contentIndex.Link.OfflineID = contentIndex.RootID.ToString();
                contentIndex.Name = contentIndex.Link.Name;
            }

            _contents.RemoveAt(0);

            _previousTotalProgress += _currentDownloader._totalProgress;
            _totalProgress = _previousTotalProgress + _remainingTotalProgress;
            _currentProgress = _totalProgress - _contents.Count;

            if (_currentDownloader.IsCompleted)
            {
                contentIndex.IsDownloaded = true;
                contentIndex.Failed = false;
                HandleDownloadProgress("下载完成: " + contentIndex.Name);
            }
            else
            {
                contentIndex.IsDownloaded = false;
                contentIndex.Failed = true;
                HandleDownloadProgress("下载失败: " + contentIndex.Name);
            }

            if (ContentDownloadCompleted != null)
            {
                ContentDownloadCompleted(
                    this,
                    new DownloadQueueContentCompletedEventArgs()
                    {
                        Content = contentIndex,
                        Success = _currentDownloader.IsCompleted
                    }
                    );
            }

            DownloadNextContent();
        }

        private void ContentDownload_Progress(object sender, OfflineDownloader.DownloadProgressEventArgs e)
        {
            MitbbsOfflineContentIndex contentIndex = _contents[0];
            contentIndex.DownloadProgress = _currentDownloader._currentProgress * 100 / _currentDownloader._totalProgress;

            _totalProgress = _previousTotalProgress + _currentDownloader._totalProgress + _remainingTotalProgress;
            _currentProgress = _previousTotalProgress + _currentDownloader._currentProgress;

            if (_currentProgress == _totalProgress)
            {
                return;
            }

            HandleDownloadProgress(e.Step);
        }

        public void SetDownloadContents(Collection<MitbbsOfflineContentIndex> contents)
        {
            _contents.Clear();
            foreach (MitbbsOfflineContentIndex content in contents)
            {
                _contents.Add(content);
            }
        }

        public override MitbbsLink CreateMitbbsLink()
        {
            throw new NotImplementedException();
        }

        public static MitbbsOfflineDownloader CreateDownloader(MitbbsOfflineContentIndex contentIndex)
        {
            MitbbsOfflineDownloader downloader = null;
            switch (contentIndex.ContentType)
            {
                case MitbbsOfflineContentType.Home:
                    downloader = new MitbbsHomeOfflineDownloader();
                    break;
                case MitbbsOfflineContentType.UserHome:
                    downloader = new MitbbsUserHomeOfflineDownloader();
                    break;
                case MitbbsOfflineContentType.Board:
                    downloader = new MitbbsBoardDownloader(new MitbbsBoard(), new MitbbsTopic());
                    break;
                case MitbbsOfflineContentType.ClubBoard:
                    downloader = new MitbbsBoardDownloader(new MitbbsClubBoard(), new MitbbsTopic());
                    break;
                case MitbbsOfflineContentType.Topic:
                    downloader = new MitbbsTopicOfflineDownloader(new MitbbsTopic());
                    break;
                case MitbbsOfflineContentType.ClubTopic:
                    downloader = new MitbbsTopicOfflineDownloader(new MitbbsTopic());
                    break;
                case MitbbsOfflineContentType.TopicEssense:
                    downloader = new MitbbsTopicOfflineDownloader(new MitbbsTopicEssenceMobile());
                    break;
                case MitbbsOfflineContentType.BoardEssense:
                    downloader = new MitbbsBoardDownloader(new MitbbsBoardEssence(), new MitbbsTopicEssenceMobile(), contentIndex.Name);
                    break;
            }

            if (downloader != null)
            {
                downloader.RootID = contentIndex.RootID;
            }

            return downloader;
        }
    }
}
