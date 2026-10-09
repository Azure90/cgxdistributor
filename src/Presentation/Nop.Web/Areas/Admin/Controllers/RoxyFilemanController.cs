using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Services.Media.RoxyFileman;
using Nop.Services.Security;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Models.Media;
public partial class RoxyFilemanController : BaseAdminController
{
    #region Fields

    private readonly IPermissionService _permissionService;
    private readonly IRoxyFilemanService _roxyFilemanService;
    private readonly IRoxyFilemanService _roxyFilemanService1;
    private readonly IWebHelper _webHelper;
    private readonly PluginRoxyFileManService _pluginRoxyFileManService;

    #endregion

    #region Ctor

    public RoxyFilemanController(IPermissionService permissionService, IRoxyFilemanService roxyFilemanService, IRoxyFilemanService roxyFilemanService1, IWebHelper webHelper, IServiceProvider serviceProvider, PluginRoxyFileManService pluginRoxyFileManService)
    {
        _permissionService = permissionService;
        _roxyFilemanService = roxyFilemanService;
        _webHelper = webHelper;
        _pluginRoxyFileManService = pluginRoxyFileManService;
        //_roxyFilemanService1 = serviceProvider.GetRequiredService<IRoxyFilemanService>("CustomFileRoxyFilemanService");
        //_roxyFilemanService1 = serviceProvider.GetRequiredService<IEnumerable<IRoxyFilemanService>>()
        //                     .FirstOrDefault(service => service is CustomFileRoxyFilemanService);
    }
    #endregion

    #region Utils

    protected virtual JsonResult JsonOk()
        {
            return Json(new { res = "ok" });
        }

        protected virtual JsonResult JsonError(string message)
        {
            return Json(new { res = "error", msg = message });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create configuration file for RoxyFileman
        /// </summary>
        public virtual async Task CreateConfiguration()
        {
            var currentPathBase = Request.PathBase.ToString();
            await _roxyFilemanService.ConfigureAsync(currentPathBase);
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> DirectoriesList(string type)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

            //var directories = _roxyFilemanService.GetDirectoryLists(type);
            var directories = _pluginRoxyFileManService.GetDirectoryLists(type);
            return Json(directories);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> FilesList(string d, string type)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

            //var directories = _roxyFilemanService.GetFile(d, type);
            var directories = _pluginRoxyFileManService.PluginGetFile(d, type);


            return Json(directories);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        public virtual async Task<IActionResult> DownloadFile(string f)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                var (stream, name) = _roxyFilemanService.GetFileStream(f);
            

            if (!new FileExtensionContentTypeProvider().TryGetContentType(f, out var contentType))
                    contentType = MimeTypes.ApplicationOctetStream;

                return File(stream, contentType, name);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> CopyDirectory(string d, string n)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

            _roxyFilemanService.CopyDirectory(d, n);
            


            return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> CopyFile(string f, string n)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

            _roxyFilemanService.CopyFile(f, n);
            

            return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> CreateDirectory(string d, string n)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

            _roxyFilemanService.CreateDirectory(d, n);
            

            return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> DeleteDirectory(string d)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

            _roxyFilemanService.DeleteDirectory(d);
            

            return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> DeleteFile(string f)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                //_roxyFilemanService.DeleteFiles(f);
            _pluginRoxyFileManService.DeleteFiles(f);

            return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        public virtual async Task<IActionResult> DownloadDirectory(string d)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                throw new Exception("You don't have required permission");

            var fileContents = _roxyFilemanService.DownloadDirectory(d);

            return File(fileContents, MimeTypes.ApplicationZip);
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> MoveDirectory(string d, string n)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                _roxyFilemanService.MoveDirectory(d, n);

                return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> MoveFile(string f, string n)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                _roxyFilemanService.MoveFile(f, n);

                return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> RenameDirectory(string d, string n)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                _roxyFilemanService.RenameDirectory(d, n);

                return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual async Task<IActionResult> RenameFile(string f, string n)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                _roxyFilemanService.RenameFile(f, n);

                return JsonOk();
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        public virtual async Task<IActionResult> CreateImageThumbnail(string f)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(f, out var contentType))
                    contentType = MimeTypes.ImageJpeg;

          // var fileContents = _roxyFilemanService.CreateImageThumbnail(f, contentType);
             var fileContents = _pluginRoxyFileManService.CreateImageThumbnail(f, contentType);

            return File(fileContents, contentType);
            }
            catch (Exception ex)
            {
                return JsonError(ex.Message);
            }
        }

        [IgnoreAntiforgeryToken]
        [HttpPost]
        public virtual async Task<IActionResult> UploadFiles([FromForm]RoxyFilemanUploadModel uploadModel)
        {
            try
            {
                if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.HtmlEditorManagePictures))
                    throw new Exception("You don't have required permission");

                if (HttpContext.Request.Form.Files.Count == 0)
                    throw new RoxyFilemanException("E_UploadNoFiles");

            //await _roxyFilemanService.UploadFileAsync(uploadModel.D, HttpContext.Request.Form.Files);
            await _pluginRoxyFileManService.UploadFileAsync(uploadModel.D, HttpContext.Request.Form.Files);

            return JsonOk();
            }
            catch (Exception ex)
            {
                if (!_webHelper.IsAjaxRequest(Request))
                {
                    var roxyError = new { res = "error", msg = ex.Message };
                    return Content($"<script>parent.fileUploaded({JsonConvert.SerializeObject(roxyError)});</script>");
                }

                return JsonError(ex.Message);
            }
        }

        #endregion

    }
