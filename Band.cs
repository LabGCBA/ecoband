using System;
using System.Collections.Generic;
using Plugin.BLE.Abstractions.Contracts;

namespace EcoBand {
    public class Band {
        public Band(IDevice device) {
            _device = device;

            Name = device.Name;
            Uuid = device.Id;
        }

        /**************************************************************************

            Internal properties
         
         **************************************************************************/

        private readonly IDevice _device;


        /**************************************************************************

            Getters/Setters
         
         **************************************************************************/

        public String Name { get; }
        public Guid Uuid { get; }

        public List<Service> Services { get; set; }
        public List<Characteristic> Characteristics { get; set; }


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