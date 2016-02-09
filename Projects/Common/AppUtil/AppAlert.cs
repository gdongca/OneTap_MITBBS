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
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Naboo.AppUtil
{
    public class AlertBlock
    {
        public class VersionInfo
        {
            [XmlAttribute("LicenseModes")]
            public String LicenseModes { get; set; }

            [XmlAttribute("Version")]
            public String Version { get; set; }
        }

        public class MessageInfo
        {
            [XmlAttribute("LicenseModes")]
            public String LicenseModes { get; set; }

            [XmlAttribute("MinimumVersion")]
            public String MinimumVersion { get; set; }

            [XmlAttribute("MaximumVersion")]
            public String MaximumVersion { get; set; }

            [XmlAttribute("Message")]
            public String Message { get; set; }
        }

        public List<VersionInfo> LatestVersions { get; set; }
        public List<MessageInfo> Messages { get; set; }

        public AlertBlock()
        {
            LatestVersions = new List<VersionInfo>();
            Messages = new List<MessageInfo>();
        }

        public String GetLatestVersion(String licenseMode)
        {
            foreach (var entry in LatestVersions)
            {
                if (MatchLicenseModes(licenseMode, entry.LicenseModes))
                {
                    return entry.Version;
                }
            }

            return null;
        }

        public bool IsItLatestVersion(String licenseMode, String version, out String latestVer)
        {
            latestVer = GetLatestVersion(licenseMode);
            if (latestVer == null)
            {
                return true;
            }

            uint ver = AppInfo.ConvertVersionToInt(version, 4);
            uint lver = AppInfo.ConvertVersionToInt(latestVer, 4);

            return ver >= lver;
        }

        public String[] GetMessages(String licenseMode, String version)
        {
            List<String> result = new List<string>();

            foreach (var entry in Messages)
            {
                if (MatchLicenseModes(licenseMode, entry.LicenseModes))
                {
                    uint minVer = AppInfo.ConvertVersionToInt(entry.MinimumVersion, 4);
                    uint maxVer = AppInfo.ConvertVersionToInt(entry.MaximumVersion, 4);
                    uint ver = AppInfo.ConvertVersionToInt(version, 4);

                    if (ver >= minVer && ver <= maxVer)
                    {
                        result.Add(entry.Message);
                    }
                }
            }

            return result.ToArray();
        }

        public bool MatchLicenseModes(String mode, String modeList)
        {
            mode = mode.Trim().ToLower();
            String[] modes = modeList.Split(';');
            foreach (var entry in modes)
            {
                String entry2 = entry.Trim().ToLower();

                if (MatchWildcards(entry2, mode))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MatchWildcards(string pattern, string text)
        {
            string pattern2 = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return Regex.IsMatch(text, pattern2, RegexOptions.IgnoreCase);
        }
    }

    public class AppAlert
    {
        public bool IsAlertLoaded { get; private set; }
        public AlertBlock AlertInfo { get; private set; }

        public event EventHandler<DataLoadedEventArgs> AlertLoaded;

        private AppLicense _license;
        private AppInfo _appInfo;
        
        public AppAlert(AppLicense license, AppInfo appInfo)
        {
            _license = license;
            _appInfo = appInfo;
        }

        public void FetchAlertPage(String url, bool showAlert)
        {
            if (String.IsNullOrEmpty(url))
            {
                return;
            }

            IsAlertLoaded = false;
            var request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.BeginGetResponse(
                r =>
                {
                    try
                    {
                        var httpRequest = (HttpWebRequest)r.AsyncState;
                        var httpResponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                        var settingsStream = httpResponse.GetResponseStream();

                        var s = new XmlSerializer(typeof(AlertBlock));
                        AlertInfo = (AlertBlock)s.Deserialize(settingsStream);
                        IsAlertLoaded = true;

                        if (showAlert)
                        {
                            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(
                                () =>
                                {
                                    ShowAlerts();
                                }
                                );
                        }
                    }
                    catch
                    {
                        AlertInfo = null;
                    }
                },
                request
                );
        }

        public void ShowAlerts()
        {
            if (!IsAlertLoaded || AlertInfo == null)
            {
                return;
            }

            string latestVer;
            if (!AlertInfo.IsItLatestVersion(_license.Mode, _appInfo.GetAppVersionString(), out latestVer))
            {
                MessageBoxResult result = MessageBox.Show(String.Format("最新的版本号是'{0}'。你想现在就下载最新的版本吗？", latestVer), "此程序有更新的版本可供下载", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    _license.GoToApp();
                    return;
                }
            }

            String[] messages = AlertInfo.GetMessages(_license.Mode, _appInfo.GetAppVersionString());
            foreach (String msg in messages)
            {
                MessageBox.Show(msg, "重要信息", MessageBoxButton.OK);
            }
        }
    }
}
