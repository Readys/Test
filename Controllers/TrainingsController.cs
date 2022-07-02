using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTS.Models;
using TTS.Tools;

namespace TTS.Controllers
{
    public class TrainingsController : Controller
    {
        private TTSEntities db = new TTSEntities();
        private HRMEntities h_db = new HRMEntities();

        public ActionResult Index(int type = 1)
        {
            #region Userblock
            var allUsers = Helper.GetAllUsers();
            var appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();

            ViewBag.AppUserId = appUser.UserId;
            #endregion

            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();

            var category = tree.Where(w => w.Deleted != 1 && w.TypeId == 1).ToList(); //.OrderBy(o=> o.NameRu)
            var department = tree.Where(w => w.Deleted != 1 && w.TypeId == 2).ToList();

            var catTree = category.GenerateTree(c => c.TreeId, c => c.ParentId);
            var depTree = department.GenerateTree(c => c.TreeId, c => c.ParentId);     

            //tree = tree.First();

            ViewBag.Tree = catTree;
            ViewBag.DepTree = depTree;

            return View(category);
        }




    }
}