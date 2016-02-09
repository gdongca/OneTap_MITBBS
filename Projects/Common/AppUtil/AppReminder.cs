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
using System.Xml.Serialization;
using Microsoft.Phone.Tasks;

namespace Naboo.AppUtil
{
    public class AppReminder
    {
        [XmlIgnore]
        public String RatingMessage = "If you like this App, please go to Marketplace to give it a good rating. Thanks!";

        [XmlIgnore]
        public String RatingMessageTitle = "Please give me a 5-star";

        [XmlIgnore]
        public String NewFeaturesMessageTitle = "New features in this version";

        [XmlIgnore]
        public String NewFeaturesMessage = null;

        public DateTime FirstLaunchTime;
        public DateTime LastRatingRemindTime;
        public DateTime LastPurchaseRemindTime;
        public int RatingRemindCount = 0;
        public int PurchaseRemindCount = 0;
        public bool IsRated = false;
        public String AppVersion = "";

        private int _ratingRemindInterval = 5;    // days
        private int _purchaseRemindInterval = 7;  // days
        private int _maxRatingRemindCount = 5;
        private int _maxPurchaseRemindCount = 3;

        [XmlIgnore]
        public AppLicense License = null;

        [XmlIgnore]
        public AppInfo AppInfo = null;

        public AppReminder()
        {
        }

        public AppReminder(AppLicense license, AppInfo appInfo)
        {
            License = license;
            AppInfo = appInfo;
        }

        public void CheckReminder(bool remindPurchase)
        {
            if (FirstLaunchTime.Year < 2000)
            {
                FirstLaunchTime = DateTime.Now;
                if (remindPurchase)
                {
                    RemindPurchase(true);
                }
                return;
            }

            //if (AppInfo != null && AppVersion != AppInfo.GetAppVersionString())
            //{
            //    AppVersion = AppInfo.GetAppVersionString();

            //    if (!String.IsNullOrEmpty(NewFeaturesMessage))
            //    {
            //        MessageBox.Show(NewFeaturesMessage, NewFeaturesMessageTitle, MessageBoxButton.OK);
            //        return;
            //    }
            //}

            DateTime now = DateTime.Now;

            if (LastRatingRemindTime.Year < 2000)
            {
                LastRatingRemindTime = now;
            }

            if ((now - LastRatingRemindTime).Days >= _ratingRemindInterval)
            {
                RemindRating();
                return;
            }

            if ((LastPurchaseRemindTime.Year < 2000) || ((now - LastPurchaseRemindTime).Days >= _purchaseRemindInterval))
            {
                if (remindPurchase)
                {
                    RemindPurchase(false);
                }
                return;
            }
        }

        public void RemindPurchase(bool firstTime)
        {
            if (!License.IsTrial)
            {
                return;
            }

            if (PurchaseRemindCount >= _maxPurchaseRemindCount)
            {
                return;
            }

            LastPurchaseRemindTime = DateTime.Now;
            PurchaseRemindCount++;

            if (firstTime)
            {
                License.ShowTrialMessage(false);
            }
            else
            {
                License.ShowTurnOffAdMessage();
            }
        }

        public void RemindRating()
        {
            if (IsRated || (RatingRemindCount >= _maxRatingRemindCount))
            {
                return;
            }

            LastRatingRemindTime = DateTime.Now;
            RatingRemindCount++;
            MessageBoxResult result = MessageBox.Show(RatingMessage, RatingMessageTitle, MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                IsRated = true;
                MarketplaceReviewTask reviewTask = new MarketplaceReviewTask();
                reviewTask.Show();
            }
        }
    }
}
