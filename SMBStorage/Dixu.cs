//
// C# 
// TCFsSMBStorage
// v 0.1, 02.05.2024
// https://github.com/dkxce
// en,ru,1251,utf-8
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace dkxce.Crypt
{
    /// <summary>
    ///     DIXU v0.1 - 
    ///     dkxce opensource free symmetric crypt algorythm (UTF8, ASCII, Win-1251)
    ///     (author: milokz@gmail.com) (https://github.com/dkxce/DIXU)
    /// </summary>
    public class DIXU
    {
        /// <summary>
        ///     512 - bit initial key (64 bytes)
        /// </summary>
        public const string STANDARD_SHIFT = "Zaau4tuhsoDrZ:@dmkbx#cxeo,smmiuljoikXzC@3gGmKauiwlw.PcPoYmU!lfzM";

        /// <summary>
        ///     512 - bit predefined initial key
        /// </summary>
        public const string PREDEFINED1_SHIFT = "zr$@snJQ~2Y4jgOiw7}d3G%BSeamRhv?|KUCoDkZ*DwDYpiWi}WmCtEXaeNiiOyK";

        /// <summary>
        ///     512 - bit predefined initial key
        /// </summary>
        public const string PREDEFINED2_SHIFT = "zjmN6ByX#K4HwprZcJu{SFgizeLoMak?~RU9Wv7Vx#rDbJYhBv~vgoDZoO?OI$H@";

        /// <summary>
        ///     512 - bit predefined initial key
        /// </summary>
        public const string PREDEFINED3_SHIFT = "Zj87NTmiElzf*2anXVP6J{tosd~v%1keD3gpu?59Os4p5g2ml6~7nQ${dBLGzVlB";

        /// <summary>
        ///     512 - bit predefined initial key
        /// </summary>
        public const string PREDEFINED4_SHIFT = "z%SflyaA@v4~tI3e1D*ZVFsxmnO5pM8}Bzq2wWJcrN7X61T~u53VZXzyqcmOzBfy";

        /// <summary>
        ///     Encrypt text to Base64-coded data (standard initial key)
        /// </summary>
        /// <param name="source">source text, UTF8</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>Base64-coded data</returns>
        public static string EncryptText(string source, string key)
        {
            if (String.IsNullOrEmpty(source)) return null;
            byte[] res = Encrypt(System.Text.Encoding.UTF8.GetBytes(source.Trim()), System.Text.Encoding.ASCII.GetBytes(STANDARD_SHIFT), key);
            return Convert.ToBase64String(res, Base64FormattingOptions.None);
        }

        /// <summary>
        ///     Decrypt text from Base64-coded data (standard initial key)
        /// </summary>
        /// <param name="source">source text, base64-coded data</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>decrypted text, UTF8</returns>
        public static string DecryptText(string source, string key)
        {
            if (String.IsNullOrEmpty(source)) return null;
            byte[] arr = Convert.FromBase64String(source.Trim());
            byte[] res = Decrypt(arr, System.Text.Encoding.ASCII.GetBytes(STANDARD_SHIFT), key);
            return System.Text.Encoding.UTF8.GetString(res);
        }

        /// <summary>
        ///     Decrypt text from Base64-coded data (specified initial key)
        /// </summary>
        /// <param name="source">source text, base64-coded data</param>
        /// <param name="shift">specified initial key, ASCII</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>decrypted text, UTF8</returns>
        public static string EncryptText(string source, byte[] shift, string key)
        {
            if (String.IsNullOrEmpty(source)) return null;
            byte[] res = Encrypt(System.Text.Encoding.UTF8.GetBytes(source.Trim()), shift, key);
            return Convert.ToBase64String(res, Base64FormattingOptions.None);
        }

        /// <summary>
        ///     Decrypt text from Base64-coded data (standard initial key)
        /// </summary>
        /// <param name="source">source text, base64-coded data</param>
        /// <param name="shift">specified initial key, ASCII</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>decrypted text, UTF8</returns>
        public static string DecryptText(string source, byte[] shift, string key)
        {
            if (String.IsNullOrEmpty(source)) return null;
            byte[] arr = Convert.FromBase64String(source.Trim());
            byte[] res = Decrypt(arr, shift, key);
            return System.Text.Encoding.UTF8.GetString(res);
        }

        /// <summary>
        ///     Encrypt data (standard initial key)
        /// </summary>
        /// <param name="source">source data, UTF8</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>crypted data</returns>
        public static byte[] Encrypt(byte[] source, string key)
        {
            return Encrypt(source, System.Text.Encoding.ASCII.GetBytes(STANDARD_SHIFT), key);
        }

        /// <summary>
        ///     Decrypt data (standard initial key)
        /// </summary>
        /// <param name="source">source data, UTF8</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>decrypted data</returns>
        public static byte[] Decrypt(byte[] source, string key)
        {
            return Decrypt(source, System.Text.Encoding.ASCII.GetBytes(STANDARD_SHIFT), key);
        }

        /// <summary>
        ///     Encrypt data (specified initial key)
        /// </summary>
        /// <param name="source">source data, UTF8</param>
        /// <param name="shift">specified initial key, ASCII</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>crypted data</returns>
        public static byte[] Encrypt(byte[] source, byte[] shift, string key)
        {
            // Check Errors
            if (source == null) return null;
            if (source.Length == 0) return null;
            if (shift == null) return null;
            if (shift.Length == 0) return null;
            if (String.IsNullOrEmpty(key)) return null;

            // Initials
            byte[] res = new byte[source.Length];
            byte[] cod = System.Text.Encoding.GetEncoding(1251).GetBytes(key.Trim());
            // Loop
            for (int i = 0; i < source.Length; i++)
            {
                byte op = source[i];
                // STEP 1 - SHIFT
                for (int j = 0; j < shift.Length; j++)
                {
                    //#1
                    byte rl1 = (byte)(shift[j] & 0x07);
                    op = rotl(op, rl1);
                    //#2
                    op = (byte)(op ^ shift[j]);
                };
                // STEP 2 - KEY
                for (int j = 0; j < cod.Length; j++)
                {
                    //#1
                    byte rl2 = (byte)(j % 7 + 1);
                    op = rotl(op, rl2);
                    //#2
                    op = (byte)(op ^ cod[j]);
                };
                // RES
                res[i] = op;
            };

            return res;
        }

        /// <summary>
        ///     Decrypt data (specified initial key)
        /// </summary>
        /// <param name="source">source data, UTF8</param>
        /// <param name="shift">specified initial key, ASCII</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>decrypted data</returns>
        public static byte[] Decrypt(byte[] source, byte[] shift, string key)
        {
            // Check Errors
            if (source == null) return null;
            if (source.Length == 0) return null;
            if (shift == null) return null;
            if (shift.Length == 0) return null;
            if (String.IsNullOrEmpty(key)) return null;

            // Initials
            byte[] res = new byte[source.Length];
            byte[] cod = System.Text.Encoding.GetEncoding(1251).GetBytes(key.Trim());
            // Loop
            for (int i = 0; i < source.Length; i++)
            {
                byte op = source[i];
                // STEP 1 - KEY
                for (int j = cod.Length - 1; j >= 0; j--)
                {
                    //#1
                    op = (byte)(op ^ cod[j]);
                    //#2
                    byte rl2 = (byte)(j % 7 + 1);
                    op = rotr(op, rl2);
                };
                // STEP 2 - SHIFT
                for (int j = shift.Length - 1; j >= 0; j--)
                {
                    //#1
                    op = (byte)(op ^ shift[j]);
                    //#2
                    byte rl = (byte)(shift[j] & 0x07);
                    op = rotr(op, rl);
                };
                // RES
                res[i] = op;
            };
            return res;
        }

        /// <summary>
        ///     Encrypt data (specified initial key)
        /// </summary>
        /// <param name="source">source data, UTF8</param>
        /// <param name="offset">source data offset</param>
        /// <param name="len">source data length</param>
        /// <param name="shift">specified initial key, ASCII</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>crypted data</returns>
        public static byte[] Encrypt(byte[] source, int offset, int len, byte[] shift, string key)
        {
            byte[] newarr = new byte[len];
            for (int i = 0; i < newarr.Length; i++)
                newarr[i] = source[offset + i];
            return Encrypt(newarr, shift, key);
        }

        /// <summary>
        ///     Decrypt data (specified initial key)
        /// </summary>
        /// <param name="source">source data, UTF8</param>
        /// <param name="offset">source data offset</param>
        /// <param name="len">source data length</param>
        /// <param name="shift">specified initial key, ASCII</param>
        /// <param name="key">key text, ASCII</param>
        /// <returns>decrypted data</returns>
        public static byte[] Decrypt(byte[] source, int offset, int len, byte[] shift, string key)
        {
            byte[] newarr = new byte[len];
            for (int i = 0; i < newarr.Length; i++)
                newarr[i] = source[offset + i];
            return Decrypt(newarr, shift, key);
        }

        /// <summary>
        ///     Generate ASCII key
        /// </summary>
        /// <param name="length">length in bytes</param>
        /// <returns>text key, ASCII</returns>
        public static string GenerateKeyText(byte length)
        {
            string valid = "~`abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890/*-+_=!@#$%^&()";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
                res.Append(valid[rnd.Next(valid.Length)]);
            return res.ToString();
        }

        /// <summary>
        ///     Generate ASCII key
        /// </summary>
        /// <param name="length">length in bytes</param>
        /// <returns>ASCII key</returns>
        public static byte[] GenerateKeyByte(byte length)
        {
            return System.Text.Encoding.ASCII.GetBytes(GenerateKeyText(length));
        }

        /// <summary>
        ///     Rotate Right
        /// </summary>
        /// <param name="value">value</param>
        /// <param name="bits">1..7</param>
        /// <returns>value</returns>
        private static byte rotr(byte value, byte bits)
        {
            if (bits == 0) return value;
            if (bits == 8) return value;
            int right = value >> 1;
            if (bits > 1)
            {
                right &= 0x7FFFFFFF;
                right >>= bits - 1;
            };
            return (byte)((value << (8 - bits)) | right);
        }

        /// <summary>
        ///     Rotate Left
        /// </summary>
        /// <param name="value">value</param>
        /// <param name="bits">1..7</param>
        /// <returns>value</returns>
        private static byte rotl(byte value, int bits)
        {
            if (bits == 0) return value;
            if (bits == 8) return value;
            int right = value >> 1;
            if ((8 - bits) > 1)
            {
                right &= 0x7FFFFFFF;
                right >>= 8 - bits - 1;
            };
            return (byte)(value << bits | right);
        }

        /// <summary>
        ///     Test
        /// </summary>
        /// <param name="source">source text</param>
        /// <param name="key">key</param>
        /// <param name="crypted">crypted text, base64</param>
        /// <param name="decrypted">decrypted text</param>
        public static void Test(string source, string key, out string crypted, out string decrypted)
        {
            crypted = EncryptText(source, key);
            decrypted = DecryptText(crypted, key);
        }

        /// <summary>
        ///     Test
        /// </summary>
        private static void Test()
        {
            string key = "DIXU v0.1 by dkxce";
            string enc, dec;

            Test("���� ����� ����� ����, ����� ����� � ���� ���!", key, out enc, out dec);

            Console.WriteLine("Encoded: {0}", enc);
            Console.WriteLine("Decoded: {0}", dec);
        }
    }

    public class DIXUFile
    {
        public static void EncyptFile(string file, byte[] shift, string key)
        {
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
            byte[] buff = new byte[65536];
            int read = 0;
            while ((read = fs.Read(buff, 0, buff.Length)) > 0)
            {
                byte[] newbuff = DIXU.Encrypt(buff, 0, read, shift, key);
                fs.Position -= read;
                fs.Write(newbuff, 0, newbuff.Length);
            };
            fs.Close();
        }

        public static void EncyptFile(string sourcefile, string destfile, byte[] shift, string key)
        {
            FileStream sf = new FileStream(sourcefile, FileMode.Open, FileAccess.Read);
            FileStream df = new FileStream(destfile, FileMode.Create, FileAccess.Write);
            byte[] buff = new byte[65536];
            int read = 0;
            while ((read = sf.Read(buff, 0, buff.Length)) > 0)
            {
                byte[] newbuff = DIXU.Encrypt(buff, 0, read, shift, key);
                df.Write(newbuff, 0, newbuff.Length);
            };
            df.Close();
            sf.Close();
        }

        public static void DecyptFile(string file, byte[] shift, string key)
        {
            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
            byte[] buff = new byte[65536];
            int read = 0;
            while ((read = fs.Read(buff, 0, buff.Length)) > 0)
            {
                byte[] newbuff = DIXU.Decrypt(buff, 0, read, shift, key);
                fs.Position -= read;
                fs.Write(newbuff, 0, newbuff.Length);
            };
            fs.Close();
        }

        public static void DecyptFile(string sourcefile, string destfile, byte[] shift, string key)
        {
            FileStream sf = new FileStream(sourcefile, FileMode.Open, FileAccess.Read);
            FileStream df = new FileStream(destfile, FileMode.Create, FileAccess.Write);
            byte[] buff = new byte[65536];
            int read = 0;
            while ((read = sf.Read(buff, 0, buff.Length)) > 0)
            {
                byte[] newbuff = DIXU.Decrypt(buff, 0, read, shift, key);
                df.Write(newbuff, 0, newbuff.Length);
            };
            df.Close();
            sf.Close();
        }
    }
}
