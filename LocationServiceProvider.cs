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
using System.IO;
using System.Xml;
using System.Globalization;

namespace GPX_trip_recorder
{
    public class LocationServiceProvider
    {
        private static readonly Lazy<LocationServiceProvider> lazy =
            new Lazy<LocationServiceProvider>(() => new LocationServiceProvider(), true);

        public static LocationServiceProvider Instance { get { return lazy.Value; } }

        public List<Location> Locations = new List<Location>();

        private BackgroundWorker _backgroundWorker;
        private bool _recording;
        private DateTime _recordingStartTime;
        private Exception _recordException = null;

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

        public Exception RecordException
        {
            get
            {
                return _recordException;
            }
        }

        public void StartRecord()
        {
            Locations.Clear();
            _recording = true;
            _recordingStartTime = DateTime.Now;
            _recordException = null;
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
                try
                {
                    var loc = await GetLocation();

                    if (LocationChanged != null)
                        LocationChanged(this, new LocationEventArgs(loc));

                    Locations.Add(loc);

                    Save();

                    System.Threading.Thread.Sleep(10 * 1000); // wait 10 secs;
                } catch (Exception ex)
                {
                    _recordException = ex;
                    _recording = false;
                }
            }
        }

        public List<string> GetSavedGPXRecords()
        {
            var files = System.IO.Directory.GetFiles(OutputDirectory, "*.gpx");

            var res = new List<string>();
            foreach (var f in files)
            {
                res.Add(System.IO.Path.GetFileName(f));
            }

            return res;
        }

        public async Task<Location> GetLastLocation()
        {
            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();

                return location;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private async Task<Location> GetLocation()
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string OutputDirectory
        {
            get
            {
                try
                {
                    // internal storage - always writable directory
                    try
                    {
                        var pathToExternalMediaDirs = Android.App.Application.Context.GetExternalMediaDirs();

                        if (pathToExternalMediaDirs.Length == 0)
                            throw new DirectoryNotFoundException();

                        return pathToExternalMediaDirs[0].AbsolutePath;
                    }
                    catch
                    {
                        // fallback for older API:

                        var internalStorageDir = Android.App.Application.Context.GetExternalFilesDir(System.Environment.SpecialFolder.MyDocuments.ToString());

                        return internalStorageDir.AbsolutePath;
                    }
                }
                catch
                {
                    var dir = Android.App.Application.Context.GetExternalFilesDir("");

                    return dir.AbsolutePath;
                }
            }
        }

        private void Save()
        {
            var outputFileName = Path.Combine(OutputDirectory, $"{_recordingStartTime.ToString("yyyy-MM-dd--HH-mm-ss")}.gpx");
            var outputFileNameTmp = outputFileName + ".tmp";

            using (var textWriter = new XmlTextWriter(outputFileNameTmp, System.Text.Encoding.UTF8))
            {
                textWriter.WriteStartDocument();

                textWriter.WriteStartElement("gpx");

                textWriter.WriteAttributeString("version", "1.1");
                textWriter.WriteAttributeString("creator", "GPX Trip Recorder");
                textWriter.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
                textWriter.WriteAttributeString("xmlns", "http://www.topografix.com/GPX/1/1");
                textWriter.WriteAttributeString("xsi:schemaLocation", "http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd");
                textWriter.WriteAttributeString("xmlns:gpxtpx", "http://www.garmin.com/xmlschemas/TrackPointExtension/v1");

                textWriter.WriteStartElement("trk");

                textWriter.WriteStartElement("name");
                textWriter.WriteCData($"GPS Trip Record ({_recordingStartTime.ToString("MM/dd/yyyy")})");
                textWriter.WriteEndElement();

                textWriter.WriteElementString("time", $"{_recordingStartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")}");

                textWriter.WriteStartElement("trkseg");

                foreach (var loc in Locations)
                {
                    textWriter.WriteStartElement("trkpt");
                    textWriter.WriteAttributeString("lat", loc.Latitude.ToString("#0.000000000", CultureInfo.InvariantCulture));
                    textWriter.WriteAttributeString("lon", loc.Longitude.ToString("#0.000000000", CultureInfo.InvariantCulture));

                    if (loc.Altitude.HasValue)
                    {
                        textWriter.WriteElementString("ele", loc.Altitude.Value.ToString("#0.0", CultureInfo.InvariantCulture));
                    }

                    textWriter.WriteElementString("time", loc.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));

                    textWriter.WriteEndElement(); // trkpt
                }

                textWriter.WriteEndElement(); // trkseg

                textWriter.WriteEndElement(); // trk

                textWriter.WriteEndElement(); // gpx

                textWriter.WriteEndDocument();
                textWriter.Flush();
                textWriter.Close();
            }

            if (File.Exists(outputFileName))
            {
                File.Delete(outputFileName);
            }

            File.Move(outputFileNameTmp, outputFileName);
        }
    }
}