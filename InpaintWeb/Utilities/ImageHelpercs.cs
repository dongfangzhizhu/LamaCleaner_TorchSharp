using System.Text;

namespace InpaintWeb.Utilities
{
    public static class ImageHelpercs
    {
        public static byte[] Base64ToBytes(string base64String)
        {
            if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                int commaIndex = base64String.IndexOf(',');
                if (commaIndex != -1)
                {
                    base64String = base64String.Substring(commaIndex + 1);
                }
            }
            return Convert.FromBase64String(base64String);
        }
        public static string BytesToBase64(byte[] imgBytes)
        {
            string mimeType = GetMimeType(imgBytes);
            string base64String = Convert.ToBase64String(imgBytes);
            return $"data:{mimeType};base64,{base64String}";
        }
        static string GetMimeType(byte[] bytes)
        {
            if (bytes.Length < 4) return "application/octet-stream";

            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return "image/png";
            if (bytes[0] == 0xFF && bytes[1] == 0xD8)
                return "image/jpeg";
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return "image/gif";

            return "application/octet-stream";
        }
    }
}
