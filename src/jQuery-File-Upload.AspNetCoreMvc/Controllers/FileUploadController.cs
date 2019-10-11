using System.Threading.Tasks;
using jQuery_File_Upload.AspNetCoreMvc.Models;
using jQuery_File_Upload.AspNetCoreMvc.Models.FileUpload;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace jQuery_File_Upload.AspNetCoreMvc.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly FilesHelper _filesHelper;
        private readonly IMediator _mediator;

        public FileUploadController(FilesHelper filesHelper, IMediator mediator)
        {
            _filesHelper = filesHelper;
            _mediator = mediator;
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
        public async Task<ActionResult> Upload(FileUploadUpload.Command command)
        {
            command.HttpContext = HttpContext;

            var result = await _mediator.Send(command);

            // I think we can move this into the mediatr class.
            var jsonFiles = new JsonFiles(result.FileResults);

            return result.FileResults.Count == 0
                ? Json("Error")
                : Json(jsonFiles);
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
