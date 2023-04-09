using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace bsk
{
    public class DataPack
    {
        public List<byte[]> preparePacketsToSend(string filePath, bool fileToSend)
        {
            List<byte[]> packetsToSend = new List<byte[]>();
            byte[] dataToDivide;
            int bytesLeft;
            if (fileToSend)
            {
                dataToDivide = prepareFileToSend(filePath);
                bytesLeft = dataToDivide.Length;
            }
            else
            {
                dataToDivide = prepareTextToSend(filePath);
                bytesLeft = dataToDivide.Length;
            }

            int packetNumber = 0;
            while (bytesLeft > 0)
            {
                byte[] buffer;
                if (bytesLeft > 1024 * 1020)
                {
                    buffer = new byte[1024 * 1020]; // ~1 MB
                }
                else
                {
                    buffer = new byte[bytesLeft];
                }
                
                Array.Copy(dataToDivide, packetNumber * buffer.Length, buffer,
                    0, buffer.Length);
                byte[] rv = BitConverter.GetBytes(packetNumber).Concat(buffer).ToArray();
                packetsToSend.Add(rv);
                bytesLeft -= rv.Length;
                packetNumber++;
            }

            return packetsToSend;
        }

        private byte[] prepareFileToSend(string filePath)
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            IEnumerable<byte> rv = BitConverter.GetBytes((Int64)getExtension(filePath))
                .Concat(BitConverter.GetBytes(fileData.Length))
                .Concat(fileData);
            return rv.ToArray();
        }
        
        private byte[] prepareTextToSend(string text)
        {
            byte[] textData = Encoding.GetEncoding(28592).GetBytes(text);
            IEnumerable<byte> rv = BitConverter.GetBytes((Int64)Extensions.TEXT)
                .Concat(BitConverter.GetBytes(textData.Length))
                .Concat(textData);
            return rv.ToArray();
        }

        private Extensions getExtension(string path)
        {
            string extension = Path.GetExtension(path);
            switch (extension)
            {
                case (""):
                    return Extensions.TEXT;
                case (".avi"):
                    return Extensions.AVI;
                case (".pdf"):
                    return Extensions.PDF;
                case (".png"):
                    return Extensions.PNG;
                case (".txt"):
                    return Extensions.TXT;
            }

            return Extensions.TEXT;
        }

    }
}