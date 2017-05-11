using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Microsoft.Kinect.SmartRoom
{
    internal class ImageHelper
    {
        public static Bitmap CropAtRect(Bitmap b, Rectangle r)
        {
            
                Bitmap nb = new Bitmap(r.Width, r.Height);
                Graphics g = Graphics.FromImage(nb);
                g.DrawImage(b, -r.X, -r.Y);
            
            return nb;
        }
        public static void TransferPixelsToBitmapObject(Bitmap bmTarget, byte[] byPixelsForBitmap)
        {
            // Create a rectangle with width and height matching those of
            // the target bitmap object.
            Rectangle rectAreaOfInterest = new Rectangle
            (0, 0, bmTarget.Width, bmTarget.Height);

            // Lock the bits of the Bitmap object.
            BitmapData bmpData = bmTarget.LockBits
            (rectAreaOfInterest,
            ImageLockMode.WriteOnly,
            bmTarget.PixelFormat);
            IntPtr ptrFirstScanLineOfBitmap = bmpData.Scan0;

            int length = byPixelsForBitmap.Length;

            // Transfer all the data from byPixelsForBitmap to 
            // the pixel buffer for bmTarget.
            System.Runtime.InteropServices.Marshal.Copy
            (byPixelsForBitmap, 0, ptrFirstScanLineOfBitmap, length);
            // Unlock the bits.
            bmTarget.UnlockBits(bmpData);
            bmpData = null;
        }
    }
}
