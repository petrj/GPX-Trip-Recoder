using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Xamarin.Essentials;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;

namespace GPX_trip_recorder
{
    public class LocationServiceProvider
    {
        private static readonly Lazy<LocationServiceProvider> lazy =
            new Lazy<LocationServiceProvider>(() => new LocationServiceProvider(), true);

        public static LocationServiceProvider Instance { get { return lazy.Value; } }

        public List<Location> Locations = new List<Location>();

        BackgroundWorker _backgroundWorker;
        private bool _recording;
        public event EventHandler LocationChanged;

        public LocationServiceProvider()
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += _backgroundWorker_DoWork;
        }

        public bool Recording
        {
            get
            {
                return _recording;
            }
        }

        public void StartRecord()
        {
            Locations.Clear();
            _recording = true;
            _backgroundWorker.RunWorkerAsync();
        }

        public void StopRecord()
        {
            _recording = false;
        }

        private async void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (_recording)
            {
                var loc = await GetLocation();

                if (LocationChanged != null)
                    LocationChanged(this, new LocationEventArgs(loc));

                Locations.Add(loc);

                System.Threading.Thread.Sleep(10 * 1000); // wait 10 secs;
            }
        }

        public async Task<Location> GetLastLocation()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                return location;
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
            }
            catch (Exception ex)
            {
                // Unable to get location
            }

            return null;
        }

        public async Task<Location> GetLocation()
        {
            try
            {
                var request = new GeolocationRequest()
                {
                    DesiredAccuracy = GeolocationAccuracy.Best,
                    Timeout = new TimeSpan(0, 0, 10)
                };

                var location = await Geolocation.GetLocationAsync(request);

                return location;
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                // Handle not supported on device exception
            }
            catch (FeatureNotEnabledException fneEx)
            {
                // Handle not enabled on device exception
            }
            catch (PermissionException pEx)
            {
                // Handle permission exception
            }
            catch (Exception ex)
            {
                // Unable to get location
            }

            return null;
        }
    }
}