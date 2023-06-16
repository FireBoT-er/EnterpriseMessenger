using System;
using System.Linq;

namespace EnterpriseMessengerUI
{
    public static class FormatCheck
    {
        public static bool IsImage(byte[] bytes)
        {
            byte[] png = new byte[] { 137, 80, 78, 71 };
            byte[] jpeg = new byte[] { 255, 216, 255, 224 };
            byte[] jpeg2 = new byte[] { 255, 216, 255, 225 };
            byte[] bmp = new byte[] { 66, 77 };
            byte[] gif = new byte[] { 71, 73, 70 };

            if (png.SequenceEqual(bytes.Take(png.Length)))
            {
                return true;
            }

            if (jpeg.SequenceEqual(bytes.Take(jpeg.Length)))
            {
                return true;
            }

            if (jpeg2.SequenceEqual(bytes.Take(jpeg2.Length)))
            {
                return true;
            }

            if (bmp.SequenceEqual(bytes.Take(bmp.Length)))
            {
                return true;
            }

            if (gif.SequenceEqual(bytes.Take(gif.Length)))
            {
                return true;
            }

            return false;
        }
    }
}
