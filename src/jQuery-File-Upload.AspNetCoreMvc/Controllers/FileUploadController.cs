using System.Collections.Generic;
using System.Linq;
using jQuery_File_Upload.AspNetCoreMvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace jQuery_File_Upload.AspNetCoreMvc.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly FilesHelper _filesHelper;
        
        public FileUploadController(FilesHelper filesHelper)
        {
            _filesHelper = filesHelper;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Show()
        {
            JsonFiles ListOfFiles = _filesHelper.GetFileList();
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

            _filesHelper.UploadAndShowResults(CurrentContext, resultList);
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
            var list = _filesHelper.GetFileList();
            return Json(list);
        }
        
        public JsonResult DeleteFile(string file)
        {
            _filesHelper.DeleteFile(file);
            return Json("OK");
        }
    }
}
