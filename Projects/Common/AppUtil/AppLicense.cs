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
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Marketplace;

namespace Naboo.AppUtil
{
    public class AppLicense
    {
        public String FreeAppMessageTitle = "You are using the free version";
        public String FreeAppMessage = "The free version has the full functionality and is subsidized by advertising. If you don't like to see Ads, please go to \"About\" page to purchase the full version.";
        public String FreeAppMessageWithPurchase = "The free version has the full functionality and is subsidized by advertising. If you don't like to see Ads, please purchase the full version. Do you want to go to Marketplace to purchase now?";
        public String FreeAppTurnOffAdMessage;
        public String FreeAppPostfix = " Free";
        public String TrialAppMessageTitle = "You are using the trial version";
        public String TrialAppMessage = "The trial version has the full functionality and is subsidized by advertising. If you don't like to see Ads, please go to \"About\" page to purchase the full version.";
        public String TrialAppMessageWithPurchase = "The trial version has the full functionality and is subsidized by advertising. If you don't like to see Ads, please purchase the full version. Do you want to go to Marketplace to purchase now?";
        public String TrialAppTurnOffAdMessage;
        public String TrialAppPostfix = " Trial";

        public String AppTitlePrefix;
        public String PaidAppSearchKeywords;
        public String PaidAppID;
        public bool IsTrial { get; private set; }
        public bool IsFreeApp { get; private set; }

        public String Mode
        {
            get
            {
                String postfix = "";

#if NODO
                postfix = "-NoDo";
#endif

                if (IsFreeApp)
                {
                    return "Free" + postfix;
                }

                if (IsTrial)
                {
                    return "Trial" + postfix;
                }

                return "Paid" + postfix;
            }
        }
        
        public AppLicense(String appTitlePrefix, String paidAppSearchKeywords, String paidAppID)
        {
            FreeAppTurnOffAdMessage = FreeAppMessage;
            TrialAppTurnOffAdMessage = TrialAppMessage;

            AppTitlePrefix = appTitlePrefix;
            PaidAppSearchKeywords = paidAppSearchKeywords;
            PaidAppID = paidAppID;

#if FREE_APP
            IsFreeApp = true;
#else
            IsFreeApp = false;
#endif

            IsTrial = false;
        }

        public void RefreshTrialState()
        {
            if (IsFreeApp)
            {
                IsTrial = true;
                return;
            }

#if DEBUG
            IsTrial = true;
#else
            LicenseInformation licenseInfo = new LicenseInformation();
            IsTrial = licenseInfo.IsTrial();
#endif
        }

        public void ShowTurnOffAdMessage()
        {
            if (IsFreeApp)
            {
                MessageBox.Show(FreeAppTurnOffAdMessage, FreeAppMessageTitle, MessageBoxButton.OK);
            }
            else
            {
                MessageBox.Show(TrialAppTurnOffAdMessage, TrialAppMessageTitle, MessageBoxButton.OK);
            }
        }

        public void ShowTrialMessage(bool linkToPurchase)
        {
            if (!IsTrial)
            {
                return;
            }

            if (IsFreeApp)
            {
                if (linkToPurchase)
                {
                    MessageBoxResult result = MessageBox.Show(FreeAppMessageWithPurchase, FreeAppMessageTitle, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        GoToPurchase();
                    }
                }
                else
                {
                    MessageBox.Show(FreeAppMessage, FreeAppMessageTitle, MessageBoxButton.OK);
                }
            }
            else
            {
                if (linkToPurchase)
                {
                    MessageBoxResult result = MessageBox.Show(TrialAppMessageWithPurchase, TrialAppMessageTitle, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        GoToPurchase();
                    }
                }
                else
                {
                    MessageBox.Show(TrialAppMessage, TrialAppMessageTitle, MessageBoxButton.OK);
                }
            }
        }

        public void GoToPurchase()
        {
            if (!IsTrial)
            {
                return;
            }

            if (IsFreeApp)
            {
                if (!String.IsNullOrEmpty(PaidAppID))
                {
                    WebBrowserTask webBrowser = new WebBrowserTask();
                    webBrowser.URL = "http://windowsphone.com/s?appId=" + PaidAppID;
                    webBrowser.Show();
                }
                else if (!String.IsNullOrEmpty(PaidAppSearchKeywords))
                {
                    MarketplaceSearchTask searchTask = new MarketplaceSearchTask();
                    searchTask.SearchTerms = PaidAppSearchKeywords;
                    searchTask.Show();
                }
            }
            else
            {
                GoToApp();
            }
        }

        public void GoToApp()
        {
            MarketplaceDetailTask mpdTask = new MarketplaceDetailTask();
            mpdTask.Show();
        }

        public void ShowAllApps()
        {
            MarketplaceSearchTask searchTask = new MarketplaceSearchTask();
            searchTask.SearchTerms = "Project Naboo";
            searchTask.Show();
        }

        public String AppTitle
        {
            get
            {
                if (IsFreeApp)
                {
                    return AppTitlePrefix + FreeAppPostfix;
                }

                if (IsTrial)
                {
                    return AppTitlePrefix + TrialAppPostfix;
                }

                return AppTitlePrefix;
            }
        }
    }
}
