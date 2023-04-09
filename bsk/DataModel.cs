using System.IO;
using System.Text;

namespace bsk
{
    public class DataModel
    {
        public string textMessage { get; set; }
        public byte[] file { get; set; }
        public Extensions extension { get; set; }

        public DataModel(byte[] file, long extension)
        {
            this.file = file;
            this.extension = getExtension(extension);
            if (this.extension == Extensions.TEXT)
            {
                this.textMessage = Encoding.GetEncoding(28592).GetString(file);
            }
            else
            {
                this.textMessage = "Received " + ExtensionMethods.toString(this.extension) + " file";
            }
        }
        
        public DataModel(string textMessage)
        {
            this.textMessage = textMessage;
            this.file = null;
            this.extension = Extensions.TEXT;
        }
        
        private Extensions getExtension(long extension)
        {
            switch (extension)
            {
                case (0):
                    return Extensions.TEXT;
                case (4):
                    return Extensions.AVI;
                case (3):
                    return Extensions.PDF;
                case (2):
                    return Extensions.PNG;
                case (1):
                    return Extensions.TXT;
            }

            return Extensions.TEXT;
        }
    }
} 