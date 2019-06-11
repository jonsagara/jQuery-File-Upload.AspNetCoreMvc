using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using jQuery_File_Upload.AspNetCoreMvc.Utilities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;

namespace jQuery_File_Upload.AspNetCoreMvc.Models.FileUpload
{
    public class FileUploadUpload
    {
        public class Command : IRequest<CommandResult>
        {
            [BindNever]
            public HttpContext HttpContext { get; set; }

            public List<IFormFile> Files { get; private set; } = new List<IFormFile>();
        }

        public class CommandResult
        {
            public List<ViewDataUploadFilesResult> FileResults { get; private set; } = new List<ViewDataUploadFilesResult>();
        }


        public class CommandHandler : IRequestHandler<Command, CommandResult>
        {
            private readonly FilesHelper _filesHelper;

            public CommandHandler(FilesHelper filesHelper)
            {
                _filesHelper = filesHelper;
            }

            public async Task<CommandResult> Handle(Command message, CancellationToken cancellationToken)
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
                const int NORMAL_IMAGE_MAX_WIDTH = 540;
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

                        // Create the thumnail directory if it doesn't exist.
                        Directory.CreateDirectory(Path.GetDirectoryName(thumbPath));

                        using (var thumb = Image.Load(ResizeImage(fullPath, 80, 80)))
                        {
                            thumb.Save(thumbPath);
                        }

                        // If the image is wider than 540px, resize it so that it is 540px wide. Otherwise, upload a copy of the original.
                        using (var originalImage = Image.Load(fullPath))
                        {
                            if (originalImage.Width > NORMAL_IMAGE_MAX_WIDTH)
                            {
                                // Resize it so that the max width is 540px. Maintain the aspect ratio.
                                var newHeight = originalImage.Height * NORMAL_IMAGE_MAX_WIDTH / originalImage.Width;

                                var normalImageName = $"{fileNameWithoutExtension}{NORMAL_IMAGE_MAX_WIDTH}x{newHeight}{extension}";
                                var normalImagePath = Path.Combine(_filesHelper.StorageRootPath, normalImageName);

                                using (var normalImage = Image.Load(ResizeImage(fullPath, NORMAL_IMAGE_MAX_WIDTH, newHeight)))
                                {
                                    normalImage.Save(normalImagePath);
                                }
                            }
                        }
                    }

                    result.FileResults.Add(UploadResult(file.FileName, file.Length));
                }
            }

            private byte[] ResizeImage(string localTempFilePath, int width, int height)
            {
                try
                {
                    using (var originalImage = Image.Load(localTempFilePath))
                    using (var thumbnailImage = originalImage.Clone())
                    using (var thumbnailStream = new MemoryStream())
                    {
                        var extension = Path.GetExtension(localTempFilePath);
                        IImageFormat format = originalImage.GetConfiguration().ImageFormatsManager.FindFormatByFileExtension(extension);
                        IImageEncoder encoder = originalImage.GetConfiguration().ImageFormatsManager.FindEncoder(format);

                        if (IsJpeg(extension))
                        {
                            // It's a JPEG, so ensure we're maintaining quality.
                            encoder = new JpegEncoder { Quality = 90 };
                        }

                        // Resize the image.
                        thumbnailImage.Mutate(op =>
                        {
                            op.Resize(width, height);
                        });

                        // Save it to the stream.
                        thumbnailImage.Save(thumbnailStream, encoder);

                        // Return the bytes to save to disk.
                        return thumbnailStream.ToArray();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Unhandled error trying to resize local image '{localTempFilePath}' to Width={width}px, Height={height}px", ex);
                }
            }

            /// <summary>
            /// The file's extension, including the &quot;.&quot;.
            /// </summary>
            private bool IsJpeg(string extension)
            {
                return ".jpeg".Equals(extension, StringComparison.OrdinalIgnoreCase)
                    || ".jpg".Equals(extension, StringComparison.OrdinalIgnoreCase);
            }

            private void UploadPartialFile(Command message, string partialFileName)
            {
                throw new NotImplementedException();
            }

            private ViewDataUploadFilesResult UploadResult(string fileName, long fileSizeInBytes)
            {
                var getType = MimeMapping.GetMimeMapping(fileName);

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
