using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                if (bytesLeft > Constants.ONE_MB - Constants.PACKET_NUMBER_BYTES_NUMBER)
                {
                    buffer = new byte[Constants.ONE_MB - Constants.PACKET_NUMBER_BYTES_NUMBER]; // ~1 MB
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
            IEnumerable<byte> rv = BitConverter.GetBytes((Int64)ExtensionMethods.getExtensionFromPath(filePath))
                .Concat(BitConverter.GetBytes(fileData.Length))
                .Concat(fileData);
            return rv.ToArray();
        }

        private byte[] prepareTextToSend(string text)
        {
            byte[] textData = Encoding.GetEncoding(28592).GetBytes(text); // kodowanie na polskie znak
            IEnumerable<byte> rv = BitConverter.GetBytes((Int64)Extensions.TEXT)
                .Concat(BitConverter.GetBytes(textData.Length))
                .Concat(textData);
            return rv.ToArray();
        }
    }
}