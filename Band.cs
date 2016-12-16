using System;
using System.Collections.Generic;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace EcoBand {
    public class Band {
        public Band(IAdapter adapter) {
            Adapter = adapter;
        }

        protected readonly IAdapter Adapter;
        protected const string DeviceIdKey = "DeviceIdNavigationKey";
        protected const string ServiceIdKey = "ServiceIdNavigationKey";
        protected const string CharacteristicIdKey = "CharacteristicIdNavigationKey";
        protected const string DescriptorIdKey = "DescriptorIdNavigationKey";

        private List<Service> _services = new List<Service>();
        private List<Characteristic> _characteristics = new List<Characteristic>();

        public List<Service> Services {
            get {
                return _services;
            }
        }

        public List<Characteristic> Characteristics {
            get {
                return _characteristics;
            }
        }
    }
}
