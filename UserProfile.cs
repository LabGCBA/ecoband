using System;

namespace EcoBand {
    public class UserProfile {
        public static readonly int GENDER_MALE = 1;
        public static readonly int GENDER_FEMALE = 0;

        private int mUid;
        private byte mGender;
        private byte mAge;
        private byte mHeight;
        private byte mWeight;
        private String mAlias = "";
        private byte mType;

        public UserProfile(int uid, int gender, int age, int height, int weight, String alias, int type) {
            mUid = uid;
            mGender = (byte) gender;
            mAge = (byte) age;
            mHeight = (byte) height;
            mWeight = (byte) weight;
            mAlias = alias;
            mType = (byte) type;
        }

        public UserProfile(byte[] data) {
            if (data.Length >= 20) {
                mUid = data[3] << 24 | (data[2] & 0xFF) << 16 | (data[1] & 0xFF) << 8 | (data[0] & 0xFF);
                mGender = data[4];
                mAge = data[5];
                mHeight = data[6];
                mWeight = data[7];
                mType = data[8];

                try {
                    mAlias = System.Text.Encoding.UTF8.GetString(data, 9, 8);
                }
                catch (Exception ex) {
                    mAlias = "";

                    Console.WriteLine($"##### Error: {ex.Message}");
                }
            }
        }

        public UserProfile[] newArray(int size) {
            return new UserProfile[size];
        }

        public byte[] getBytes(String address) {
            byte crcb;
            byte[] aliasBytes;
            byte[] crcSequence;
            System.IO.MemoryStream buffer;

            try {
                aliasBytes = System.Text.Encoding.UTF8.GetBytes(mAlias);
            }
            catch (Exception ex) {
                aliasBytes = new byte[0];

                Console.WriteLine($"##### Error: {ex.Message}");
            }

            buffer = new System.IO.MemoryStream(20);

            buffer.WriteByte((byte) mUid);
            buffer.WriteByte((byte) (mUid >> 8));
            buffer.WriteByte((byte) (mUid >> 16));
            buffer.WriteByte((byte) (mUid >> 24));
            buffer.WriteByte(mGender);
            buffer.WriteByte(mAge);
            buffer.WriteByte(mHeight);
            buffer.WriteByte(mWeight);
            buffer.WriteByte(mType);

            if (address.StartsWith("88:0F:10", StringComparison.CurrentCultureIgnoreCase)) buffer.WriteByte(5);
            else if (address.StartsWith("C8:0F:10", StringComparison.CurrentCultureIgnoreCase)) buffer.WriteByte(4);
            else buffer.WriteByte(0);

            buffer.WriteByte(0);

            foreach (byte item in aliasBytes) buffer.WriteByte(item);

            if (aliasBytes.Length <= 8) {
                byte[] pad = new byte[8 - aliasBytes.Length];

                foreach (byte item in pad) buffer.WriteByte(item);
            }
            else {
                buffer.WriteByte(0);
                buffer.WriteByte(8);
            }

            crcSequence = new byte[19];

            for (int i = 0; i < crcSequence.Length; i++) crcSequence[i] = buffer.ToArray()[i];

            crcb = (byte) (getCRC8(crcSequence) ^ Int16.Parse(address.Substring(address.Length - 2), System.Globalization.NumberStyles.HexNumber) & 0xFF);

            buffer.WriteByte(crcb);

            return buffer.ToArray();
        }

        private int getCRC8(byte[] seq) {
            int len = seq.Length;
            int i = 0;
            byte crc = 0x00;

            while (len-- > 0) {
                byte extract = seq[i++];

                for (byte tempI = 8; tempI != 0; tempI--) {
                    byte sum = (byte) ((crc & 0xFF) ^ (extract & 0xFF));

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
