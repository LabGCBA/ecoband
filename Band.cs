using System;
using System.Collections.Generic;
using Plugin.BLE.Abstractions.Contracts;

namespace EcoBand {
    public class Band {
        public Band(IDevice device) {
            Device = device;
        }


        /**************************************************************************

            Public readonly properties
         
         **************************************************************************/

        public readonly IDevice Device;

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