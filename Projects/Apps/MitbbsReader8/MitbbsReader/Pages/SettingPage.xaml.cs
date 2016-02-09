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
using Microsoft.Phone.Tasks;

namespace Naboo.MitbbsReader.Pages
{
    public partial class SettingPage : PhoneApplicationPage
    {
        private bool _siteSettingOn = false;
        private bool _displaySettingOnly = false;
        private bool _accountSettingOnly = false;
        private bool _extraPageDeleted = false;
        MitbbsUserInfo _selectedUser = null;
        MitbbsUserInfo _lastLogOnUser = null;

        public SettingPage()
        {
            InitializeComponent();

            UserListBox.ItemsSource = App.Settings.Users;
            SiteListBox.ItemsSource = App.Settings.Sites;

            App.Settings.ApplyPageSettings(this, LayoutRoot, false);
            App.Track("Navigation", "OpenPage", "Settings");
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            var parameters = NavigationContext.QueryString;

            if (parameters.ContainsKey("SiteSettingOn"))
            {
                _siteSettingOn = bool.Parse(parameters["SiteSettingOn"]);
            }

            if (!_siteSettingOn)
            {
                if (parameters.ContainsKey("DisplaySettingOnly"))
                {
                    _displaySettingOnly = bool.Parse(parameters["DisplaySettingOnly"]);
                }
                else
                {
                    _displaySettingOnly = false;
                }

                if (parameters.ContainsKey("AccountSettingOnly"))
                {
                    _accountSettingOnly = bool.Parse(parameters["AccountSettingOnly"]);
                }
                else
                {
                    _accountSettingOnly = false;
                }
            }

            if (!_extraPageDeleted)
            {
#if NODO
                MiniAppbarSwitch.Visibility = Visibility.Collapsed;
                ShareInfoSwitch.Visibility = Visibility.Collapsed;
#endif

                if (!App.IsTrial)
                {
                    AdSwitch.Visibility = Visibility.Collapsed;
                    UseLocationSwitch.Visibility = Visibility.Collapsed;
                }

                if (_siteSettingOn)
                {
                }
                else
                {
                    PivotControl.Items.RemoveAt(0); //remove the site setting

                    if (_displaySettingOnly)
                    {
                        if (PivotControl.Items.Count >= 4)
                        {
                            PivotControl.Items.RemoveAt(0);
                        }
                    }
                    else if (_accountSettingOnly)
                    {
                        while (PivotControl.Items.Count > 1)
                        {
                            PivotControl.Items.RemoveAt(PivotControl.Items.Count - 1);
                        }
                    }
                }

                _extraPageDeleted = true;
            }

            base.OnNavigatedTo(e);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_displaySettingOnly)
            {
                if (App.WebSession.IsConnecting)
                {
                    NavigationService.GoBack();
                    return;
                }
            }

            if (App.IsTrial)
            {
                AdSwitch.IsChecked = true;
                UseLocationSwitch.IsChecked = App.Settings.UseLocationForAds;
            }

            SystemTraySwitch.IsChecked = App.Settings.ShowSystemTray;
            ReadingHistorySwitch.IsChecked = App.Settings.KeepHistory;
            HideQuoteSwitch.IsChecked = App.Settings.HideFullQuote;
            PreloadSwitch.IsChecked = App.Settings.Preload;
            MiniAppbarSwitch.IsChecked = App.Settings.MiniAppbar;
            ShareInfoSwitch.IsChecked = App.Settings.ShareInfo;
            RestoreLastVisitSwitch.IsChecked = App.Settings.RestoreLastVisit;
            HideTopSwitch.IsChecked = App.Settings.HideTop;
            AutoDownloadSwitch.IsChecked = App.Settings.AutoStartDownload;
            WifiDownloadOnlySwitch.IsChecked = App.Settings.DownloadUnderWifiOnly;
            AutoCheckSwitch.IsChecked = App.Settings.AutoCheckUpdate;
            AppendSentFromSwitch.IsChecked = App.Settings.AppendSentFrom;
            SiteListBox.SelectedItem = App.Settings.Site;

            switch (App.Settings.OrientationMode)
            {
                case SupportedPageOrientation.PortraitOrLandscape:
                    OrientationPicker.SelectedIndex = 0;
                    break;
                case SupportedPageOrientation.Portrait:
                    OrientationPicker.SelectedIndex = 1;
                    break;
                case SupportedPageOrientation.Landscape:
                    OrientationPicker.SelectedIndex = 2;
                    break;
            }

            switch (App.Settings.Theme.ThemeType)
            {
                case MitbbsCustomTheme.CustomThemeType.DefaultTheme:
                    ThemePicker.SelectedIndex = 0;
                    break;
                case MitbbsCustomTheme.CustomThemeType.DarkTheme:
                    ThemePicker.SelectedIndex = 1;
                    break;
                case MitbbsCustomTheme.CustomThemeType.LightTheme:
                    ThemePicker.SelectedIndex = 2;
                    break;
            }

            switch (App.Settings.Theme.FontSize)
            {
                case MitbbsCustomTheme.CustomFontSize.Small:
                    FontSizePicker.SelectedIndex = 0;
                    break;
                case MitbbsCustomTheme.CustomFontSize.Medium:
                    FontSizePicker.SelectedIndex = 1;
                    break;
                case MitbbsCustomTheme.CustomFontSize.Large:
                    FontSizePicker.SelectedIndex = 2;
                    break;
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (DisableRect.Visibility == Visibility.Visible)
            {
                e.Cancel = true;
            }
            else
            {
                base.OnBackKeyPress(e);
            }
        }

        private void LogOnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser != null)
            {
                if (UsernameTextBox.IsEnabled)
                {
                    if (String.IsNullOrEmpty(UsernameTextBox.Text))
                    {
                        MessageBox.Show("用户名不能为空！");
                        return;
                    }

                    _selectedUser.Username = UsernameTextBox.Text;
                    _selectedUser.Password = PasswordTextBox.Password;
                }

                DisableRect.Visibility = Visibility.Visible;
                LogOnProgressBar.Visibility = Visibility.Visible;
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false; //save button
                (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false; //close button

                if (App.WebSession.IsLoggedIn)
                {
                    MitbbsUserInfo defaultUser = App.Settings.DefaultUser;
                    _lastLogOnUser = defaultUser;

                    App.UserHome.ClearContent();
                    App.WebSession.LogOutCompleted += OnLogOutCompleted;
                    App.WebSession.StartLogOut();
                }
                else
                {
                    App.WebSession.LogInCompleted += OnLogOnCompleted;
                    App.UserHome.ClearContent();

                    App.WebSession.StartLogIn(_selectedUser.Username, _selectedUser.Password);
                }
            }
        }

        private void OnLogOnCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            App.WebSession.LogInCompleted -= OnLogOnCompleted;
            
            if (args.Success)
            {
                App.Settings.LogOn = true;
                App.Settings.SetDefaultUser(_selectedUser);

                UsernameTextBox.IsEnabled = false;
                PasswordTextBox.IsEnabled = false;
                LogOnButton.Content = "退出";
                DeleteUserButton.IsEnabled = false;
                SaveUserButton.IsEnabled = false;

                UserListBox.SelectedItem = null;
            }
            else
            {
                App.Settings.SetDefaultUser(null);

                App.Settings.LogOn = false;
                MessageBox.Show("登录不成功!");
            }

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //save button
            (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //close button

            DisableRect.Visibility = Visibility.Collapsed;
            LogOnProgressBar.Visibility = Visibility.Collapsed;
        }

        private void OnLogOutCompleted(object sender, MitbbsWebSessionEventArgs args)
        {
            App.WebSession.LogOutCompleted -= OnLogOutCompleted;
            
            App.Settings.LogOn = false;
            UsernameTextBox.IsEnabled = true;
            PasswordTextBox.IsEnabled = true;
            LogOnButton.Content = "登录";
            DeleteUserButton.IsEnabled = true;
            SaveUserButton.IsEnabled = true;

            if (_selectedUser != null && _selectedUser != _lastLogOnUser)
            {
                App.WebSession.LogInCompleted += OnLogOnCompleted;
                App.UserHome.ClearContent();
                App.WebSession.StartLogIn(_selectedUser.Username, _selectedUser.Password);
            }
            else
            {
                App.Settings.SetDefaultUser(null);

                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true; //save button
                (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = true; //close button

                DisableRect.Visibility = Visibility.Collapsed;
                LogOnProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (App.IsTrial)
            {
                App.Settings.UseLocationForAds = UseLocationSwitch.IsChecked.Value;
            }

            App.Settings.ShowSystemTray = (bool)SystemTraySwitch.IsChecked;
            App.Settings.KeepHistory = (bool)ReadingHistorySwitch.IsChecked;

            if (App.Settings.HideFullQuote != HideQuoteSwitch.IsChecked)
            {
                App.ForceRefreshContent = true;
            }

            App.Settings.HideFullQuote = (bool)HideQuoteSwitch.IsChecked;
            App.Settings.Preload = (bool)PreloadSwitch.IsChecked;
            App.Settings.ShareInfo = (bool)ShareInfoSwitch.IsChecked;
            App.Settings.RestoreLastVisit = (bool)RestoreLastVisitSwitch.IsChecked;
            App.Settings.HideTop = (bool)HideTopSwitch.IsChecked;
            App.Settings.AutoStartDownload = (bool)AutoDownloadSwitch.IsChecked;
            App.Settings.DownloadUnderWifiOnly = (bool)WifiDownloadOnlySwitch.IsChecked;
            App.Settings.AutoCheckUpdate = (bool)AutoCheckSwitch.IsChecked;
            
            if (!App.License.IsTrial)
            {
                App.Settings.AppendSentFrom = (bool)AppendSentFromSwitch.IsChecked;
            }

            if (App.Settings.CanDownloadStarts())
            {
                App.Settings.RestoreDownload();
            }
            else
            {
                App.Settings.StopDownload();
            }

            if (App.Settings.AutoCheckUpdate)
            {
                App.Settings.NotficationCenter.StartTimer();
            }
            else
            {
                App.Settings.NotficationCenter.StopTimer();
            }

#if !NODO
            if (App.Settings.MiniAppbar != (bool)MiniAppbarSwitch.IsChecked)
            {
                App.Settings.MiniAppbar = (bool)MiniAppbarSwitch.IsChecked;
                MessageBox.Show("隐藏按钮设置只会在新打开的页面中生效");
            }
#endif

            switch (OrientationPicker.SelectedIndex)
            {
                case 0:
                    App.Settings.OrientationMode = SupportedPageOrientation.PortraitOrLandscape;
                    break;
                case 1:
                    App.Settings.OrientationMode = SupportedPageOrientation.Portrait;
                    break;
                case 2:
                    App.Settings.OrientationMode = SupportedPageOrientation.Landscape;
                    break;
            }

            switch (ThemePicker.SelectedIndex)
            {
                case 0:
                    App.Settings.Theme.ThemeType = MitbbsCustomTheme.CustomThemeType.DefaultTheme;
                    break;
                case 1:
                    App.Settings.Theme.ThemeType = MitbbsCustomTheme.CustomThemeType.DarkTheme;
                    break;
                case 2:
                    App.Settings.Theme.ThemeType = MitbbsCustomTheme.CustomThemeType.LightTheme;
                    break;
            }

            MitbbsCustomTheme.CustomFontSize newFontSize = MitbbsCustomTheme.CustomFontSize.Medium;
            switch (FontSizePicker.SelectedIndex)
            {
                case 0:
                    newFontSize = MitbbsCustomTheme.CustomFontSize.Small;
                    break;
                case 1:
                    newFontSize = MitbbsCustomTheme.CustomFontSize.Medium;
                    break;
                case 2:
                    newFontSize = MitbbsCustomTheme.CustomFontSize.Large;
                    break;
            }

            if (newFontSize != App.Settings.Theme.FontSize)
            {
                App.Settings.Theme.FontSize = newFontSize;
                MessageBox.Show("字体大小设置只会在新打开的页面中生效");
                //MessageBoxResult result = MessageBox.Show("某些页面的字体将在你重新运行此程序之后才会更新。你想现在关闭程序吗？", "你已更改字体大小", MessageBoxButton.OKCancel);
                //if (result == MessageBoxResult.OK)
                //{
                //    App.NeedToExit = true; ;
                //}
            }

            if (SiteListBox.SelectedItem != App.Settings.Site)
            {
                App.MitbbsHome = new MitbbsHome() ;
                App.UserHome = new MitbbsUserHome();
                App.Settings.SiteIndex = SiteListBox.SelectedIndex;
                App.Settings.OfflineContentManager.CleanupOfflineContent(App.Settings.BoardGroupPreloadOfflineID);
                
                if (App.Settings.LogOn)
                {
                    App.WebSession.StartLogIn(App.Settings.Username, App.Settings.Password);
                }
            }

            App.Settings.SaveToStorage();
            NavigationService.GoBack();
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            NavigationService.GoBack();
        }

        private void UsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedUser != null)
            {
                if ((UsernameTextBox.Text != _selectedUser.Username) && (_selectedUser.Username != ""))
                {
                    PasswordTextBox.Password = "";
                }
            }
        }

        private void AdSwitch_Click(object sender, RoutedEventArgs e)
        {
            App.License.ShowTurnOffAdMessage();

            AdSwitch.IsChecked = true;
        }

        private void UserListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedUser = (sender as ListBox).SelectedItem as MitbbsUserInfo;

            if (_selectedUser != null)
            {
                UsernameTextBox.Text = _selectedUser.Username;
                PasswordTextBox.Password = _selectedUser.Password;

                UserEditPanel.Visibility = System.Windows.Visibility.Visible;
                
                if (_selectedUser.IsDefault && App.WebSession.IsLoggedIn)
                {
                    UsernameTextBox.IsEnabled = false;
                    PasswordTextBox.IsEnabled = false;
                    SaveUserButton.IsEnabled = false;
                    DeleteUserButton.IsEnabled = false;
                    LogOnButton.Content = "退出";
                }
                else
                {
                    UsernameTextBox.IsEnabled = true;
                    PasswordTextBox.IsEnabled = true;
                    SaveUserButton.IsEnabled = true;
                    DeleteUserButton.IsEnabled = true;

                    LogOnButton.Content = "登录";

                    UsernameTextBox.Focus();
                    UsernameTextBox.SelectAll();
                }
            }
            else
            {
                UserEditPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void SaveUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser != null)
            {
                if (String.IsNullOrEmpty(UsernameTextBox.Text))
                {
                    MessageBox.Show("用户名不能为空！");
                    return;
                }

                _selectedUser.Username = UsernameTextBox.Text;
                _selectedUser.Password = PasswordTextBox.Password;

                _selectedUser = null;
                UserListBox.SelectedItem = null;

                UserEditPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser != null)
            {
                App.Settings.Users.Remove(_selectedUser);

                _selectedUser = null;
                UserListBox.SelectedItem = null;

                UserEditPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            MitbbsUserInfo newUser =
                new MitbbsUserInfo()
                {
                    Username = "username",
                    Password = "",
                    IsDefault = false
                };

            App.Settings.Users.Add(newUser);

#if NODO
            UserListBox.SelectedItem = null;
#else
            UserListBox.SelectedItem = newUser;
#endif
        }

        private void AppendSentFromSwitch_Click(object sender, RoutedEventArgs e)
        {
            if (App.License.IsTrial)
            {
                AppendSentFromSwitch.IsChecked = true;
                MessageBoxResult result = MessageBox.Show("只有在收费的正式版本中才能改变此选项。", "你现在使用的是免费使用版本", MessageBoxButton.OK);
            }
        }
    }
}