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
            _state = BluetoothState.Off;

            _ble.StateChanged += OnStateChanged;
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.ScanTimeoutElapsed += OnScanTimeoutElapsed;
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
        private readonly IUserDialogs _userDialogs;
        private const string _deviceIdKey = "DeviceIdNavigationKey";
        private const string _serviceIdKey = "ServiceIdNavigationKey";
        private const string _characteristicIdKey = "CharacteristicIdNavigationKey";
        private const string _descriptorIdKey = "DescriptorIdNavigationKey";


        /**************************************************************************

            Event handlers
         
         **************************************************************************/

        private void OnConnectButtonClick(object sender, EventArgs e) {
            Discover();

            _connectButton.Text = "Buscando...";
            // TODO: Implement
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e) {
            // TODO: Log state change
            _state = e.NewState;
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args) {
            // TODO: Implement
        }

        private void OnScanTimeoutElapsed(object sender, EventArgs e) {
            // TODO: Implement
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            // TODO: Implement
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            // TODO: Implement
        }


        /**************************************************************************

            Private methods
         
         **************************************************************************/

        private async Task Discover() {
            if (_state == BluetoothState.On) {
                if (_adapter.IsScanning) return;

                _devices.Clear();

                foreach (var device in _adapter.ConnectedDevices) {
                    //update rssi for already connected devices (so the 0 is not shown in the list)
                    try {
                        await device.UpdateRssiAsync();
                        // TODO: Check if rssi was really updated
                    }
                    catch (Exception ex) {
                        _userDialogs.ShowError($"Failed to update RSSI for {device.Name}\n Error: {ex.Message}");
                    }

                    if (!_devices.Any(item => item.Device == device)) {
                        // TODO: Limit connection to actual Mi Bands
                        _devices.Add(new Band(device));
                    }
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _adapter.StopScanningForDevicesAsync();
                _adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token);
            }
        }

        private async Task Connect(Band device) {
            // TODO: Implement
        }

        private async Task Disconnect(Band band) {
            try {
                if (band.Device.State != DeviceState.Connected) return;

                _userDialogs.ShowLoading($"Disconnecting {band.Device.Name}...");

                await _adapter.DisconnectDeviceAsync(band.Device);
            }
            catch (Exception ex) {
                _userDialogs.Alert(ex.Message, "Disconnect error");
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

            // Get our button from the layout resource,
            // and attach an event to it
            _connectButton = FindViewById<Button>(Resource.Id.btnConnect);
            _connectButton.Click += OnConnectButtonClick;
        }
    }
}