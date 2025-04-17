using LamaCleaner_TorchSharp.Common;
using OpenCvSharp;
using System.Reflection;
using System.Runtime.InteropServices;
using TorchSharp;
using static TorchSharp.torch;

namespace LamaCleaner_TorchSharp.Models
{
    public class LaMa : InpaintModel
    {
        private jit.ScriptModule<Tensor,Tensor,Tensor> model;

        public LaMa(Device device, Dictionary<string, object> kwargs = null)
            : base(device, kwargs)
        {
            Name = "lama";
            PadMod = 8;
        }

        protected override void InitModel(Device device, Dictionary<string, object> kwargs)
        {
            // 获取当前执行的Assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // 获取Assembly的代码基地或位置
            string assemblyLocation = assembly.Location;

            // 如果代码基地为空，则尝试使用CodeBase
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                UriBuilder uri = new UriBuilder(assembly.CodeBase);
                assemblyLocation = Uri.UnescapeDataString(uri.Path);
            }

            // 获取Assembly所在的目录
            string assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            // 构建完整的文件路径
            string modelfilefullPath = Path.Combine(assemblyDirectory, "big-lama.pt");
            // 加载TorchScript模型
            model = jit.load<Tensor, Tensor, Tensor>(modelfilefullPath, device);
            model.eval();
        }

        protected override Mat Forward(Mat image, Mat mask, Config config)
        {
            // 将图像和掩码转换为Tensor
            using var imageTensor = NormImg(image);
            using var maskTensor = NormImg(mask);

            // 将掩码二值化 (mask > 0) * 1
            using var binaryMask = maskTensor.gt(0).to(ScalarType.Float32);

            // 添加批次维度并移至设备
            using var batchedImage = imageTensor.unsqueeze(0).to(Device);
            using var batchedMask = binaryMask.unsqueeze(0).to(Device);

            //var pysafetensor = Safetensors.LoadStateDict("D:\\AI\\lama-cleaner\\compare.safetensors");
            //TensorUtils.TensorCompare(pysafetensor["mask"], batchedMask);
            //TensorUtils.TensorCompare(pysafetensor["image"], batchedImage);
            // 运行模型推理
            using var inpaintedImage = model.forward(batchedImage, batchedMask);
            // 使用torch.clip替代np.clip
            using var scaledClippedTensor = inpaintedImage[0].mul(255.0f).clip(0, 255).to(ScalarType.Byte);

            // 处理结果
            using var resultTensor = scaledClippedTensor.permute(1, 2, 0).detach().cpu();
            // 获取张量数据
            int height = (int)resultTensor.shape[0];
            int width = (int)resultTensor.shape[1];
            int channels = (int)resultTensor.shape[2];
            // 创建输出Mat
            using var resultMat = new Mat(height, width, MatType.CV_8UC3);
            // 获取tensor数据
            byte[] data = resultTensor.data<byte>().ToArray();            

            // 将数据复制到Mat
            Marshal.Copy(data, 0, resultMat.Data, data.Length);
            var bgrMat = new Mat();
            Cv2.CvtColor(resultMat, bgrMat, ColorConversionCodes.RGB2BGR);

            return bgrMat;
        }

        // 辅助方法：归一化图像
        private Tensor NormImg(Mat img)
        {
            // 转换为float32并归一化
            using var floatMat = new Mat();
            img.ConvertTo(floatMat, MatType.CV_32F, 1.0 / 255.0);

            // 获取图像尺寸和通道数
            int height = floatMat.Height;
            int width = floatMat.Width;
            int channels = floatMat.Channels();
            int totalSize = height * width * channels;

            // 创建数组并复制数据
            float[] data = new float[totalSize];
            Marshal.Copy(floatMat.Data, data, 0, totalSize);

            // 创建tensor并重塑为(H,W,C)
            var tensor = torch.tensor(data).reshape(height, width, channels);

            // 使用permute重排维度 (H,W,C) -> (C,H,W)
            return tensor.permute(2, 0, 1).contiguous();
        }
    }
}
