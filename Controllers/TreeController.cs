using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTS.Models;
using TTS.Tools;

namespace TTS.Controllers
{
    public class TreeController : Controller
    {
        private TTSEntities db = new TTSEntities();
        private HRMEntities h_db = new HRMEntities();
        // GET: Tree
        public ActionResult Index(int type = 1)
        {
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();

            ViewBag.AppUserId = appUser.UserId;

            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();

            var category = tree.Where(w => w.Deleted != 1 && w.TypeId == type).ToList(); //.OrderBy(o=> o.NameRu)

            var catTree = category.GenerateTree(c => c.TreeId, c => c.ParentId);

            ViewBag.Tree = catTree;

            ViewBag.Type = type;

            return View(category);
        }

    }
}