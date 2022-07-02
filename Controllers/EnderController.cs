using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTS.Models;
using System.Data.Entity;

namespace TTS.Controllers
{
    public class EnderController : Controller
    {
        private TTSEntities db = new TTSEntities();
        private HRMEntities h_db = new HRMEntities();
        // GET: Tree
        public string EndTests(int type = 1)
        {
            var tests = db.Tests.Where(w => w.Finished != 1).ToList();
            DateTime startTime = new DateTime();
           
            foreach (var item in tests)
            {
                var duration = item.Duration.Value;
                startTime = item.CreateDate.Value;
                var endTime = startTime.AddMinutes(duration);

                if (endTime < DateTime.Now)
                {
                    item.Finished = 1;
                    db.Entry(item).State = EntityState.Modified;
                    
                }
            }
            db.SaveChanges();

            return "OK";
        }

        public string AddUsersNames()
        {
            var allUsers = db.L_User.Where(x => x.AccountAD != null || x.PrivateEmail != null).ToList();
            var tests = db.Tests.ToList();

            foreach (var item in tests)
            {
                var thisNameRu = allUsers.SingleOrDefault(s => s.UserId == item.UserId).NameRu;
                item.NameRu = thisNameRu;
                db.Entry(item).State = EntityState.Modified;
                db.SaveChanges();
            }

            return "OK";
        }

    }
}