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
using Plugin.BLE.Abstractions;
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
        private Button _connectButton;
        private CancellationTokenSource _cancellationTokenSource;
        private IUserDialogs _userDialogs;
        private const int _requestEnableBluetooth = 2;


        /**************************************************************************

            Event handlers
         
         **************************************************************************/

        private void OnConnectButtonClick(object sender, EventArgs e) {
            Console.WriteLine("##### Clicked Connect button");

            if (!_ble.IsOn) {
                Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableIntent, _requestEnableBluetooth);
            }

            if (IsPaired()) {
                try {
                    Connect().NoAwait();
                }
                catch (Exception ex) {
                    Console.WriteLine($"##### Error: {ex.Message}");
                }
            }
            else { 
                Console.WriteLine("##### Band is not paired");

                Discover();
            }
        }

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
            // TODO: Implement. Get needed services and characteristics, show data in UI.

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

            Console.WriteLine($"##### Disconnected from device {e.Device.Name}");
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            Console.WriteLine($"##### Device {e.Device.Name} disconnected :(");
            Console.WriteLine("Trying to reconnect...");

            try {
                Connect().NoAwait();
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error: {ex.Message}");
            }
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

        private void Discover() {
            if (_ble.IsOn) {
                Console.WriteLine("##### Bluetooth is on");

                if (_adapter.IsScanning) return;

                _cancellationTokenSource = new CancellationTokenSource();

                Console.WriteLine("##### Beginning scan...");

                _userDialogs.ShowLoading("Buscando dispositivos...");
                _adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token).NoAwait();
            }
            else {
                _userDialogs.Toast("Bluetooth está desactivado");

                Console.WriteLine("##### Bluetooth is not on :(");
            }
        }

        private async Task Connect() {
            BluetoothDevice nativeDevice = (BluetoothDevice) _device.Device.NativeDevice;

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
                _userDialogs.HideLoading();
            }

            _device = new Band(_device.Device);
            _userDialogs.ShowSuccess("Conectado a Mi Band");

            try {
                await GetData();
            }
            catch (Exception ex) { 
                Console.WriteLine($"##### Error: {ex.Message}");
            }
        }

        private async Task Disconnect(Band band) {
            try {
                _userDialogs.ShowLoading($"Desconectando de {band.Device.Name}...");

                await _adapter.DisconnectDeviceAsync(band.Device);
            }
            catch (Exception ex) {
                _userDialogs.Alert(ex.Message, $"Error al desconectarse de {band.Device.Name}");

                return;
            }
            finally {
                _userDialogs.HideLoading();
            }
        }

        private async Task GetData() {
            try {
                Console.WriteLine("##### Trying to get steps...");

                //int steps = await _device.GetSteps();
                int heartRate = await _device.GetHeartRate();

                //Console.WriteLine($"##### STEPS: {steps}");
                Console.WriteLine($"##### HEART RATE: {heartRate}");

                /*
                bool stepsResult = await _device.SubscribeToSteps((o, arguments) => {
                    Byte[] stepsBytes;
                    int stepsValue;

                    stepsBytes = arguments.Characteristic.Value;
                    stepsValue = _device.DecodeSteps(stepsBytes);

                    Console.WriteLine($"##### STEPS UPDATED: {stepsValue}");
                    _userDialogs.Alert("Steps", stepsValue.ToString());
                });*/

                bool heartRateResult = await _device.SubscribeToHeartRate((o, arguments) => {
                    Byte[] heartRateBytes;
                    int heartRateValue;

                    heartRateBytes = arguments.Characteristic.Value;
                    heartRateValue = _device.DecodeHeartRate(heartRateBytes);

                    Console.WriteLine($"##### HEART RATE UPDATED: {heartRateValue}");
                    _userDialogs.Alert("Heart Rate", heartRateValue.ToString());
                });

                // if (!stepsResult) Console.WriteLine($"##### Error subscribing to steps");
                if (!heartRateResult) Console.WriteLine($"##### Error subscribing to heart rate");
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error: {ex.Message}");
            }
        }

        private bool IsPaired() {
            List<IDevice> pairedDevices = _adapter.GetSystemConnectedOrPairedDevices();
            bool foundDevice = false;

            if (pairedDevices.Count > 0) {
                foreach (IDevice device in pairedDevices) {
                    if (Band.MAC_ADDRESS_FILTER.Any(x => ((BluetoothDevice) device.NativeDevice).Address.StartsWith(x, StringComparison.InvariantCulture))) {
                        Console.WriteLine($"##### Paired device: {device.Name}");

                        foundDevice = true;
                        _device = new Band(device);
                    }
                }

                if (foundDevice && _device != null) {
                    Console.WriteLine("##### Band is already paired");

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

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            UserDialogs.Init(this);

            _userDialogs = UserDialogs.Instance;
            _connectButton = FindViewById<Button>(Resource.Id.btnConnect);
            _connectButton.Click += OnConnectButtonClick;

            if (IsPaired()) {
                try {
                    Connect().NoAwait();
                }
                catch (Exception ex) {
                    Console.WriteLine($"##### Error: {ex.Message}");
                }
            }
        }
    }
}