using LamaCleaner_TorchSharp.Models;
using OpenCvSharp;
using static TorchSharp.torch;

namespace LamaCleaner_TorchSharp.Common
{
    public static class Processor
    {
        private static Config config = new Config()
        {
            LdmSteps = 25,
            LdmSampler = LDMSampler.plms,
            ZitsWireframe = true,
            HdStrategy = HDStrategy.Crop,
            HdStrategyCropMargin = 196,
            HdStrategyCropTriggerSize = 800,
            HdStrategyResizeLimit = 2048,
            Prompt = "",
            NegativePrompt = "",
            UseCroper = false,
            CroperX = 284,
            CroperY = 464,
            CroperHeight = 512,
            CroperWidth = 512,
            SdScale = 1.0f,
            SdMaskBlur = 5,
            SdStrength = 0.75f,
            SdSteps = 50,
            SdGuidanceScale = 7.5f,
            SdSampler = SDSampler.uni_pc,
            SdSeed = -1,
            SdMatchHistograms = false,
            Cv2Flag = "INPAINT_NS",
            Cv2Radius = 5,
            P2pSteps = 50,
            P2pImageGuidanceScale = 1.5f,
            P2pGuidanceScale = 7.5f,
            ControlnetConditioningScale = 0.4f
        };
        private static LaMa model = new LaMa(cuda.is_available() ? CUDA : CPU);
        public static byte[] Process(byte[] sourceImgBytes, byte[] maskImgBytes)
        {                    

            try
            {
                using var img = Cv2.ImDecode(sourceImgBytes, ImreadModes.Unchanged);

                using var mask = Cv2.ImDecode(maskImgBytes, ImreadModes.Grayscale);


                // 处理alpha通道
                Mat finalResult = Process(img,mask);

                byte[] resultBytes;
                Cv2.ImEncode(".png", finalResult, out resultBytes);
                return resultBytes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理过程中发生错误: {ex.Message}");
                throw;
            }
        }
        public static Mat Process(Mat img, Mat mask)
        {

            try
            {

                // 加载并处理图像
                var (image, alphaChannel) = LoadImg(img);

                // 二值化掩码
                Cv2.Threshold(mask, mask, 127, 255, ThresholdTypes.Binary);

                // 设置插值方法和大小限制
                int sizeLimit = Math.Max(image.Height, image.Width);

                // 调整图像大小
                using var resizedImage = ResizeMaxSize(image, sizeLimit);
                using var resizedMask = ResizeMaxSize(mask, sizeLimit);

                // 执行修复
                using var resNpImg = model.Process(resizedImage, resizedMask, config);

                // 处理alpha通道
                Mat finalResult = ProcessAlphaChannel(resNpImg, alphaChannel);
                return finalResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理过程中发生错误: {ex.Message}");
                throw;
            }
        }

        private static (Mat, Mat) LoadImg(Mat image)
        {
            Mat alphaChannel = null;
            Mat result;

            if (image.Channels() == 4) // BGRA
            {
                Mat rgba = new Mat();
                Cv2.CvtColor(image, rgba, ColorConversionCodes.BGRA2RGBA);
                Mat[] channels = new Mat[4];
                Cv2.Split(rgba, out channels);
                alphaChannel = channels[3].Clone();

                result = new Mat();
                Cv2.Merge(new[] { channels[0], channels[1], channels[2] }, result);

                foreach (var channel in channels)
                {
                    channel.Dispose();
                }
                rgba.Dispose();
            }
            else if (image.Channels() == 1) // 灰度图
            {
                result = new Mat();
                Cv2.CvtColor(image, result, ColorConversionCodes.GRAY2RGB);
            }
            else // BGR
            {
                result = new Mat();
                Cv2.CvtColor(image, result, ColorConversionCodes.BGR2RGB);
            }

            return (result, alphaChannel);
        }

        private static Mat ResizeMaxSize(Mat img, int sizeLimit, InterpolationFlags interpolation = InterpolationFlags.Cubic)
        {
            if (Math.Max(img.Height, img.Width) > sizeLimit)
            {
                double ratio = (double)sizeLimit / Math.Max(img.Height, img.Width);
                int newWidth = (int)(img.Width * ratio + 0.5);
                int newHeight = (int)(img.Height * ratio + 0.5);

                Mat resized = new Mat();
                Cv2.Resize(img, resized, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, interpolation);
                return resized;
            }

            return img.Clone();
        }

        private static Mat ProcessAlphaChannel(Mat resNpImg, Mat alphaChannel)
        {
            if (alphaChannel == null)
            {
                return resNpImg.Clone();
            }

            if (alphaChannel.Size() != resNpImg.Size())
            {
                Mat resizedAlpha = new Mat();
                Cv2.Resize(alphaChannel, resizedAlpha, resNpImg.Size(), 0, 0, InterpolationFlags.Cubic);
                alphaChannel = resizedAlpha;
            }

            Mat[] channels = new Mat[4];
            Cv2.Split(resNpImg, out channels);
            List<Mat> channelsList = new List<Mat>(channels);
            channelsList.Add(alphaChannel);
            Mat result = new Mat();
            Cv2.Merge(channelsList.ToArray(), result);

            foreach (var channel in channels)
            {
                if (channel != alphaChannel) // 避免重复释放alphaChannel
                {
                    channel.Dispose();
                }
            }

            return result;
        }

    }
}
