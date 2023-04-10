using System.IO;

namespace bsk
{
    public enum Extensions
    {
        TEXT = 0,
        TXT = 1,
        PNG = 2,
        PDF = 3,
        AVI = 4
    }

    public static class ExtensionMethods
    {
        public static string toString(Extensions extension)
        {
            switch (extension)
            {
                case (Extensions.TEXT):
                    return "text";
                case (Extensions.AVI):
                    return "avi";
                case (Extensions.PDF):
                    return "pdf";
                case (Extensions.PNG):
                    return "png";
                case (Extensions.TXT):
                    return "txt";
            }

            return "text";
        }
        
        public static string setFileFilter(Extensions extension)
        {
            switch (extension)
            {
                case (Extensions.AVI):
                    return "Video files|*.avi";
                case (Extensions.PDF):
                    return "PDF files|*.pdf";
                case (Extensions.PNG):
                    return "Image files|*.png";
                case (Extensions.TXT):
                    return "Text files|*.txt";
            }

            return "All files|*.*";
        }

        public static Extensions getExtensionFromLongValue(long extension)
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

        public static Extensions getExtensionFromPath(string path)
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