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

            if (isPaired()) {
                try {
                    GetData().NoAwait();
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

                await StopScanning();
                await Connect(new Band(args.Device));
            }
        }

        private void OnDeviceConnected(object sender, DeviceEventArgs args) {
            // TODO: Implement. Get needed services and characteristics, show data in UI.

            Console.WriteLine($"##### Connected to device {args.Device.Name}");
        }

        private async void OnScanTimeoutElapsed(object sender, EventArgs e) {
            await StopScanning();

            Console.WriteLine("##### Scan timeout elapsed");
            Console.WriteLine("##### No devices found");

            _userDialogs.Toast("No se pudo encontrar una Mi Band.\nIntenta de nuevo");
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            _device = null;

            Console.WriteLine($"##### Disconnected from device {e.Device.Name}");
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            _device = null;

            Console.WriteLine($"##### Device {e.Device.Name} disconnected :(");
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

        private async Task Connect(Band band) {
            try {
                using (band.Device) {
                    await _adapter.ConnectToDeviceAsync(band.Device, true);

                    IService mainService = await band.Device.GetServiceAsync(Guid.Parse("0000fee0-0000-1000-8000-00805f9b34fb"));
                    ICharacteristic steps = await mainService.GetCharacteristicAsync(Guid.Parse("0000ff06-0000-1000-8000-00805f9b34fb"));
                    Byte[] stepsValue = await steps.ReadAsync();

                    int stepsValueConverted = 0xff & stepsValue[0] | (0xff & stepsValue[1]) << 8;

                    _userDialogs.Alert("Steps", stepsValueConverted.ToString());
                }
            }
            catch (Exception ex) {
                _userDialogs.Alert(ex.Message, "Falló la conexión con Mi Band.");

                return;
            }
            finally { 
                _userDialogs.HideLoading();
            }

            _device = band;
            _userDialogs.ShowSuccess("Conectado a Mi Band");
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
            IService service;
            ICharacteristic steps;
            IDescriptor enableNotifications;
            Byte[] stepsValue;

            await _adapter.ConnectToDeviceAsync(_device.Device);

            _device = new Band(_device.Device);

            try {
                Console.WriteLine("##### Trying to get steps...");

                service = await _device.Device.GetServiceAsync(Guid.Parse("0000fee0-0000-1000-8000-00805f9b34fb"));
                steps = await ((IService) service).GetCharacteristicAsync(Guid.Parse("0000ff06-0000-1000-8000-00805f9b34fb"));
                enableNotifications = await steps.GetDescriptorAsync(Guid.Parse("00002902-0000-1000-8000-00805f9b34fb"));

                await enableNotifications.WriteAsync(BluetoothGattDescriptor.EnableNotificationValue.ToArray());

                stepsValue = await steps.ReadAsync();

                int stepsValueConverted = 0xff & stepsValue[0] | (0xff & stepsValue[1]) << 8;

                Console.WriteLine($"##### STEPS: {stepsValueConverted}");

                steps.ValueUpdated += (o, arguments) => {
                    stepsValue = arguments.Characteristic.Value;

                    stepsValueConverted = 0xff & stepsValue[0] | (0xff & stepsValue[1]) << 8;

                    Console.WriteLine($"##### STEPS UPDATED: {stepsValueConverted}");
                    _userDialogs.Alert("Steps", stepsValueConverted.ToString());
                };

                await steps.StartUpdatesAsync();
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error: {ex.Message}");
            }
        }

        private bool isPaired() {
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

                if (foundDevice) {
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
            // Get our button from the layout resource,
            // and attach an event to it
            _connectButton = FindViewById<Button>(Resource.Id.btnConnect);
            _connectButton.Click += OnConnectButtonClick;

            if (isPaired()) {
                try {
                    GetData().NoAwait();
                }
                catch (Exception ex) {
                    Console.WriteLine($"##### Error: {ex.Message}");
                }
            }
        }
    }
}