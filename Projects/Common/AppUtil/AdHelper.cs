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
using System.Device.Location;
using Microsoft.Advertising.Mobile.UI;
using AdRotator;

namespace Naboo.AppUtil
{
    public class AdHelper
    {
#if DEBUG
        public TimeSpan HidePeriod = new TimeSpan(1, 0, 0);
#else
        public TimeSpan HidePeriod = new TimeSpan(1, 0, 0);
#endif
        public DateTime LastTapTime = DateTime.MinValue;

        public GeoCoordinate GeoLocation = null;
        public Func<bool> UseLocationDelegate = null;

        private AppLicense _license = null;
        private GeoCoordinateWatcher _geoWatcher;
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private TimeSpan _minimumAdRefreshInterval = new TimeSpan(0, 1, 0);
        
        public AdRotatorControl AdControlTemplate = null;

        public AdHelper(AppLicense license, Func<bool> useLocation)
        {
            _license = license;
            UseLocationDelegate = useLocation;
        }

        public void InitAdControl(Grid adGrid)
        {
            if (!AppInfo.IsNetworkConnected())
            {
                return;
            }

            if (_license.IsTrial && UseLocationDelegate() && GeoLocation == null && _geoWatcher == null)
            {
                _geoWatcher = new GeoCoordinateWatcher(GeoPositionAccuracy.Default);
                _geoWatcher.PositionChanged += Position_Changed;
                _geoWatcher.Start();
            }

            if (!_license.IsTrial || DateTime.Now - LastTapTime < HidePeriod)
            {
                //CleanupAdControl(adGrid);
            }
            else
            {
                //if (App.GeoLocation != null)
                //{
                //    adControl.Longitude = App.GeoLocation.Longitude;
                //    adControl.Latitude = App.GeoLocation.Latitude;
                //}

                //AdRotatorControl adControl = new AdRotatorControl()
                //{
                //    DefaultSettingsFileUri = AdControlTemplate.DefaultSettingsFileUri,
                //    SettingsUrl = AdControlTemplate.SettingsUrl,
                //    DefaultAdType = AdControlTemplate.DefaultAdType,
                //    PubCenterAppId = AdControlTemplate.PubCenterAppId,
                //    PubCenterAdUnitId = AdControlTemplate.PubCenterAdUnitId,
                //    AdMobAdUnitId = AdControlTemplate.AdMobAdUnitId,
                //    AdDuplexAppId = AdControlTemplate.AdDuplexAppId,
                //    InneractiveAppId = AdControlTemplate.InneractiveAppId,
                //    Width = 480,
                //    Height = 80
                //};

                //if (UseLocationDelegate())
                //{
                //    adControl.GeoLocation = GeoLocation;
                //}

                //adControl.FetchAdSettingsFile();

                AdControl adControl = new AdControl(
                   AdControlTemplate.PubCenterAppId,
                   AdControlTemplate.PubCenterAdUnitId,
                   true);

                adControl.Width = 480;
                adControl.MaxHeight = 80;
                adControl.Height = 80;

                adControl.ErrorOccurred += 
                    (s, e) =>
                    {
                        adControl.Height = 0;
                    };

                adGrid.Children.Add(adControl);
                adGrid.Visibility = Visibility.Visible;
                adGrid.Tap += AdControl_Tapped;

                _lastRefreshTime = DateTime.Now;
            }
        }

        public void CleanupAdControl(Grid adGrid)
        {
            if (adGrid.Children.Count > 0)
            {
                adGrid.Tap -= AdControl_Tapped;
                adGrid.Children.Clear();
                adGrid.Visibility = Visibility.Collapsed;
            }
        }

        public bool RefreshAdControl(Grid adGrid)
        {
            if (_lastRefreshTime > DateTime.Now)
            {
                _lastRefreshTime = DateTime.MinValue;
            }

            if (DateTime.Now - _lastRefreshTime > _minimumAdRefreshInterval)
            {
                CleanupAdControl(adGrid);
                InitAdControl(adGrid);
                return true;
            }

            return false;
        }

        private void Position_Changed(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            _geoWatcher.PositionChanged -= Position_Changed;
            GeoLocation = e.Position.Location;

            _geoWatcher.Stop();
            _geoWatcher = null;
        }

        private void AdControl_Tapped(object sender, GestureEventArgs e)
        {
            LastTapTime = DateTime.Now;
        }
    }
}
