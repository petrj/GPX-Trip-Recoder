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
    public class LocationEventArgs : EventArgs
    {
        private Location _loc;
        public LocationEventArgs(Location location)
        {
            _loc = location;
        } // eo ctor

        public Location Location { get { return _loc; } }
    }
}
