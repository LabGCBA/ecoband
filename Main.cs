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
using Android.OS;
using Android.Content.PM;
using Android.Bluetooth;

using Acr.UserDialogs;

using Plugin.BLE;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Extensions;

namespace EcoBand {
    [Activity(Label = "Mi Band", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]

    public class Main : Activity {
        public Main() {
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;

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

        private readonly Plugin.BLE.Abstractions.Contracts.IAdapter _adapter;
        private readonly IBluetoothLE _ble;
        private Band _device;
        private TextView _heartRateLabel;
        private CancellationTokenSource _cancellationTokenSource;
        private IUserDialogs _userDialogs;
        private Timer _measurements;
        private const int _measurementInterval = 15000;
        private const int _requestEnableBluetooth = 2;


        /**************************************************************************

            Event handlers
         
         **************************************************************************/

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e) {
            Console.WriteLine($"##### State changed to {e.NewState.ToString()}");
        }

        private async void OnDeviceDiscovered(object sender, DeviceEventArgs args) {
            if (Band.NAME_FILTER.Contains(args.Device.Name) && 
                Band.MAC_ADDRESS_FILTER.Any(x => ((BluetoothDevice) args.Device.NativeDevice).Address.StartsWith(x, StringComparison.InvariantCulture))) {
                Console.WriteLine($"##### Discovered device {args.Device.Name}");

                _device = new Band(args.Device);

                try {
                    await StopScanning();
                    await Connect();
                }
                catch (Exception ex) { 
                    Console.WriteLine($"##### Error: {ex.Message}");

                    return;
                }
            }
        }

        private void OnDeviceConnected(object sender, DeviceEventArgs args) {
            Console.WriteLine($"##### Connected to device {args.Device.Name}");
        }

        private void OnScanTimeoutElapsed(object sender, EventArgs e) {
            StopScanning().NoAwait();

            Console.WriteLine("##### Scan timeout elapsed");
            Console.WriteLine("##### No devices found");

            _userDialogs.Toast("No se pudo encontrar una Mi Band.\nIntenta de nuevo");
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            _device = null;

            Console.WriteLine("##### Trying to reconnect...");

            try {
                CheckConnection().NoAwait();
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error connecting to device: {ex.Message}");
            }
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            _device = null;

            Console.WriteLine($"##### Device {e.Device.Name} disconnected :(");
            Console.WriteLine("##### Trying to reconnect...");

            try {
                CheckConnection().NoAwait();
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error connecting to device: {ex.Message}");
            }
        }

        private void OnMeasurementTime (object timerState) {
            TimerState state;

            state = (TimerState) timerState;

            state.Dispose();
            state = null;

            try {
                Console.WriteLine("##### Starting new measurement cycle...");

                SetTimer(_measurementInterval);
                StartMeasuring().NoAwait();
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error starting measurements: {ex.Message}");

                HideFirstMeasurementSpinner();
            }
        }

        private void OnStepsChange(object sender, MeasureEventArgs e) {
            Console.WriteLine($"##### Received steps value: {e.Measure}");

            HideFirstMeasurementSpinner();

        }

        private void OnHeartRateChange(object sender, MeasureEventArgs e) {
            RunOnUiThread(() => {
                _heartRateLabel.Text = e.Measure.ToString();

                HideFirstMeasurementSpinner();
            });
        }


        /**************************************************************************

            Private methods
         
         **************************************************************************/

        private async Task StopScanning() {
            try {
                await _adapter.StopScanningForDevicesAsync();
            }
            catch (Exception ex) {
                Console.WriteLine($"##### StopScanning() failed :( with error: {ex.Message}");

                return;
            }
            finally {
                CleanupCancellationToken();
            }

            Console.WriteLine("##### Stopped scanning");
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

        private async Task CheckConnection() {
            if (!_ble.IsOn) {
                EnableBluetooth();
            }
            else { 
                if (IsPaired()) {
                    if (_adapter.ConnectedDevices.Count == 0) {
                        try {
                            await Connect();
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"##### Error connecting to device: {ex.Message}");
                        }
                    }
                    else Console.WriteLine("##### Device is connected");
                }
                else {
                    Console.WriteLine("##### Band is not paired");

                    await Discover();
                    CheckConnection().NoAwait();
                }
            }
        }

        private async Task Discover() {
            if (_ble.IsOn) {
                Console.WriteLine("##### Bluetooth is on");

                if (_adapter.IsScanning) return;

                _cancellationTokenSource = new CancellationTokenSource();

                Console.WriteLine("##### Beginning scan...");

                RunOnUiThread(() => {
                    _userDialogs.ShowLoading("Buscando dispositivos...");
                });

                await _adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token);
            }
            else {
                _userDialogs.Toast("Bluetooth está desactivado");

                Console.WriteLine("##### Bluetooth is not on :(");
            }
        }

        private async Task Connect() {
            BluetoothDevice nativeDevice;

            nativeDevice = (BluetoothDevice) _device.Device.NativeDevice;

            try {
                await _adapter.ConnectToDeviceAsync(_device.Device, true);

                if (nativeDevice.BondState == Bond.None) {
                    Console.WriteLine("##### Bonding...");

                    nativeDevice.CreateBond();
                }
                else Console.WriteLine("##### Already bonded");
            }
            catch (Exception ex) {
                _userDialogs.Alert(ex.Message, "Falló la conexión con Mi Band.");

                return;
            }
            finally { 
                RunOnUiThread(() => {
                    _userDialogs.HideLoading();
                });
            }

            _device = new Band(_device.Device);

            SetDeviceEventHandlers().NoAwait();
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

        private void SetTimer(int time) { 
            TimerCallback timerDelegate;
            TimerState state;

            timerDelegate = new TimerCallback(OnMeasurementTime);
            state = new TimerState();
            _measurements = new Timer(timerDelegate, state, time, time);
            state.instance = _measurements;
        }

        private async Task SetDeviceEventHandlers() { 
            try {
                _device.Steps += OnStepsChange;
                _device.HeartRate += OnHeartRateChange;

                await StartMeasuring();

                SetTimer(_measurementInterval);
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error setting device event handlers: {ex.Message}");
            }
        }

        private async Task StartMeasuring() {
            try {
                await _device.StartMeasuringHeartRate();
                await _device.StartMeasuringSteps();
                // await StartMeasuringLocation();
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error starting measurements: {ex.Message}");
            }
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

        private bool IsPaired() {
            List<IDevice> pairedDevices;
            bool foundDevice = false;

            pairedDevices = _adapter.GetSystemConnectedOrPairedDevices();

            if (pairedDevices.Count > 0) {
                foreach (IDevice device in pairedDevices) {
                    if (Band.MAC_ADDRESS_FILTER.Any(x => ((BluetoothDevice) device.NativeDevice).Address.StartsWith(x, StringComparison.InvariantCultureIgnoreCase))) {
                        Console.WriteLine($"##### Paired device: {device.Name}");

                        foundDevice = true;
                        _device = new Band(device);
                    }
                }

                if (foundDevice && _device != null) {
                    Console.WriteLine("##### Band is already paired");

                    ShowFirstMeasurementSpinner();

                    return true;
                }
            }

            Console.WriteLine("##### Band is not paired");

            return false;
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

            CheckConnection().NoAwait();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data) {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == _requestEnableBluetooth) { 
                if (resultCode == Result.Ok) CheckConnection().NoAwait();
                else RunOnUiThread(() => {
                    _userDialogs.ShowError("No es posible usar la aplicación sin Bluetooth");
                });
            }
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