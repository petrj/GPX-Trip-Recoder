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

namespace GPX_trip_recorder
{
    [Service(Label = "LocationForegroundService", Icon = "@drawable/icon", ForegroundServiceType = Android.Content.PM.ForegroundService.TypeLocation)]
    public class LocationForegroundService : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
    }
}