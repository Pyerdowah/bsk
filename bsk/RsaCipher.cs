using System;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Windows;

namespace bsk
{
    /// <summary>
    /// Class for creating, storing and using RSA keys
    /// </summary>
    public class RsaCipher
    {
        public RSAParameters _my_private_key { get; set; }
        public RSAParameters _my_public_key { get; set; }
        public RSAParameters _other_public_key { get; set; }
        private string _other_public_key_path = "klucz.pub";

        /// <summary>
        /// Tries to load public key of partnering application.
        /// Creates new key pair for client.
        /// </summary>
        public RsaCipher()
        {
            using (RSA rsa = new RSACryptoServiceProvider(2048))
            {
                this._my_private_key = rsa.ExportParameters(true);
                this._my_public_key = rsa.ExportParameters(false);
                try
                {
                    ImportPublicKeyFromFile(_other_public_key_path);
                }
                catch (System.IO.FileNotFoundException e)
                {
                    //no file klucz.pub
                    Application.Current.Shutdown();
                }

            }
        }


        public RsaCipher(RSAParameters p)
        {
            this._my_private_key = p;
        }


        public byte[] ExportPrivateKey()

        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_my_private_key);
                string temp = rsa.ToXmlString(true);
                byte[] keys = Encoding.UTF8.GetBytes(temp);
                return keys;
            }
        }

        public byte[] ExportPublicKey()
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportParameters(_my_public_key);
                string temp = rsa.ToXmlString(false);
                byte[] key = Encoding.UTF8.GetBytes(temp);
                return key;
            }
        }

        public void ImportPrivateKey(byte[] keys)
        {
            using (RSA rsa = RSA.Create(_my_private_key))
            {
                string xmlString = Encoding.UTF8.GetString(keys);
                rsa.FromXmlString(xmlString);
                this._my_private_key = rsa.ExportParameters(true);
            }
        }
        public void ImportOtherPublicKey(byte[] keys)
        {
            using (RSA rsa = RSA.Create(_my_private_key))
            {
                string xmlString = Encoding.UTF8.GetString(keys);
                rsa.FromXmlString(xmlString);
                this._other_public_key = rsa.ExportParameters(false);
            }
        }

        public void ImportPublicKeyFromFile(string filePath)
        {

            using (FileStream fs = File.OpenRead(filePath))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    byte[] fileContent = ms.ToArray();
                    ImportOtherPublicKey(fileContent);
                }
            }
        }

        public void ExportPublicKeyToFile(string filePath)
        {
            byte[] key = ExportPublicKey();
            File.WriteAllBytes(filePath, key);
        }

        public byte[] Encrypt(byte[] arg)
        {
            using (RSA rsa = RSA.Create(_other_public_key))
            {
                byte[] encrypted = rsa.Encrypt(arg, RSAEncryptionPadding.OaepSHA512);
                return encrypted;
            }
        }
        public byte[] Decrypt(byte[] arg)
        {
            using (RSA rsa = RSA.Create(_my_private_key))
            {
                byte[] decrypted = rsa.Decrypt(arg, RSAEncryptionPadding.OaepSHA512);
                return decrypted;
            }
        }

    }

}