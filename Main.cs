using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
using Android.Widget;
using Android.Locations;
using Android.Util;
using Android.OS;
using Android.Content.PM;
using Android.Bluetooth;
using Android.Support.V7.App;
using Android.Views;
using Android.Graphics.Drawables;
using Android.Graphics;

using AndroidHUD;
using Acr.UserDialogs;

using Firebase.Xamarin.Token;
using Firebase.Xamarin.Database;

using Plugin.BLE;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Extensions;
using System.Net;

namespace EcoBand {
    [Activity(Label = "EcoBand", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]

    public class Main : AppCompatActivity, ILocationListener {
        public Main() {
            _firebase = new FirebaseClient("https://demo.firebaseio.com/");
            _firebaseTokenGenerator = new TokenGenerator("demo");
            _beatsBuffer = new Queue<int>(7);

            _ble.StateChanged += OnStateChanged;
            _adapter.ScanTimeoutElapsed += OnScanTimeoutElapsed;
            _adapter.DeviceAdvertised += OnDeviceAdvertised;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.DeviceConnected += OnDeviceConnected;
            _adapter.DeviceDisconnected += OnDeviceDisconnected;
            _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
        }


        /**************************************************************************

            Internal properties
         
         **************************************************************************/

        private static readonly Plugin.BLE.Abstractions.Contracts.IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        private static readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
        private static Band _band;
        private static LocationManager _locationManager;

        private static FirebaseClient _firebase;
        private static TokenGenerator _firebaseTokenGenerator;
        private static string _firebaseToken;

        private static Android.Support.V7.Widget.Toolbar _toolbar;
        private static IMenuItem _heartRateIcon;
        private static TextView _heartRateLabel;
        private static TextView _stepsLabel;
        private static TextView _latitudeLabel;
        private static TextView _longitudeLabel;
        private static IUserDialogs _userDialogs;

        private static Timer _measurementsTimer;
        private static Timer _stepsTimer;

        // State
        private static bool _isConnecting;
        private static int _heartRateErrors;
        private static int _stepsErrors;
        private static bool _gotFirstMeasurement;
        private static Queue<int> _beatsBuffer;
        private static int _stepsBuffer;
        private static DateTime? _lastStepTimestamp;
        private const int _measurementInterval = 15000;
        private const int _stepsInterval = 5000;
        private const int _requestEnableBluetooth = 2;


        /**************************************************************************

            Event handlers
         
         **************************************************************************/

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e) {
            Log.Debug("MAIN", $"##### State changed to {e.NewState.ToString()}");
        }

        private async void OnScanTimeoutElapsed(object sender, EventArgs e) {
            await StopScanning();

            if (_band == null) await CheckConnection();

            Log.Debug("MAIN", "##### Scan timeout elapsed");
            Log.Debug("MAIN", "##### No devices found");

            Toast("No se pudo encontrar una Mi Band", 2000);
        }

        private async void OnDeviceAdvertised(object sender, DeviceEventArgs args) {
            if (IsKnownDevice(args.Device)) {
                Log.Debug("MAIN", $"##### Device advertised {args.Device.Name}");

                await SetUpDevice(args.Device);
            }
        }

        private async void OnDeviceDiscovered(object sender, DeviceEventArgs args) {
            if (IsKnownDevice(args.Device)) {
                Log.Debug("MAIN", $"##### Discovered device {args.Device.Name}");

                await SetUpDevice(args.Device);
            }
        }

        private void OnDeviceConnected(object sender, DeviceEventArgs args) {
            Log.Debug("MAIN", $"##### Connected to device {args.Device.Name}");

            SetFirebaseToken(((BluetoothDevice) _band.Device.NativeDevice).Address);
        }

        private async void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            _band = null;

            ((AnimationDrawable) _heartRateIcon.Icon).Stop();

            Log.Debug("MAIN", "##### Trying to reconnect...");

            try {
                await CheckConnection();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"##### Error connecting to device: {ex.Message}");
            }
        }

        private async void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            _band = null;

            ((AnimationDrawable) _heartRateIcon.Icon).Stop();

            Log.Debug("MAIN", $"##### Device {e.Device.Name} disconnected :(");
            Log.Debug("MAIN", "##### Trying to reconnect...");

            try {
                await CheckConnection();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error connecting to device: {ex.Message}");
            }
        }

        private void OnMeasurementTime(object timerState) {
            TimerState state;

            state = (TimerState) timerState;

            state.Dispose();

            state = null;

            Log.Debug("MAIN", "##### Starting new measurement cycle...");

            PauseAnimation(_heartRateIcon);

            try {
                StartMeasuringActivity().NoAwait();
                SetMeasurementsTimer();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"##### Error starting measurements: {ex.Message}");
            }
        }

        private void OnStepsTime(object timerState) {
            TimerState state;
            DateTime now;
            TimeSpan interval;
            double steps;
            double roundedSteps;

            state = (TimerState) timerState;

            state.Dispose();

            state = null;
            now = DateTime.Now;

            Log.Debug("MAIN", "##### Starting new steps cycle...");

            try {
                Refresh().NoAwait();

                if (_lastStepTimestamp != null) {
                    interval = now - ((DateTime) _lastStepTimestamp);
                    steps = _stepsBuffer * (60000 / interval.TotalMilliseconds);
                    roundedSteps = Math.Round(steps, MidpointRounding.AwayFromZero);

                    SaveActivity((int) roundedSteps, "stepsPerMinute").NoAwait();

                    RunOnUiThread(() => {
                        _stepsLabel.Text =roundedSteps.ToString();
                    });

                    _lastStepTimestamp = now;
                    _stepsBuffer = 0;
                }

                SetStepsTimer();

            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error starting a new steps cycle: {ex.Message}");
            }
        }

        private void OnStepsChange(object sender, MeasureEventArgs e) {
            if (_lastStepTimestamp == null) _lastStepTimestamp = DateTime.Now;
            if (!_gotFirstMeasurement) {
                _gotFirstMeasurement = true;

                HideSpinner();
            }

            _stepsBuffer++;
        }

        private void OnHeartRateChange(object sender, MeasureEventArgs e) {
            double average;
            double upperLimit;
            double lowerLimit;

            if (_beatsBuffer.Count > 0) {
                average = _beatsBuffer.Average();
                upperLimit = average * 1.5;
                lowerLimit = average / 2f;

                if (e.Measure > upperLimit || e.Measure < lowerLimit) return;
            }

            _beatsBuffer.Enqueue(e.Measure);
            SaveActivity(e.Measure, "beatsPerMinute").NoAwait();

            if (!_gotFirstMeasurement) {
                _gotFirstMeasurement = true;

                LoadHeartAnimation(Resource.Drawable.heart_animation);
                HideSpinner();
            }

            RunOnUiThread(() => {
                AnimationDrawable heartRateAnimation;

                heartRateAnimation = (AnimationDrawable) _heartRateIcon.Icon;
                _heartRateLabel.Text = e.Measure.ToString();

                if (!heartRateAnimation.IsRunning) heartRateAnimation.Start();
            });
        }

        public void OnProviderDisabled(string provider) {
            _locationManager.RemoveUpdates(this);
        }

        public void OnProviderEnabled(string provider) {
            StartMeasuringLocation();
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras) {

        }

        public void OnLocationChanged(Location location) {
            RunOnUiThread(() => {
                _latitudeLabel.Text = location.Latitude.ToString();
                _longitudeLabel.Text = location.Longitude.ToString();
            });

            /*
            // demo geocoder
            new Thread(() => {
                Geocoder geocoder = new Geocoder(this);

                IList<Address> addresses = geocoder.GetFromLocation(location.Latitude, location.Longitude, 5);

               addresses.ToList().ForEach((addr) => addrText.Append(addr.ToString() + "\r\n\r\n"));
            }).Start();
            */
        }

        public override bool OnCreateOptionsMenu(IMenu menu) {
            MenuInflater.Inflate(Resource.Menu.status, menu);

            _heartRateIcon = menu.FindItem(Resource.Id.menuStatusHeartState);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item) {
            if (item.ItemId == Resource.Id.menuStatusHeartState) {
                if (_heartRateErrors > 0) Toast("No está midiendo pulsaciones, hubo un error al comunicarse con el dispositivo", 1000);
                else { 
                    if (!_gotFirstMeasurement) Toast("Activando sensores...", 1000);
                    else Toast("Está midiendo pulsaciones", 1000);
                }
            }

            return base.OnOptionsItemSelected(item);
        }


        /**************************************************************************

            Private methods
         
         **************************************************************************/

        private async Task StopScanning() {
            try {
                await _adapter.StopScanningForDevicesAsync();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error while stopping device scan: {ex.Message}");

                return;
            }

            Log.Debug("MAIN", "##### Stopped scanning");
        }

        private void EnableBluetooth() {
            Intent intent;

            intent = new Intent(BluetoothAdapter.ActionRequestEnable);

            StartActivityForResult(intent, _requestEnableBluetooth);
        }

        private void CheckGPS() {
            if (!_locationManager.IsProviderEnabled(LocationManager.GpsProvider)) _userDialogs.ShowError("El GPS está desactivado");
        }

        private async Task CheckConnection() {
            if (!_ble.IsOn) EnableBluetooth();
            else {
                if (IsPaired()) {
                    if (_adapter.ConnectedDevices.Count == 0) await TryConnect();
                }
                else if (!_adapter.IsScanning) {
                    Log.Debug("MAIN", "##### Band is not paired");

                    await Discover();
                }
            }
        }

        private bool IsKnownDevice(IDevice device) {
            return Band.NAME_FILTER.Contains(device.Name) &&
                       Band.MAC_ADDRESS_FILTER.Any(x => ((BluetoothDevice) device.NativeDevice).Address.StartsWith(x, StringComparison.InvariantCulture));
        }

        private bool IsPaired() {
            List<IDevice> pairedDevices;

            pairedDevices = _adapter.GetSystemConnectedOrPairedDevices();

            if (pairedDevices.Count > 0) {
                foreach (IDevice device in pairedDevices) {
                    if (IsKnownDevice(device)) {
                        Log.Debug("MAIN", $"##### Paired device: {device.Name}");

                        if (_band == null) _band = new Band(device);

                        return true;
                    }
                }
            }

            Log.Debug("MAIN", "##### Band is not paired");

            return false;
        }

        private async Task Discover() {
            if (_ble.IsOn) {
                Log.Debug("MAIN", "##### Bluetooth is on");

                if (!_adapter.IsScanning) {
                    using (CancellationTokenSource tokenSource = new CancellationTokenSource()) { 
                        Log.Debug("MAIN", "##### Beginning scan...");

                        ShowSpinner("Buscando dispositivos");

                        await _adapter.StartScanningForDevicesAsync(tokenSource.Token);

                        Log.Debug("MAIN", $"##### Just finished scanning, found {_adapter.ConnectedDevices.Count} devices");
                    }
                }
            }
            else { 
                Log.Debug("MAIN", "##### Bluetooth is not on :(");

                Toast("Bluetooth está desactivado", 2000);
            }
        }

        private async Task Connect() {
            BluetoothDevice nativeDevice;

            if (!_isConnecting) {
                _isConnecting = true;

                nativeDevice = (BluetoothDevice) _band.Device.NativeDevice;

                using (CancellationTokenSource tokenSource = new CancellationTokenSource()) {
                    try {
                        Log.Debug("MAIN", "##### Trying to connect...");

                        await _adapter.ConnectToDeviceAsync(_band.Device, true, tokenSource.Token).TimeoutAfter(TimeSpan.FromSeconds(10), tokenSource);

                        if (nativeDevice.BondState == Bond.None) {
                            Log.Debug("MAIN", "##### Bonding...");

                            nativeDevice.CreateBond();
                        }
                        else Log.Debug("MAIN", "##### Already bonded");

                        _isConnecting = false;
                    }
                    catch (Exception ex) {
                        Log.Error("MAIN", $"Connection attempt failed: {ex.Message}");

                        _isConnecting = false;

                        await Connect();
                    }
                }
            }
            else Log.Debug("MAIN", "##### Connection underway, skipping attempt");
        }

        private async Task Disconnect() {
            try {
                await _adapter.DisconnectDeviceAsync(_band.Device);
            }
            catch (Exception ex) {
                _userDialogs.Alert(ex.Message, $"Error al desconectarse de {_band.Device.Name}");
            }
        }

        private async Task Refresh() { 
            try {
                if (_heartRateErrors > 1 || _stepsErrors > 1) {
                    Log.Debug("MAIN", "Refreshing connection...");

                    await Disconnect();
                    await CheckConnection();
                    await SetUpActivities();
                }
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error refreshing connection: {ex.Message}");
            }
        }

        private async Task<bool> SaveActivity(int measurement, string type) {
            Dictionary<string, object> record;
            FirebaseObject<Dictionary<string, object>> result;

            if (measurement <= 0) return false;

            record = new Dictionary<string, object>() {
                { "type", type },
                { "value", measurement },
                { "timestamp", DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds }
            };

            try {
                result = await _firebase
                    .Child(((BluetoothDevice) _band.Device.NativeDevice).Address + "/activity")
                    // .WithAuth(_firebaseToken)
                    .PostAsync(record);

                if (result.Object.Count > 0) return true;

                return false;

            }
            catch (WebException ex) {
                // if (ex.Status == WebExceptionStatus.NameResolutionFailure) return false;
                Log.Error("MAIN", $"Error saving data: {ex.Message}");

                return false;
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error saving data: {ex.Message}");

                return false;
            }
        }

        private async Task TryConnect() { 
            ShowSpinner("Conectando");

            try {
                await Connect();

                SetDeviceEventHandlers();

                await SetUpActivities();

            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error connecting to device: {ex.Message}");

                HideSpinner();
            }
        }

        private async Task SetUpDevice(IDevice device) { 
            if (_band == null) {
                _band = new Band(device);

                try {
                    await StopScanning();

                    HideSpinner();

                    await TryConnect();
                }
                catch (Exception ex) {
                    Log.Error("MAIN", $"Error setting up device: {ex.Message}");

                    return;
                }
            }
        }

        private async Task SetUpActivities() {
            try {
                await StartMeasuringActivity();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error starting to measure activity: {ex.Message}");
            }

            SetMeasurementsTimer();
            SetStepsTimer();
        }

        private void SetTimer(int time, Timer instance, TimerCallback callback) {
            TimerCallback timerDelegate;
            TimerState state;

            timerDelegate = new TimerCallback(callback);
            state = new TimerState();
            instance = new Timer(timerDelegate, state, time, time);
            state.instance = instance;
        }

        private void SetMeasurementsTimer() {
            SetTimer(_measurementInterval, _measurementsTimer, OnMeasurementTime);
        }

        private void SetStepsTimer() {
            SetTimer(_stepsInterval, _stepsTimer, OnStepsTime);
        }

        private void SetDeviceEventHandlers() {
            _band.Steps += OnStepsChange;
            _band.HeartRate += OnHeartRateChange;
        }

        private void SetFirebaseToken(string macAddress) { 
            Dictionary<string, object> payload;

            payload = new Dictionary<string, object>() {
                { "uid", macAddress}
            };
            _firebaseToken = _firebaseTokenGenerator.CreateToken(payload);
        }

        private void StartMeasuringLocation() {
            Criteria locationCriteria;

            locationCriteria = new Criteria();
            locationCriteria.Accuracy = Accuracy.Fine;
            locationCriteria.PowerRequirement = Power.NoRequirement;

            string locationProvider = _locationManager.GetBestProvider(locationCriteria, true);

            if (!string.IsNullOrEmpty(locationProvider)) _locationManager.RequestLocationUpdates(locationProvider, 10000, 1, this);
            else Log.Warn("MAIN", "Could not determine a location provider.");
        }

        private async Task StartMeasuringActivity() {
            bool isMeasuringHeartRate;
            bool isMeasuringSteps;

            try {
                isMeasuringHeartRate = await _band.StartMeasuringHeartRate();
                isMeasuringSteps = await _band.StartMeasuringSteps();

                if (!isMeasuringHeartRate) {
                    _heartRateErrors++;

                    PauseAnimation(_heartRateIcon);
                }
                else _heartRateErrors = 0;

                if (!isMeasuringSteps) _stepsErrors++;
                else _stepsErrors = 0;
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error starting measurements: {ex.Message}");
            }
        }

        private void Alert(string title, string message) {
            RunOnUiThread(() => {
                _userDialogs.Alert(message, title);
            });
        }

        private void Toast(string message, int milliseconds, bool centered=false) { 
            AndHUD.Shared.ShowToast(this, message, AndroidHUD.MaskType.Clear, TimeSpan.FromMilliseconds(milliseconds), centered);
        }

        private void ShowSpinner(string message) {
            AndHUD.Shared.Dismiss(this);

            RunOnUiThread(() => {
                _userDialogs.ShowLoading(message);
            });
        }

        private void HideSpinner() {
            RunOnUiThread(() => {
                _userDialogs.HideLoading();
            });
        }

        private void LoadHeartAnimation(int animation) {
            RunOnUiThread(() => { 
                _heartRateIcon.SetIcon(animation);

                ((AnimationDrawable) _heartRateIcon.Icon).Start();
            });
        }

        private void PauseAnimation(IMenuItem target) {
            Drawable currentFrame;
            Drawable checkFrame;
            AnimationDrawable animation;
            AnimationDrawable newAnimation;

            newAnimation = new AnimationDrawable();

            RunOnUiThread(() => {
                animation = (AnimationDrawable) target.Icon;
                animation.Stop();

                currentFrame = animation.Current;

                for (int i = 0; i < animation.NumberOfFrames; i++) {
                    checkFrame = animation.GetFrame(i);

                    if (checkFrame == currentFrame) { 
                        for (int k = i; k < animation.NumberOfFrames; k++) {
                            newAnimation.AddFrame(animation.GetFrame(k), 100);
                        }

                        for (int k = 0; k < i; k++) {
                            newAnimation.AddFrame(animation.GetFrame(k), 100);
                        }

                        break;
                    }
                }

                target.SetIcon(newAnimation);
            });
        }


        /**************************************************************************

            Protected methods
         
         **************************************************************************/

        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);

            Typeface rubikLight;
            Typeface rubikRegular;

            SetContentView(Resource.Layout.Main);
            UserDialogs.Init(this);

            _toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);

            SetSupportActionBar(_toolbar);

            _userDialogs = UserDialogs.Instance;
            _heartRateLabel = FindViewById<TextView>(Resource.Id.lblHeartRateCount);
            _stepsLabel = FindViewById<TextView>(Resource.Id.lblStepsCount);
            _latitudeLabel = FindViewById<TextView>(Resource.Id.lblLatitudeValue);
            _longitudeLabel = FindViewById<TextView>(Resource.Id.lblLongitudeValue);
            _locationManager = (LocationManager) GetSystemService(LocationService);

            rubikLight = Typeface.CreateFromAsset(Application.Context.Assets, "fonts/Rubik-Light.ttf");
            rubikRegular = Typeface.CreateFromAsset(Application.Context.Assets, "fonts/Rubik-Regular.ttf");

            _heartRateLabel.Typeface = rubikLight;
            _stepsLabel.Typeface = rubikLight;
            FindViewById<TextView>(Resource.Id.lblHeartRateTitle).Typeface = rubikRegular;
            FindViewById<TextView>(Resource.Id.lblStepsTitle).Typeface = rubikRegular;
            FindViewById<TextView>(Resource.Id.lblLatitudeTitle).Typeface = rubikRegular;
            FindViewById<TextView>(Resource.Id.lblLatitudeValue).Typeface = rubikRegular;
            FindViewById<TextView>(Resource.Id.lblLongitudeTitle).Typeface = rubikRegular;
            FindViewById<TextView>(Resource.Id.lblLongitudeValue).Typeface = rubikRegular;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data) {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == _requestEnableBluetooth) {
                if (resultCode == Result.Ok) CheckConnection().NoAwait();
                else _userDialogs.ShowError("No es posible usar la aplicación sin Bluetooth");
            }
        }

        protected override void OnPause() {
            base.OnPause();

            _locationManager.RemoveUpdates(this);
        }

        protected override void OnResume() {
            base.OnResume();

            CheckGPS();
            CheckConnection().NoAwait();
            StartMeasuringLocation();

            _gotFirstMeasurement = false;
        }
    }

    class TimerState : IDisposable {
        public Timer instance;
        private bool _disposed;

        public TimerState() {
            _disposed = false;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!_disposed && disposing) {
                instance.Dispose();
                instance = null;

                _disposed = true;
            }
        }
    }
}