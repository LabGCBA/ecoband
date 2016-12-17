using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Android.App;
// using Android.Content;
// using Android.Runtime;
// using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;

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
            _devices = new List<Band>();

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
        private IBluetoothLE _ble;
        private Button _connectButton;
        private List<Band> _devices;
        private BluetoothState _state;
        private CancellationTokenSource _cancellationTokenSource;
        private IUserDialogs _userDialogs;
        private const string _deviceIdKey = "DeviceIdNavigationKey";
        private const string _serviceIdKey = "ServiceIdNavigationKey";
        private const string _characteristicIdKey = "CharacteristicIdNavigationKey";
        private const string _descriptorIdKey = "DescriptorIdNavigationKey";


        /**************************************************************************

            Event handlers
         
         **************************************************************************/

        private void OnConnectButtonClick(object sender, EventArgs e) {
            Discover();

            Console.WriteLine("Clicked Connect button");
            // TODO: Implement
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e) {
            Console.WriteLine($"##### State changed to {e.NewState.ToString()}");
            _state = e.NewState;
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args) {
            // TODO: Implement. Show modal, updating items in real time.
            Console.WriteLine($"##### Discovered device {args.Device.Name}");
        }

        private void OnDeviceConnected(object sender, DeviceEventArgs args) {
            // TODO: Implement. Hide modal and get needed services and characteristics, show data in UI.
            Console.WriteLine($"##### Connected to device {args.Device.Name}");
        }

        private void OnScanTimeoutElapsed(object sender, EventArgs e) {
            // TODO: Implement
            _adapter.StopScanningForDevicesAsync();
            _connectButton.Text = "Conectar";
            _userDialogs.Toast("No se encontraron dispositivos");
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

        private async Task Discover() {
            Console.WriteLine("##### Inside Discover()");

            if (_ble.IsOn) {
                if (_adapter.IsScanning) return;

                _connectButton.Text = "Buscando...";
                _cancellationTokenSource = new CancellationTokenSource();
                _devices.Clear();

                Console.WriteLine("##### Beginning scan...");

                await _adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token);

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

                        Console.WriteLine($"##### Added device {device.Name} to the list");
                    }
                }
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