using System;

namespace EcoBand {
    public class Service {
        public Service(String label, String uuint) {
            Label = label;
            Uuint = uuint;
        }

        public String Label { get; }
        public String Uuint { get; }
    }
}
