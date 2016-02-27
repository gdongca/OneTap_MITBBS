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
using HtmlAgilityPack;
using System.Threading;
using Naboo.AppUtil;

namespace Naboo.MitbbsReader
{
    public class MitbbsWebSession
    {
        public bool IsLoggedIn { get; private set; }

        public event EventHandler<MitbbsWebSessionEventArgs> LogInCompleted;
        public event EventHandler<MitbbsWebSessionEventArgs> LogOutCompleted;
        public DateTime LastConnectTime { get; private set; }

        private String _username;
        private String _password;
        
        private CookieContainer _cookies = new CookieContainer();
        private HtmlWeb _web = new HtmlWeb();

        private String _logInStartPageUrl = "/mwap/home/index.php";
        private String _logInPageUrl = "/mwap/login.php";
        private String _logOutPageUrl = "/mwap/logout.php";

        private Gb2312Encoding _encoding = new Gb2312Encoding();
        private uint _retries = 0;

        public CookieContainer Cookies
        {
            get
            {
                return _cookies;
            }
        }

        public String Username
        {
            get
            {
                if (IsConnecting)
                {
                    return "正在登录...";
                }
                else if (IsLoggedIn)
                {
                    return _username;
                }
                else
                {
                    return "未登录";
                }
            }
        }

        public bool IsConnecting { get; private set; }

        public MitbbsWebSession()
        {
            IsLoggedIn = false;
            _web.Cookies = _cookies;
            _web.GenerateFormElements = true;
            _web.Encoding = _encoding;

            IsConnecting = false;
            LastConnectTime = DateTime.MinValue;
        }

        public void StartLogIn(String username, String password, uint retries = 1)
        {
            bool createNewWebObj = !IsLoggedIn;

            if (IsConnecting)
            {
                MitbbsWebSessionEventArgs logInCompleteArgs = new MitbbsWebSessionEventArgs();
                logInCompleteArgs.Success = false;

                if (LogInCompleted != null)
                {
                    LogInCompleted(this, logInCompleteArgs);
                }

                return;
            }

            IsConnecting = true;

            if (IsLoggedIn)
            {
                IsLoggedIn = false;
                MitbbsWebSessionEventArgs logOutCompleteArgs = new MitbbsWebSessionEventArgs();
                logOutCompleteArgs.Success = true;

                if (LogOutCompleted != null)
                {
                    LogOutCompleted(this, logOutCompleteArgs);
                }
            }

            //_cookies = new CookieContainer();

            if (username == null)
            {
                username = "";
            }

            if (password == null)
            {
                password = "";
            }

            _username = username;
            _password = password;

            IsLoggedIn = false;

            if (createNewWebObj)
            {
                if (_web != null)
                {
                    _web.LoadCompleted -= OnLogInStartPageLoaded;
                    _web.LoadCompleted -= OnLogInPageLoaded;
                    _web.LoadCompleted -= OnLogOutPageLoaded;
                }

                _web = new HtmlWeb();
                _web.Cookies = _cookies;
                _web.GenerateFormElements = true;
                _web.Encoding = _encoding;
            }

            _retries = retries;
            _web.LoadCompleted += OnLogInStartPageLoaded;
            _web.LoadAsync(App.Settings.BuildUrl(_logInStartPageUrl));
        }

        public void StartLogOut()
        {
            if (IsConnecting)
            {
                MitbbsWebSessionEventArgs logOutCompleteArgs = new MitbbsWebSessionEventArgs();
                logOutCompleteArgs.Success = false;

                if (LogOutCompleted != null)
                {
                    LogOutCompleted(this, logOutCompleteArgs);
                }

                return;
            }

            IsConnecting = true;
            IsLoggedIn = false;
            _web.LoadCompleted += OnLogOutPageLoaded;
            _web.LoadAsync(App.Settings.BuildUrl(_logOutPageUrl));
        }

        private void OnLogInStartPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnLogInStartPageLoaded;

            if ((args.Document != null) && (_web.FormElements.ContainsKey("id")))
            {
                _web.FormElements["id"] = _username;
                _web.FormElements["passwd"] = _password;
                _web.LoadCompleted += OnLogInPageLoaded;
                _web.LoadAsync(App.Settings.BuildUrl(_logInPageUrl), HtmlWeb.OpenMode.Post);
            }
            else
            {
                MitbbsWebSessionEventArgs logInCompleteArgs = new MitbbsWebSessionEventArgs();
                logInCompleteArgs.Success = false;

                if ((args.Document != null))
                {
                    IEnumerable<HtmlNode> textNodes = args.Document.DocumentNode.Descendants("#text");
                    foreach (HtmlNode textNode in textNodes)
                    {
                        if (HtmlUtilities.GetPlainHtmlText(textNode.InnerText) == "您已经登录!")
                        {
                            // Already logged in
                            //
                            logInCompleteArgs.Success = true;
                            IsLoggedIn = true;
                            break;
                        }
                    }
                }

                IsConnecting = false;

                if (!IsLoggedIn)
                {
                    if (_retries <= 0)
                    {
                        MessageBoxResult result = MessageBox.Show("需要再重试一次吗？", "[" + _username + "]用户登录失败", MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.OK)
                        {
                            StartLogIn(_username, _password);
                            return;
                        }
                    }
                    else
                    {
                        StartLogIn(_username, _password, _retries - 1);
                        return;
                    }
                }

                if (LogInCompleted != null)
                {
                    LogInCompleted(this, logInCompleteArgs);
                }
            }
        }

        private void OnLogInPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnLogInPageLoaded;

            MitbbsWebSessionEventArgs logInCompleteArgs = new MitbbsWebSessionEventArgs();
            logInCompleteArgs.Success = false;

            if (args.Document != null)
            {
                IEnumerable<HtmlNode> textNodes = args.Document.DocumentNode.Descendants("#text");
                foreach (HtmlNode textNode in textNodes)
                {
                    String text = HtmlUtilities.GetPlainHtmlText(textNode.InnerText);
                    if (text!=null && text.Contains("家页"))
                    {
                        logInCompleteArgs.Success = true;
                        IsLoggedIn = true;
                        break;
                    }
                }
            }
            else
            {
                logInCompleteArgs.Error = args.Error;
            }

            IsConnecting = false;

            if (!IsLoggedIn)
            {
                if (_retries <= 0)
                {
                    MessageBoxResult result = MessageBox.Show("需要再重试一次吗？", "[" + _username + "]用户登录失败", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        StartLogIn(_username, _password);
                        return;
                    }
                }
                else
                {
                    StartLogIn(_username, _password, _retries - 1);
                    return;
                }
            }

            if (LogInCompleted != null)
            {
                LogInCompleted(this, logInCompleteArgs);
            }
        }

        private void OnLogOutPageLoaded(object sender, HtmlDocumentLoadCompleted args)
        {
            _web.LoadCompleted -= OnLogOutPageLoaded;

            MitbbsWebSessionEventArgs logOutCompleteArgs = new MitbbsWebSessionEventArgs();
            logOutCompleteArgs.Success = false;

            if (args.Document != null)
            {
                IsLoggedIn = false;
                logOutCompleteArgs.Success = true;
            }

            IsConnecting = false;
            if (LogOutCompleted != null)
            {
                LogOutCompleted(this, logOutCompleteArgs);
            }
        }

        public HtmlWeb CreateWebClient()
        {
            HtmlWeb client = new HtmlWeb();
            client.Cookies = _cookies;
            client.Encoding = _encoding;
            LastConnectTime = DateTime.Now;

            return client;
        }
    }

    public class MitbbsWebSessionEventArgs : EventArgs
    {
        public bool Success = false;
        public Exception Error;
    }
}
