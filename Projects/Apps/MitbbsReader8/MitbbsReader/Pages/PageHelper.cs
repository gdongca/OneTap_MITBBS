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
using System.Windows.Navigation;
using Microsoft.Phone.Shell;
using System.Collections.Generic;
using Microsoft.Phone.Tasks;
using System.Windows.Data;
using System.Threading;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using Microsoft.Advertising.Mobile.UI;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public static class PageHelper
    {
        public static void OpenMitbbsLink(GenericLink link0, NavigationService nav, bool openFromHistory = false)
        {
            if (link0 != null)
            {
                link0.Select();
                App.ForceRefreshContent = false;

                if (link0 is AppMenuLink)
                {
                    OpenAppMenuLink(link0 as AppMenuLink, nav);
                    return;
                }

                MitbbsLink link = link0 as MitbbsLink;

                if (link == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(link.OfflineID))
                {
                    App.Track("Navigation", "OpenOnline", null);
                }
                else
                {
                    App.Track("Navigation", "OpenOffline", null);
                }

                App.Track("Navigation", "OpenPageTime", DateTime.Now.Hour.ToString());

                
                if (link is MitbbsHomeLink)
                {
                    String pageUrl = String.Format("/Pages/MitbbsHomePage.xaml?OfflineID={0}", link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Track("Navigation", "OpenPage", "MitbbsHome");
                }
                else if (link is MitbbsUserHomeLink)
                {
                    String pageUrl = String.Format("/Pages/UserHomePage.xaml?OfflineID={0}", link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Track("Navigation", "OpenPage", "UserHome");
                }
                else if (link is MitbbsBoardGroupLink)
                {
                    String pageUrl = String.Format("/Pages/BoardGroupPage.xaml?Url={0}&Name={1}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.Name));
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Track("Navigation", "OpenPage", "BoardGroup");
                }
                else if (link is MitbbsBoardLinkMobile)
                {
                    String pageUrl = String.Format("/Pages/BoardPage.xaml?Url={0}&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.GetBoardName()), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                    App.Track("Navigation", "OpenPage", "Board");
                }
                else if (link is MitbbsBoardLink)
                {
                    String pageUrl = String.Format("/Pages/BoardPage.xaml?Url={0}&FullPage=True&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.GetBoardName()), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                    App.Track("Navigation", "OpenPage", "Board");
                }
                else if (link is MitbbsSimpleTopicLinkMobile)
                {
                    if (link.Url.ToLower().Contains("mbbsann2"))
                    {
                        String pageUrl = String.Format("/Pages/TopicEssencePage.xaml?Url={0}&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.Name), link.OfflineID);
                        nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                        App.Track("Navigation", "OpenPage", "TopicEssence");
                    }
                    else
                    {
                        String pageUrl = String.Format("/Pages/TopicPage.xaml?Url={0}&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.Name), link.OfflineID);
                        nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                        App.Track("Navigation", "OpenPage", "Topic");
                    }
                    App.Settings.AddReadingHistory(link);
                }
                else if (link is MitbbsSimpleTopicLink)
                {
                    String pageUrl = String.Format("/Pages/TopicPage.xaml?Url={0}&FullPage=true&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.Name), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                    App.Track("Navigation", "OpenPage", "Topic");
                }
                else if (link is MitbbsClubTopicLink)
                {
                    String pageUrl = String.Format("/Pages/TopicPage.xaml?Url={0}&OpenFromBoard={1}&FullPage=true&Type=Club&Name={2}&OfflineID={3}", Uri.EscapeDataString(link.Url), !openFromHistory, Uri.EscapeDataString(link.Name), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                    App.Track("Navigation", "OpenPage", "ClubTopic");
                }
                else if (link is MitbbsTopicLink)
                {
                    String pageUrl = String.Format("/Pages/TopicPage.xaml?Url={0}&OpenFromBoard={1}&FullPage=true&Name={2}&OfflineID={3}", Uri.EscapeDataString(link.Url), !openFromHistory, Uri.EscapeDataString(link.Name), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                    App.Track("Navigation", "OpenPage", "Topic");
                }
                else if (link is MitbbsTopicLinkMobile)
                {
                    String pageUrl = String.Format("/Pages/TopicPage.xaml?Url={0}&OpenFromBoard={1}&Name={2}&OfflineID={3}", Uri.EscapeDataString(link.Url), !openFromHistory, Uri.EscapeDataString(link.Name), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                    App.Track("Navigation", "OpenPage", "Topic");
                }
                else if (link is MitbbsTopicSearchLink)
                {
                    String pageUrl;
                    if ((link as MitbbsTopicSearchLink).IsMobile)
                    {
                        pageUrl = String.Format("/Pages/TopicPage.xaml?Url={0}&OpenFromBoard={1}&Name={2}", Uri.EscapeDataString(link.Url), !openFromHistory, Uri.EscapeDataString(link.Name));
                        App.Track("Navigation", "OpenPage", "Topic");
                    }
                    else
                    {
                        pageUrl = String.Format("/Pages/TopicPage.xaml?Url={0}&OpenFromBoard={1}&FullPage=true&Type=Club&Name={2}", Uri.EscapeDataString(link.Url), !openFromHistory, Uri.EscapeDataString(link.Name));
                        App.Track("Navigation", "OpenPage", "ClubTopic");
                    }

                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                }
                else if (link is MitbbsBoardEssenceLink)
                {
                    String pageUrl = String.Format("/Pages/BoardEssencePage.xaml?Url={0}&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.Name), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Track("Navigation", "OpenPage", "BoardEssence");
                }
                else if (link is MitbbsTopicEssenceLink)
                {
                    String pageUrl = String.Format("/Pages/TopicEssencePage.xaml?Url={0}&OpenFromBoard={1}&Name={2}&OfflineID={3}", Uri.EscapeDataString(link.Url), !openFromHistory, Uri.EscapeDataString(link.Name), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Settings.AddReadingHistory(link);
                    App.Track("Navigation", "OpenPage", "TopicEssence");
                }
                else if (link is MitbbsMailLink)
                {
                    String pageUrl = String.Format("/Pages/MailPage.xaml?Url={0}&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.Name), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Track("Navigation", "OpenPage", "Mail");
                }
                else if (link is MitbbsClubGroupLink)
                {
                    String pageUrl = String.Format("/Pages/ClubGroupPage.xaml?Url={0}&Name={1}&ClubHome={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.GetBoardName()), (link as MitbbsClubGroupLink).IsClubHome.ToString());
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Track("Navigation", "OpenPage", "ClubGroup");
                }
                else if (link is MitbbsClubLink)
                {
                    String pageUrl = String.Format("/Pages/BoardPage.xaml?Url={0}&FullPage=true&Type=Club&Name={1}&OfflineID={2}", Uri.EscapeDataString(link.Url), Uri.EscapeDataString(link.GetBoardName()), link.OfflineID);
                    nav.Navigate(new Uri(pageUrl, UriKind.Relative));
                    App.Track("Navigation", "OpenPage", "Club");
                    App.Settings.AddReadingHistory(link);
                }
            }
        }

        public static void OpenAppMenuLink(AppMenuLink menuLink, NavigationService nav)
        {
            if (menuLink != null)
            {
                if (menuLink.BlockIfLoggingIn && App.WebSession.IsConnecting)
                {
                    MessageBox.Show("用户正在登录。请稍后再试。", "无法打开" + menuLink.Name, MessageBoxButton.OK);
                    return;
                }

                if (menuLink.RequiresLogIn && !App.WebSession.IsLoggedIn && !App.WebSession.IsConnecting)
                {
                    MessageBox.Show("用户尚未登录。请到设置页面中设置账户信息并登录。", "无法打开" + menuLink.Name, MessageBoxButton.OK);
                    return;
                }

                if (menuLink.AppAction != null)
                {
                    menuLink.AppAction();
                    menuLink.Select();
                }

                if (!String.IsNullOrEmpty(menuLink.Url))
                {
                    if (menuLink.Url.StartsWith("http"))
                    {
                        PageHelper.OpenGeneralLink(menuLink.Url, nav);
                    }
                    else
                    {
                        nav.Navigate(new Uri(menuLink.Url, UriKind.Relative));
                    }
                }
            }
        }

        public static void OpenGeneralLink(String url, NavigationService nav)
        {
            if (MitbbsLinkConverter.IsMitbbsFullTopicLink(url))
            {
                MitbbsTopicLink link = new MitbbsTopicLink() { ParentUrl = url, Name = "", Url = url };
                PageHelper.OpenMitbbsLink(link, nav, true);
            }
            else
            {
#if NODO
                WebBrowserTask webBrowser = new WebBrowserTask();
                webBrowser.URL = url;
                webBrowser.Show();
#else
                WebBrowserTask webBrowser = new WebBrowserTask();
                webBrowser.Uri = new Uri(url);
                webBrowser.Show();
#endif
            }
        }

        public static void OpenLinkInBrowser(String url)
        {
#if NODO
            WebBrowserTask webBrowser = new WebBrowserTask();
            webBrowser.URL = url;
            webBrowser.Show();
#else
                WebBrowserTask webBrowser = new WebBrowserTask();
                webBrowser.Uri = new Uri(url);
                webBrowser.Show();
#endif
        }

        public static void OpenVideoLink(String url)
        {
            WebBrowserTask webBrowser = new WebBrowserTask();
            webBrowser.URL = url;
            webBrowser.Show();
        }

        public static void OpenDownloadPage(MitbbsOfflineContentType type, String url, String name, NavigationService nav)
        {
            //String link = String.Format("/Pages/OfflineDownloadPage.xaml?Type={0}&Url={1}&Name={2}", type, Uri.EscapeDataString(url), Uri.EscapeDataString(name));
            //nav.Navigate(new Uri(link, UriKind.Relative));

            if (App.Settings.OfflineContentManager.AddContentToQueue(url, name, type))
            {
                App.Settings.RestoreDownload();

                if (App.Settings.CanDownloadStarts(true, true))
                {
                    MessageBox.Show("将在本程序运行的同时自动下载。如果你不想中断离线内容的下载，请不要切换到其它程序或者锁屏。如果下载被中断，将会在下次程序运行的时候自动恢复。", "此内容已加入下载队列", MessageBoxButton.OK);
                }
                //else
                //{
                //    MessageBox.Show("自动下载已被关闭，请到'离线内容'页面中手动开始下载", "此内容已加入下载队列", MessageBoxButton.OK);
                //}
            }
            else
            {
                MessageBox.Show("无法识别离线内容的类型", "无法添加离线内容到下载队列", MessageBoxButton.OK);
            }
        }

        public static void OpenDownloadPage(NavigationService nav)
        {
            String link = String.Format("/Pages/OfflineDownloadPage.xaml");
            nav.Navigate(new Uri(link, UriKind.Relative));
        }

        public static void OpenSettingPage(NavigationService nav, bool displaySettingOnly = false, bool accountSettingOnly = false)
        {
            nav.Navigate(new Uri(String.Format("/Pages/SettingPage.xaml?DisplaySettingOnly={0}&AccountSettingOnly={1}", displaySettingOnly, accountSettingOnly), UriKind.Relative));
        }

        public static void OpenSettingPageWithSiteSetting(NavigationService nav)
        {
            nav.Navigate(new Uri(String.Format("/Pages/SettingPage.xaml?SiteSettingOn=true"), UriKind.Relative));
        }

        public static bool SessionHistoryHandleNavigateTo(NavigationService nav)
        {
            App.Settings.CurrentSessionHistory.AddPageToHistory(nav);

            if (App.Settings.PreviousSessionHistory.RestoringHistory)
            {
                return true;
            }

            return false;
        }

        public static bool SessionHistoryHandlePageLoaded(NavigationService nav)
        {
            return App.Settings.PreviousSessionHistory.TryRestoreFromHistory(nav);
        }

        public static bool SessionHistoryHandleNavigateFrom(NavigationService nav)
        {
            if (App.Settings.PreviousSessionHistory.LastTrySucceeded)
            {
                return true;
            }

            return false;
        }

        public static  String GetBoardName(this MitbbsLink link)
        {
            String boardNameTemplate = @"(?<1>.*)\((?<2>.*)\)";

            Match match = Regex.Match(link.Name, boardNameTemplate);
            if (match.Success)
            {
                return match.Groups[1].ToString();
            }
            else
            {
                return link.Name;
            }
        }

        public static String GetFilteredEssenseItemName(String name)
        {
            String template = "\\[(?<1>.*)\\](?<2>.*)";

            Match match = Regex.Match(name.Trim(), template);
            if (match.Success)
            {
                return match.Groups[2].Value.Trim();
            }

            return name;
        }

        public static void TryMyLuck(NavigationService nav)
        {
            MitbbsLink boardLink = MitbbsBoardSearch.Instance.GetRandomBoard(true);
            if (boardLink != null)
            {
                App.Track("Navigation", "EntryPoint", "I-Feel-Lucky");
                PageHelper.OpenMitbbsLink(boardLink, nav);
            }
            else
            {
                MessageBox.Show("请稍后再试", "正在创建版面列表的索引", MessageBoxButton.OK);
            }
        }

        public static void SendFeedbackAboutContent(String originalUrl, String url)
        {
            String urls;
            if (url == originalUrl)
            {
                urls = url;
            }
            else
            {
                urls = originalUrl + "\n" + url;
            }

            App.TheAppInfo.SendFeedback("Feedback on MITBBS Reader App", "There may be a problem reading this content: \n" + urls);
        }
    }

    public class UrlHistoryItem
    {
        public String Url = null;
        public double ScrollOffset = 0;
        public object Data = null;
        public String Name = null;
    }

    public class UrlHistory : Stack<UrlHistoryItem>
    {
        public void SaveState(IDictionary<string, object> state, String key)
        {
            UrlHistoryItem [] items = this.ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                state[key + "_Url_" + i] = items[i].Url;
                state[key + "_ScrollOffset_" + i] = items[i].ScrollOffset;
            }
        }

        public void LoadState(IDictionary<string, object> state, String key)
        {
            Clear();

            int i = 0;
            while (state.ContainsKey(key + "_Url_" + i) && state.ContainsKey(key + "_ScrollOffset_" + i))
            {
                UrlHistoryItem item = new UrlHistoryItem();
                
                item.Url = (String)state[key + "_Url_" + i];
                item.ScrollOffset = (double)state[key + "_ScrollOffset_" + i];
                item.Data = null;

                Push(item);
                i++;
            }
        }
    }

    public class LinkStateColorMapper : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            MitbbsLinkState linkState = (MitbbsLinkState)value;

            if (linkState == MitbbsLinkState.Selected)
            {
                //return new SolidColorBrush(Color.FromArgb(255, 0, 162, 232));
                return (Brush)App.Current.Resources["PhoneAccentBrush"];
            }
            else if (linkState == MitbbsLinkState.InHistory)
            {
                return (Brush)App.Current.Resources["PhoneSubtleBrush"];
            }
            else
            {
                return (Brush)App.Current.Resources["PhoneForegroundBrush"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class VisibilityStateMapper : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool visible = (bool)value;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class HistoryStatusColorMapper : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                return (Brush)App.Current.Resources["PhoneSubtleBrush"];
            }
            else
            {
                return (Brush)App.Current.Resources["PhoneForegroundBrush"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
