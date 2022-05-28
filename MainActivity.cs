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
using System.ComponentModel;

namespace GPX_trip_recorder
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, AdapterView.IOnItemLongClickListener
    {
        private DialogService _dialogService;
        private BackgroundWorker _backgroundRecordchecker;

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

            // workaround for not using FileProvider (necessary for file sharing):
            // https://stackoverflow.com/questions/38200282/android-os-fileuriexposedexception-file-storage-emulated-0-test-txt-exposed
            StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
            StrictMode.SetVmPolicy(builder.Build());

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            //CrossCurrentActivity.Current.Activity = this;
            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            // Settings button
            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            var startBtn = FindViewById<FloatingActionButton>(Resource.Id.startButton);
            startBtn.Click += StartClick;

            var stopBtn = FindViewById<FloatingActionButton>(Resource.Id.stopButton);
            stopBtn.Click += StopClick;

            LocationProvider.LocationChanged += LocationProvider_LocationChanged;

            _dialogService = new DialogService(this);
            _backgroundRecordchecker = new BackgroundWorker();
            _backgroundRecordchecker.DoWork += _backgroundRecordchecker_DoWork;

            var listView = FindViewById<ListView>(Resource.Id.listView);
            listView.OnItemLongClickListener = this;

            StartService(new Intent(this, typeof(LocationForegroundService)));

            RefreshGUI();
        }

        public bool OnItemLongClick(AdapterView? parent, View? view, int position, long id)
        {
            ShowMenuForGPXRecord(id);
            return true;
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            //IMenuItem menitm = menu.FindItem(Resource.Id.action_settings);
            return false;
        }

        private async Task ShowMenuForGPXRecord(long index)
        {
            var savedRecords = LocationProvider.GetSavedGPXRecords();
            var fileName = savedRecords[Convert.ToInt32(index)];

            await ShareFile(System.IO.Path.Join(LocationServiceProvider.OutputDirectory,fileName));
        }

        private async Task ShareFile(string fileName)
        {
            try
            {
                var intent = new Intent(Intent.ActionSend);
                var file = new Java.IO.File(fileName);
                var uri = Android.Net.Uri.FromFile(file);

                intent.PutExtra(Intent.ExtraStream, uri);
                intent.SetDataAndType(uri, "text/plain");
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);
                intent.SetFlags(ActivityFlags.NewTask);

                Android.App.Application.Context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                _dialogService.Warning(ex.Message, "Error");
            }
        }

        private void _backgroundRecordchecker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (LocationProvider.Recording)
            {
                System.Threading.Thread.Sleep(1000); // wait
            }

            if (LocationProvider.RecordException != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _dialogService.Warning($"Record terminated: {LocationProvider.RecordException.Message}","Error");
                    RefreshGUI();
                });
            }
        }

        private void LocationProvider_LocationChanged(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var loc = (e as LocationEventArgs).Location;

                if (loc == null)
                    return;

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
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync<LocationPermission>();

                if (status != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Location))
                    {
                        _dialogService.Information("Location services must be enabled for this application");
                    }

                    var prms = new List<Permission>();

                    prms.Add(Permission.Location);
                    prms.Add(Permission.LocationWhenInUse);

                    Console.WriteLine(Android.OS.Build.VERSION.SdkInt);

                    if (Android.OS.Build.VERSION.SdkInt <= BuildVersionCodes.R)
                    {
                        // https://developer.android.com/training/location/permissions
                        prms.Add(Permission.LocationAlways);
                    }
                    
                    var statuses = await CrossPermissions.Current.RequestPermissionsAsync(prms.ToArray());

                    if (
                        (statuses.ContainsKey(Permission.Location) && statuses[Permission.Location] == PermissionStatus.Granted) ||
                        (statuses.ContainsKey(Permission.LocationWhenInUse) && statuses[Permission.LocationWhenInUse] == PermissionStatus.Granted) ||
                        (statuses.ContainsKey(Permission.LocationAlways) && statuses[Permission.LocationAlways] == PermissionStatus.Granted)
                        )
                    {
                        return true;
                    }

                    return false;

                }
                else
                {
                    return true;
                }                
                
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

            var savedRecords = LocationProvider.GetSavedGPXRecords();

            var tv = FindViewById<TextView>(Resource.Id.textView);
            var lv = FindViewById<ListView>(Resource.Id.listView);

            if (LocationProvider.Recording)
            {
                tv.Text = "Recording ...";
                tv.Visibility = ViewStates.Visible;
                lv.Visibility = ViewStates.Invisible;
            } else
            {
                if (savedRecords.Count == 0)
                {
                    tv.Text = "No location recorded...";
                    tv.Visibility = ViewStates.Visible;
                    lv.Visibility = ViewStates.Invisible;
                } else
                {
                    tv.Visibility = ViewStates.Invisible;
                    lv.Visibility = ViewStates.Visible;

                    var adapter = new ArrayAdapter<String>(this, Android.Resource.Layout.SimpleListItem1, savedRecords);
                    lv.Adapter = adapter;
                }
            }
        }

        private async void StartClick(object sender, EventArgs eventArgs)
        {
            var btrMng = new BatteryOptimizationManager();
            if (!btrMng.AppIgnoringBatteryOptimizations())
            {
                var res = await _dialogService.ConfirmYesNoContinueDialog("Application runs in background and therefore Android must ignore battery optimization.", "", "Go to settings", "Close", "Continue");

                if (res.HasValue && !res.Value)
                {
                    return; // close
                }

                if (res.HasValue && res.Value)
                {
                    btrMng.RequestIngoreBatteryOptimizations();
                    return;
                }
            }

            if (await RequestLocationPermission())
            {
                LocationProvider.StartRecord();
                _backgroundRecordchecker.RunWorkerAsync();
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
