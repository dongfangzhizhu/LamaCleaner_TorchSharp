using InpaintWeb.Models;
using InpaintWeb.Utilities;
using LamaCleaner_TorchSharp.Common;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InpaintWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("inpaint")]
        public async Task<IActionResult> Inpaint([FromBody] InpaintRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Image) || string.IsNullOrEmpty(request.Mask))
                {
                    return BadRequest("图像或遮罩数据不能为空");
                }
                byte[] sourceImgBytes = ImageHelpercs.Base64ToBytes(request.Image);
                byte[] maskImgBytes = ImageHelpercs.Base64ToBytes(request.Mask);
                var inpaintResult = Processor.Process(sourceImgBytes, maskImgBytes);
                var resultBase64 = ImageHelpercs.BytesToBase64(inpaintResult);
                return Ok(new { image =resultBase64 });
            }
            catch (Exception ex)
            {
                // 在实际生产环境中应该使用日志记录系统
                Console.WriteLine($"Inpaint处理错误: {ex.Message}");
                return StatusCode(500, "图像处理过程中发生错误");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
