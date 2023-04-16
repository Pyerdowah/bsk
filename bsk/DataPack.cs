using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace bsk
{
    public class DataPack
    {
        public byte[] header { get; set; }
        public long messSize { get; set; }
        public long packetsNumber { get; set; }
        public bool fileToSend { get; set; }
        public string filePath { get; set; }

        public void prepareHeaderToSend(string filePath, bool fileToSend)
        {
            this.filePath = filePath;
            this.fileToSend = fileToSend;
            long bytesLeft;
            FileInfo fileInfo;
            Guid guid = Guid.NewGuid();
            IEnumerable<byte> rv;
            if (fileToSend)
            {
                fileInfo = new FileInfo(filePath);
                rv = guid.ToByteArray().Concat(BitConverter.GetBytes((Int64)ExtensionMethods.getExtensionFromPath(filePath))
                    .Concat(BitConverter.GetBytes(fileInfo.Length)));
                bytesLeft = fileInfo.Length;
            }
            else
            {
                rv = guid.ToByteArray().Concat(BitConverter.GetBytes((Int64)Extensions.TEXT)
                    .Concat(BitConverter.GetBytes((Int64)filePath.Length)));
                bytesLeft = filePath.Length;
            }

            long numberOfPackets = bytesLeft / (Constants.ONE_KB - Constants.HEADER_BYTES_NUMBER) +
                                   (bytesLeft % (Constants.ONE_KB - Constants.HEADER_BYTES_NUMBER) != 0 ? 1 : 0);
            header = rv.ToArray().Concat(BitConverter.GetBytes(numberOfPackets)).ToArray();
            messSize = bytesLeft;
            packetsNumber = numberOfPackets;
        }
    }
}