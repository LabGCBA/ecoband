using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Android.Bluetooth;
using Plugin.BLE.Abstractions.Contracts;

namespace EcoBand {
    public class Band {
        public Band(IDevice device) {
            Device = device;
        }


        /**************************************************************************

            Static properties

         **************************************************************************/

        public static readonly List<string> NAME_FILTER = new List<string>() {
            "MI",
            "MI1S",
            "MI1A",
            "MI1"
        };
        public static readonly List<string> MAC_ADDRESS_FILTER = new List<string>() {
            "88:0F:10",
            "C8:0F:10"
        };


        /**************************************************************************

            Public properties

         **************************************************************************/

        public readonly IDevice Device;


        /**************************************************************************

            Private properties

         **************************************************************************/

        private readonly IAdapter _adapter;
        private readonly IBluetoothLE _ble;
        private IService _mainService;
        private IService _heartRateService;


        // Services
        private readonly Guid UUID_SV_MAIN = Guid.Parse("0000fee0-0000-1000-8000-00805f9b34fb");
        private readonly Guid UUID_SV_GENERIC_SERVICE = Guid.Parse("00001800-0000-1000-8000-00805f9b34fb");
        private readonly Guid UUID_SV_GENERIC_ATTRIBUTE = Guid.Parse("00001801-0000-1000-8000-00805f9b34fb");
        private readonly Guid UUID_SV_VIBRATION = Guid.Parse("00001802-0000-1000-8000-00805f9b34fb");
        private readonly Guid UUID_SV_HEART_RATE = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");
        private readonly Guid UUID_SV_WEIGHT = Guid.Parse("00001530-0000-3512-2118-0009af100700");


        // Characteristics
        private readonly Guid UUID_CH_DEVICE_INFO = Guid.Parse("0000ff01-0000-1000-8000-00805f9b34fb"); // read
        private readonly Guid UUID_CH_DEVICE_NAME = Guid.Parse("0000ff02-0000-1000-8000-00805f9b34fb"); // read write
        private readonly Guid UUID_CH_NOTIFICATION = Guid.Parse("0000ff03-0000-1000-8000-00805f9b34fb"); // read notify
        private readonly Guid UUID_CH_USER_INFO = Guid.Parse("0000ff04-0000-1000-8000-00805f9b34fb"); // read write
        private readonly Guid UUID_CH_CONTROL_POINT = Guid.Parse("0000ff05-0000-1000-8000-00805f9b34fb"); // write
        private readonly Guid UUID_CH_REALTIME_STEPS = Guid.Parse("0000ff06-0000-1000-8000-00805f9b34fb"); // read notify
        private readonly Guid UUID_CH_ACTIVITY_DATA = Guid.Parse("0000ff07-0000-1000-8000-00805f9b34fb"); // read indicate
        private readonly Guid UUID_CH_FIRMWARE_DATA = Guid.Parse("0000ff08-0000-1000-8000-00805f9b34fb"); // write without response
        private readonly Guid UUID_CH_LE_PARAMS = Guid.Parse("0000ff09-0000-1000-8000-00805f9b34fb"); // read write
        private readonly Guid UUID_CH_DATE_TIME = Guid.Parse("0000ff0a-0000-1000-8000-00805f9b34fb"); // read write
        private readonly Guid UUID_CH_STATISTICS = Guid.Parse("0000ff0b-0000-1000-8000-00805f9b34fb"); // read write
        private readonly Guid UUID_CH_BATTERY = Guid.Parse("0000ff0c-0000-1000-8000-00805f9b34fb"); // read notify
        private readonly Guid UUID_CH_TEST = Guid.Parse("0000ff0d-0000-1000-8000-00805f9b34fb"); // read write
        private readonly Guid UUID_CH_SENSOR_DATA = Guid.Parse("0000ff0e-0000-1000-8000-00805f9b34fb"); // read notify
        private readonly Guid UUID_CH_PAIR = Guid.Parse("0000ff0f-0000-1000-8000-00805f9b34fb"); // read write
        private readonly Guid UUID_CH_VIBRATION = Guid.Parse("00002a06-0000-1000-8000-00805f9b34fb");
        private readonly Guid UUID_CH_HEART_RATE_MEASUREMENT = Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb");
        private readonly Guid UUID_CH_HEART_RATE_CONTROL_POINT = Guid.Parse("00002a39-0000-1000-8000-00805f9b34fb");


        // Descriptors
        private readonly Guid UUID_DC_NOTIFY_CHARACTERISTIC_DETECTION = Guid.Parse("00002902-0000-1000-8000-00805f9b34fb");


        // Notifications to receive from UUID_CH_NOTIFICATION
        private readonly byte NOTIFY_NORMAL = 0x0;
        private readonly byte NOTIFY_FIRMWARE_UPDATE_FAILED = 0x1;
        private readonly byte NOTIFY_FIRMWARE_UPDATE_SUCCESS = 0x2;
        private readonly byte NOTIFY_CONN_PARAM_UPDATE_FAILED = 0x3;
        private readonly byte NOTIFY_CONN_PARAM_UPDATE_SUCCESS = 0x4;
        private readonly byte NOTIFY_AUTHENTICATION_SUCCESS = 0x5;
        private readonly byte NOTIFY_AUTHENTICATION_FAILED = 0x6;
        private readonly byte NOTIFY_FITNESS_GOAL_ACHIEVED = 0x7;
        private readonly byte NOTIFY_SET_LATENCY_SUCCESS = 0x8;
        private readonly byte NOTIFY_RESET_AUTHENTICATION_FAILED = 0x9;
        private readonly byte NOTIFY_RESET_AUTHENTICATION_SUCCESS = 0xa;
        private readonly byte NOTIFY_FW_CHECK_FAILED = 0xb;
        private readonly byte NOTIFY_FW_CHECK_SUCCESS = 0xc;
        private readonly byte NOTIFY_STATUS_MOTOR_NOTIFY = 0xd;
        private readonly byte NOTIFY_STATUS_MOTOR_CALL = 0xe;
        private readonly byte NOTIFY_STATUS_MOTOR_DISCONNECT = 0xf;
        private readonly byte NOTIFY_STATUS_MOTOR_SMART_ALARM = 0x10;
        private readonly byte NOTIFY_STATUS_MOTOR_ALARM = 0x11;
        private readonly byte NOTIFY_STATUS_MOTOR_GOAL = 0x12;
        private readonly byte NOTIFY_STATUS_MOTOR_AUTH = 0x13;
        private readonly byte NOTIFY_STATUS_MOTOR_SHUTDOWN = 0x14;
        private readonly byte NOTIFY_STATUS_MOTOR_AUTH_SUCCESS = 0x15;
        private readonly byte NOTIFY_STATUS_MOTOR_TEST = 0x16;
        // 0x18 is returned when we cancel data sync, perhaps is an ack for this message
        private readonly sbyte NOTIFY_UNKNOWN = Convert.ToSByte(-0x1);
        private readonly byte NOTIFY_PAIR_CANCEL = 0xef;
        private readonly byte NOTIFY_DEVICE_MALFUNCTION = 0xff;


        // Commands to send to UUID_CH_CONTROL_POINT
        private readonly byte CP_SET_HEART_RATE_SLEEP = 0x0;
        private readonly byte CP_SET_HEART_RATE_CONTINUOUS = 0x1;
        private readonly byte CP_SET_HEART_RATE_MANUAL = 0x2;
        private readonly byte CP_NOTIFY_REALTIME_STEPS = 0x3;
        private readonly byte CP_SET_ALARM = 0x4;
        private readonly byte CP_SET_GOAL = 0x5;
        private readonly byte CP_FETCH_DATA = 0x6;
        private readonly byte CP_SEND_FIRMWARE_INFO = 0x7;
        private readonly byte CP_SEND_NOTIFICATION = 0x8;
        private readonly byte CP_FACTORY_RESET = 0x9;
        private readonly byte CP_SET_REALTIME_STEPS = 0x10;
        private readonly byte CP_STOP_SYNC = 0x11;
        private readonly byte CP_NOTIFY_SENSOR_DATA = 0x12;
        private readonly byte CP_STOP_VIBRATION = 0x13;
        private readonly byte CP_CONFIRM_SYNC = 0xA;
        private readonly byte CP_SYNC = 0xB;
        private readonly byte CP_REBOOT = 0xC;
        private readonly byte CP_SET_THEME = 0xE;
        private readonly byte CP_SET_WEAR_LOCATION = 0xF;

        private readonly byte[] startHeartMeasurementManual = { 0x15, 0x2, 1 };
        private readonly byte[] stopHeartMeasurementManual = { 0x15, 0x2, 0 };
        private readonly byte[] startHeartMeasurementContinuous = { 0x15, 0x1, 1 };
        private readonly byte[] stopHeartMeasurementContinuous = { 0x15, 0x1, 0 };
        private readonly byte[] startHeartMeasurementSleep = { 0x15, 0x0, 1 };
        private readonly byte[] stopHeartMeasurementSleep = { 0x15, 0x0, 0 };


        // Test commands to send to UUID_CH_TEST
        private readonly byte TEST_REMOTE_DISCONNECT = 0x1;
        private readonly byte TEST_SELFTEST = 0x2;
        private readonly byte TEST_NOTIFICATION = 0x3;
        private readonly byte TEST_WRITE_MD5 = 0x4;
        private readonly byte TEST_DISCONNECTED_REMINDER = 0x5;


        // Battery status
        private readonly byte BATTERY_NORMAL = 0;
        private readonly byte BATTERY_LOW = 1;
        private readonly byte BATTERY_CHARGING = 2;
        private readonly byte BATTERY_CHARGING_FULL = 3;
        private readonly byte BATTERY_CHARGE_OFF = 4;


        /**************************************************************************

            Public methods

         **************************************************************************/

        public async Task<int> GetSteps() {
            byte[] steps = await GetData(UUID_CH_REALTIME_STEPS);

            return DecodeSteps(steps);
        }

        public int DecodeSteps(byte[] steps) {
            return 0xff & steps[0] | (0xff & steps[1]) << 8;
        }

        public async Task<bool> SubscribeToSteps(EventHandler<Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs> callback) {
            return await SubscribeTo(UUID_CH_REALTIME_STEPS, callback);
        }

        public async Task<int> GetHeartRate() {
            IService service;
            ICharacteristic controlPoint;

            try {
                Console.WriteLine("##### Trying to get heart rate...");

                service = await GetHeartRateService();
                controlPoint = await service.GetCharacteristicAsync(UUID_CH_HEART_RATE_CONTROL_POINT);

                await controlPoint.WriteAsync(stopHeartMeasurementSleep);
                await controlPoint.WriteAsync(stopHeartMeasurementContinuous);
                await controlPoint.WriteAsync(stopHeartMeasurementManual);
                await controlPoint.WriteAsync(startHeartMeasurementManual);

                byte[] heartRate = await GetData(UUID_CH_HEART_RATE_CONTROL_POINT, service);

                return DecodeHeartRate(heartRate);
            }
            catch (Exception ex) { 
                Console.WriteLine($"##### Error getting heart rate: {ex.Message}");

                return -1;
            }
        }

        public int DecodeHeartRate(byte[] heartRate) {
            if (heartRate.Count() == 2 && heartRate[0] == 6) return (heartRate[1] & 0xff);
            else {
                Console.WriteLine("##### Received invalid heart rate value");
                Console.WriteLine($"##### Byte array length: {heartRate.Count().ToString()}");

                return -1;
            }
        }

        public async Task<bool> SubscribeToHeartRate(EventHandler<Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs> callback) {
            IService service;
            ICharacteristic characteristic;
            ICharacteristic controlPoint;

            try {
                Console.WriteLine("##### Trying to subscribe to characteristic...");

                service = await GetHeartRateService();
                controlPoint = await service.GetCharacteristicAsync(UUID_CH_HEART_RATE_CONTROL_POINT);
                characteristic = await service.GetCharacteristicAsync(UUID_CH_HEART_RATE_MEASUREMENT);

                characteristic.ValueUpdated += callback;

                await characteristic.StartUpdatesAsync();
                await controlPoint.WriteAsync(stopHeartMeasurementManual);
                await controlPoint.WriteAsync(stopHeartMeasurementSleep);
                await controlPoint.WriteAsync(stopHeartMeasurementContinuous);
                await controlPoint.WriteAsync(startHeartMeasurementContinuous);

                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error subscribing to characteristic: {ex.Message}");

                return false;
            }
        }


        /**************************************************************************

            Private methods

         **************************************************************************/

        private async Task<IService> GetMainService() {
            try {
                if (_mainService == null) _mainService = await Device.GetServiceAsync(UUID_SV_MAIN);

                return _mainService;
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error getting main service: {ex.Message}");

                return null;
            }
        }

        private async Task<IService> GetHeartRateService() {
            try {
                if (_heartRateService == null) _heartRateService = await Device.GetServiceAsync(UUID_SV_HEART_RATE);

                return _heartRateService;
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error getting heart rate service: {ex.Message}");

                return null;
            }
        }

        private async Task<byte[]> GetData(Guid characteristic, IService customService = null) {
            ICharacteristic data;
            IService service;

            if (customService == null) service = await GetMainService();
            else service = customService;

            try {
                Console.WriteLine("##### Trying to get characteristic data...");

                data = await service.GetCharacteristicAsync(characteristic);

                if (data.CanRead) return await data.ReadAsync();
                else return null;
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error getting characteristic data: {ex.Message}");

                return null;
            }
        }

        private async Task<bool> SubscribeTo(Guid uuid, EventHandler<Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs> callback, IService customService = null) {
            IService service;
            ICharacteristic characteristic;

            try {
                Console.WriteLine("##### Trying to subscribe to characteristic...");

                if (customService == null) service = await GetMainService();
                else service = customService;

                characteristic = await service.GetCharacteristicAsync(uuid);
                characteristic.ValueUpdated += callback;

                await characteristic.StartUpdatesAsync();

                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"##### Error subscribing to characteristic: {ex.Message}");

                return false;
            }
        }
    }
}