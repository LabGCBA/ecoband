using System;
using System.Collections.Generic;

namespace EcoBand {
    public class Characteristic {
        public Characteristic(String label, String uuint) {
            Label = label;
            Uuint = uuint;
            Properties = new List<Properties>();
        }

        public String Label { get; }
        public String Uuint { get; }
        public List<Properties> Properties { get; }
    }
}
