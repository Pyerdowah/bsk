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
    }
}