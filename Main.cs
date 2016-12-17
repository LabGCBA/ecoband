using System;
using System.Threading.Tasks;

using Android.App;
// using Android.Content;
// using Android.Runtime;
// using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;

using Plugin.BLE;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Contracts;

namespace EcoBand {
    [Activity(Label = "EcoBand", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait)]

    public class Main : Activity {
        public Main() {
            _ble = CrossBluetoothLE.Current;

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
        private const string _deviceIdKey = "DeviceIdNavigationKey";
        private const string _serviceIdKey = "ServiceIdNavigationKey";
        private const string _characteristicIdKey = "CharacteristicIdNavigationKey";
        private const string _descriptorIdKey = "DescriptorIdNavigationKey";


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
            Button button = FindViewById<Button>(Resource.Id.btnConnect);

            button.Click += delegate {
                // Complete
            };
        }


        /**************************************************************************

            Private methods
         
         **************************************************************************/

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e) {

        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args) {

        }

        private void OnScanTimeoutElapsed(object sender, EventArgs e) {

        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e) {

        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {

        }

        private async Task Discover() {

        }

        private async Task Connect(IDevice device) {

        }
    }
}