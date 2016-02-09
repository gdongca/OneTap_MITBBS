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
using Microsoft.Phone.Tasks;
using Naboo.AppUtil;

namespace Naboo.MitbbsReader.Pages
{
    public partial class AboutPage : PhoneApplicationPage
    {
        public AboutPage()
        {
            InitializeComponent();
            DataContext = this;

            App.Settings.ApplyPageSettings(this, LayoutRoot);

            App.Track("Navigation", "OpenPage", "About");
        }

        public Visibility TrialPanelVisibility
        {
            get
            {
                //if (App.IsTrial)
                //{
                //    return Visibility.Visible;
                //}
                //else
                //{
                //    return Visibility.Collapsed;
                //}
                // Temporarily disable the full version for now
                return Visibility.Collapsed;
            }
        }

        public Visibility FullVerPanelVisibility
        {
            get
            {
                if (App.IsTrial)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.NavigationService.GoBack();
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            App.License.GoToPurchase();
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
#if CHINA
            App.TheAppInfo.SendFeedback("对未名空间程序的意见反馈");
#else
            App.TheAppInfo.SendFeedback("Feedback on MITBBS Reader App");
#endif
        }

        private void RateButton_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.Reminder.IsRated = true;
            MarketplaceReviewTask reviewTask = new MarketplaceReviewTask();
            reviewTask.Show();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            VersionText.Text = "version " + App.TheAppInfo.GetFullVerstionText();
            SiteText.Text = "site: " + App.Settings.Site.Url;

#if CHINA
            VersionText.Text = VersionText.Text + "-CN";
#endif
        }

        private void OtherAppsButton_Click(object sender, RoutedEventArgs e)
        {
            App.License.ShowAllApps();
        }

        private void PrivacyButton_Click(object sender, RoutedEventArgs e)
        {
            PageHelper.OpenLinkInBrowser("http://charmingco2.com/2011/12/02/onetap-mitbbs-reader-privacy-statement/");
        }
    }
}