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
using System.IO;
using System.Xml.Serialization;
using Microsoft.Advertising.Mobile.UI;
using System.Threading;
using System.IO.IsolatedStorage;
using InneractiveAdSDK;

namespace AdRotator
{
    public partial class AdRotatorControl : UserControl
    {
        private bool _loaded = false;

        private const string SETTINGS_FILE_NAME = "AdRotatorSettings";

        /// <summary>
        /// The displayed ad control instance
        /// </summary>
        private FrameworkElement _currentAdControl;

        /// <summary>
        /// Random generato
        /// </summary>
        private static Random _rnd = new Random();

        /// <summary>
        /// List of the ad types that have failed to load
        /// </summary>
        private static List<AdType> _failedAdTypes = new List<AdType>();

        /// <summary>
        /// The ad settings based on which the ad descriptor for the current UI culture can be selected
        /// </summary>
        private static AdSettings _settings;

        /// <summary>
        /// Indicates whether there has been an attemt to fetch the remote settings file
        /// </summary>
        private static bool _remoteAdSettingsFetched = false;

        #region SettingsUrl

        /// <summary>
        /// Gets or sets the URL of the remote ad descriptor file
        /// </summary>
        public string SettingsUrl
        {
            get { return (string)GetValue(SettingsUrlProperty); }
            set { SetValue(SettingsUrlProperty, value); }
        }

        public System.Device.Location.GeoCoordinate GeoLocation { get; set; }

        public static readonly DependencyProperty SettingsUrlProperty = DependencyProperty.Register("SettingsUrl", typeof(string), typeof(AdRotatorControl), new PropertyMetadata("",SettingsUrlChanged));

        private static void SettingsUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if(sender != null)
            {
                sender.OnSettingsUrlChanged(e);
            }
        }

        private void OnSettingsUrlChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }

#endregion

        #region DefaultAdType

        public AdType DefaultAdType
        {
            get { return (AdType)GetValue(DefaultAdTypeProperty); }
            set { SetValue(DefaultAdTypeProperty, value); }
        }

        public static readonly DependencyProperty DefaultAdTypeProperty = DependencyProperty.Register("DefaultAdType", typeof(AdType), typeof(AdRotatorControl), new PropertyMetadata(AdType.None, DefaultAdTypeChanged));

        private static void DefaultAdTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if(sender != null)
            {
                sender.OnDefaultAdTypeChangedChanged(e);
            }
        }

        private void OnDefaultAdTypeChangedChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }

#endregion

        #region IsEnabled

        /// <summary>
        /// When set to false the control does not display
        /// </summary>
        public new bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static new readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled", typeof(bool), typeof(AdRotatorControl), new PropertyMetadata(true, IsEnabledChanged));

        private static new void IsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnIsEnabledChangedChanged(e);
            }
        }

        private void OnIsEnabledChangedChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }

        #endregion

        #region PubCenterAppId

        public string PubCenterAppId
        {
            get { return (string)GetValue(PubCenterAppIdProperty); }
            set { SetValue(PubCenterAppIdProperty, value); }
        }

        public static readonly DependencyProperty PubCenterAppIdProperty = DependencyProperty.Register("PubCenterAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty, PubCenterAppIdChanged));

        private static void PubCenterAppIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnPubCenterAppIdChanged(e);
            }
        }

        private void OnPubCenterAppIdChanged(DependencyPropertyChangedEventArgs e)
        {
            RemoveAdFromFailedAds(AdType.PubCenter);
            Invalidate();
        }


        #endregion

        #region PubCenterAdUnitId

        public string PubCenterAdUnitId
        {
            get { return (string)GetValue(PubCenterAdUnitIdProperty); }
            set { SetValue(PubCenterAdUnitIdProperty, value); }
        }

        public static readonly DependencyProperty PubCenterAdUnitIdProperty = DependencyProperty.Register("PubCenterAdUnitId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata("", PubCenterAdUnitIdChanged));

        private static void PubCenterAdUnitIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnPubCenterAdUnitIdChanged(e);
            }
        }

        private void OnPubCenterAdUnitIdChanged(DependencyPropertyChangedEventArgs e)
        {
            RemoveAdFromFailedAds(AdType.PubCenter);
            Invalidate();
        }


        #endregion

        #region AdMobAdUnitId

        public string AdMobAdUnitId
        {
            get { return (string)GetValue(AdMobAdUnitIdProperty); }
            set { SetValue(AdMobAdUnitIdProperty, value); }
        }

        public static readonly DependencyProperty AdMobAdUnitIdProperty = DependencyProperty.Register("AdMobAdUnitId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata("", AdMobAdUnitIdChanged));

        private static void AdMobAdUnitIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnAdMobAdUnitIdChanged(e);
            }
        }

        private void OnAdMobAdUnitIdChanged(DependencyPropertyChangedEventArgs e)
        {
            RemoveAdFromFailedAds(AdType.AdMob);
            Invalidate();
        }


        #endregion

        #region AdDuplexAppId

        public string AdDuplexAppId
        {
            get { return (string)GetValue(AdDuplexAppIdProperty); }
            set { SetValue(AdDuplexAppIdProperty, value); }
        }

        public static readonly DependencyProperty AdDuplexAppIdProperty = DependencyProperty.Register("AdDuplexAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata("", AdDuplexAppIdChanged));

        private static void AdDuplexAppIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnAdDuplexAppIdChanged(e);
            }
        }

        private void OnAdDuplexAppIdChanged(DependencyPropertyChangedEventArgs e)
        {
            RemoveAdFromFailedAds(AdType.AdDuplex);
            Invalidate();
        }


        #endregion

        #region InneractiveAppId

        public string InneractiveAppId
        {
            get { return (string)GetValue(InneractiveAppIdProperty); }
            set { SetValue(InneractiveAppIdProperty, value); }
        }

        public static readonly DependencyProperty InneractiveAppIdProperty = DependencyProperty.Register("InneractiveAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty, InneractiveAppIdChanged));

        private static void InneractiveAppIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnInneractiveAppIdChanged(e);
            }
        }

        private void OnInneractiveAppIdChanged(DependencyPropertyChangedEventArgs e)
        {
            RemoveAdFromFailedAds(AdType.InnerActive);
            Invalidate();
        }


        #endregion

        #region InneractiveExternalId

        public string InneractiveExternalId
        {
            get { return (string)GetValue(InneractiveExternalIdProperty); }
            set { SetValue(InneractiveExternalIdProperty, value); }
        }

        public static readonly DependencyProperty InneractiveExternalIdProperty = DependencyProperty.Register("InneractiveExternalId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty, InneractiveExternalIdChanged));

        private static void InneractiveExternalIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnInneractiveExternalIdChanged(e);
            }
        }

        private void OnInneractiveExternalIdChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }


        #endregion

        #region InneractiveGender

        public string InneractiveGender
        {
            get { return (string)GetValue(InneractiveGenderProperty); }
            set { SetValue(InneractiveGenderProperty, value); }
        }

        public static readonly DependencyProperty InneractiveGenderProperty = DependencyProperty.Register("InneractiveGender", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty, InneractiveGenderChanged));

        private static void InneractiveGenderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnInneractiveGenderChanged(e);
            }
        }

        private void OnInneractiveGenderChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }


        #endregion

        #region InneractiveAge

        public string InneractiveAge
        {
            get { return (string)GetValue(InneractiveAgeProperty); }
            set { SetValue(InneractiveAgeProperty, value); }
        }

        public static readonly DependencyProperty InneractiveAgeProperty = DependencyProperty.Register("InneractiveAge", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty, InneractiveAgeChanged));

        private static void InneractiveAgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnInneractiveAgeChanged(e);
            }
        }

        private void OnInneractiveAgeChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }


        #endregion

        #region InneractiveKeywords

        public string InneractiveKeywords
        {
            get { return (string)GetValue(InneractiveKeywordsProperty); }
            set { SetValue(InneractiveKeywordsProperty, value); }
        }

        public static readonly DependencyProperty InneractiveKeywordsProperty = DependencyProperty.Register("InneractiveKeywords", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty, InneractiveKeywordsChanged));

        private static void InneractiveKeywordsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnInneractiveKeywordsChanged(e);
            }
        }

        private void OnInneractiveKeywordsChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }


        #endregion

        #region InneractiveReloadTime

        public int InneractiveReloadTime
        {
            get { return (int)GetValue(InneractiveReloadTimeProperty); }
            set { SetValue(InneractiveReloadTimeProperty, value); }
        }

        public static readonly DependencyProperty InneractiveReloadTimeProperty = DependencyProperty.Register("InneractiveReloadTime", typeof(int), typeof(AdRotatorControl), new PropertyMetadata(60, InneractiveReloadTimeChanged));

        private static void InneractiveReloadTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnInneractiveReloadTimeChanged(e);
            }
        }

        private void OnInneractiveReloadTimeChanged(DependencyPropertyChangedEventArgs e)
        {
            Invalidate();
        }


        #endregion      

        #region DefaultSettingsFileUri

        public Uri DefaultSettingsFileUri
        {
            get { return (Uri)GetValue(DefaultSettingsFileUriProperty); }
            set { SetValue(DefaultSettingsFileUriProperty, value); }
        }

        public static readonly DependencyProperty DefaultSettingsFileUriProperty = DependencyProperty.Register("DefaultSettingsFileUri", typeof(Uri), typeof(AdRotatorControl), new PropertyMetadata(DefaultSettingsFileUriChanged));

        private static void DefaultSettingsFileUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnDefaultSettingsFileUriChanged(e);
            }
        }

        private void OnDefaultSettingsFileUriChanged(DependencyPropertyChangedEventArgs e)
        {
            FetchAdSettingsFile();
            if (_settings == null)
            {
                _settings = LoadAdSettings();
                Invalidate();
            }
        }

        #endregion

        private bool IsPubCenterValid
        {
            get
            {
                return !String.IsNullOrEmpty(PubCenterAppId) && !String.IsNullOrEmpty(PubCenterAdUnitId);
            }
        }

        private bool IsAdDuplexValid
        {
            get
            {
                return !String.IsNullOrEmpty(AdDuplexAppId);
            }
        }

        private bool IsAdMobValid
        {
            get
            {
                return !String.IsNullOrEmpty(AdMobAdUnitId);
            }
        }

        public AdRotatorControl()
        {
            InitializeComponent();
            FetchAdSettingsFile();
            this.Loaded += new RoutedEventHandler(AdRotatorControl_Loaded);
        }

        void AdRotatorControl_Loaded(object sender, RoutedEventArgs e)
        {
            _loaded = true;
            Invalidate();
        }

        /// <summary>
        /// Fetches the ad settings file from the address specified at <see cref=""/>
        /// </summary>
        public void FetchAdSettingsFile()
        {
            if (_remoteAdSettingsFetched || !IsEnabled || String.IsNullOrEmpty(SettingsUrl))
            {
                return;
            }
            var request = (HttpWebRequest)WebRequest.Create(new Uri(SettingsUrl));
            request.BeginGetResponse(r =>
            {
                try
                {
                    var httpRequest = (HttpWebRequest)r.AsyncState;
                    var httpResponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                    var settingsStream = httpResponse.GetResponseStream();

                    var s = new XmlSerializer(typeof(AdSettings));
                    _settings = (AdSettings)s.Deserialize(settingsStream);
                    // Only persist the settings if they've been retreived from the remote file
                    SaveAdSettings(_settings);                    
                }
                catch
                {
                    Dispatcher.BeginInvoke(() => { _settings = GetDefaultSettings(); });
                }
                finally
                {
                    _remoteAdSettingsFetched = true;
                    Dispatcher.BeginInvoke(Init);
                }
            }, request);
        }

        /// <summary>
        /// Displays a new ad
        /// </summary>
        public void Invalidate()
        {
            if (!_loaded)
            {
                return;
            }
            if (!IsEnabled)
            {
                Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                Visibility = Visibility.Visible;
            }
            if (LayoutRoot == null)
            {
                return;
            }

            RemoveEventHandlersFromAdControl();
            LayoutRoot.Children.Clear();
            var adType = GetNextAdType();
            switch (adType)
            {                
                case AdType.PubCenter:
                    _currentAdControl = CreatePubCentertAdControl();
                    break;
                case AdType.AdMob:
                    _currentAdControl = CreateAdMobAdControl();
                    break;
                case AdType.AdDuplex:
                    _currentAdControl = CreateAdDuplexControl();
                    break;
                case AdType.InnerActive:
                    Visibility = Visibility.Visible;
                    bool success = CreateInneractiveControl(LayoutRoot);
                    if (!success)
                    {
                        OnAdLoadFailed(adType);
                    }
                    return;
                    break;
                default:
                    _currentAdControl = CreatePubCentertAdControl();
                    break;                    
            }
            if (_currentAdControl == null)
            {
                OnAdLoadFailed(adType);
                return;
            }
            if (adType == AdType.None)
            {
                Visibility = Visibility.Collapsed;
            }
            else
            {
                Visibility = Visibility.Visible;
                AddEventHandlersToAdControl();
                LayoutRoot.Children.Add(_currentAdControl);
            }
        }

        /// <summary>
        /// Generates what the next ad type to display should be
        /// </summary>
        /// <returns></returns>
        private AdType GetNextAdType()
        {
            var possibleAds = GetAdDescriptorBasedOnUICulture();
            if (possibleAds == null)
            {
                return DefaultAdType;
            }

            var validDescriptors = possibleAds.AdProbabilities
                .Where(x => !_failedAdTypes.Contains(x.AdType)
                            && IsAdTypeValid(x.AdType))
                .ToList();
            if (validDescriptors.Count == 0)
            {
                return DefaultAdType;
            }
            var totalValueBetweenValidAds = validDescriptors.Sum(x => x.ProbabilityValue);
            var randomValue = _rnd.NextDouble() * totalValueBetweenValidAds;
            double totalCounter = 0;
            foreach (var probabilityDescriptor in validDescriptors)
            {
                totalCounter += probabilityDescriptor.ProbabilityValue;
                if (randomValue < totalCounter)
                {
                    return probabilityDescriptor.AdType;
                }
            }
            return DefaultAdType;
        }

        /// <summary>
        /// Called when the settings have been loaded. Clears all failed ad types and invalidates the control
        /// </summary>
        private void Init()
        {
            _failedAdTypes.Clear();
            Invalidate();
        }

        private bool IsAdTypeValid(AdType adType)
        {
            switch (adType)
            {
                case AdType.PubCenter:
                    return IsPubCenterValid;
                case AdType.AdDuplex:
                    return IsAdDuplexValid;
                case AdType.AdMob:
                    return IsAdMobValid;
            }
            return true;
        }

        private void AddEventHandlersToAdControl()
        {
            var pubCenterAd = _currentAdControl as Microsoft.Advertising.Mobile.UI.AdControl;
            if (pubCenterAd != null)
            {
                pubCenterAd.AdRefreshed += new EventHandler(pubCenterAd_AdRefreshed);
                pubCenterAd.ErrorOccurred += new EventHandler<Microsoft.Advertising.AdErrorEventArgs>(pubCenterAd_ErrorOccurred);
            }
            var adMobAd = _currentAdControl as Google.AdMob.Ads.WindowsPhone7.WPF.BannerAd;
            if (adMobAd != null)
            {
                adMobAd.AdFailed += new Google.AdMob.Ads.WindowsPhone7.ErrorEventHandler(adMobAd_AdFailed);
                adMobAd.AdReceived += new RoutedEventHandler(adMobAd_AdReceived);
            }
        }

        private void RemoveEventHandlersFromAdControl()
        {
            var pubCenterAd = _currentAdControl as Microsoft.Advertising.Mobile.UI.AdControl;
            if (pubCenterAd != null)
            {
                pubCenterAd.AdRefreshed -= new EventHandler(pubCenterAd_AdRefreshed);
                pubCenterAd.ErrorOccurred -= new EventHandler<Microsoft.Advertising.AdErrorEventArgs>(pubCenterAd_ErrorOccurred);
            }
            var adMobAd = _currentAdControl as Google.AdMob.Ads.WindowsPhone7.WPF.BannerAd;
            if (adMobAd != null)
            {
                adMobAd.AdFailed -= new Google.AdMob.Ads.WindowsPhone7.ErrorEventHandler(adMobAd_AdFailed);
                adMobAd.AdReceived -= new RoutedEventHandler(adMobAd_AdReceived);
            }
        }

        private AdCultureDescriptor GetAdDescriptorBasedOnUICulture()
        {
            if (_settings == null || _settings.CultureDescriptors == null)
            {
                return null;
            }
            var cultureLongName = Thread.CurrentThread.CurrentUICulture.Name;
            if(String.IsNullOrEmpty(cultureLongName))
            {
                cultureLongName = AdSettings.DEFAULT_CULTURE;
            }
            var cultureShortName = cultureLongName.Substring(0, 2);
            var descriptor = _settings.CultureDescriptors.Where(x => x.CultureName == cultureLongName).FirstOrDefault();
            if (descriptor != null)
            {
                return descriptor;
            }
            var sameLanguageDescriptor = _settings.CultureDescriptors.Where(x => x.CultureName.StartsWith(cultureShortName)).FirstOrDefault();
            if (sameLanguageDescriptor != null)
            {
                return sameLanguageDescriptor;
            }
            var defaultDescriptor = _settings.CultureDescriptors.Where(x => x.CultureName == AdSettings.DEFAULT_CULTURE).FirstOrDefault();
            if (defaultDescriptor != null)
            {
                return defaultDescriptor;
            }
            return null;
        }

        private void RemoveAdFromFailedAds(AdType adType)
        {
            if(_failedAdTypes.Contains(adType))
            {
                _failedAdTypes.Remove(adType);
            }
        }

        /// <summary>
        /// Called when <paramref name="adType"/> has failed to load
        /// </summary>
        /// <param name="adType"></param>
        private void OnAdLoadFailed(AdType adType)
        {
            if (!_failedAdTypes.Contains(adType))
            {
                _failedAdTypes.Add(adType);
            }
            Dispatcher.BeginInvoke(() =>
            {
                Invalidate();
            });
        }

        /// <summary>
        /// Called when <paramref name="adType"/> has succeeded to load
        /// </summary>
        /// <param name="adType"></param>
        private void OnAdLoadSucceeded(AdType adType)
        {
            if (_failedAdTypes.Contains(adType))
            {
                _failedAdTypes.Remove(adType);
            }
        }

        private void SaveAdSettings()
        {
        }

        /// <summary>
        /// Loads the ad settings object either from isolated storage or from the resource path defined in DefaultSettingsFileUri.
        /// </summary>
        /// <returns></returns>
        private AdSettings LoadAdSettings()
        {
            try
            {
                var isfData = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream isfStream = null;
                if (isfData.FileExists(SETTINGS_FILE_NAME))
                {
                    using (isfStream = new IsolatedStorageFileStream(SETTINGS_FILE_NAME, FileMode.Open, isfData))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(AdSettings));
                        try
                        {
                            var settings = (AdSettings)xs.Deserialize(isfStream);
                            return settings;
                        }
                        catch
                        {

                        }
                    }
                }
            }
            catch (IsolatedStorageException)
            {
            }

            if (_settings == null)
            {
                _settings = GetDefaultSettings();
            }
            return _settings;
        }

        private AdSettings GetDefaultSettings()
        {
            if (DefaultSettingsFileUri != null)
            {
                var defaultSettingsFileInfo = Application.GetResourceStream(DefaultSettingsFileUri);
                var xs = new XmlSerializer(typeof(AdSettings));
                try
                {
                    var settings = (AdSettings)xs.Deserialize(defaultSettingsFileInfo.Stream);
                    return settings;
                }
                catch 
                {
                }
            }
            return new AdSettings();
        }

        /// <summary>
        /// Saves the passed settings file to isolated storage
        /// </summary>
        /// <param name="settings"></param>
        private static void SaveAdSettings(AdSettings settings)
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(AdSettings));
                IsolatedStorageFileStream isfStream = new IsolatedStorageFileStream(SETTINGS_FILE_NAME, FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication());
                xs.Serialize(isfStream, settings);
                isfStream.Close();
            }
            catch
            {
            }
        }

        #region Specific ad controls

        private FrameworkElement CreatePubCentertAdControl()
        {
            var pubCenterAdControl = new Microsoft.Advertising.Mobile.UI.AdControl(
                PubCenterAppId,
                PubCenterAdUnitId,               
                true); // isAutoRefreshEnabled
            pubCenterAdControl.Width = 480;
            pubCenterAdControl.Height = 80;

            if (GeoLocation != null)
            {
                pubCenterAdControl.Longitude = GeoLocation.Longitude;
                pubCenterAdControl.Latitude = GeoLocation.Latitude;
            }

            return pubCenterAdControl;
        }

        private bool CreateInneractiveControl(Grid container)
        {
            Dictionary<InneractiveAd.IaOptionalParams, string> optionalParams = new Dictionary<InneractiveAd.IaOptionalParams, string>();
            optionalParams.Add(InneractiveAd.IaOptionalParams.Key_Distribution_Id, "659");

            if (InneractiveExternalId != String.Empty)
            {
                optionalParams.Add(InneractiveAd.IaOptionalParams.Key_External_Id, InneractiveExternalId);
            }
            if (InneractiveGender != String.Empty)
            {
                optionalParams.Add(InneractiveAd.IaOptionalParams.Key_Gender, InneractiveGender);
            }
            if (InneractiveAge != String.Empty)
            {
                optionalParams.Add(InneractiveAd.IaOptionalParams.Key_Age, InneractiveAge);
            }
            if (InneractiveKeywords != String.Empty)
            {
                optionalParams.Add(InneractiveAd.IaOptionalParams.Key_Keywords, InneractiveKeywords);
            }

            if (GeoLocation != null)
            {
                string geoLocStr = GeoLocation.ToString();
                optionalParams.Add(InneractiveAd.IaOptionalParams.Key_Gps_Coordinates, geoLocStr);
            }

            return InneractiveAd.DisplayAd(InneractiveAppId, InneractiveAd.IaAdType.IaAdType_Banner, container, InneractiveReloadTime, optionalParams);
        }

        private FrameworkElement CreateAdDuplexControl()
        {
            var adDuplexAd = new AdDuplex.AdControl();
            adDuplexAd.AppId = AdDuplexAppId;
            //adDuplexAd.IsTest = true;

            return adDuplexAd;
        }

        private FrameworkElement CreateAdMobAdControl()
        {
            var adMobAd = new Google.AdMob.Ads.WindowsPhone7.WPF.BannerAd();
            adMobAd.AdUnitID = AdMobAdUnitId;

            if (GeoLocation != null)
            {
                adMobAd.GpsLocation = new Google.AdMob.Ads.WindowsPhone7.GpsLocation()
                    {
                        Longitude = GeoLocation.Longitude,
                        Latitude = GeoLocation.Latitude,
                        Accuracy = GeoLocation.VerticalAccuracy + GeoLocation.HorizontalAccuracy
                    };
            }

            return adMobAd;
        }

        void adMobAd_AdReceived(object sender, RoutedEventArgs e)
        {
            OnAdLoadSucceeded(AdType.AdMob);
        }

        void adMobAd_AdFailed(object sender, Google.AdMob.Ads.WindowsPhone7.AdException exception)
        {
            OnAdLoadFailed(AdType.AdMob);
        }

        void pubCenterAd_AdRefreshed(object sender, EventArgs e)
        {
            OnAdLoadSucceeded(AdType.PubCenter);
        }

        void pubCenterAd_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            OnAdLoadFailed(AdType.PubCenter);
        }

        #endregion
    }
}
