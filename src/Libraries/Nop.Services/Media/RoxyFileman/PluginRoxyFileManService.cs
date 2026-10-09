using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Nop.Core;
using Nop.Core.Domain.Media;
using Nop.Core.Infrastructure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.Text;
using Nop.Core.Caching;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;

namespace Nop.Services.Media.RoxyFileman
{
    public class PluginRoxyFileManService : BaseRoxyFilemanService
    {

        private static CloudBlobContainer _container;
        protected readonly IRoxyFilemanFileProvider _fileProvider1;
        private readonly IWorkContext _workContext;
        protected string _fileRootPath;
        private static string _azureBlobStorageConnectionString; // Storage Connection String
        private static string _azureBlobStorageContainerName; // Container Name for Uploaded Image folder
        private readonly IConfiguration _configuration;
        private readonly IStaticCacheManager _staticCacheManager;

        public static CacheKey FileExistsCacheKey => new CacheKey("Nop.azure.File.exists-{0}", FileExistsPrefixCacheKey);
        public static string FileExistsPrefixCacheKey => "Nop.azure.File.exists";
        #region Ctor

        public PluginRoxyFileManService(IRoxyFilemanFileProvider fileProvider,
            IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor,
            INopFileProvider fileProvider2,
            IWebHelper webHelper,
            MediaSettings mediaSettings,
            IConfiguration configuration,
            IWorkContext workContext) : base(webHostEnvironment, httpContextAccessor, fileProvider2, webHelper, workContext, mediaSettings)
        {
            _fileProvider1 = fileProvider;
            _workContext = workContext;
            _configuration = configuration;
        }
        public IEnumerable<object> GetDirectoryLists(string type)
        {

            var rootDirectoryPath = GetFullPath(GetVirtualPath(null));
            if (!_fileProvider.DirectoryExists(rootDirectoryPath))
                throw new Exception("Invalid files root directory. Check your configuration.");

            var allDirectories = GetDirectories(rootDirectoryPath);
            allDirectories.Insert(0, rootDirectoryPath);

            var localPath = GetFullPath(null);
            GetHttpContext().Response.WriteAsync("[");
            for (var i = 0; i < allDirectories.Count; i++)
            {
                var directoryPath = (string)allDirectories[i];
                GetHttpContext().Response.WriteAsync($"{{\"p\":\"/{directoryPath.Replace(localPath, string.Empty).Replace("\\", "/").TrimStart('/')}\",\"f\":\"{GetFile(directoryPath, type).Count}\",\"d\":\"{_fileProvider.GetDirectories(directoryPath).Length}\"}}");
                if (i < allDirectories.Count - 1)
                    GetHttpContext().Response.WriteAsync(",");
            }

            GetHttpContext().Response.WriteAsync("]");
            return new List<object>();
        }

        protected virtual List<string> GetFile(string directoryPath, string type)
        {
            if (!IsPathAllowed(directoryPath))
                return new List<string>();

            if (type == "#")
                type = string.Empty;

            var files = new List<string>();
            foreach (var fileName in _fileProvider.GetFiles(directoryPath))
            {
                if (string.IsNullOrEmpty(type) || GetFileType(_fileProvider.GetFileExtension(fileName)) == type)
                    files.Add(fileName);
            }

            return files;
        }
        protected virtual bool IsPathAllowed(string path)
        {
            var absp = _fileProvider.GetAbsolutePath(path);

            if (string.IsNullOrEmpty(_fileRootPath))
                Configure();

            return new DirectoryInfo(absp).FullName.StartsWith(_fileRootPath);
        }
        public IEnumerable<object> PluginGetFile(string directoryPath, string type)
        {
            // Custom function differfent than original

            // by pass directoryPath,type parameters
            // we dont need these parameters in Azure Blobs
            GetBlobFilesAsync();
            return new List<object>();

            // *********** ORIGINAL FUNCTION
            //directoryPath = GetVirtualPath(directoryPath);
            //var files = GetFiles(GetFullPath(directoryPath), type);

            //await GetHttpContext().Response.WriteAsync("[");
            //for (var i = 0; i < files.Count; i++)
            //{
            //    var width = 0;
            //    var height = 0;
            //    var physicalPath = files[i];

            //    if (GetFileType(_fileProvider1.GetFileExtension(files[i])) == "image")
            //    {
            //        using var stream = new FileStream(physicalPath, FileMode.Open);
            //        using var image = Image.FromStream(stream);
            //        width = image.Width;
            //        height = image.Height;
            //    }

            //    await GetHttpContext().Response.WriteAsync($"{{\"p\":\"{directoryPath.TrimEnd('/')}/{_fileProvider1.GetFileName(physicalPath)}\",\"t\":\"{Math.Ceiling(GetTimestamp(_fileProvider1.GetLastWriteTime(physicalPath)))}\",\"s\":\"{_fileProvider1.FileLength(physicalPath)}\",\"w\":\"{width}\",\"h\":\"{height}\"}}");

            //    if (i < files.Count - 1)
            //        await GetHttpContext().Response.WriteAsync(",");
            //}
            //await GetHttpContext().Response.WriteAsync("]");
        }
        public virtual async Task GetBlobFilesAsync()
        {
            // Custom function differfent than original

            await GetHttpContext().Response.WriteAsync("[");
            CreateCloudBlobContainerPrdImage();
            IEnumerable<IListBlobItem> listBlobs = this.ListBlobsAsync(_container).Result;

            int fileCount = listBlobs.Count();

            int i = 1;
            foreach (CloudBlockBlob cloudBlockBlob in listBlobs)
            {
                var cloudPath = cloudBlockBlob.Uri.ToString();

                //await GetHttpContext().Response.WriteAsync($"{{\"p\":\"{directoryPath.TrimEnd('/')}/{_fileProvider1.GetFileName(physicalPath)}\",\"t\":\"{Math.Ceiling(GetTimestamp(_fileProvider1.GetLastWriteTime(physicalPath)))}\",\"s\":\"{_fileProvider1.FileLength(physicalPath)}\",\"w\":\"{width}\",\"h\":\"{height}\"}}")

                // Convert from UTCDateTime to DateTime 
                var filedate = cloudBlockBlob.Properties.Created.Value.UtcDateTime;
                var filesize = cloudBlockBlob.Properties.Length;

                var completePathString = $"{{\"p\":\"{cloudPath}\",\"t\":\"{Math.Ceiling(GetTimestamp(filedate))} \",\"s\":\"{filesize} \"}}";

                await GetHttpContext().Response.WriteAsync(completePathString);

                if (i < fileCount)
                    await GetHttpContext().Response.WriteAsync(",");

                i = i + 1;
            }
            await GetHttpContext().Response.WriteAsync("]");
        }

        protected virtual async void CreateCloudBlobContainerPrdImage()
        {
            // Implemented
            _azureBlobStorageConnectionString = _configuration["Nop:AzureBlobStorageConnectionString"];
            _azureBlobStorageContainerName = "citagenix-uploaded-folder-image";
            var storageAccount = CloudStorageAccount.Parse(_azureBlobStorageConnectionString);
            if (storageAccount == null)
                throw new Exception("Azure connection string for BLOB is not working");

            //should we do it for each HTTP request?
            var blobClient = storageAccount.CreateCloudBlobClient();

            //GetContainerReference doesn't need to be async since it doesn't contact the server yet
            _container = blobClient.GetContainerReference(_azureBlobStorageContainerName);


        }
        protected virtual async Task<List<IListBlobItem>> ListBlobsAsync(CloudBlobContainer container)
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                bool useFlatBlobListing = true;
                BlobListingDetails blobListingDetails = BlobListingDetails.None;
                int maxBlobsPerRequest = 500000;
                var response = container.ListBlobsSegmentedAsync(null, useFlatBlobListing, blobListingDetails, maxBlobsPerRequest, continuationToken, null, null).Result;
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);
            return results;
        }
        protected virtual double GetTimestamp(DateTime date)
        {
            return (date.ToLocalTime() - new DateTime(1970, 1, 1, 0, 0, 0).ToLocalTime()).TotalSeconds;
        }
        public virtual void Configure()
        {
            CreateConfiguration();

            var existingText = _fileProvider.ReadAllText(GetConfigurationFilePath(), Encoding.UTF8);
            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(existingText);
            _fileRootPath = _fileProvider.GetAbsolutePath(config["FILES_ROOT"]);
        }
        public async Task UploadFileAsync(string directoryPath, IEnumerable<IFormFile> files)
        {
            // File Upload entry point

            var result = GetSuccessResponse();
            var hasErrors = false;

            directoryPath = GetFullPath(GetVirtualPath(directoryPath));

            if (!IsPathAllowed(directoryPath))
                throw new Exception(GetLanguageResource("E_UploadNotAll"));

            try
            {

                foreach (var formFile in GetHttpContext().Request.Form.Files)
                {
                    var fileName = formFile.FileName;
                    if (CanHandleFile(fileName))
                    {
                        var uniqueFileName = GetUniqueFileName(directoryPath, _fileProvider.GetFileName(fileName));
                        var destinationFile = _fileProvider.Combine(directoryPath, uniqueFileName);

                        //A warning (SCS0018 - Path Traversal) from the "Security Code Scan" analyzer may appear at this point. 
                        //In this case, it is not relevant. The input is not supplied by user.

                        byte[] imgBytes;
                        using (var stream = new FileStream(destinationFile, FileMode.OpenOrCreate))
                        {
                            // First Copy in Upload folder
                            formFile.CopyTo(stream);

                            // after copy image to uploaded folder then get Bytes array
                            // Custom Code
                            imgBytes = ReadToEnd(stream);
                        }

                        // after download to uploaded folder then movr to Blob
                        // Custom Code
                        SavePictureInFileAsync(imgBytes, fileName);

                        // delete from Uploaded folder
                        // Custom Code
                        File.Delete(destinationFile);

                        // if file is not image then dont Reize
                        if (GetFileType(new FileInfo(uniqueFileName).Extension) != "image")
                            continue;

                        int.TryParse(GetSetting("MAX_IMAGE_WIDTH"), out var w);
                        int.TryParse(GetSetting("MAX_IMAGE_HEIGHT"), out var h);
                        ImageResize(destinationFile, destinationFile, w, h);
                    }
                    else
                    {
                        hasErrors = true;
                        result = GetErrorResponse(GetLanguageResource("E_UploadNotAll"));
                    }
                }
            }
            catch (Exception ex)
            {
                result = GetErrorResponse(ex.Message);
            }

            if (IsAjaxRequest())
            {
                if (hasErrors)
                    result = GetErrorResponse(GetLanguageResource("E_UploadNotAll"));

                GetHttpContext().Response.WriteAsync(result);
            }
            else
                GetHttpContext().Response.WriteAsync($"<script>parent.fileUploaded({result});</script>");
        }
        public static byte[] ReadToEnd(Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }
        protected virtual async Task SavePictureInFileAsync(byte[] pictureBinary, string fileName)
        {
            try
            {
                // Implemented
                //GetBlockBlobReference doesn't need to be async since it doesn't contact the server yet
                var blockBlob = _container.GetBlockBlobReference(fileName);

                //set cache control
                if (!string.IsNullOrEmpty(_mediaSettings.AzureCacheControlHeader))
                    blockBlob.Properties.CacheControl = _mediaSettings.AzureCacheControlHeader;

                await blockBlob.UploadFromByteArrayAsync(pictureBinary, 0, pictureBinary.Length);
                if (_staticCacheManager != null)
                {
                    _staticCacheManager.RemoveByPrefix(FileExistsPrefixCacheKey);
                }
            }
            catch (Exception ex) { }
        }
        protected virtual void ImageResize(string sourcePath, string destinstionPath, int width, int height)
        {
            if (string.IsNullOrEmpty(destinstionPath))
                return;

            if (!_fileProvider.FileExists(sourcePath))
                return;

            using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
            using var image = Image.FromStream(stream);
            var ratio = image.Width / (float)image.Height;
            if (image.Width <= width && image.Height <= height)
                return;

            if (width == 0 && height == 0)
                return;

            var newWidth = width;
            int newHeight = Convert.ToInt16(Math.Floor(newWidth / ratio));
            if ((height > 0 && newHeight > height) || width == 0)
            {
                newHeight = height;
                newWidth = Convert.ToInt16(Math.Floor(newHeight * ratio));
            }

            using var newImage = new Bitmap(newWidth, newHeight);
            using var graphics = Graphics.FromImage(newImage);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            //close the stream to prevent access error if sourcePath and destinstionPath match
            stream.Close();
            newImage.Save(destinstionPath, GetImageFormat(destinstionPath));
        }
        protected virtual ImageFormat GetImageFormat(string path)
        {
            var fileExtension = _fileProvider.GetFileExtension(path).ToLower();
            return fileExtension switch
            {
                ".png" => ImageFormat.Png,
                ".gif" => ImageFormat.Gif,
                _ => ImageFormat.Jpeg,
            };
        }
        public void DeleteFiles(string path)
        {
            // Delete blob
            try
            {
                DeleteBlobFileAsync(path);
                GetHttpContext().Response.WriteAsync(GetSuccessResponse());
            }
            catch (Exception ex)
            {
                throw new Exception(GetLanguageResource("E_DeletеFile"));
            }


            // ********** ACTUAL CODE

            //path = GetFullPath(GetVirtualPath(path));
            //if (!_fileProvider.FileExists(path))
            //    throw new Exception(GetLanguageResource("E_DeleteFileInvalidPath"));

            //if (!IsPathAllowed(path))
            //    throw new Exception(GetLanguageResource("E_DeleteFileInvalidPath"));

            //try
            //{
            //    _fileProvider.DeleteFile(path);
            //    await GetHttpContext().Response.WriteAsync(GetSuccessResponse());
            //}
            //catch
            //{
            //    throw new Exception(GetLanguageResource("E_DeletеFile"));
            //}
        }
        public virtual async Task DeleteBlobFileAsync(string path)
        {
            try
            {
                BlobContinuationToken continuationToken = null;
                do
                {
                    //FileInfo fi = new FileInfo(path);
                    //var blockBlob = _container.GetBlockBlobReference(fi.Name);
                    //await blockBlob.DeleteAsync();

                    FileInfo fi = new FileInfo(path);
                    var resultSegment = await _container.ListBlobsSegmentedAsync(fi.Name, true, BlobListingDetails.All, null, continuationToken, null, null);
                    //delete one file in result segment
                    await Task.WhenAny(resultSegment.Results.Select(blobItem => ((CloudBlockBlob)blobItem).DeleteAsync()));
                    //get the continuation token.
                    continuationToken = resultSegment.ContinuationToken;
                }
                while (continuationToken != null);
                if (_staticCacheManager != null)
                {
                    _staticCacheManager.RemoveByPrefix(FileExistsPrefixCacheKey);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public byte[] CreateImageThumbnail(string path, string contentType)
        {

            // this is show thumbnail on mouse over on link
           // path = GetFullPath(GetVirtualPath(path));
            int.TryParse(GetHttpContext().Request.Query["width"].ToString().Replace("px", string.Empty), out var width);
            int.TryParse(GetHttpContext().Request.Query["height"].ToString().Replace("px", string.Empty), out var height);


            byte[] imageData = null;

            using (var wc = new System.Net.WebClient())
                imageData = wc.DownloadData(path);

            //return new MemoryStream(imageData);

            var stream = new MemoryStream(imageData);
            //using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var image = new Bitmap(Image.FromStream(stream));
            var cropX = 0;
            var cropY = 0;

            var imgRatio = image.Width / (double)image.Height;

            if (height == 0)
                height = Convert.ToInt32(Math.Floor(width / imgRatio));

            if (width > image.Width)
                width = image.Width;
            if (height > image.Height)
                height = image.Height;

            var cropRatio = width / (double)height;
            var cropWidth = Convert.ToInt32(Math.Floor(image.Height * cropRatio));
            var cropHeight = Convert.ToInt32(Math.Floor(cropWidth / cropRatio));

            if (cropWidth > image.Width)
            {
                cropWidth = image.Width;
                cropHeight = Convert.ToInt32(Math.Floor(cropWidth / cropRatio));
            }

            if (cropHeight > image.Height)
            {
                cropHeight = image.Height;
                cropWidth = Convert.ToInt32(Math.Floor(cropHeight * cropRatio));
            }

            if (cropWidth < image.Width)
                cropX = Convert.ToInt32(Math.Floor((double)(image.Width - cropWidth) / 2));
            if (cropHeight < image.Height)
                cropY = Convert.ToInt32(Math.Floor((double)(image.Height - cropHeight) / 2));

            using var cropImg = image.Clone(new Rectangle(cropX, cropY, cropWidth, cropHeight), PixelFormat.DontCare);
            GetHttpContext().Response.Headers.Add("Content-Type", MimeTypes.ImagePng);

            using var thumbnailImageStream = new MemoryStream();
            cropImg.GetThumbnailImage(width, height, () => false, IntPtr.Zero).Save(thumbnailImageStream, ImageFormat.Png);
            var thumbnailImageBinary = thumbnailImageStream.ToArray();
            GetHttpContext().Response.Body.WriteAsync(thumbnailImageBinary);
            GetHttpContext().Response.Body.Close();
            byte[] thumbnailBytes = new byte[0];
            return thumbnailBytes;
        }
    }
    #endregion
}
