using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials;

namespace GPX_trip_recorder
{
    public class BatteryOptimizationManager
    {
        public bool AppIgnoringBatteryOptimizations()
        {
            var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
            return pm.IsIgnoringBatteryOptimizations(AppInfo.PackageName);
        }

        public void RequestIngoreBatteryOptimizations()
        {
            var intent = new Intent();
            intent.SetAction(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
            intent.SetFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
    }
}