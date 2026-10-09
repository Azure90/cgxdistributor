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
    public class RoxyFilemanService : BaseRoxyFilemanService, IRoxyFilemanService
    {
        #region Fields

        private static CloudBlobContainer _container;
        protected readonly IRoxyFilemanFileProvider _fileProvider1;
        private readonly IWorkContext _workContext;
        protected string _fileRootPath;
        private static string _azureBlobStorageConnectionString; // Storage Connection String
        private static string _azureBlobStorageContainerName; // Container Name for Uploaded Image folder
        private readonly IConfiguration _configuration;
        private readonly IStaticCacheManager _staticCacheManager;
        #endregion
        public static CacheKey FileExistsCacheKey => new CacheKey("Nop.azure.File.exists-{0}", FileExistsPrefixCacheKey);
        public static string FileExistsPrefixCacheKey => "Nop.azure.File.exists";
        #region Ctor

        public RoxyFilemanService(IRoxyFilemanFileProvider fileProvider,
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
        //public RoxyFilemanService(IWebHostEnvironment webHostEnvironment,
        //   IHttpContextAccessor httpContextAccessor,
        //   INopFileProvider fileProvider,
        //   IWebHelper webHelper,
        //   IWorkContext workContext,
        //   MediaSettings mediaSettings) : base(webHostEnvironment, httpContextAccessor, fileProvider, webHelper, workContext, mediaSettings)
        //{
        //    _fileRootPath = null;
       // }

        #endregion

        #region Utils

        /// <summary>
        /// Check whether there are any restrictions on handling the file
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns>
        /// The result contains the rue if the file can be handled; otherwise false
        /// </returns>
        protected virtual bool CanHandleFile(string path)
        {
            var result = false;

            var fileExtension = Path.GetExtension(path).Replace(".", string.Empty).ToLowerInvariant();

            var roxyConfig = Singleton<RoxyFilemanConfig>.Instance;

            var forbiddenUploads = roxyConfig.FORBIDDEN_UPLOADS.Trim().ToLowerInvariant();

            if (!string.IsNullOrEmpty(forbiddenUploads))
                result = !Regex.Split(forbiddenUploads, "\\s+").Contains(fileExtension);

            var allowedUploads = roxyConfig.ALLOWED_UPLOADS.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(allowedUploads))
                return result;

            return Regex.Split(allowedUploads, "\\s+").Contains(fileExtension);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Initial service configuration
        /// </summary>
        /// <param name="pathBase">The base path for the current request</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task ConfigureAsync(string pathBase)
        {
            var currentLanguage = await _workContext.GetWorkingLanguageAsync();
            await _fileProvider1.GetOrCreateConfigurationAsync(pathBase, currentLanguage.UniqueSeoCode);
        }

        #endregion

        #region Directories

        /// <summary>
        /// Delete the directory
        /// </summary>
        /// <param name="path">Path to the directory</param>
        public void DeleteDirectory(string path)
        {
            _fileProvider1.DeleteDirectory(path);
        }

        /// <summary>
        /// Download the directory from the server as a zip archive
        /// </summary>
        /// <param name="path">Path to the directory</param>
        public byte[] DownloadDirectory(string path)
        {
            return _fileProvider1.CreateZipArchiveFromDirectory(path);
        }

        /// <summary>
        /// Copy the directory
        /// </summary>
        /// <param name="sourcePath">Path to the source directory</param>
        /// <param name="destinationPath">Path to the destination directory</param>
        public void CopyDirectory(string sourcePath, string destinationPath)
        {
            _fileProvider1.CopyDirectory(sourcePath, destinationPath);
        }

        /// <summary>
        /// Create the new directory
        /// </summary>
        /// <param name="parentDirectoryPath">Path to the parent directory</param>
        /// <param name="name">Name of the new directory</param>
        public virtual void CreateDirectory(string parentDirectoryPath, string name)
        {
            _fileProvider1.CreateDirectory(parentDirectoryPath, name);
        }

        /// <summary>
        /// Get all available directories as a directory tree
        /// </summary>
        /// <param name="type">Type of the file</param>
        /// <returns>List of directories</returns>
        public IEnumerable<object> GetDirectoryList(string type)
        {
            var contents = _fileProvider1.GetDirectories(type);

            var result = new List<object>() { new
                {
                    p = "/",
                    f = _fileProvider1.GetFiles("/", type).Count(),
                    d = 0
                } };

            foreach (var (path, countFiles, countDirectories) in contents)
            {
                result.Add(new
                {
                    p = path.Replace("\\", "/"),
                    f = countFiles,
                    d = countDirectories
                });
            }

            return result;
        }

        /// <summary>
        /// Move the directory
        /// </summary>
        /// <param name="sourcePath">Path to the source directory</param>
        /// <param name="destinationPath">Path to the destination directory</param>
        public void MoveDirectory(string sourcePath, string destinationPath)
        {
            if (destinationPath.IndexOf(sourcePath, StringComparison.InvariantCulture) == 0)
                throw new RoxyFilemanException("E_CannotMoveDirToChild");

            _fileProvider1.DirectoryMove(sourcePath, destinationPath);
        }

        /// <summary>
        /// Get files in the passed directory
        /// </summary>
        /// <param name="directoryPath">Path to the files directory</param>
        /// <param name="type">Type of the files</param>
        public IEnumerable<object> GetFiles(string directoryPath, string type)
        {
            return _fileProvider1.GetFiles(directoryPath, type?.Trim('#'))
                .Select(f => new
                {
                    p = f.RelativePath.Replace("\\", "/"),
                    t = f.LastWriteTime.ToUnixTimeSeconds(),
                    s = f.FileLength,
                    w = f.Width,
                    h = f.Height
                });
        }

        /// <summary>
        /// Rename the directory
        /// </summary>
        /// <param name="sourcePath">Path to the source directory</param>
        /// <param name="newName">New name of the directory</param>
        public void RenameDirectory(string sourcePath, string newName)
        {
            _fileProvider1.RenameDirectory(sourcePath, newName);
        }

        /// <summary>
        /// Upload files to a directory on passed path
        /// </summary>
        /// <param name="directoryPath">Path to directory to upload files</param>
        /// <param name="files">Files sent with the HttpRequest</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task UploadFilesAsync(string directoryPath, IEnumerable<IFormFile> files)
        {
            foreach (var formFile in files)
            {
                await _fileProvider1.SaveFileAsync(directoryPath, formFile.FileName, formFile.ContentType, formFile.OpenReadStream());
            }
        }

        #endregion

        #region Files

        /// <summary>
        /// Copy the file
        /// </summary>
        /// <param name="sourcePath">Path to the source file</param>
        /// <param name="destinationPath">Path to the destination file</param>
        public void CopyFile(string sourcePath, string destinationPath)
        {
            _fileProvider1.CopyFile(sourcePath, destinationPath);
        }

        /// <summary>
        /// Get binary image thumbnail data
        /// </summary>
        /// <param name="path">Path to the image</param>
        /// <param name="contentType">The resulting MIME type</param>
        public virtual byte[] CreateImageThumbnail(string path, string contentType)
        {
            return _fileProvider1.CreateImageThumbnail(path, contentType);
        }

        /// <summary>
        /// Delete the directory
        /// </summary>
        /// <param name="path">Path to the directory</param>
        public void DeleteFile(string path)
        {
            _fileProvider1.DeleteFile(path);
        }

        /// <summary>
        /// Get filename and read-only content stream
        /// </summary>
        /// <param name="path">Path to the file</param>
        public (Stream stream, string name) GetFileStream(string path)
        {
            var file = _fileProvider1.GetFileInfo(path);

            if (!file.Exists)
                throw new FileNotFoundException();

            return (file.CreateReadStream(), file.Name);
        }

        /// <summary>
        /// Move the file
        /// </summary>
        /// <param name="sourcePath">Path to the source file</param>
        /// <param name="destinationPath">Path to the destination file</param>
        public void MoveFile(string sourcePath, string destinationPath)
        {
            if (!CanHandleFile(sourcePath) && !CanHandleFile(destinationPath))
                throw new RoxyFilemanException("E_FileExtensionForbidden");

            _fileProvider1.FileMove(sourcePath, destinationPath);
        }

        /// <summary>
        /// Rename the file
        /// </summary>
        /// <param name="sourcePath">Path to the source file</param>
        /// <param name="newName">New name of the file</param>
        public void RenameFile(string sourcePath, string newName)
        {
            if (!CanHandleFile(sourcePath) && !CanHandleFile(newName))
                throw new RoxyFilemanException("E_FileExtensionForbidden");

            _fileProvider1.RenameFile(sourcePath, newName);
        }


       
        #endregion
    }
}