using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace bsk
{
    [Serializable]
    public class AesParams
    {
        public const int keySize = 32;
        public const int ivSize = blockSize / BitsPerByte;
        public const int iterations = 300;
        public const int blockSize = 128;
        public const PaddingMode Padding = PaddingMode.None;
        public const int BitsPerByte = 8;

        public readonly byte[] salt = { 10, 20, 30, 40, 50, 60, 70, 80 };
        public byte[] key { get; set; }
        public byte[] iv { get; set; }
        public CipherMode cipherMode { get; set; }
        public byte[] sha { get; set; }
        public string fileName { get; set; }

        public AesParams(CipherMode cipherMode, FileInfo file)
        {
            this.cipherMode = cipherMode;
            this.sha = ComputeSha(file);
            this.fileName = file.Name;
            byte[] randomBytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }
            string password = Convert.ToBase64String(randomBytes)
                                .Replace('+', '-')
                                .Replace('/', '_')
                                .Replace("=", "");
            ComputeKeyFromPassword(password, this);
        }
        public AesParams(CipherMode cipherMode, FileInfo file, string password)
        {
            this.cipherMode = cipherMode;
            this.sha = ComputeSha(file);
            this.fileName = file.Name;
            ComputeKeyFromPassword(password, this);
        }
        
        public AesParams(CipherMode cipherMode)
        {
            this.cipherMode = cipherMode;
            this.sha = null;
            this.fileName = null;
            using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.GenerateKey();
                ComputeKeyFromPassword(Encoding.UTF8.GetString(aesProvider.Key), this);
            }     
        }

        public byte[] ComputeSha(FileInfo file)
        {
            using (var mySha256 = SHA256.Create())
            {
                using (var fileStream = file.Open(FileMode.Open))
                {
                    fileStream.Position = 0;
                    return mySha256.ComputeHash(fileStream);
                }
            }
        }

        public byte[] ComputeSha(string password)

        {
            byte[] byte_pass = System.Text.Encoding.UTF8.GetBytes(password);
            using (var mySha256 = SHA256.Create())
            {
                return mySha256.ComputeHash(byte_pass);
            }
        }

        public byte[] ComputeSha(byte[] data)

        {
            using (var mySha256 = SHA256.Create())
            {
                return mySha256.ComputeHash(data);
            }
        }

        public void ComputeKeyFromPassword(string password, AesParams aesParams)
        {
            byte[] hash = ComputeSha(password);
            var keyGenerator = new Rfc2898DeriveBytes(hash, aesParams.salt, AesParams.iterations);
            aesParams.key = keyGenerator.GetBytes(keySize);
            aesParams.iv = keyGenerator.GetBytes(ivSize);
            Console.WriteLine(iv.Length);
        }
        public Aes InstantiateAes()
        {
            Aes aes = Aes.Create();
            aes.Mode = this.cipherMode;
            aes.Key = this.key;
            aes.IV = this.iv;
            return aes;
        }

        public FileInfo StoreKey()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (FileStream stream = new FileStream("key.aes", FileMode.Create))
            {
                formatter.Serialize(stream, this);
            }
            return new FileInfo("key.aes");
        }
        
        public AesParams LoadKey(byte[] aes)
        {
            AesParams ap;
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(aes))
            {
                ap = (AesParams) formatter.Deserialize(stream);
            }
            return ap;
        }
    }

    public class AesCipher
    {
        private const string decrFolder = "tmp/dec";
        private const string encFolder = "tmp/enc";


        public AesCipher()
        {
            Directory.CreateDirectory(decrFolder);
            Directory.CreateDirectory(encFolder);
        }

        public byte[] EncryptByte(byte[] buffer, AesParams aesParams)
        {
            Aes aes = aesParams.InstantiateAes();
            aes.Padding = PaddingMode.Zeros;
            aes.BlockSize = AesParams.blockSize;
            ICryptoTransform encryptor;
            if (aesParams.cipherMode == CipherMode.CBC)
            {
                encryptor = aes.CreateEncryptor(aesParams.key, aesParams.iv);
            }
            else
            {
                encryptor = aes.CreateEncryptor();
            }
            byte[] ciphertext = encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
            return ciphertext;
        }

        public byte[] DecryptByte(byte[] buffer, AesParams aesParams)
        {
            Aes aes = aesParams.InstantiateAes();
            aes.Padding = PaddingMode.Zeros;
            aes.BlockSize = AesParams.blockSize;
            ICryptoTransform decryptor;
            if (aesParams.cipherMode == CipherMode.CBC)
            {
                decryptor = aes.CreateDecryptor(aesParams.key, aesParams.iv);
            }
            else
            {
                decryptor = aes.CreateDecryptor();
            }
            byte[] plaintext = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);
            return plaintext;
        }

        public void EncryptFile(FileInfo file, AesParams aesParams)
        {
            aesParams.fileName = file.Name;
            aesParams.sha = aesParams.ComputeSha(file);
            byte[] encryptedData;
            using (Aes aesAlg = aesParams.InstantiateAes())
            {
                ICryptoTransform encryptor = aesAlg.CreateEncryptor();
                using (FileStream fsInput = file.OpenRead())
                using (MemoryStream msOutput = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(msOutput, encryptor, CryptoStreamMode.Write))
                {
                    byte[] buffer = new byte[AesParams.blockSize];
                    int bytesRead;
                    while ((bytesRead = fsInput.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        cs.Write(buffer, 0, bytesRead);
                    }
                    cs.FlushFinalBlock();
                    encryptedData = msOutput.ToArray();
                }
            }
            string encFilePath = Path.ChangeExtension(file.Name, ".enc");
            File.WriteAllBytes(encFilePath, encryptedData);
        }

        public void EncryptFile(FileInfo file, AesParams aesParams, string password)
        {
            aesParams.sha = aesParams.ComputeSha(file);
            aesParams.ComputeKeyFromPassword(password, aesParams);
            EncryptFile(file, aesParams);
        }

        public void DecryptFile(FileInfo encrypted_file, AesParams aesParams)
        {
            using (Aes aes = aesParams.InstantiateAes())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor();
                using (FileStream fsInput = encrypted_file.OpenRead())
                using (MemoryStream msOutput = new MemoryStream())
                using (CryptoStream cs = new CryptoStream(fsInput, decryptor, CryptoStreamMode.Read))
                {
                    byte[] buffer = new byte[AesParams.blockSize];
                    int bytesRead;
                    while ((bytesRead = cs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        msOutput.Write(buffer, 0, bytesRead);
                    }
                    byte[] decryptedData = msOutput.ToArray();
                    byte[] decryptedSha = aesParams.ComputeSha(decryptedData);
                    if (!decryptedSha.SequenceEqual(aesParams.sha))
                    {
                        System.Windows.MessageBox.Show("Wrong SHA of decrypted file. File corrupted.", "Error", MessageBoxButton.OK);
                        throw new CryptographicException("Decrypted data does not match the SHA stored in aesParams.");
                    }
                    File.WriteAllBytes(aesParams.fileName, decryptedData);
                }
            }
        }

        public void DecryptFile(FileInfo file, AesParams aesParams, string password)
        {
            aesParams.ComputeKeyFromPassword(password, aesParams);
            DecryptFile(file, aesParams);
        }
        
        public byte[] EncryptPrivateKey(byte[] privateKey, AesParams aesParams, string password)
        {
            aesParams.sha = aesParams.ComputeSha(privateKey);
            aesParams.ComputeKeyFromPassword(password, aesParams);
            return EncryptByte(privateKey, aesParams);
        }
        
        public byte[] DecryptPrivateKey(byte[] privateKey, AesParams aesParams, string password)
        {
            aesParams.ComputeKeyFromPassword(password, aesParams);
            return DecryptByte(privateKey, aesParams);
        }

    }
}