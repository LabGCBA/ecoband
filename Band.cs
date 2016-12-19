using System;
using System.Collections.Generic;
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

        // Notifications to receive from UUID_CHARACTERISTIC_NOTIFICATION
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
        private readonly byte CP_SET_HR_SLEEP = 0x00;
        private readonly byte CP_SET_HR_CONTINUOUS = 0x01;
        private readonly byte CP_SET_HR_MANUAL = 0x02;
        private readonly byte CP_NOTIFY_REALTIME_STEPS = 0x03;
        private readonly byte CP_SET_ALARM = 0x04;
        private readonly byte CP_SET_GOAL = 0x05;
        private readonly byte CP_FETCH_DATA = 0x06;
        private readonly byte CP_SEND_FIRMWARE_INFO = 0x07;
        private readonly byte CP_SEND_NOTIFICATION = 0x08;
        private readonly byte CP_FACTORY_RESET = 0x09;
        private readonly byte CP_SET_REALTIME_STEPS = 0x10;
        private readonly byte CP_STOP_SYNC = 0x11;
        private readonly byte CP_NOTIFY_SENSOR_DATA = 0x12;
        private readonly byte CP_STOP_VIBRATION = 0x13;
        private readonly byte CP_CONFIRM_SYNC = 0x0A;
        private readonly byte CP_SYNC = 0x0B;
        private readonly byte CP_REBOOT = 0x0C;
        private readonly byte CP_SET_THEME = 0x0E;
        private readonly byte CP_SET_WEAR_LOCATION = 0x0F;

        // Test commands to send to UUID_CH_TEST
        private readonly byte TEST_REMOTE_DISCONNECT = 0x01;
        private readonly byte TEST_SELFTEST = 0x02;
        private readonly byte TEST_NOTIFICATION = 0x03;
        private readonly byte TEST_WRITE_MD5 = 0x04;
        private readonly byte TEST_DISCONNECTED_REMINDER = 0x05;

        // Battery status
        private readonly byte BATTERY_NORMAL = 0;
        private readonly byte BATTERY_LOW = 1;
        private readonly byte BATTERY_CHARGING = 2;
        private readonly byte BATTERY_CHARGING_FULL = 3;
        private readonly byte BATTERY_CHARGE_OFF = 4;


        /**************************************************************************

            Getters/Setters
         
         **************************************************************************/

        public List<Service> Services { get; }
        public List<Characteristic> Characteristics { get; }


        /**************************************************************************

            Public methods
         
         **************************************************************************/

        public Service getService(Guid uuid) {
            return new Service("Test", "Test"); // TODO: Implement
        }

        public Characteristic getCharacteristic(Guid uuid, Service service) {
            return new Characteristic("Test", "Test"); // TODO: Implement
        }
    }
}