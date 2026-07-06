using System.Security.Cryptography;

namespace JHLib.Util.ByteControl
{
    public class AES
    {
        public static byte[] Decrypt(byte[] dat, byte[] key, byte[] iv)
        {
            if (dat == null || dat.Length == 0)
                return null;

            try
            {
                using var aes = Aes.Create();
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var dc = aes.CreateDecryptor(aes.Key, aes.IV);

                return dc.TransformFinalBlock(dat, 0, dat.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public static byte[] Decrypt16(byte[] dat, byte[] key, byte[] iv)
        {
            if (dat == null || dat.Length == 0)
                return null;

            try
            {
                using var aes = Aes.Create();
                aes.BlockSize = 128;
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var dc = aes.CreateDecryptor(aes.Key, aes.IV);

                var t = dc.TransformFinalBlock(dat, 0, dat.Length);
                if (t == null) return null;

                var l = t.Length;
                if (l == 16) return t;
                if (l == 0 || l > 16) return null;

                var r = Create_AES_PKCS7_Pad16(16 - l);
                for (var i = 0; i < l; i++) r[i] = t[i];
                return r;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }

        public unsafe static byte[] Create_AES_PKCS7_Pad16(int padding)
        {
            var f = (uint)padding; f |= f << 8; f |= f << 16;
            var r = new byte[16];
            fixed (byte* p = &r[0])
            {
                *(uint*)(p + 00) = f;
                *(uint*)(p + 04) = f;
                *(uint*)(p + 08) = f;
                *(uint*)(p + 12) = f;
                return r;
            }
        }
    }
}