using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using jQuery_File_Upload.AspNetCoreMvc.Utilities;
using MediatR;
using Microsoft.AspNetCore.Http;
using SkiaSharp;

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

            private async Task UploadWholeFileAsync(Command message, CommandResult result)
            {
                const string THUMBS = "thumbs";

                // Ensure the storage root and thumbnail directories exist.
                Directory.CreateDirectory(_filesHelper.StorageRootPath);
                Directory.CreateDirectory(Path.Combine(_filesHelper.StorageRootPath, THUMBS));

                foreach (var file in message.Files)
                {
                    if (file.Length > 0L)
                    {
                        var fullPath = Path.Combine(_filesHelper.StorageRootPath, Path.GetFileName(file.FileName));
                        using (var fs = new FileStream(fullPath, FileMode.Create))
                        {
                            await file.CopyToAsync(fs);
                        }

                        // Create thumbnail: 80x80 for sure, and if the image is > 540x360, create one that's limited to
                        //   either 540 wide or 360 tall, depending on which dimension is larger.
                        var extension = Path.GetExtension(fullPath).ToLower();
                        if (extension != ".jpg" && extension != ".jpeg" && extension != "png" && extension != ".gif")
                        {
                            // Not a supported image type; don't resize.
                            continue;
                        }


                        const int THUMB_WIDTH = 80;
                        const int THUMB_HEIGHT = 80;
                        const int REGULAR_WIDTH = 540;
                        //const int REGULAR_HEIGHT = 360;

                        // Create the 80x80 thumbnail first.
                        using (var input = File.OpenRead(fullPath))
                        using (var inputStream = new SKManagedStream(input))
                        using (var original = SKBitmap.Decode(inputStream))
                        {
                            using (var resized = original.Resize(new SKImageInfo(THUMB_WIDTH, THUMB_HEIGHT), SKBitmapResizeMethod.Lanczos3))
                            {
                                if (resized == null)
                                {
                                    throw new Exception($"Unable to create {THUMB_WIDTH}x{THUMB_HEIGHT} thumbnail image for {fullPath}");
                                }

                                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
                                var outputPath = Path.Combine(_filesHelper.StorageRootPath, THUMBS, $"{fileNameWithoutExt}{THUMB_WIDTH}x{THUMB_HEIGHT}{extension}");

                                SKEncodedImageFormat imageFormat = SKEncodedImageFormat.Jpeg;
                                switch(extension)
                                {
                                    case ".jpeg":
                                    case ".jpg":
                                        imageFormat = SKEncodedImageFormat.Jpeg;
                                        break;

                                    case ".png":
                                        imageFormat = SKEncodedImageFormat.Png;
                                        break;

                                    case ".gif":
                                        imageFormat = SKEncodedImageFormat.Gif;
                                        break;
                                }

                                using (var image = SKImage.FromBitmap(resized))
                                using (var output = File.OpenWrite(outputPath))
                                {
                                    image
                                        .Encode(imageFormat, quality: 75)
                                        .SaveTo(output);
                                }
                            }
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
                    //thumbnailUrl = CheckThumb(getType, FileName),
                    deleteType = _filesHelper.DeleteType,
                };
                return result;
            }
        }        
    }
}
