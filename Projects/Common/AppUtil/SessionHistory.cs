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
using System.Windows.Navigation;
using System.Xml.Serialization;

namespace Naboo.AppUtil
{
    public class SessionHistory
    {
        public class PageEntry
        {
            public string PageUrl;
        }

        public List<PageEntry> PageHistoryStack { get; set; }

        [XmlIgnore]
        public bool RestoringHistory { get; private set; }

        bool _lastTrySucceeded = false;
        [XmlIgnore]
        public bool LastTrySucceeded
        {
            get
            {
                bool result = _lastTrySucceeded;
                _lastTrySucceeded = false;
                return result;
            }
        }

        [XmlIgnore]
        public int Count
        {
            get
            {
                return PageHistoryStack.Count;
            }
        }

        public string LastPageName { get; set; }

        public SessionHistory()
        {
            PageHistoryStack = new List<PageEntry>();
            RestoringHistory = false;
            _lastTrySucceeded = false;
        }

        public void StartRestoreFromHistory(NavigationService nav)
        {
            if (PageHistoryStack.Count <= 0)
            {
                return;
            }

            RestoringHistory = true;

            TryRestoreFromHistory(nav);
        }

        public bool TryRestoreFromHistory(NavigationService nav)
        {
            _lastTrySucceeded = false;

            if (!RestoringHistory)
            {
                return false;
            }

            if (PageHistoryStack.Count <= 0)
            {
                RestoringHistory = false;
                return false;
            }

            PageEntry entry = PageHistoryStack[0];
            PageHistoryStack.RemoveAt(0);
            if (PageHistoryStack.Count <= 0)
            {
                RestoringHistory = false;
            }

            nav.Navigate(new Uri(entry.PageUrl, UriKind.Relative));

            _lastTrySucceeded = true;
            return true;
        }

        public void Reset()
        {
            _lastTrySucceeded = false;
            PageHistoryStack.Clear();
        }

        public void SetLastPageName(NavigationService nav, string name, string fallbackName = null)
        {
            int count = GetBackStackCount(nav);

            if (count != PageHistoryStack.Count)
            {
                return;
            }

            if (!string.IsNullOrEmpty(name))
            {
                LastPageName = name;
            }
            else
            {
                LastPageName = fallbackName;
            }
        }

        public void AddPageToHistory(NavigationService nav)
        {
            int count = GetBackStackCount(nav);

            if (count > PageHistoryStack.Count + 1)
            {
                return;
            }

            if (count < PageHistoryStack.Count)
            {
                PageHistoryStack.RemoveAt(PageHistoryStack.Count - 1);
                LastPageName = null;
            }

            if (count > PageHistoryStack.Count)
            {
                PageHistoryStack.Add(
                    new PageEntry()
                    {
                        PageUrl = nav.CurrentSource.OriginalString
                    }
                    );
                LastPageName = null;
            }
        }

        private void RemovePageFromHistory(NavigationService nav)
        {
            int count = GetBackStackCount(nav);

            if (count >= PageHistoryStack.Count || PageHistoryStack.Count <= 0)
            {
                return;
            }

            PageHistoryStack.RemoveAt(PageHistoryStack.Count - 1);
        }

        private int GetBackStackCount(NavigationService nav)
        {
            int count = 0;
            foreach (var item in nav.BackStack)
            {
                count++;
            }

            return count;
        }

    }
}
