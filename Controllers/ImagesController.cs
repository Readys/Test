using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTS.Models;

namespace TTS.Controllers
{
    public class ImagesController : Controller
    {
        private TTSEntities db = new TTSEntities();
        // GET: Images
        public ActionResult Index( int id)
        {
            var upload = db.Uploads.SingleOrDefault(x => x.UploadId == id);
           
            var path = upload.Path; //validate the path for security or use other means to generate the path.
            return base.File(path, "image/jpeg");
            
        }

        [HttpPost]
        public string RemoveFile(int id)
        {
            Upload sl = db.Uploads.SingleOrDefault(s => s.UploadId == id);
            db.Uploads.Remove(sl);
            db.SaveChanges();

            return "OK";
        }

    }
}