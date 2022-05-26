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
using Android.Widget;
using Android.Content;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading;
using System.Collections.Generic;
using Plugin.CurrentActivity;
using PermissionStatus = Plugin.Permissions.Abstractions.PermissionStatus;

namespace GPX_trip_recorder
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private LocationServiceProvider LocationProvider
        {
            get
            {
                return LocationServiceProvider.Instance;
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            CrossCurrentActivity.Current.Activity = this;
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var startBtn = FindViewById<FloatingActionButton>(Resource.Id.startButton);
            startBtn.Click += StartClick;

            var stopBtn = FindViewById<FloatingActionButton>(Resource.Id.stopButton);
            stopBtn.Click += StopClick;

            LocationProvider.LocationChanged += LocationProvider_LocationChanged;

            RefreshGUI();
        }

        private void LocationProvider_LocationChanged(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var loc = (e as LocationEventArgs).Location;

                if (loc == null)
                    return;

                var tv = FindViewById<TextView>(Resource.Id.textView);
                tv.Text += $"Lat: {loc.Latitude} , Long: {loc.Longitude}, Alt: {loc.Altitude}{System.Environment.NewLine}";

                RefreshGUI();
            });
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;
            if (id == Resource.Id.action_settings)
            {
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public async Task<bool> RequestLocationPermission()
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync<LocationAlwaysPermission>();

                if (status != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.LocationAlways))
                    {

                    }

                    status = await CrossPermissions.Current.RequestPermissionAsync<LocationAlwaysPermission>();
                } else
                {
                    return true;
                }

                if (status == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void RefreshGUI()
        {
            var startBtn = FindViewById<FloatingActionButton>(Resource.Id.startButton);
            startBtn.Visibility = LocationProvider.Recording ? ViewStates.Invisible : ViewStates.Visible;

            var stopBtn = FindViewById<FloatingActionButton>(Resource.Id.stopButton);
            stopBtn.Visibility = LocationProvider.Recording ? ViewStates.Visible : ViewStates.Invisible;

            var tv = FindViewById<TextView>(Resource.Id.textView);
            tv.Text = LocationProvider.Recording ? "Record in progres ..." : "No location recorded";

            //var lv = FindViewById<ListView>(Resource.Id.listView);
            //var items = new string[] { "ABC", "DEF"};
            //var adapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItem1, items);
            //lv.Adapter = adapter;
        }

        private async void StartClick(object sender, EventArgs eventArgs)
        {
            if (await RequestLocationPermission())
            {
                LocationProvider.StartRecord();
                RefreshGUI();
            }
        }

        private void StopClick(object sender, EventArgs eventArgs)
        {
            LocationProvider.StopRecord();
            RefreshGUI();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
