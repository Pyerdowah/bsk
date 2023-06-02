using System;
using System.Text;

namespace bsk
{
    public class DataModel
    {
        public string textMessage { get; set; }
        public byte[] file { get; set; }
        public Extensions extension { get; set; }
        public string filePath { get; set; }

        public DataModel(byte[] file, long extension, string filePath)
        {
            this.extension = ExtensionMethods.getExtensionFromLongValue(extension);
            if (this.extension == Extensions.TEXT)
            {
                this.textMessage = Encoding.GetEncoding(28592).GetString(file); // kodowanie na polskie znaki
                this.filePath = null;
            }
            else
            {
                this.textMessage = "Received " + ExtensionMethods.toString(this.extension) + " file.";
                this.filePath = filePath;
            }
        }

        public DataModel(string textMessage)
        {
            this.textMessage = textMessage;
            this.file = null;
            this.extension = Extensions.TEXT;
        }
    }
}