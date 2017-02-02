using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
using Android.Content;
// using Android.Runtime;
// using Android.Views;
using Android.Widget;
using Android.Locations;
using Android.Util;
using Android.OS;
using Android.Content.PM;
using Android.Bluetooth;
using Android.Support.V7.App;
using Toolbar = Android.Support.V7.Widget.Toolbar;

using Acr.UserDialogs;

using Plugin.BLE;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Extensions;

namespace EcoBand {
    [Activity(Label = "EcoBand", Icon = "@drawable/heart_on", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]

    public class Main : AppCompatActivity, ILocationListener {
        public Main() {
            _beatsBuffer = new Queue<int>(5);

            _ble.StateChanged += OnStateChanged;
            _adapter.ScanTimeoutElapsed += OnScanTimeoutElapsed;
            _adapter.DeviceConnected += OnDeviceConnected;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.DeviceDisconnected += OnDeviceDisconnected;
            _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
        }

        ~Main() {
            CleanupCancellationToken();
        }


        /**************************************************************************

            Internal properties
         
         **************************************************************************/

        private static readonly Plugin.BLE.Abstractions.Contracts.IAdapter _adapter = CrossBluetoothLE.Current.Adapter;
        private static readonly IBluetoothLE _ble = CrossBluetoothLE.Current;
        private static Band _device;
        private static TextView _heartRateLabel;
        private static TextView _stepsLabel;
        private static TextView _latitudeLabel;
        private static TextView _longitudeLabel;
        private static CancellationTokenSource _cancellationTokenSource;
        private static IUserDialogs _userDialogs;
        private static Timer _measurementsTimer;
        private static Timer _stepsTimer;
        private static LocationManager _locationManager;
        private static bool _isConnecting;
        private static Queue<int> _beatsBuffer;
        private static int _stepsBuffer = 0;
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

        private async void OnDeviceDiscovered(object sender, DeviceEventArgs args) {
            if (Band.NAME_FILTER.Contains(args.Device.Name) &&
                Band.MAC_ADDRESS_FILTER.Any(x => ((BluetoothDevice) args.Device.NativeDevice).Address.StartsWith(x, StringComparison.InvariantCulture))) {
                Log.Debug("MAIN", $"##### Discovered device {args.Device.Name}");

                _device = new Band(args.Device);

                try {
                    await StopScanning();
                    await CheckConnection();
                }
                catch (Exception ex) {
                    Log.Error("MAIN", $"Error: {ex.Message}");

                    return;
                }
            }
        }

        private void OnDeviceConnected(object sender, DeviceEventArgs args) {
            Log.Debug("MAIN", $"##### Connected to device {args.Device.Name}");
        }

        private async void OnScanTimeoutElapsed(object sender, EventArgs e) {
            await StopScanning();
            await CheckConnection();

            Log.Debug("MAIN", "##### Scan timeout elapsed");
            Log.Debug("MAIN", "##### No devices found");

            _userDialogs.Toast("No se pudo encontrar una Mi Band.\nIntenta de nuevo");
        }

        private async void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            _device = null;

            Log.Debug("MAIN", "##### Trying to reconnect...");

            try {
                await CheckConnection();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"##### Error connecting to device: {ex.Message}");
            }
        }

        private async void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            _device = null;

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

            try {
                SetMeasurementsTimer();
                StartMeasuring().NoAwait();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"##### Error starting measurements: {ex.Message}");

                HideFirstMeasurementSpinner();
            }
        }

        private void OnStepsTime(object timerState) {
            TimerState state;
            DateTime now;
            TimeSpan interval;
            double steps;

            state = (TimerState) timerState;

            state.Dispose();

            state = null;
            now = DateTime.Now;

            Log.Debug("MAIN", "##### Starting new steps cycle...");

            try {
                if (_device == null) CheckConnection().NoAwait();
                if (_lastStepTimestamp != null) {
                    interval = now - ((DateTime) _lastStepTimestamp);
                    steps = _stepsBuffer * (60000 / interval.TotalMilliseconds);

                    RunOnUiThread(() => {
                        _stepsLabel.Text = Math.Round(steps, MidpointRounding.AwayFromZero).ToString();
                    });

                    _lastStepTimestamp = now;
                    _stepsBuffer = 0;
                }

                SetStepsTimer();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error starting a new steps cycle: {ex.Message}");

                HideFirstMeasurementSpinner();
            }
        }

        private void OnStepsChange(object sender, MeasureEventArgs e) {
            if (_lastStepTimestamp == null) _lastStepTimestamp = DateTime.Now;

            _stepsBuffer++;
            HideFirstMeasurementSpinner();
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

            RunOnUiThread(() => { 
                _heartRateLabel.Text = e.Measure.ToString();

                Log.Debug("UI THREAD", $"##### BEATS: {e.Measure}");
            });

            HideFirstMeasurementSpinner();
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


        /**************************************************************************

            Private methods
         
         **************************************************************************/

        private async Task StopScanning() {
            try {
                await _adapter.StopScanningForDevicesAsync();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"StopScanning() failed :( with error: {ex.Message}");

                return;
            }
            finally {
                CleanupCancellationToken();
            }

            Log.Debug("MAIN", "##### Stopped scanning");
        }

        private void CleanupCancellationToken() {
            if (_cancellationTokenSource != null) {
                _cancellationTokenSource.Dispose();

                _cancellationTokenSource = null;
            }
        }

        private void EnableBluetooth() {
            Intent intent;

            intent = new Intent(BluetoothAdapter.ActionRequestEnable);

            StartActivityForResult(intent, _requestEnableBluetooth);
        }

        private void CheckGPS() {
            if (!_locationManager.IsProviderEnabled(LocationManager.GpsProvider)) _userDialogs.ShowError("El GPS está desactivado");
        }

        private bool IsPaired() {
            List<IDevice> pairedDevices;

            pairedDevices = _adapter.GetSystemConnectedOrPairedDevices();

            if (pairedDevices.Count > 0) {
                foreach (IDevice device in pairedDevices) {
                    if (Band.MAC_ADDRESS_FILTER.Any(x => ((BluetoothDevice) device.NativeDevice).Address.StartsWith(x, StringComparison.InvariantCultureIgnoreCase))) {
                        Log.Debug("MAIN", $"##### Paired device: {device.Name}");

                        if (_device == null) _device = new Band(device);

                        return true;
                    }
                }
            }

            Log.Debug("MAIN", "##### Band is not paired");

            return false;
        }

        private async Task CheckConnection() {
            if (!_ble.IsOn) EnableBluetooth();
            else {
                if (IsPaired()) {
                    if (_adapter.ConnectedDevices.Count == 0) {
                        ShowFirstMeasurementSpinner();

                        try {
                            await Connect();
                        }
                        catch (Exception ex) {
                            Log.Error("MAIN", $"Error connecting to device: {ex.Message}");

                            HideFirstMeasurementSpinner();
                        }
                    }
                }
                else if (!_adapter.IsScanning) {
                    Log.Debug("MAIN", "##### Band is not paired");

                    await Discover();
                }
            }
        }

        private async Task Discover() {
            if (_ble.IsOn) {
                Log.Debug("MAIN", "##### Bluetooth is on");

                if (!_adapter.IsScanning) {
                    _cancellationTokenSource = new CancellationTokenSource();

                    Log.Debug("MAIN", "##### Beginning scan...");

                    RunOnUiThread(() => {
                        _userDialogs.ShowLoading("Buscando dispositivos...");
                    });

                    await _adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token);
                    await CheckConnection();
                }
            }
            else { 
                Log.Debug("MAIN", "##### Bluetooth is not on :(");

                _userDialogs.Toast("Bluetooth está desactivado");
            }
        }

        private async Task Connect() {
            BluetoothDevice nativeDevice;

            if (!_isConnecting) { 
                _isConnecting = true;

                nativeDevice = (BluetoothDevice) _device.Device.NativeDevice;

                try {
                    Log.Debug("MAIN", "##### Trying to connect...");

                    await _adapter.ConnectToDeviceAsync(_device.Device, true);

                    if (nativeDevice.BondState == Bond.None) {
                        Log.Debug("MAIN", "##### Bonding...");

                        nativeDevice.CreateBond();
                    }
                    else Log.Debug("MAIN", "##### Already bonded");
                }
                catch (Exception ex) {
                    _userDialogs.Alert(ex.Message, "Falló la conexión con Mi Band.");

                    return;
                }
                finally {
                    RunOnUiThread(() => {
                        _userDialogs.HideLoading();
                    });

                    _isConnecting = false;
                }

                _device = new Band(_device.Device);

                SetDeviceEventHandlers().NoAwait();
            }
        }

        private async Task Disconnect(Band band) {
            try {
                RunOnUiThread(() => {
                    _userDialogs.ShowLoading($"Desconectando de {band.Device.Name}...");
                });

                await _adapter.DisconnectDeviceAsync(band.Device);
            }
            catch (Exception ex) {
                _userDialogs.Alert(ex.Message, $"Error al desconectarse de {band.Device.Name}");

                return;
            }
            finally {
                RunOnUiThread(() => {
                    _userDialogs.HideLoading();
                });
            }
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

        private async Task SetDeviceEventHandlers() {
            try {
                _device.Steps += OnStepsChange;
                _device.HeartRate += OnHeartRateChange;

                await StartMeasuring();

                SetMeasurementsTimer();
                SetStepsTimer();
            }
            catch (Exception ex) {
                Log.Error("MAIN", $"Error setting device event handlers: {ex.Message}");
            }
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

        private async Task StartMeasuring() {
            bool isMeasuringHeartRate;
            bool isMeasuringSteps;

            try {
                isMeasuringHeartRate = await _device.StartMeasuringHeartRate();
                isMeasuringSteps = await _device.StartMeasuringSteps();

                if (!isMeasuringHeartRate) Alert("Error del dispositivo", "No se pudo empezar a medir las pulsaciones");
                if (!isMeasuringSteps) Alert("Error del dispositivo", "No se pudo empezar a medir los pasos");
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

        private void ShowFirstMeasurementSpinner() {
            RunOnUiThread(() => {
                _userDialogs.ShowLoading("Activando sensores...");
            });
        }

        private void HideFirstMeasurementSpinner() {
            RunOnUiThread(() => {
                _userDialogs.HideLoading();
            });
        }


        /**************************************************************************

            Protected methods
         
         **************************************************************************/

        protected override void OnCreate(Bundle savedInstanceState) {
            base.OnCreate(savedInstanceState);

            // LayoutInflater inflater = Application.Context.GetSystemService(Context.LayoutInflaterService) as LayoutInflater;
            // View layout = inflater.Inflate(Resource.Layout.Main, null);

            SetContentView(Resource.Layout.Main);
            UserDialogs.Init(this);

            _userDialogs = UserDialogs.Instance;
            _heartRateLabel = FindViewById<TextView>(Resource.Id.lblHeartBeats);
            _stepsLabel = FindViewById<TextView>(Resource.Id.lblStepsPerMinute);
            _latitudeLabel = FindViewById<TextView>(Resource.Id.lblLatitude);
            _longitudeLabel = FindViewById<TextView>(Resource.Id.lblLongitude);
            _locationManager = (LocationManager) GetSystemService(LocationService);
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