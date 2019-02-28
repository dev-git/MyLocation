using System;
using System.Threading.Tasks;
using System.Net.Http;

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util;
using Java.Net;
using System.Text;

namespace MyLocation
{

    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        const long ONE_MINUTE = 60 * 1000;
        const long FIVE_MINUTES = 5 * ONE_MINUTE;
        const long TWO_MINUTES = 2 * ONE_MINUTE;

        static readonly int RC_LAST_LOCATION_PERMISSION_CHECK = 1000;
        static readonly int RC_LOCATION_UPDATES_PERMISSION_CHECK = 1100;

        static readonly string KEY_REQUESTING_LOCATION_UPDATES = "requesting_location_updates";

        FusedLocationProviderClient fusedLocationProviderClient;
        Button getLastLocationButton;
        bool isGooglePlayServicesInstalled;
        bool isRequestingLocationUpdates;
        TextView latitude;
        internal TextView latitude2;
        LocationCallback locationCallback;
        LocationRequest locationRequest;
        TextView longitude;
        internal TextView longitude2;
        TextView provider;
        internal TextView provider2;

        internal Button requestLocationUpdatesButton;

        TextView addressText;
        TextView macAddressText;
        TextView batteryLevelText;
        int batteryLevel = 0;

        View rootLayout;

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RC_LAST_LOCATION_PERMISSION_CHECK || requestCode == RC_LOCATION_UPDATES_PERMISSION_CHECK)
            {
                if (grantResults.Length == 1 && grantResults[0] == Permission.Granted)
                {
                    if (requestCode == RC_LAST_LOCATION_PERMISSION_CHECK)
                    {
                        await GetLastLocationFromDevice();
                    }
                    else
                    {
                        await StartRequestingLocationUpdates();
                        isRequestingLocationUpdates = true;
                    }
                }
                else
                {
                    Snackbar.Make(rootLayout, Resource.String.permission_not_granted_termininating_app, Snackbar.LengthIndefinite)
                            .SetAction(Resource.String.ok, delegate { FinishAndRemoveTask(); })
                            .Show();
                    return;
                }
            }
            else
            {
                Log.Debug("FusedLocationProviderSample", "Don't know how to handle requestCode " + requestCode);
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (bundle != null)
            {
                isRequestingLocationUpdates = bundle.KeySet().Contains(KEY_REQUESTING_LOCATION_UPDATES) &&
                                              bundle.GetBoolean(KEY_REQUESTING_LOCATION_UPDATES);
            }
            else
            {
                isRequestingLocationUpdates = false;
            }

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            isGooglePlayServicesInstalled = IsGooglePlayServicesInstalled();
            rootLayout = FindViewById(Resource.Id.root_layout);

            // UI to display last location
            getLastLocationButton = FindViewById<Button>(Resource.Id.get_last_location_button);
            latitude = FindViewById<TextView>(Resource.Id.latitude);
            longitude = FindViewById<TextView>(Resource.Id.longitude);
            provider = FindViewById<TextView>(Resource.Id.provider);

            // UI to display location updates
            requestLocationUpdatesButton = FindViewById<Button>(Resource.Id.request_location_updates_button);
            latitude2 = FindViewById<TextView>(Resource.Id.latitude2);
            longitude2 = FindViewById<TextView>(Resource.Id.longitude2);
            provider2 = FindViewById<TextView>(Resource.Id.provider2);

            //addressText = FindViewById<TextView>(Resource.Id.address_text);
            //locationText = FindViewById<TextView>(Resource.Id.location_text);
            macAddressText = FindViewById<TextView>(Resource.Id.macaddress);
            batteryLevelText = FindViewById<TextView>(Resource.Id.batterylevel);

            //batteryLevelText = FindViewById<TextView>(Resource.Id.batterylevel_text);
            //FindViewById<TextView>(Resource.Id.get_address_button).Click += AddressButton_OnClick;


            if (isGooglePlayServicesInstalled)
            {
                locationRequest = new LocationRequest()
                                  .SetPriority(LocationRequest.PriorityHighAccuracy)
                                  .SetInterval(FIVE_MINUTES)
                                  .SetFastestInterval(TWO_MINUTES);
                locationCallback = new FusedLocationProviderCallback(this);

                fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
                getLastLocationButton.Click += GetLastLocationButtonOnClick;
                requestLocationUpdatesButton.Click += RequestLocationUpdatesButtonOnClick;
            }
            else
            {
                // If there is no Google Play Services installed, then this sample won't run.
                Snackbar.Make(rootLayout, Resource.String.missing_googleplayservices_terminating, Snackbar.LengthIndefinite)
                        .SetAction(Resource.String.ok, delegate { FinishAndRemoveTask(); })
                        .Show();
            }
        }

        async void RequestLocationUpdatesButtonOnClick(object sender, EventArgs eventArgs)
        {
            // No need to request location updates if we're already doing so.
            if (isRequestingLocationUpdates)
            {
                StopRequestionLocationUpdates();
                isRequestingLocationUpdates = false;
            }
            else
            {
                if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
                {
                    await StartRequestingLocationUpdates();
                    isRequestingLocationUpdates = true;
                }
                else
                {
                    RequestLocationPermission(RC_LAST_LOCATION_PERMISSION_CHECK);
                }
            }
        }

        async void GetLastLocationButtonOnClick(object sender, EventArgs eventArgs)
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) == Permission.Granted)
            {
                await GetLastLocationFromDevice();
            }
            else
            {
                RequestLocationPermission(RC_LAST_LOCATION_PERMISSION_CHECK);
            }

            var location = await fusedLocationProviderClient.GetLastLocationAsync();

            // Get the MAC Address
            String macAddress = GetMACAddress2();
            if (macAddress.Length > 17)
            {
                macAddress = macAddress.Substring(0, 17);
            }
            macAddressText.Text = macAddress;

            // Get the battery level
            int batteryLevel = GetBatteryLevel();
            batteryLevelText.Text = batteryLevel.ToString();

            // Post the data
            PostData(macAddress.Replace(':', '-'), location.Latitude, location.Longitude, batteryLevel);

        }

        async Task GetLastLocationFromDevice()
        {
            getLastLocationButton.SetText(Resource.String.getting_last_location);
            var location = await fusedLocationProviderClient.GetLastLocationAsync();

            if (location == null)
            {
                latitude.SetText(Resource.String.location_unavailable);
                longitude.SetText(Resource.String.location_unavailable);
                provider.SetText(Resource.String.could_not_get_last_location);
            }
            else
            {
                latitude.Text = Resources.GetString(Resource.String.latitude_string, location.Latitude);
                longitude.Text = Resources.GetString(Resource.String.longitude_string, location.Longitude);
                provider.Text = Resources.GetString(Resource.String.provider_string, location.Provider);
                getLastLocationButton.SetText(Resource.String.get_last_location_button_text);
            }
        }

        void RequestLocationPermission(int requestCode)
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
            {
                Snackbar.Make(rootLayout, Resource.String.permission_location_rationale, Snackbar.LengthIndefinite)
                        .SetAction(Resource.String.ok,
                                   delegate
                                   {
                                       ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.AccessFineLocation }, requestCode);
                                   })
                        .Show();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.AccessFineLocation }, requestCode);
            }
        }

        async Task StartRequestingLocationUpdates()
        {
            // added 
            requestLocationUpdatesButton.SetText(Resource.String.request_location_in_progress_button_text);
            await fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
        }

        async void StopRequestionLocationUpdates()
        {
            latitude2.Text = string.Empty;
            longitude2.Text = string.Empty;
            provider2.Text = string.Empty;

            requestLocationUpdatesButton.SetText(Resource.String.request_location_button_text);

            if (isRequestingLocationUpdates)
            {
                await fusedLocationProviderClient.RemoveLocationUpdatesAsync(locationCallback);
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean(KEY_REQUESTING_LOCATION_UPDATES, isRequestingLocationUpdates);
            base.OnSaveInstanceState(outState);
        }

        protected override async void OnResume()
        {
            base.OnResume();
            if (CheckSelfPermission(Manifest.Permission.AccessFineLocation) == Permission.Granted)
            {
                if (isRequestingLocationUpdates)
                {
                    await StartRequestingLocationUpdates();
                }
            }
            else
            {
                RequestLocationPermission(RC_LAST_LOCATION_PERMISSION_CHECK);
            }
        }

        protected override void OnPause()
        {
            StopRequestionLocationUpdates();
            base.OnPause();
        }

        bool IsGooglePlayServicesInstalled()
        {
            var queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (queryResult == ConnectionResult.Success)
            {
                Log.Info("MainActivity", "Google Play Services is installed on this device.");
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
            {
                var errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                Log.Error("MainActivity", "There is a problem with Google Play Services on this device: {0} - {1}",
                          queryResult, errorString);
            }

            return false;
        }

        private String GetMACAddress()
        {
            // http://www.technetexperts.com/mobile/getting-unique-device-id-of-an-android-smartphone/
            String macAddress = "00-00-00-00-00-01";

            try
            {
                //WLAN MAC Address              
                Android.Net.Wifi.WifiManager wifiManager = (Android.Net.Wifi.WifiManager)GetSystemService(Android.Content.Context.WifiService);

                macAddress = wifiManager.ConnectionInfo.MacAddress;
                if (String.IsNullOrEmpty(macAddress))
                {
                    macAddress = "00-00-00-00-01-A1";
                }

            }
            catch (Exception ex)
            {
                macAddress = "00-00-00-00-00-A1";
                //Log.Error("MainActivity", "There is a problem with Google Play Services on this device: {0} - {1}", ex.ToString());
            }

            return macAddress;
        }

        private String GetMACAddress2()
        {
            StringBuilder sb = new StringBuilder();
            // NetworkInterface is from Java.Net namespace, not System.Net
            var all = Collections.List(NetworkInterface.NetworkInterfaces);

            foreach (var intf in all)
            {
                var macBytes = (intf as NetworkInterface).GetHardwareAddress();

                if (macBytes == null) continue;

                
                foreach (var b in macBytes)
                {
                    sb.Append((b & 0xFF).ToString("X2") + ":");
                }

                //Console.WriteLine(sb.ToString().Remove(sb.Length - 1));
            }


            return sb.ToString();
        }

        private int GetBatteryLevel()
        {
            // Get battery level
            var filter = new IntentFilter(Intent.ActionBatteryChanged);
            var battery = RegisterReceiver(null, filter);
            int level = battery.GetIntExtra(BatteryManager.ExtraLevel, -1);
            int scale = battery.GetIntExtra(BatteryManager.ExtraScale, -1);

            batteryLevel = (int)Math.Floor(level * 100D / scale);

            return batteryLevel;
        }

        private async void PostData(string macAddress, double latitude, double longitude, int batteryLevel)
        {
            // url: 'http://ibrium.webhop.me/plog/api/pLog/D2-A0-F1-00/-3.1254/0.34534/25'
            // http://maps.google.co.nz/maps?q=-36.85538833,174.77183783

            using (var client = new HttpClient())
            {
            /*
            // Build the JSON object to pass parameters
JSONObject jsonObj = new JSONObject();
jsonObj.put("username", username);
jsonObj.put("apikey", apikey);
// Create the POST object and add the parameters
HttpPost httpPost = new HttpPost(url);
StringEntity entity = new StringEntity(jsonObj.toString(), HTTP.UTF_8);
entity.setContentType("application/json");
httpPost.setEntity(entity);
HttpClient client = new DefaultHttpClient();
HttpResponse response = client.execute(httpPost)
*/
                // var content = new FormUrlEncodedContent(values);
                var jsonText = "{ \"macAddress\" : \"" +  macAddress + "\", "latitude" : "-36.92577745", "longitude" : "174.63647775" , "batteryLevel" : "45" }"
                var content = new StringContent(macAddress);
                //body: JSON.stringify('{ "macAddress" : "C0-11-73-6C-70-27", "latitude" : "-36.92577745", "longitude" : "174.63647775" , "batteryLevel" : "45" }')
                //  headers:{'Content-Type': 'application/json' }
                var url = String.Format("http://ibrium.33713/api/log");

                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
            }
        }
    }
}

