using System.Collections.Generic;
using System.IO;
using System.Linq;
using jQuery_File_Upload.AspNetCoreMvc.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace jQuery_File_Upload.AspNetCoreMvc.Controllers
{
    public class FileUploadController : Controller
    {
        FilesHelper filesHelper;
        const string TEMP_PATH = "~/somefiles/";
        const string SERVER_MAP_PATH = "~/Files/somefiles/";

        private readonly string _storageRootPath;

        private string UrlBase = "/Files/somefiles/";
        string DeleteURL = "/FileUpload/DeleteFile/?file=";
        string DeleteType = "GET";

        public FileUploadController(IHostingEnvironment env)
        {
            _storageRootPath = Path.Combine(env.ContentRootPath, SERVER_MAP_PATH);

            filesHelper = new FilesHelper(DeleteURL, DeleteType, _storageRootPath, UrlBase, TEMP_PATH, SERVER_MAP_PATH);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Show()
        {
            JsonFiles ListOfFiles = filesHelper.GetFileList();
            var model = new FilesViewModel()
            {
                Files = ListOfFiles.files
            };

            return View(model);
        }

        public ActionResult Edit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload()
        {
            var resultList = new List<ViewDataUploadFilesResult>();

            var CurrentContext = HttpContext;

            filesHelper.UploadAndShowResults(CurrentContext, resultList);
            JsonFiles files = new JsonFiles(resultList);

            bool isEmpty = !resultList.Any();
            if (isEmpty)
            {
                return Json("Error ");
            }
            else
            {
                return Json(files);
            }
        }

        public JsonResult GetFileList()
        {
            var list = filesHelper.GetFileList();
            return Json(list);
        }
        
        public JsonResult DeleteFile(string file)
        {
            filesHelper.DeleteFile(file);
            return Json("OK");
        }
    }
}
