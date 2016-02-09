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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Marketplace;
using Microsoft.Advertising.Mobile.UI;
using Naboo.AppUtil;
using Naboo.MitbbsReader.Pages;

namespace Naboo.MitbbsReader
{
    public class MitbbsAnalyticsService : AnalyticsService
    {
        public MitbbsAnalyticsService()
        {
#if DEBUG
            WebPropertyId = "UA-29027836-1";
#else
            WebPropertyId = "UA-29027836-2";
#endif
        }
    }

    public partial class App : Application
    {
        public static bool IsTrial
        {
            get
            {
                return License.IsTrial;
            }
        }
        
        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        public static MitbbsWebSession WebSession = new MitbbsWebSession();
        public static MitbbsSettings Settings = null;
        public static bool NeedToExit = false;
        public static bool RefreshMailbox = false;
        public static bool ForceRefreshContent = false;

        public static MitbbsHomeBase MitbbsHome = new MitbbsHome();
        public static MitbbsUserHome UserHome = new MitbbsUserHome();
        public static AppLicense License = new AppLicense("一网无际•未名空间", "OneTap MITBBS", "82937718-3300-44ca-bccc-d3aaf56df930")
        {
            FreeAppMessage = "你可以无限期地使用免费版本。在免费版本中，有些页面将会显示广告。如果你不想看到广告，请点击“关于”菜单购买正式版本。",
            FreeAppMessageWithPurchase = "在免费版本中，有些页面将会显示广告。如果你不想看到广告，请付费购买正式版本。你需要现在打开Marketplace购买正式版本吗？",
            FreeAppMessageTitle = "你现在使用的是免费版本",
            FreeAppTurnOffAdMessage = "在免费版本中，有些页面将会显示广告。如果你点击某个广告，之后一个小时之内将不会显示任何广告",
            TrialAppMessage = "你可以无限期地使用免费试用版本。在免费试用版本中，有些页面将会显示广告。如果你不想看到广告，请点击“关于”菜单购买正式版本。",
            TrialAppMessageWithPurchase = "在免费试用版本中，有些页面将会显示广告。如果你不想看到广告，请付费购买正式版本。你需要现在打开Marketplace购买正式版本吗？",
            TrialAppMessageTitle = "你现在使用的是免费试用版本",
            TrialAppTurnOffAdMessage = "在免费试用版本中，有些页面将会显示广告。如果你点击某个广告，之后一个小时之内将不会显示任何广告",
            FreeAppPostfix = "•免费版",
            TrialAppPostfix = "•试用版"
        };

        public static Dictionary<String, String> DeveloperUserNames = new Dictionary<string, string>();

        public static AdHelper AdHelper = new AdHelper(
                                                License,
                                                () => App.Settings != null ? App.Settings.UseLocationForAds : false
                                                );

        public static AppInfo TheAppInfo = new AppInfo(System.Reflection.Assembly.GetExecutingAssembly(), License);

        private bool wasRelaunched = false;
        
        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Initialize image tools 
            ImageTools.IO.Decoders.AddDecoder<ImageTools.IO.Bmp.BmpDecoder>();
            ImageTools.IO.Decoders.AddDecoder<ImageTools.IO.Gif.GifDecoder>();

            AdHelper.AdControlTemplate = new AdRotator.AdRotatorControl()
            {
                DefaultSettingsFileUri = new Uri(@"/MitbbsReader;component/defaultAdSettings.xml", UriKind.Relative),
#if DEBUG
                SettingsUrl = @"http://dl.dropbox.com/u/16498469/ProjectNaboo/MitbbsReaderAdSettingsTest.xml",
                PubCenterAdUnitId = "81994",
                AdMobAdUnitId = "a14f37265e89cc3",
                InneractiveAppId = "ProjectNABOO_OneTapMITBBSReaderTest_WP7",
#else
                SettingsUrl = @"http://dl.dropbox.com/u/16498469/ProjectNaboo/MitbbsReaderAdSettings.xml",
                PubCenterAdUnitId = "68836",
                AdMobAdUnitId = "a14f17c359ca2af",
                InneractiveAppId = "PorjectNABOO_OneTapMITBBSReader_WP7",
#endif
                DefaultAdType = AdRotator.AdType.PubCenter,
                PubCenterAppId = "cd40ad3c-3229-402a-9beb-27d8f8fe17cb",
                AdDuplexAppId = "6831"
            };

            DeveloperUserNames.Add("onetap", "onetap");
            
            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disable user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
        }

        public static void Track(string category, string name, string actionValue)
        {
#if !NODO
#if !DEBUG
            if (App.Settings.Username != null && DeveloperUserNames.ContainsKey(App.Settings.Username.ToLower()))
            {
                return;
            }
#endif

            if (!AppInfo.IsNetworkConnected())
            {
                return;
            }

            if (App.Settings.ShareInfo)
            {
                AppUtil.AnalyticsTracker.Instance.Track(category, name, actionValue);
            }
#endif
        }

        public static void Quit()
        {
            throw new QuitException();
        }

        private void LoadSettings()
        {
            Settings = MitbbsSettings.LoadFromStorage();
            if (Settings == null)
            {
                Settings = new MitbbsSettings();
            }

            AdHelper.LastTapTime = Settings.LastAdTapTime;

            //if (Settings.LogOn)
            //{
            //    //WebSession.LogInCompleted += OnLogOnCompleted;
            //    WebSession.StartLogIn(Settings.Username, Settings.Password);
            //}

            Settings.Reminder.AppInfo = TheAppInfo;
            Settings.Reminder.NewFeaturesMessageTitle = TheAppInfo.GetAppMajorVersionString() + "版本中新添加的功能:";
            Settings.Reminder.NewFeaturesMessage = "选择MITBBS站点\n";

            MitbbsBoardSearch.Instance.StartRestoreBoardList();
        }

        //private void OnLogOnCompleted(object sender, MitbbsWebSessionEventArgs args)
        //{
        //    WebSession.LogInCompleted -= OnLogOnCompleted;
        //}

        private void SaveSettings()
        {
            Settings.LastAdTapTime = AdHelper.LastTapTime;
            Settings.SaveToStorage();

            MitbbsBoardSearch.Instance.SaveToStorage();
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //IsolatedStorageExplorer.Explorer.Start("192.168.2.94");
            }
#endif

            License.RefreshTrialState();
            LoadSettings();
            App.Settings.ResetHistory();

            Microsoft.Phone.Reactive.IScheduler scheduler = Microsoft.Phone.Reactive.Scheduler.ThreadPool;
            scheduler.Schedule(new Action(
                () =>
                {
                    App.Settings.OfflineContentManager.CleanupUnindexedFiles();
                }
                ),
                TimeSpan.FromSeconds(1)
                );

#if !CHINA
            //App.Settings.Reminder.CheckReminder(false);
#endif

            App.Settings.RestoreDownload(false);
            App.Settings.NotficationCenter.StartTimer();
            
            App.Track("AppInfo", "Version", TheAppInfo.GetAppVersionString());
#if CHINA
            App.Track("AppInfo", "License", License.Mode + "-CN");
#else
            App.Track("AppInfo", "License", License.Mode);
#endif
            App.Track("Statistics", "Bookmarks", Settings.ReadingBookmarks.Count.ToString());
            App.Track("Statistics", "OfflineContents", Settings.OfflineContentManager.TotalCount.ToString());
            App.Track("Statistics", "WatchedArticles", Settings.WatchList.Count.ToString());
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
#if NODO
            License.RefreshTrialState();
            LoadSettings();
#else
            if (!e.IsApplicationInstancePreserved)
            {
                License.RefreshTrialState();
                LoadSettings();
                App.Settings.PreviousSessionHistory.Reset();
            }
            else
            {
                License.RefreshTrialState();
                if (Settings.LogOn && AppInfo.IsNetworkConnected())
                    //&& (DateTime.Now - WebSession.LastConnectTime) > new TimeSpan(0, 25, 0))
                {
                    WebSession.StartLogIn(Settings.Username, Settings.Password);
                }
            }
#endif

            App.Settings.RestoreDownload(false);
            App.Settings.NotficationCenter.StartTimer();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            App.Settings.PauseDownload();
            //App.Settings.NotficationCenter.StopTimer();

            SaveSettings();
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            App.Settings.PauseDownload();
            App.Settings.NotficationCenter.StopTimer();

            SaveSettings();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is QuitException)
            {
                return;
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }

            App.Settings.SaveToStorage();
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Assign the custom URI mapper class to the application frame.
            RootFrame.UriMapper = new CustomUriMapper();

            // Monitor deep link launching 
            RootFrame.Navigating += RootFrame_Navigating;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Event handler for the Navigating event of the root frame. Use this handler to modify
        // the default navigation behavior.
        void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {

            if (e.NavigationMode == NavigationMode.Reset)
            {
                // This block will execute if the current navigation is a relaunch.
                // If so, another navigation will be coming, so this records that a relaunch just happened
                // so that the next navigation can use this info.
                wasRelaunched = true;

                if (Settings.LogOn && AppInfo.IsNetworkConnected())
                {
                    WebSession.StartLogIn(Settings.Username, Settings.Password);
                }
            }
            else if (e.NavigationMode == NavigationMode.New && wasRelaunched)
            {
                // This block will run if the previous navigation was a relaunch
                wasRelaunched = false;

                e.Cancel = true;
            }
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        private class QuitException : Exception { }
    }
}