using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace bsk
{
    internal class Cryptography
    {
        private const string DecrFolder = "C:/ProgramData/Decription";
        private const string EncFolder = "C:/ProgramData/Encription";
        private readonly byte[] _salt = { 10, 20, 30, 40, 50, 60, 70, 80 };

        public Cryptography()
        {
            Directory.CreateDirectory(DecrFolder);
            Directory.CreateDirectory(EncFolder);
        }

        private byte[] ComputeSha(FileInfo file)
        {
            using (var mySha256 = SHA256.Create())
            {
                using (var fileStream = file.Open(FileMode.Open))
                {
                    fileStream.Position = 0;
                    // Compute the hash of the fileStream.
                    return mySha256.ComputeHash(fileStream);
                }
            }
        }

        public int Cipher(bool decrypt, string fName, CipherMode cipherMode, string password, int err)
        {
            if (decrypt)
                return Decrypt(new FileInfo(fName), cipherMode, err, password);

            Encrypt(new FileInfo(fName), cipherMode, password);

            return 0;
        }

        private void Encrypt(FileInfo file, CipherMode cipherMode, string password)
        {
            // Create instance of Aes for
            // symmetric encryption of the data.
            var aes = Aes.Create();
            aes.Mode = cipherMode;

            var keyGenerator = new Rfc2898DeriveBytes(password, _salt, 300);

            aes.Key = keyGenerator.GetBytes(32);
            aes.IV = keyGenerator.GetBytes(16);

            var transform = aes.CreateEncryptor();

            // Write the following to the FileStream
            // for the encrypted file (outFs):
            // - extension
            // - sha256
            // - the encrypted cipher content

            var ext = Encoding.ASCII.GetBytes(Path.GetExtension(file.FullName));

            // Change the file's extension to ".enc"
            var outFile = Path.Combine(EncFolder, Path.ChangeExtension(file.Name, ".enc"));

            using (var outFs = new FileStream(outFile, FileMode.Create))
            {
                outFs.Write(ext, 0, 4);
                outFs.Seek(4, SeekOrigin.Begin);
                var sha = ComputeSha(file);

                for (var i = 0; i < sha.Length; i++) outFs.WriteByte(sha[i]);

                // Now write the cipher text using
                // a CryptoStream for encrypting.
                using (var outStreamEncrypted =
                       new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                {
                    // blockSizeBytes can be any arbitrary size.
                    var blockSizeBytes = aes.BlockSize / 8;
                    var data = new byte[blockSizeBytes];

                    using (var inFs = new FileStream(file.FullName, FileMode.Open))
                    {
                        var count = 0;
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            outStreamEncrypted.Write(data, 0, count);
                        } while (count > 0);
                    }

                    outStreamEncrypted.FlushFinalBlock();
                }
            }
        }

        private int Decrypt(FileInfo file, CipherMode cipherMode, int modification, string password)
        {
            // Create instance of Aes for
            // symmetric decryption of the data.
            var aes = Aes.Create();
            aes.Mode = cipherMode;

            // Create byte arrays to get extension of
            // the encrypted file and SHA256.
            // These values were stored as 4 and 32 bytes
            // at the beginning of the encrypted package.

            var extBytes = new byte[4];
            var sha = new byte[32];


            // Use FileStream objects to read the encrypted
            // file (inFs) and save the decrypted file (outFs).
            using (var inFs = new FileStream(file.FullName, FileMode.Open))
            {
                inFs.Seek(0, SeekOrigin.Begin);
                inFs.Read(extBytes, 0, 4);
                inFs.Seek(4, SeekOrigin.Begin);
                inFs.Read(sha, 0, 32);

                var ext = Encoding.ASCII.GetString(extBytes);

                // Construct the file name for the decrypted file.
                var outFile = Path.Combine(DecrFolder, Path.ChangeExtension(file.Name, ext));

                // Determine the start position of
                // the cipher text (startC)
                // and its length(lenC).
                var startC = 36;

                if (modification != -1)
                {
                    var rnd = new Random();
                    var modifi = new byte[1];
                    rnd.NextBytes(modifi);

                    if (modification + startC > (int)inFs.Length)
                        inFs.Seek(-startC, SeekOrigin.End);
                    else
                        inFs.Seek(startC + modification, SeekOrigin.Begin);

                    inFs.WriteByte(modifi[0]);
                }

                // Create the byte arrays for
                // the encrypted Aes key,
                // the IV, and the cipher text.
                var Key = new byte[32];
                var IV = new byte[16];

                var keyGenerator = new Rfc2898DeriveBytes(password, _salt, 300);

                Key = keyGenerator.GetBytes(32);
                IV = keyGenerator.GetBytes(16);


                var transform = aes.CreateDecryptor();
                try
                {
                    // Decrypt the key.
                    transform = aes.CreateDecryptor(Key, IV);
                }
                catch (Exception)
                {
                    return -2;
                }

                // Decrypt the cipher text from
                // from the FileSteam of the encrypted
                // file (inFs) into the FileStream
                // for the decrypted file (outFs).
                using (var outFs = new FileStream(outFile, FileMode.Create))
                {


                    // blockSizeBytes can be any arbitrary size.
                    var blockSizeBytes = aes.BlockSize / 8;
                    var data = new byte[blockSizeBytes];

                    // By decrypting a chunk a time,
                    // you can save memory and
                    // accommodate large files.

                    // Start at the beginning
                    // of the cipher text.
                    inFs.Seek(startC, SeekOrigin.Begin);
                    using (var outStreamDecrypted =
                           new CryptoStream(outFs, transform, CryptoStreamMode.Write))
                    {
                        var count = 0;
                        do
                        {
                            count = inFs.Read(data, 0, blockSizeBytes);
                            outStreamDecrypted.Write(data, 0, count);
                        } while (count > 0);

                        try
                        {
                            outStreamDecrypted.FlushFinalBlock();
                        }
                        catch (Exception)
                        {
                            return -1;
                        }
                    }
                }

                try
                {
                    if (ext == ".txt")
                        new Process
                        {
                            StartInfo = new ProcessStartInfo("notepad++.exe", outFile) { UseShellExecute = true }
                        }.Start();
                    else
                        new Process
                        {
                            StartInfo = new ProcessStartInfo(outFile) { UseShellExecute = true }
                        }.Start();
                }
                catch (Exception)
                {
                    new Process
                    {
                        StartInfo = new ProcessStartInfo(outFile) { UseShellExecute = true }
                    }.Start();
                }

                if (!sha.SequenceEqual(ComputeSha(new FileInfo(outFile))))
                    return -3;
            }

            return 0;
        }
    }
}