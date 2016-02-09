using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using HtmlAgilityPack;

namespace Naboo.MitbbsReader.Pages
{
    public partial class ForwardPostPage : PhoneApplicationPage
    {
        private String _url;
        private MitbbsPostForward _postForward = new MitbbsPostForward();
        private ImageLoader _imageLoader = new ImageLoader();
        
        public ForwardPostPage()
        {
            InitializeComponent();

#if DEBUG
#else
            ApplicationBar.MenuItems.RemoveAt(0); //remove the "open in browser" menu item
#endif

            App.Settings.ApplyPageSettings(this, LayoutRoot, false);

            _imageLoader.DisplayPanel = VerifyImagePanel;
            _imageLoader.ScrollPanel = null;
            _imageLoader.Page = this;
            _imageLoader.FontSize = (double)App.Current.Resources["MitbbsFontSizeText"];
            _imageLoader.ShowButtons = false;
            _imageLoader.CacheImage = false;

            App.Track("Navigation", "OpenPage", "ForwardPost");

            _postForward.InputPageLoaded += InputPage_Loaded;
            _postForward.ForwardCompleted += Forward_Completed;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            PageHelper.InitAdControl(AdGrid);

            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("Url"))
            {
                _url = parameters["Url"];
            }
            else
            {
                _url = null;
            }

            if (parameters.ContainsKey("Title"))
            {
                TitleText.Text = "标题: " + parameters["Title"];
            }

            if (parameters.ContainsKey("Author"))
            {
                AuthorText.Text = "作者: " + parameters["Author"];
            }

            if (State.ContainsKey("BoardName"))
            {
                BoardNameTextBox.Text = (String)State["BoardName"];
            }

            if (State.ContainsKey("DestType"))
            {
                DestPicker.SelectedIndex = (int)State["DestType"];
            }
            else
            {
                DestPicker.SelectedIndex = 0;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            PageHelper.CleanupAdControl(AdGrid);

            State["Url"] = _url;
            State["BoardName"] = BoardNameTextBox.Text;
            State["DestType"] = DestPicker.SelectedIndex;

            base.OnNavigatedFrom(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_postForward.IsInputPageLoaded)
            {
                return;
            }

            if (!App.WebSession.IsLoggedIn)
            {
                if (App.WebSession.IsConnecting)
                {
                    (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //send button
                    (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //close button

                    LoadProgressBar.Visibility = Visibility.Visible;
                    DisableRect.Visibility = Visibility.Visible;

                    App.WebSession.LogInCompleted += OnLogOnCompleted;
                    App.WebSession.LogOutCompleted += OnLogOnCompleted;
                }
                else
                {
                    MessageBox.Show("用户未登录！");
                    NavigationService.GoBack();
                }

                return;
            }

            ApplicationTitle.Text = App.License.AppTitle + " (" + App.WebSession.Username + ")";

            LoadInputPage();
        }

        private void OnLogOnCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            App.WebSession.LogInCompleted -= OnLogOnCompleted;
            App.WebSession.LogOutCompleted -= OnLogOnCompleted;

            ApplicationTitle.Text = App.License.AppTitle + " (" + App.WebSession.Username + ")";

            if (App.WebSession.IsLoggedIn)
            {
                LoadInputPage();
            }
            else
            {
                MessageBox.Show("用户未登录！");
                NavigationService.GoBack();
            }
        }

        private void LoadInputPage()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //forward button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //close button

            LoadProgressBar.Visibility = Visibility.Visible;
            DisableRect.Visibility = Visibility.Visible;

            if (_url != null)
            {
                _postForward.LoadInputPage(App.WebSession.CreateWebClient(), _url);
            }
            else
            {
                PageTitle.Text = "参数错误!";
            }
        }

        private void InputPage_Loaded(object sender, DataLoadedEventArgs e)
        {
            LoadProgressBar.Visibility = Visibility.Collapsed;
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //close button

            if (_postForward.IsInputPageLoaded)
            {
                CookieAwareClient web = new CookieAwareClient();
                web.Cookies = App.WebSession.Cookies;
                _imageLoader.Web = web;

                DisableRect.Visibility = Visibility.Collapsed;

                if (BoardNameTextBox.Text == "")
                {
                    BoardNameTextBox.Text = _postForward.BoardName;
                }

                _imageLoader.ClearImages();

                if (_postForward.VerifyImageUrl != null)
                {
                    _imageLoader.LoadImage(
                                            _postForward.VerifyImageUrl,
                                            _postForward.InputPageUrl,
                                            null,
                                            false,
                                            false,
                                            "<正在打开验证码图片...>",
                                            "<无法开打验证码图片>"
                                            );

                    VerifyCodeTextBox.Text = "";
                    VerifyPanel.Visibility = Visibility.Visible;
                }
                else
                {
                    VerifyPanel.Visibility = Visibility.Collapsed;
                }

                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //forward button
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("是否要重试？如果你已多次遇到此错误，请尝试重新登录。", "读取转帖页面失败！", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    LoadInputPage();
                }
                else
                {
                    NavigationService.GoBack();
                }
            }
        }

        private void ForwardPost()
        {
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //forward button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //close button

            LoadProgressBar.Visibility = Visibility.Visible;
            DisableRect.Visibility = Visibility.Visible;

            _postForward.BoardName = BoardNameTextBox.Text;

            if (DestPicker.SelectedIndex == 1)
            {
                _postForward.Mode = MitbbsPostForward.ForwardMode.Club;
            }
            else
            {
                _postForward.Mode = MitbbsPostForward.ForwardMode.Board;
            }

            if (_postForward.VerifyImageUrl != null)
            {
                _postForward.VerifyCode = VerifyCodeTextBox.Text;
            }

            _postForward.ForwardPost();
        }

        private void Forward_Completed(object sender, DataLoadedEventArgs e)
        {
            LoadProgressBar.Visibility = Visibility.Collapsed;
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //close button

            if (_postForward.IsPostForwarded)
            {
                NavigationService.GoBack();
            }
            else
            {
                MessageBox.Show("请确认版面名字输入正确，并选择正确的版面类型。如果需要输入验证码，请确认验证码输入正确。", "转帖失败！", MessageBoxButton.OK);
                LoadInputPage();
            }
        }

        private void ForwardButton_Click(object sender, EventArgs e)
        {
            ForwardPost();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void OpenInBrowserMenu_Click(object sender, EventArgs e)
        {
            PageHelper.OpenLinkInBrowser(_url);
        }

        private void PhoneApplicationPage_Loaded_1(object sender, RoutedEventArgs e)
        {

        }

        private void FeedbackMenu_Click(object sender, EventArgs e)
        {
            PageHelper.SendFeedbackAboutContent("", _url);
        }
    }
}