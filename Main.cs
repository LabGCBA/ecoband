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
    [Activity(Label = "EcoBand", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]

    public class Main : Activity {
        public Main() {
            _ble = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            _nativeAdapter = BluetoothAdapter.DefaultAdapter;
            _devices = new List<Band>();
            _nativeConnections = new List<BluetoothDevice>();

            _ble.StateChanged += OnStateChanged;
            _adapter.ScanTimeoutElapsed += OnScanTimeoutElapsed;
            _adapter.DeviceConnected += OnDeviceConnected;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.DeviceDisconnected += OnDeviceDisconnected;
            _adapter.DeviceConnectionLost += OnDeviceConnectionLost;
        }


        /**************************************************************************

            Internal properties
         
         **************************************************************************/

        private readonly Plugin.BLE.Abstractions.Contracts.IAdapter _adapter;
        private readonly IBluetoothLE _ble;
        private readonly BluetoothAdapter _nativeAdapter;
        private List<BluetoothDevice> _nativeConnections;
        private Button _connectButton;
        private List<Band> _devices;
        private CancellationTokenSource _cancellationTokenSource;
        private IUserDialogs _userDialogs;
        private ActionSheetConfig _devicesModal = new ActionSheetConfig();
        private const string _deviceIdKey = "DeviceIdNavigationKey";
        private const string _serviceIdKey = "ServiceIdNavigationKey";
        private const string _characteristicIdKey = "CharacteristicIdNavigationKey";
        private const string _descriptorIdKey = "DescriptorIdNavigationKey";
        private const int _requestEnableBluetooth = 2;


        /**************************************************************************

            Event handlers
         
         **************************************************************************/

        private void OnConnectButtonClick(object sender, EventArgs e) {
            if (!_ble.IsOn) {
                Intent enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableIntent, _requestEnableBluetooth);
            }

            Discover();

            Console.WriteLine("##### Clicked Connect button");
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e) {
            Console.WriteLine($"##### State changed to {e.NewState.ToString()}");
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args) {
            // TODO: Implement. Add device to modal.

            Console.WriteLine($"##### Discovered device {args.Device.Name}");
            Console.WriteLine($"##### Number of options in modal: {_devicesModal.Options.Count}");
        }

        private void OnDeviceConnected(object sender, DeviceEventArgs args) {
            _devicesModal.Options = new List<ActionSheetOption>();

            // TODO: Implement. Get needed services and characteristics, show data in UI.

            Console.WriteLine($"##### Connected to device {args.Device.Name}");
        }

        private async void OnScanTimeoutElapsed(object sender, EventArgs e) {
            stopScanning();

            Console.WriteLine($"##### Scan timeout elapsed");

            if (_adapter.DiscoveredDevices.Count == 0) {
                _userDialogs.Toast("No se encontraron dispositivos");

                Console.WriteLine($"##### No devices found");
            }
            else {
                List<ActionSheetOption> options = new List<ActionSheetOption>();

                foreach (var device in _adapter.ConnectedDevices) {
                    //update rssi for already connected devices (so the 0 is not shown in the list)
                    try {
                        await device.UpdateRssiAsync();
                        // TODO: Check if rssi was really updated
                    }
                    catch (Exception ex) {
                        _userDialogs.ShowError($"Falló actualización de RSSI de {device.Name}\nError: {ex.Message}");
                    }
                }

                foreach (var device in _adapter.DiscoveredDevices) {
                    if (!_devices.Any(item => item.Device == device)) {
                        // TODO: Limit connection to actual Mi Bands
                        _devices.Add(new Band(device));
                        options.Add(new ActionSheetOption("Desconocido"));

                        Console.WriteLine($"##### Added device {device.Name} to the list");
                    }
                }

                _userDialogs.HideLoading();
                _devicesModal.Cancel = new ActionSheetOption("Cancelar", stopScanning);
                _devicesModal.Options = options;
                _devicesModal.UseBottomSheet = true;
                _userDialogs.ActionSheet(_devicesModal);
            }
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            // TODO: Implement
            Console.WriteLine($"##### Disconnected from device {e.Device.Name}");
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            // TODO: Implement
            Console.WriteLine($"##### Device {e.Device.Name} disconnected :(");
        }


        /**************************************************************************

            Private methods
         
         **************************************************************************/

        private void stopScanning() { 
            _adapter.StopScanningForDevicesAsync();
            _connectButton.Text = "Conectar";

            Console.WriteLine("##### Stopped scanning");
        }

        private void Discover() {
            if (_ble.IsOn) {
                Console.WriteLine("##### Bluetooth is on");

                if (_adapter.IsScanning) return;

                _connectButton.Text = "Buscando...";
                _cancellationTokenSource = new CancellationTokenSource();
                _devices.Clear();

                Console.WriteLine("##### Beginning scan...");

                _userDialogs.ShowLoading("Buscando dispositivos...");
                _adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token).NoAwait();
            }
            else {
                _userDialogs.Toast("Bluetooth está desactivado");

                Console.WriteLine("##### Bluetooth is not on :(");
            }
        }

        private async Task Connect(Band device) {
            await _adapter.StopScanningForDevicesAsync();
            // TODO: Implement
        }

        private async Task Disconnect(Band band) {
            if (band.Device.State != DeviceState.Connected) return;

            try {
                _userDialogs.ShowLoading($"Desconectando de {band.Device.Name}...");

                await _adapter.DisconnectDeviceAsync(band.Device);
            }
            catch (Exception ex) {
                _userDialogs.Alert(ex.Message, $"Error al desconectarse de {band.Device.Name}");
            }
            finally {
                _userDialogs.HideLoading();
            }
        }


        /**************************************************************************

            Protected methods
         
         **************************************************************************/

        protected override void OnCreate(Bundle bundle) {
            base.OnCreate(bundle);

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
        }
    }
}