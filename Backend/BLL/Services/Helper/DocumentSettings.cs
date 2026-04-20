using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BLL.Services.Helper
{
    public static class DocumentSettings
    {
        public static string UploadFile(IFormFile file,string rootPath,string foldername)
        {
            var path = Path.Combine(rootPath,"files", foldername);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var filePath = Path.Combine(path, fileName);
            using(var filestream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(filestream);
            }
            return fileName;
        }

        public static void DeleteFile(string fileName, string rootPath, string foldername)
        {
                var filePath = Path.Combine(rootPath,"files", foldername, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
        }
    }
}
