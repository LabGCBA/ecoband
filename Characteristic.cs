using System;
using System.Collections.Generic;

namespace EcoBand {
    public class Characteristic {
        public Characteristic(String label, String uuint) {
            Label = label;
            Uuint = uuint;
        }

        private List<Properties> _properties = new List<Properties>();

        public String Label { get; }
        public String Uuint { get; }
        public List<Properties> Properties { 
            get {
                return _properties;
            } 
        }
    }
}
