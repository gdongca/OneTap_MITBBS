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
using HtmlAgilityPack;

namespace Naboo.MitbbsReader
{
    public class NewTopicContentChecker
    {
        public event EventHandler<DataLoadedEventArgs> CheckCompleted;

        public bool CheckIsSuccessful { get; protected set; }
        public bool OldContentDetected { get; protected set; }

        protected HtmlWeb _web;
        
        protected MitbbsTopicBase _topic;
        protected List<MitbbsLink> _watchList;
        protected MitbbsLink _currentLink;
        
        public void StartCheck(HtmlWeb web, List<MitbbsLink> watchList)
        {
            OldContentDetected = false;
            CheckIsSuccessful = false;

            _web = web;

            _watchList = new List<MitbbsLink>();
            foreach (var link in watchList)
            {
                _watchList.Add(link);
            }

            _currentLink = null;

            _topic = new MitbbsTopic();
            _topic.TopicLoaded += Topic_Loaded;

            CheckNextItem();
        }

        protected void CheckNextItem()
        {
            _currentLink = null;
            while (_watchList.Count > 0)
            {
                _currentLink = _watchList[0];
                _watchList.RemoveAt(0);

                if (!_currentLink.HasNewContent)
                {
                    break;
                }
            }

            if (_currentLink != null && !String.IsNullOrEmpty(_currentLink.LastVisitedUrl))
            {
                if (DateTime.Now - _currentLink.AccessDate > TimeSpan.FromDays(5))
                {
                    OldContentDetected = true;
                }

                _topic.LoadFromUrl(_web, _currentLink.LastVisitedUrl);
            }
            else
            {
                DataLoadedEventArgs args = new DataLoadedEventArgs();
            
                if (CheckCompleted != null)
                {
                    CheckCompleted(this, args);
                }
            }
        }

        protected void Topic_Loaded(object sender, DataLoadedEventArgs e)
        {
            bool newContentDetected = false;
            if (_topic.IsLoaded)
            {
                CheckIsSuccessful = true;
                if (_topic.LastPageIndex > _currentLink.LastPage)
                {
                    newContentDetected = true;
                }
                else if (_topic.LastPageIndex == _currentLink.LastPage)
                {
                    if (_topic.Posts.Count > _currentLink.LastVisitedPageContentCount)
                    {
                        newContentDetected = true;
                    }
                }

                MitbbsLink link = _currentLink;
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                    () =>
                    {
                        link.HasNewContent = newContentDetected;
                    }
                    );
            }
            else
            {
                CheckIsSuccessful = false;
            }

            CheckNextItem();
        }
    }
}
