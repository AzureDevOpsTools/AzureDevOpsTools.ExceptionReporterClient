using System;
using System.Text;
using System.Security.Cryptography;

namespace Kongsberg.Nemo.ExceptionReporter.Sec
{
    public class Encryption
    {
        private const string Key = "";
        private readonly byte[] vector = new byte[] { 27, 9, 45, 27, 0, 72, 171, 54 };

        public string Encrypt(string inputString)
        {
            var buffer = Encoding.ASCII.GetBytes(inputString);
            using (var tripleDes = new TripleDESCryptoServiceProvider())
            using (var md5 = new MD5CryptoServiceProvider())
            {
                tripleDes.Key = md5.ComputeHash(Encoding.ASCII.GetBytes(Key));
                tripleDes.IV = vector;
                var transform = tripleDes.CreateEncryptor();
                return Convert.ToBase64String(transform.TransformFinalBlock(buffer, 0, buffer.Length));
            }
        }

        public string Decrypt(string inputString)
        {
            var buffer = Convert.FromBase64String(inputString);
            using (var md5 = new MD5CryptoServiceProvider())
            using(var tripleDes = new TripleDESCryptoServiceProvider())
            {
                tripleDes.Key = md5.ComputeHash(Encoding.ASCII.GetBytes(Key));
                tripleDes.IV = vector;
                var transform = tripleDes.CreateDecryptor();
                return Encoding.ASCII.GetString(transform.TransformFinalBlock(buffer, 0, buffer.Length));
            }
        }
    }
}