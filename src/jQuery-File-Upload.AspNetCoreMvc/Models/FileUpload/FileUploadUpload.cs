using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageSharp;
using jQuery_File_Upload.AspNetCoreMvc.Utilities;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace jQuery_File_Upload.AspNetCoreMvc.Models.FileUpload
{
    public class FileUploadUpload
    {
        public class Command : IRequest<CommandResult>
        {
            public HttpContext HttpContext { get; set; }
            public List<IFormFile> Files { get; private set; } = new List<IFormFile>();
        }

        public class CommandResult
        {
            public List<ViewDataUploadFilesResult> FileResults { get; private set; } = new List<ViewDataUploadFilesResult>();
        }


        public class CommandHandler : IAsyncRequestHandler<Command, CommandResult>
        {
            private readonly FilesHelper _filesHelper;

            public CommandHandler(FilesHelper filesHelper)
            {
                _filesHelper = filesHelper;
            }

            public async Task<CommandResult> Handle(Command message)
            {
                var result = new CommandResult();

                var partialFileName = message.HttpContext.Request.Headers["X-File-Name"];
                if (string.IsNullOrWhiteSpace(partialFileName))
                {
                    await UploadWholeFileAsync(message, result);
                }
                else
                {
                    UploadPartialFile(message, partialFileName);
                }

                return result;
            }

            private static readonly HashSet<string> _allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".gif",
                ".jpeg",
                ".jpg",
                ".png"
            };

            private async Task UploadWholeFileAsync(Command message, CommandResult result)
            {
                const int THUMB_WIDTH = 80;
                const int THUMB_HEIGHT = 80;
                const string THUMBS_FOLDER_NAME = "thumbs";

                // Ensure the storage root exists.
                Directory.CreateDirectory(_filesHelper.StorageRootPath);

                foreach (var file in message.Files)
                {
                    var extension = Path.GetExtension(file.FileName);
                    if (!_allowedExtensions.Contains(extension))
                    {
                        // This is not a supported image type.
                        throw new InvalidOperationException($"Unsupported image type: {extension}. The supported types are: {string.Join(", ", _allowedExtensions)}");
                    }

                    if (file.Length > 0L)
                    {
                        var fullPath = Path.Combine(_filesHelper.StorageRootPath, Path.GetFileName(file.FileName));
                        using (var fs = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(fs);
                        }


                        //
                        // Create an 80x80 thumbnail.
                        //

                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                        var thumbName = $"{fileNameWithoutExtension}{THUMB_WIDTH}x{THUMB_HEIGHT}{extension}";
                        var thumbPath = Path.Combine(_filesHelper.StorageRootPath, THUMBS_FOLDER_NAME, thumbName);

                        using (var originalImage = Image.Load(fullPath))
                        using (var thumbStream = File.OpenWrite(thumbPath))
                        {
                            originalImage
                                .Resize(THUMB_WIDTH, THUMB_HEIGHT)
                                .Save(thumbStream);
                        }
                    }


                    

                    ////Create thumb
                    //string[] imageArray = file.FileName.Split('.');
                    //if (imageArray.Length != 0)
                    //{
                    //    string extansion = imageArray[imageArray.Length - 1].ToLower();
                    //    if (extansion != "jpg" && extansion != "png" && extansion != "jpeg") //Do not create thumb if file is not an image
                    //    {

                    //    }
                    //    else
                    //    {
                    //        var ThumbfullPath = Path.Combine(pathOnServer, "thumbs");
                    //        //string fileThumb = file.FileName + ".80x80.jpg";
                    //        string fileThumb = Path.GetFileNameWithoutExtension(file.FileName) + "80x80.jpg";
                    //        var ThumbfullPath2 = Path.Combine(ThumbfullPath, fileThumb);
                    //        using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(fullPath)))
                    //        {
                    //            var thumbnail = new WebImage(stream).Resize(80, 80);
                    //            thumbnail.Save(ThumbfullPath2, "jpg");
                    //        }

                    //    }
                    //}

                    result.FileResults.Add(UploadResult(file.FileName, file.Length));
                }
            }

            private void UploadPartialFile(Command message, string partialFileName)
            {
                throw new NotImplementedException();
            }

            private ViewDataUploadFilesResult UploadResult(string fileName, long fileSizeInBytes)
            {
                string getType = MimeMapping.GetMimeMapping(fileName);

                var result = new ViewDataUploadFilesResult()
                {
                    name = fileName,
                    size = fileSizeInBytes,
                    type = getType,
                    url = _filesHelper.UrlBase + fileName,
                    deleteUrl = _filesHelper.DeleteUrl + fileName,
                    thumbnailUrl = _filesHelper.CheckThumb(getType, fileName),
                    deleteType = _filesHelper.DeleteType,
                };
                return result;
            }
        }        
    }
}
