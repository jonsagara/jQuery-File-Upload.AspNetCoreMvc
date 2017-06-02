using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace jQuery_File_Upload.AspNetCoreMvc.Models
{
    public class FilesHelper
    {
        private const string TEMP_PATH = "/somefiles/";
        private const string FILE_DIR_PATH = "/Files/somefiles/";
        private const string URL_BASE = "/Files/somefiles/";
        private const string DELETE_URL = "/FileUpload/DeleteFile/?file=";
        private const string DELETE_TYPE = "GET";

        private readonly IHostingEnvironment _env;
        private readonly string _storageRootPath;
        private readonly string _storageTempPath;

        public FilesHelper(IHostingEnvironment env)
        {
            _env = env;
            _storageRootPath = Path.Combine(env.ContentRootPath, FILE_DIR_PATH);
            _storageTempPath = Path.Combine(env.ContentRootPath, TEMP_PATH);
        }

        // This method is never called
        //public void DeleteFiles(string pathToDelete)
        //{

        //    string path = HostingEnvironment.MapPath(pathToDelete);

        //    System.Diagnostics.Debug.WriteLine(path);
        //    if (Directory.Exists(path))
        //    {
        //        DirectoryInfo di = new DirectoryInfo(path);
        //        foreach (FileInfo fi in di.GetFiles())
        //        {
        //            File.Delete(fi.FullName);
        //            System.Diagnostics.Debug.WriteLine(fi.Name);
        //        }

        //        di.Delete(true);
        //    }
        //}

        public string DeleteFile(string file)
        {
            System.Diagnostics.Debug.WriteLine("DeleteFile");
            //    var req = HttpContext.Current;
            System.Diagnostics.Debug.WriteLine(file);

            string fullPath = Path.Combine(_storageRootPath, file);
            System.Diagnostics.Debug.WriteLine(fullPath);
            System.Diagnostics.Debug.WriteLine(File.Exists(fullPath));
            string thumbPath = "/" + file + "80x80.jpg";
            string partThumb1 = Path.Combine(_storageRootPath, "thumbs");
            string partThumb2 = Path.Combine(partThumb1, file + "80x80.jpg");

            System.Diagnostics.Debug.WriteLine(partThumb2);
            System.Diagnostics.Debug.WriteLine(File.Exists(partThumb2));
            if (File.Exists(fullPath))
            {
                //delete thumb 
                if (File.Exists(partThumb2))
                {
                    File.Delete(partThumb2);
                }
                File.Delete(fullPath);
                string succesMessage = "Ok";
                return succesMessage;
            }
            string failMessage = "Error Delete";
            return failMessage;
        }
        public JsonFiles GetFileList()
        {

            var r = new List<ViewDataUploadFilesResult>();

            string fullPath = Path.Combine(_storageRootPath);
            if (Directory.Exists(fullPath))
            {
                DirectoryInfo dir = new DirectoryInfo(fullPath);
                foreach (FileInfo file in dir.GetFiles())
                {
                    int SizeInt = unchecked((int)file.Length);
                    r.Add(UploadResult(file.Name, SizeInt, file.FullName));
                }

            }
            JsonFiles files = new JsonFiles(r);

            return files;
        }

        public void UploadAndShowResults(HttpContextBase ContentBase, List<ViewDataUploadFilesResult> resultList)
        {
            var httpRequest = ContentBase.Request;
            System.Diagnostics.Debug.WriteLine(Directory.Exists(_storageTempPath));

            string fullPath = Path.Combine(_storageRootPath);
            Directory.CreateDirectory(fullPath);
            // Create new folder for thumbs
            Directory.CreateDirectory(fullPath + "/thumbs/");

            foreach (string inputTagName in httpRequest.Files)
            {

                var headers = httpRequest.Headers;

                var file = httpRequest.Files[inputTagName];
                System.Diagnostics.Debug.WriteLine(file.FileName);

                if (string.IsNullOrEmpty(headers["X-File-Name"]))
                {

                    UploadWholeFile(ContentBase, resultList);
                }
                else
                {

                    UploadPartialFile(headers["X-File-Name"], ContentBase, resultList);
                }
            }
        }


        private void UploadWholeFile(HttpContextBase requestContext, List<ViewDataUploadFilesResult> statuses)
        {

            var request = requestContext.Request;
            for (int i = 0; i < request.Files.Count; i++)
            {
                var file = request.Files[i];
                string pathOnServer = Path.Combine(_storageRootPath);
                var fullPath = Path.Combine(pathOnServer, Path.GetFileName(file.FileName));
                file.SaveAs(fullPath);

                //Create thumb
                string[] imageArray = file.FileName.Split('.');
                if (imageArray.Length != 0)
                {
                    string extansion = imageArray[imageArray.Length - 1].ToLower();
                    if (extansion != "jpg" && extansion != "png" && extansion != "jpeg") //Do not create thumb if file is not an image
                    {

                    }
                    else
                    {
                        var ThumbfullPath = Path.Combine(pathOnServer, "thumbs");
                        //string fileThumb = file.FileName + ".80x80.jpg";
                        string fileThumb = Path.GetFileNameWithoutExtension(file.FileName) + "80x80.jpg";
                        var ThumbfullPath2 = Path.Combine(ThumbfullPath, fileThumb);
                        using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(fullPath)))
                        {
                            var thumbnail = new WebImage(stream).Resize(80, 80);
                            thumbnail.Save(ThumbfullPath2, "jpg");
                        }

                    }
                }
                statuses.Add(UploadResult(file.FileName, file.ContentLength, file.FileName));
            }
        }



        private void UploadPartialFile(string fileName, HttpContextBase requestContext, List<ViewDataUploadFilesResult> statuses)
        {
            var request = requestContext.Request;
            if (request.Files.Count != 1) throw new HttpRequestValidationException("Attempt to upload chunked file containing more than one fragment per request");
            var file = request.Files[0];
            var inputStream = file.InputStream;
            string patchOnServer = Path.Combine(_storageRootPath);
            var fullName = Path.Combine(patchOnServer, Path.GetFileName(file.FileName));
            var ThumbfullPath = Path.Combine(fullName, Path.GetFileName(file.FileName + "80x80.jpg"));
            ImageHandler handler = new ImageHandler();

            var ImageBit = ImageHandler.LoadImage(fullName);
            handler.Save(ImageBit, 80, 80, 10, ThumbfullPath);
            using (var fs = new FileStream(fullName, FileMode.Append, FileAccess.Write))
            {
                var buffer = new byte[1024];

                var l = inputStream.Read(buffer, 0, 1024);
                while (l > 0)
                {
                    fs.Write(buffer, 0, l);
                    l = inputStream.Read(buffer, 0, 1024);
                }
                fs.Flush();
                fs.Close();
            }
            statuses.Add(UploadResult(file.FileName, file.ContentLength, file.FileName));
        }
        public ViewDataUploadFilesResult UploadResult(string FileName, int fileSize, string FileFullPath)
        {
            string getType = System.Web.MimeMapping.GetMimeMapping(FileFullPath);
            var result = new ViewDataUploadFilesResult()
            {
                name = FileName,
                size = fileSize,
                type = getType,
                url = URL_BASE + FileName,
                deleteUrl = DELETE_URL + FileName,
                thumbnailUrl = CheckThumb(getType, FileName),
                deleteType = DELETE_TYPE,
            };
            return result;
        }

        public string CheckThumb(string type, string FileName)
        {
            var splited = type.Split('/');
            if (splited.Length == 2)
            {
                string extansion = splited[1].ToLower();
                if (extansion.Equals("jpeg") || extansion.Equals("jpg") || extansion.Equals("png") || extansion.Equals("gif"))
                {
                    string thumbnailUrl = URL_BASE + "thumbs/" + Path.GetFileNameWithoutExtension(FileName) + "80x80.jpg";
                    return thumbnailUrl;
                }
                else
                {
                    if (extansion.Equals("octet-stream")) //Fix for exe files
                    {
                        return "/Content/Free-file-icons/48px/exe.png";

                    }
                    if (extansion.Contains("zip")) //Fix for exe files
                    {
                        return "/Content/Free-file-icons/48px/zip.png";
                    }
                    string thumbnailUrl = "/Content/Free-file-icons/48px/" + extansion + ".png";
                    return thumbnailUrl;
                }
            }
            else
            {
                return URL_BASE + "/thumbs/" + Path.GetFileNameWithoutExtension(FileName) + "80x80.jpg";
            }

        }

        // This method is never called
        //public List<string> FilesList()
        //{

        //    List<string> Filess = new List<string>();
        //    string path = HostingEnvironment.MapPath(serverMapPath);
        //    System.Diagnostics.Debug.WriteLine(path);
        //    if (Directory.Exists(path))
        //    {
        //        DirectoryInfo di = new DirectoryInfo(path);
        //        foreach (FileInfo fi in di.GetFiles())
        //        {
        //            Filess.Add(fi.Name);
        //            System.Diagnostics.Debug.WriteLine(fi.Name);
        //        }

        //    }
        //    return Filess;
        //}
    }
}
