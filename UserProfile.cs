using System;
using System.IO;

namespace EcoBand {
    /**
     *
     *  Code from https://github.com/pangliang/miband-sdk-android
     *  MIT Licensed
     * 
     *  Ported to C# (original in java)
     * 
     **/

    public class UserProfile {
        public static readonly int GENDER_MALE = 1;
        public static readonly int GENDER_FEMALE = 0;

        private int _uuid;
        private byte _gender;
        private byte _age;
        private byte _height;
        private byte _weight;
        private string _alias = "";
        private byte _type;

        public UserProfile(int uuid, int gender, int age, int height, int weight, string alias, int type) {
            _uuid = uuid;
            _gender = (byte) gender;
            _age = (byte) age;
            _height = (byte) height;
            _weight = (byte) weight;
            _alias = alias;
            _type = (byte) type;
        }

        public UserProfile(byte[] data) {
            if (data.Length >= 20) {
                _uuid = data[3] << 24 | (data[2] & 0xFF) << 16 | (data[1] & 0xFF) << 8 | (data[0] & 0xFF);
                _gender = data[4];
                _age = data[5];
                _height = data[6];
                _weight  = data[7];
                _type = data[8];

                try {
                    _alias = System.Text.Encoding.UTF8.GetString(data, 9, 8);
                }
                catch (Exception ex) {
                    _alias = "";

                    Console.WriteLine($"##### Error getting alias: {ex.Message}");
                }
            }
        }

        public UserProfile[] newArray(int size) {
            return new UserProfile[size];
        }

        public int Uuid {
            get {
                return _uuid;
            }
        }

        public byte Gender {
            get {
                return _gender;
            }
        }

        public byte Age {
            get {
                return _age;
            }
        }

        public int Height {
            get {
                return (_height & 0xFF);
            }
        }

        public int Weight {
            get {
                return _weight & 0xFF;
            }
        }

        public String Alias {
            get {
                return _alias;
            }
        }

        public byte Type {
            get {
                return _type;
            }
        }

        public byte[] toByteArray(string address) {
            byte crcByte;
            byte[] crcSequence;
            MemoryStream buffer;

            buffer = getBuffer(20, address);
            crcSequence = new byte[19];

            for (int i = 0; i < crcSequence.Length; i++) crcSequence[i] = buffer.ToArray()[i];

            crcByte = (byte) (getCRC8(crcSequence) ^ Int16.Parse(address.Substring(address.Length - 2), System.Globalization.NumberStyles.HexNumber) & 0xFF);

            buffer.WriteByte(crcByte);

            return buffer.ToArray();
        }

        private MemoryStream getBuffer(int size, string address) { 
            MemoryStream buffer;
            byte[] aliasBytes;

            try {
                aliasBytes = System.Text.Encoding.UTF8.GetBytes(_alias);
            }
            catch (Exception ex) {
                aliasBytes = new byte[0];

                Console.WriteLine($"##### Error: {ex.Message}");
            }

            buffer = new MemoryStream(size);

            buffer.WriteByte((byte) _uuid);
            buffer.WriteByte((byte) (_uuid >> 8));
            buffer.WriteByte((byte) (_uuid >> 16));
            buffer.WriteByte((byte) (_uuid >> 24));
            buffer.WriteByte(_gender);
            buffer.WriteByte(_age);
            buffer.WriteByte(_height);
            buffer.WriteByte(_weight);
            buffer.WriteByte(_type);

            if (address.StartsWith(Band.MAC_ADDRESS_FILTER[0], StringComparison.CurrentCultureIgnoreCase)) buffer.WriteByte(5);
            else if (address.StartsWith(Band.MAC_ADDRESS_FILTER[1], StringComparison.CurrentCultureIgnoreCase)) buffer.WriteByte(4);
            else buffer.WriteByte(4);

            buffer.WriteByte(0);

            foreach (byte item in aliasBytes) buffer.WriteByte(item);

            if (aliasBytes.Length <= 8) {
                byte[] pad;

                pad = new byte[8 - aliasBytes.Length];

                foreach (byte item in pad) buffer.WriteByte(item);

            }
            else {
                buffer.WriteByte(0);
                buffer.WriteByte(8);
            }

            return buffer;
        }

        private int getCRC8(byte[] seq) {
            int len = seq.Length;
            int i = 0;
            byte crc = 0x00;

            while (len-- > 0) {
                byte extract = seq[i++];

                for (byte tempI = 8; tempI != 0; tempI--) {
                    byte sum;

                    sum = (byte) ((crc & 0xFF) ^ (extract & 0xFF));
                    sum = (byte) ((sum & 0xFF) & 0x01);
                    crc = (byte) ((crc & 0xFF) >> 1);

                    if (sum != 0) crc = (byte) ((crc & 0xFF) ^ 0x8C);

                    extract = (byte) ((extract & 0xFF) >> 1);
                }
            }

            return (crc & 0xFF);
        }
    }
}
