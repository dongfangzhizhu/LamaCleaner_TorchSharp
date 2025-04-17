using OpenCvSharp;
using System.Runtime.InteropServices;
using static TorchSharp.torch;

namespace LamaCleaner_TorchSharp.Common
{
    public static class Utils
    {
        public static int CeilModulo(int x, int mod)
        {
            if (x % mod == 0)
                return x;
            return (x / mod + 1) * mod;
        }

        public static Tensor PadImgToModulo(Tensor img, int mod, bool square = false, int? minSize = null)
        {
            long[] shape = img.shape;
            int height = (int)shape[0];
            int width = (int)shape[1];
            int channels = shape.Length > 2 ? (int)shape[2] : 1;

            if (channels == 1 && shape.Length == 2)
            {
                img = img.unsqueeze(-1);
            }

            int outHeight = CeilModulo(height, mod);
            int outWidth = CeilModulo(width, mod);

            if (minSize.HasValue)
            {
                if (minSize.Value % mod != 0)
                    throw new ArgumentException("min_size must be divisible by mod");

                outWidth = Math.Max(minSize.Value, outWidth);
                outHeight = Math.Max(minSize.Value, outHeight);
            }

            if (square)
            {
                int maxSize = Math.Max(outHeight, outWidth);
                outHeight = maxSize;
                outWidth = maxSize;
            }

            // Pad using reflection padding
            var padding = nn.functional.pad(img,new long[] { 0, 0, 0, outWidth - width, 0, outHeight - height },TorchSharp.PaddingModes.Reflect);

            return padding;
        }

        public static Mat PadImgToModulo(Mat img, int mod, bool square = false, int? minSize = null)
        {
            int height = img.Height;
            int width = img.Width;
            int channels = img.Channels();

            int outHeight = CeilModulo(height, mod);
            int outWidth = CeilModulo(width, mod);

            if (minSize.HasValue)
            {
                if (minSize.Value % mod != 0)
                    throw new ArgumentException("min_size must be divisible by mod");

                outWidth = Math.Max(minSize.Value, outWidth);
                outHeight = Math.Max(minSize.Value, outHeight);
            }

            if (square)
            {
                int maxSize = Math.Max(outHeight, outWidth);
                outHeight = maxSize;
                outWidth = maxSize;
            }

            Mat result = new Mat(outHeight, outWidth, img.Type());

            // Create ROI for the original image area
            using var roi = new Mat(result, new Rect(0, 0, width, height));
            img.CopyTo(roi);

            // Pad the rest using symmetric padding
            if (outWidth > width)
            {
                for (int x = width; x < outWidth; x++)
                {
                    int srcX = width - (x - width + 1) % width - 1;
                    for (int y = 0; y < height; y++)
                    {
                        result.Set(y, x, img.Get<Vec3b>(y, srcX));
                    }
                }
            }

            if (outHeight > height)
            {
                for (int y = height; y < outHeight; y++)
                {
                    int srcY = height - (y - height + 1) % height - 1;
                    for (int x = 0; x < outWidth; x++)
                    {
                        if (x < width)
                        {
                            result.Set(y, x, img.Get<Vec3b>(srcY, x));
                        }
                        else
                        {
                            int srcX = width - (x - width + 1) % width - 1;
                            result.Set(y, x, img.Get<Vec3b>(srcY, srcX));
                        }
                    }
                }
            }

            return result;
        }

        public static List<int[]> BoxesFromMask(Mat mask)
        {
            int height = mask.Height;
            int width = mask.Width;

            Mat thresh = new Mat();
            Cv2.Threshold(mask, thresh, 127, 255, ThresholdTypes.Binary);

            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            var boxes = new List<int[]>();
            foreach (var cnt in contours)
            {
                var rect = Cv2.BoundingRect(cnt);
                int[] box = new int[] { rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height };

                // Clip to image boundaries
                box[0] = Math.Max(box[0], 0);
                box[2] = Math.Min(box[2], width);
                box[1] = Math.Max(box[1], 0);
                box[3] = Math.Min(box[3], height);

                boxes.Add(box);
            }

            return boxes;
        }

        public static Tensor NormImg(Tensor npImg)
        {
            long[] shape = npImg.shape;

            if (shape.Length == 2)
            {
                npImg = npImg.unsqueeze(-1);
            }

            // Transpose from HWC to CHW
            npImg = npImg.permute(2, 0, 1);

            // Convert to float and normalize
            npImg = npImg.to(ScalarType.Float32).div(255.0f);

            return npImg;
        }

        public static Mat NormImg(Mat img)
        {
            Mat result = new Mat();
            img.ConvertTo(result, MatType.CV_32FC3, 1.0 / 255.0);
            return result;
        }

        public static Mat ResizeMaxSize(Mat npImg, int sizeLimit, InterpolationFlags interpolation = InterpolationFlags.Cubic)
        {
            int h = npImg.Height;
            int w = npImg.Width;

            if (Math.Max(h, w) > sizeLimit)
            {
                double ratio = (double)sizeLimit / Math.Max(h, w);
                int newW = (int)(w * ratio + 0.5);
                int newH = (int)(h * ratio + 0.5);

                Mat resized = new Mat();
                Cv2.Resize(npImg, resized, new OpenCvSharp.Size(newW, newH), 0, 0, interpolation);
                return resized;
            }

            return npImg.Clone();
        }

        public static (Mat, byte[]) LoadImg(Mat image)
        {
            byte[] alphaChannel = null;

            if (image.Channels() == 4) // BGRA
            {
                Mat npImg = new Mat();
                Cv2.CvtColor(image, npImg, ColorConversionCodes.BGRA2RGBA);

                // Extract alpha channel
                Mat[] channels = Cv2.Split(npImg);
                alphaChannel = new byte[channels[3].Width * channels[3].Height];
                Marshal.Copy(channels[3].Data, alphaChannel, 0, alphaChannel.Length);

                return (npImg, alphaChannel);
            }
            else if (image.Channels() == 1) // Grayscale
            {
                Mat npImg = new Mat();
                Cv2.CvtColor(image, npImg, ColorConversionCodes.GRAY2RGB);
                return (npImg, alphaChannel);
            }
            else if (image.Channels() == 3) // BGR
            {
                Mat npImg = new Mat();
                Cv2.CvtColor(image, npImg, ColorConversionCodes.BGR2RGB);
                return (npImg, alphaChannel);
            }

            return (image.Clone(), alphaChannel);
        }

       
    }

}
