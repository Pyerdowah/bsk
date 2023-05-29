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
        public void ImportPublicKey(byte[] keys)
        {
            using (RSA rsa = RSA.Create(_my_private_key))
            {
                string xmlString = Encoding.UTF8.GetString(keys);
                rsa.FromXmlString(xmlString);
                this._my_public_key = rsa.ExportParameters(false);
            }
        }
        
        public void ImportOtherPublicKey(byte[] keys)
        {
            using (RSA rsa = RSA.Create())
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
                    ImportPublicKey(fileContent);
                }
            }
        }
        
        public void ImportOtherPublicKeyFromFile(string filePath)
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
        
        public byte[] ImportPrivateKeyFromFile(string filePath)
        {

            using (FileStream fs = File.OpenRead(filePath))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    byte[] fileContent = ms.ToArray();
                    return fileContent;
                }
            }
        }

        public void ExportPublicKeyToFile(string filePath)
        {
            byte[] key = ExportPublicKey();
            File.WriteAllBytes(filePath, key);
        }
        
        public void ExportPrivateKeyToFile(byte[] key, string filePath)
        {
            File.WriteAllBytes(filePath, key);
        }

        public byte[] Encrypt(FileInfo fileInfo)
        {
            byte[] fileBytes;
            using (FileStream stream = fileInfo.OpenRead())
            {
                fileBytes = new byte[stream.Length];
                stream.Read(fileBytes, 0, fileBytes.Length);
            }

            int blockSize = 126;
            byte[] encrypted = new byte[0];
            for (int i = 0; i < fileBytes.Length; i+= blockSize)
            {
                byte[] encryptedPart;
                long chunkSize = (fileBytes.Length - i > blockSize) ? blockSize : fileBytes.Length - i;
                byte[] temp = new byte[chunkSize];
                Array.Copy(fileBytes, i, temp, 0, chunkSize);
                using (RSA rsa = RSA.Create(_other_public_key))
                {
                    encryptedPart = rsa.Encrypt(temp, RSAEncryptionPadding.OaepSHA512);
                    encrypted.Concat(encryptedPart).ToArray();
                }
            }
            return encrypted;
        }
        public byte[] Decrypt(byte[] fileBytes)
        {
            int blockSize = 126;
            byte[] decrypted = new byte[0];
            for (int i = 0; i < fileBytes.Length; i+= blockSize)
            {
                byte[] decryptedPart;
                long chunkSize = (fileBytes.Length - i > blockSize) ? blockSize : fileBytes.Length - i;
                byte[] temp = new byte[chunkSize];
                Array.Copy(fileBytes, i, temp, 0, chunkSize);
                using (RSA rsa = RSA.Create(_my_private_key))
                {
                    decryptedPart = rsa.Decrypt(temp, RSAEncryptionPadding.OaepSHA512);
                    decrypted.Concat(decryptedPart).ToArray();
                }
            }
            return decrypted;
        }

    }

}