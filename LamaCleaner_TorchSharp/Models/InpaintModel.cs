using LamaCleaner_TorchSharp.Common;
using OpenCvSharp;
using static TorchSharp.torch;

namespace LamaCleaner_TorchSharp.Models
{
    public class InpaintModel
    {
        public string Name { get; protected set; } = "base";
        public int? MinSize { get; protected set; } = null;
        public int PadMod { get; protected set; } = 8;
        public bool PadToSquare { get; protected set; } = false;
        protected Device Device;

        public InpaintModel(Device device, Dictionary<string, object> kwargs = null)
        {
            Device = device;
            InitModel(device, kwargs ?? new Dictionary<string, object>());
        }

        protected virtual void InitModel(Device device, Dictionary<string, object> kwargs)
        {
            // 基类中为空实现，由子类重写
        }

        protected Mat _padForward(Mat image, Mat mask, Config config)
        {
            int originHeight = image.Height;
            int originWidth = image.Width;

            Mat padImage = PadImgToModulo(image, PadMod, PadToSquare, MinSize);
            Mat padMask = PadImgToModulo(mask, PadMod, PadToSquare, MinSize);

            Mat result = Forward(padImage, padMask, config);
            result = new Mat(result, new Rect(0, 0, originWidth, originHeight));

            (result, image, mask) = ForwardPostProcess(result, image, mask, config);

            // 将mask转换为3通道
            Mat maskExpanded = new Mat();
            Cv2.CvtColor(mask, maskExpanded, ColorConversionCodes.GRAY2BGR);

            // 将mask归一化到0-1范围
            Mat maskNormalized = new Mat();
            maskExpanded.ConvertTo(maskNormalized, MatType.CV_32FC3, 1.0 / 255.0);

            // 将image颜色通道顺序反转 (BGR -> RGB)
            Mat imageRGB = new Mat();
            Cv2.CvtColor(image, imageRGB, ColorConversionCodes.BGR2RGB);

            // 结果 = 结果 * (mask / 255) + image * (1 - (mask / 255))
            Mat resultFloat = new Mat();
            result.ConvertTo(resultFloat, MatType.CV_32FC3);

            Mat imageFloat = new Mat();
            imageRGB.ConvertTo(imageFloat, MatType.CV_32FC3);

            Mat inverseMask = new Mat();
            Cv2.Subtract(new Scalar(1, 1, 1), maskNormalized, inverseMask);

            Mat maskedResult = new Mat();
            Cv2.Multiply(resultFloat, maskNormalized, maskedResult);

            Mat maskedImage = new Mat();
            Cv2.Multiply(imageFloat, inverseMask, maskedImage);

            Mat combined = new Mat();
            Cv2.Add(maskedResult, maskedImage, combined);

            Mat finalResult = new Mat();
            combined.ConvertTo(finalResult, MatType.CV_8UC3);

            return finalResult;
        }

        protected virtual (Mat, Mat, Mat) ForwardPostProcess(Mat result, Mat image, Mat mask, Config config)
        {
            return (result, image, mask);
        }

        protected virtual Mat Forward(Mat image, Mat mask, Config config)
        {
            // 基类中为空实现，由子类重写
            return new Mat();
        }

        public Mat Process(Mat image, Mat mask, Config config)
        {
            Mat inpaintResult = null;

            if (config.HdStrategy == HDStrategy.Crop)
            {
                int maxDimension = Math.Max(image.Height, image.Width);
                if (maxDimension > config.HdStrategyCropTriggerSize)
                {
                    List<Mat> boxes = BoxesFromMask(mask);
                    List<(Mat, int[])> cropResults = new List<(Mat, int[])>();

                    foreach (var box in boxes)
                    {
                        (Mat cropImage, int[] cropBox) = _runBox(image, mask, box, config);
                        cropResults.Add((cropImage, cropBox));
                    }

                    // 创建结果图像，并将BGR转为RGB
                    inpaintResult = new Mat();
                    Cv2.CvtColor(image, inpaintResult, ColorConversionCodes.BGR2RGB);

                    foreach (var (cropImage, cropBox) in cropResults)
                    {
                        int x1 = cropBox[0], y1 = cropBox[1], x2 = cropBox[2], y2 = cropBox[3];
                        cropImage.CopyTo(new Mat(inpaintResult, new Rect(x1, y1, x2 - x1, y2 - y1)));
                    }
                }
            }
            else if (config.HdStrategy == HDStrategy.Resize)
            {
                int maxDimension = Math.Max(image.Height, image.Width);
                if (maxDimension > config.HdStrategyResizeLimit)
                {
                    var originSize = new OpenCvSharp.Size(image.Width, image.Height);
                    Mat downsizeImage = ResizeMaxSize(image, config.HdStrategyResizeLimit);
                    Mat downsizeMask = ResizeMaxSize(mask, config.HdStrategyResizeLimit);

                    inpaintResult = _padForward(downsizeImage, downsizeMask, config);

                    // 将结果调整回原始大小
                    Cv2.Resize(inpaintResult, inpaintResult, originSize, 0, 0, InterpolationFlags.Cubic);

                    // 只在掩码区域应用结果
                    Mat originalPixelMask = new Mat();
                    Cv2.Threshold(mask, originalPixelMask, 127, 255, ThresholdTypes.BinaryInv);

                    Mat imageRGB = new Mat();
                    Cv2.CvtColor(image, imageRGB, ColorConversionCodes.BGR2RGB);

                    imageRGB.CopyTo(inpaintResult, originalPixelMask);
                }
            }

            if (inpaintResult == null)
            {
                inpaintResult = _padForward(image, mask, config);
            }

            return inpaintResult;
        }

        private (Mat, Mat, int[]) _cropBox(Mat image, Mat mask, Mat box, Config config)
        {
            int[] boxArray = new int[4];
            for (int i = 0; i < 4; i++)
                boxArray[i] = (int)box.Get<float>(0, i);

            int boxH = boxArray[3] - boxArray[1];
            int boxW = boxArray[2] - boxArray[0];
            int cx = (boxArray[0] + boxArray[2]) / 2;
            int cy = (boxArray[1] + boxArray[3]) / 2;
            int imgH = image.Height;
            int imgW = image.Width;

            int w = boxW + config.HdStrategyCropMargin * 2;
            int h = boxH + config.HdStrategyCropMargin * 2;

            int _l = cx - w / 2;
            int _r = cx + w / 2;
            int _t = cy - h / 2;
            int _b = cy + h / 2;

            int l = Math.Max(_l, 0);
            int r = Math.Min(_r, imgW);
            int t = Math.Max(_t, 0);
            int b = Math.Min(_b, imgH);

            // 尝试在裁剪图像边缘时获取更多上下文
            if (_l < 0)
                r += Math.Abs(_l);
            if (_r > imgW)
                l -= _r - imgW;
            if (_t < 0)
                b += Math.Abs(_t);
            if (_b > imgH)
                t -= _b - imgH;

            l = Math.Max(l, 0);
            r = Math.Min(r, imgW);
            t = Math.Max(t, 0);
            b = Math.Min(b, imgH);

            Mat cropImg = new Mat(image, new Rect(l, t, r - l, b - t));
            Mat cropMask = new Mat(mask, new Rect(l, t, r - l, b - t));

            return (cropImg, cropMask, new int[] { l, t, r, b });
        }

        private (Mat, int[]) _runBox(Mat image, Mat mask, Mat box, Config config)
        {
            (Mat cropImg, Mat cropMask, int[] cropBox) = _cropBox(image, mask, box, config);
            return (_padForward(cropImg, cropMask, config), cropBox);
        }

        // 辅助方法
        public static Mat PadImgToModulo(Mat img, int mod, bool square = false, int? minSize = null)
        {
            // 处理灰度图像
            Mat result;
            bool isGray = img.Channels() == 1;

            if (isGray)
            {
                // 创建一个与原图像相同大小的单通道图像
                result = new Mat(img.Size(), MatType.CV_8UC1);
                img.CopyTo(result);

                // 扩展为三维数组，但保持单通道
                // 这相当于Python中的 img[:, :, np.newaxis]
                result = result.Reshape(1, img.Rows);
            }
            else
            {
                result = img.Clone();
            }

            int height = result.Height;
            int width = result.Width;

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

            Mat padded = new Mat(outHeight, outWidth, result.Type());

            // 复制原始图像到填充图像
            result.CopyTo(new Mat(padded, new Rect(0, 0, width, height)));

            // 对称填充剩余部分
            int bottomPad = outHeight - height;
            int rightPad = outWidth - width;

            if (bottomPad > 0 || rightPad > 0)
            {
                Cv2.CopyMakeBorder(
                    result,
                    padded,
                    0, bottomPad,
                    0, rightPad,
                    BorderTypes.Reflect101
                );
            }

            return padded;
        }

        private static int CeilModulo(int x, int mod)
        {
            if (x % mod == 0)
                return x;
            return (x / mod + 1) * mod;
        }

        public static List<Mat> BoxesFromMask(Mat mask)
        {
            int height = mask.Height;
            int width = mask.Width;

            Mat thresh = new Mat();
            Cv2.Threshold(mask, thresh, 127, 255, ThresholdTypes.Binary);

            Point[][] contours;
            HierarchyIndex[] hierarchy;
            Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            List<Mat> boxes = new List<Mat>();
            foreach (var cnt in contours)
            {
                Rect rect = Cv2.BoundingRect(cnt);

                // 创建包含边界框坐标的矩阵 [x, y, x+w, y+h]
                Mat box = new Mat(1, 4, MatType.CV_32F);
                box.Set<float>(0, 0, rect.X);
                box.Set<float>(0, 1, rect.Y);
                box.Set<float>(0, 2, rect.X + rect.Width);
                box.Set<float>(0, 3, rect.Y + rect.Height);

                // 裁剪到图像边界
                float x1 = Math.Max(box.Get<float>(0, 0), 0);
                float y1 = Math.Max(box.Get<float>(0, 1), 0);
                float x2 = Math.Min(box.Get<float>(0, 2), width);
                float y2 = Math.Min(box.Get<float>(0, 3), height);

                box.Set<float>(0, 0, x1);
                box.Set<float>(0, 1, y1);
                box.Set<float>(0, 2, x2);
                box.Set<float>(0, 3, y2);

                boxes.Add(box);
            }

            return boxes;
        }

        public static Mat ResizeMaxSize(Mat img, int sizeLimit, InterpolationFlags interpolation = InterpolationFlags.Cubic)
        {
            int h = img.Height;
            int w = img.Width;

            if (Math.Max(h, w) > sizeLimit)
            {
                double ratio = (double)sizeLimit / Math.Max(h, w);
                int newW = (int)(w * ratio + 0.5);
                int newH = (int)(h * ratio + 0.5);

                Mat resized = new Mat();
                Cv2.Resize(img, resized, new OpenCvSharp.Size(newW, newH), 0, 0, interpolation);
                return resized;
            }

            return img.Clone();
        }
    }
}
