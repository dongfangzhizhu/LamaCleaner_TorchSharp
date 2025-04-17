using System.ComponentModel.DataAnnotations;

namespace InpaintWeb.Models
{
    public class InpaintRequest
    {
        [Required(ErrorMessage = "图像数据不能为空")]
        public string Image { get; set; }

        [Required(ErrorMessage = "遮罩数据不能为空")]
        public string Mask { get; set; }
    }
}
