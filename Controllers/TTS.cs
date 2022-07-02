using System.Web.Mvc;
using System.Data.Entity;
using System.Configuration;
using System.Collections.Specialized;
using System.IO;
using TTS.Models;
using System.Collections.Generic;
using System.Linq;
using System;
using PagedList.Mvc;
using PagedList;
using System.Globalization;
using TTS.Tools;
using System.Text;
using System.Security.Principal;

namespace TTS.Controllers
{
    public class TTSController : Controller
    {
        private TTSEntities db = new TTSEntities();
        //private TTS2Entities db2 = new TTS2Entities();
        private HRMEntities h_db = new HRMEntities();
        private static NameValueCollection configMail = ConfigurationManager.GetSection("MailBox") as NameValueCollection;
        public const int testUser = 0; //Тестовый вход под любым пользователем//598 181 211 783 471 37 97 39
        
        public ActionResult Index(int page = 1)
        {
            #region user
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId || x.UserId == appUser.OldUserId).ToList();
            #endregion
            // Удаление недосозданных записей
            var DelNull = db.TestTemplate.Where(w => w.NameRu == null).ToList();
            if(DelNull.Count() > 0) { 
                db.TestTemplate.RemoveRange(DelNull);
                db.SaveChanges();
            }

            var sevenSamurais = db.UserInRole.Where(x => x.AccessLevelId == 2).Select(s => s.UserId).ToArray();
            var testTemplates = db.TestTemplate.Where(w => w.Deleted != 1 && w.Saved != -1).ToList();

            if (access.Any())
            {
                if (access.Any(x => x.AccessLevelId == 2))
                {
                    testTemplates = testTemplates.Where(x => sevenSamurais.Contains(x.CreatedByUserId)).ToList();
                }
                else
                {
                    testTemplates = testTemplates.Where(x => x.CreatedByUserId == appUser.UserId || x.CreatedByUserId == appUser.OldUserId).ToList();
                }
            }
            else
            {
                return View("Access");
            }


            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();

            var testTemplateItem = db.TestTemplateItem.ToList();

            List<ViewTest> testTemplateItemList = new List<ViewTest>();

            foreach (var item in testTemplates)
            {
                var allCategoryList = db.Tree.Where(w => w.TypeId == 1 && w.Deleted != 1).ToList();
                var categoryListArr = testTemplateItem.Where(w => item.TestTemplateId == w.TestTemplateId && w.CategoryType == 1).Select(w => w.CategoryId).ToList();
                var departmentsListArr = testTemplateItem.Where(w => w.TestTemplateId == item.TestTemplateId && w.CategoryType == 2).Select(s => s.CategoryId).ToList();
                var levelsListArr = testTemplateItem.Where(w => item.TestTemplateId == w.TestTemplateId && w.CategoryType == 3).Select(w => w.CategoryId).ToList();

                var categoryList = tree.Where(w => categoryListArr.Contains(w.TreeId)).ToList();
                var departmentsList = tree.Where(w => departmentsListArr.Contains(w.TreeId)).ToList();
                var levels = tree.Where(w => levelsListArr.Contains(w.TreeId)).ToList();
                var duration = testTemplates.SingleOrDefault(s => item.TestTemplateId == s.TestTemplateId).Duration;

                var titleTestTemplate = item.NameRu;
                //var tryCount = db.TryCounts

                allCategoryList = allCategoryList.Except(categoryList).ToList();

                testTemplateItemList.Add(new ViewTest()
                {
                    Duration = duration,
                    TestTemplateId = item.TestTemplateId,
                    NameRu = titleTestTemplate,
                    SelectCategoryList = categoryList,
                    DepartmentList = departmentsList,
                    LevelsList = levels,
                    AllCategoryList = allCategoryList,
                    TryCount = item.TryCount
                });

            }

            ViewBag.Tree = tree;
            ViewBag.TestTemplateItem = testTemplateItem;
            ViewBag.AppUserId = appUser.UserId;
            ViewBag.Tests = db.Tests.ToList();
            ViewBag.Access = access.ToList();

            testTemplateItemList.Reverse();

            return View(testTemplateItemList.ToPagedList(page, 10));
        }

        public ActionResult DisplayAtt(int id)
        {
            #region user
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();

            #endregion

            var thisAtt = db.Attestations.SingleOrDefault(s => s.AttestationId == id);
            var tests = db.Tests.Where(w => w.AttestationId == id && w.UserId == appUser.UserId).ToList();

            var thisTestTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == thisAtt.TestTemplateId);
            var threshold = thisTestTemplate.TargetProcent;

            var thisUserAtt = thisAtt.empItems.SingleOrDefault(s => s.UserID == appUser.UserId);
            var maxTryCount = thisTestTemplate.TryCount;
            var actualTryCount = maxTryCount - tests.Count();

            if (tests.Count > 0)
            {
                var maxtest = tests.Max(m => m.WeightResult);

                ViewBag.Tests = tests;
                ViewBag.Maxtest = maxtest;
            }
            else
            {
                ViewBag.Tests = null;
                ViewBag.Maxtest = 0;
            }


            var statusTest = 0;
            var statusTestMsg = "";

            var checkActiveTest = tests.Any(a => a.Finished == 0);
            if (checkActiveTest)
            {
                var thisActivtest = tests.SingleOrDefault(s=> s.Finished == 0);

                var createTestTimePlusDuration = thisActivtest.CreateDate.Value.AddMinutes(thisTestTemplate.Duration);
                if (createTestTimePlusDuration > DateTime.Now)
                {
                    statusTest = 9;
                }
                else
                {
                    return RedirectToAction("FinTest", new { testId = thisActivtest.TestId });
                }

            }

            if (DateTime.Now < thisAtt.StartTime && statusTest != 9)
            {
                statusTest = 6; // Период аттестации еще не наступил.
            }

            if (DateTime.Now > thisAtt.FinishTime && statusTest != 9)
            {
                statusTest = 7; // Аттестация завершена.
            }


            if (tests.Count() != 0 && statusTest != 9)
            {
                var checkPassSuccessTest = tests.Any(a => a.UserId == appUser.UserId && a.AttestationId == thisAtt.AttestationId && a.WeightResult >= thisTestTemplate.TargetProcent - 1);
                statusTest = checkPassSuccessTest ? 2 : 3;
            }
            else if (statusTest == 0 && actualTryCount != 0)
            {
                if (thisUserAtt.ManagerApprove == true) {
                    statusTest = 1; // "Начать тестирование"
                }
                else
                {
                    statusTest = 5; // Должен активировать руководитель аттестации
                }
                
            }

            if (statusTest == 3 && actualTryCount != 0)
            {
                if (DateTime.Now < thisAtt.FinishTime)
                {
                    if (thisUserAtt.ManagerApprove == true)
                    {
                        statusTest = 8;
                    }
                    else {
                        statusTest = 5;
                    }
                }
            }


            switch (statusTest)
            {
                // Все условия выполнены для начала тестирования
                case 1:
                    statusTestMsg = "Начать тестирование";
                    break;
                case 2: // 
                    statusTestMsg = "Тестирование успешно пройдено";
                    break;
                case 3: //
                    statusTestMsg = "Тестирование не пройдено";
                    break;
                case 4: //
                    statusTestMsg = "Попытки закончились.";
                    break;
                case 5: //
                    statusTestMsg = "Должен активировать руководитель аттестации.";
                    break;
                case 6: //
                    statusTestMsg = "Период аттестации еще не наступил.";
                    break;
                case 7: //
                    statusTestMsg = "Аттестация завершена и вы не успели ее пройти.";
                    break;
                case 8: //
                    statusTestMsg = "Тестирование не пройдено. Есть еще одна попытка.";
                    break;
                case 9: //
                    statusTestMsg = "Тестирование все еще активно. Продолжить?";
                    break;
                default:
                    statusTestMsg = "Неизвестная ошибка";
                    break;
            }

            ViewBag.StatusTestMsg = statusTestMsg;
            ViewBag.StatusTest = statusTest;
            ViewBag.Threshold = threshold;

            return View(thisAtt);
        }

        public ActionResult CreateCertification(int id = 0)
        {

            Attestation attest;
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            var temlates = db.TestTemplate.Where(w => w.Deleted != 1 && w.TypeTemplateId != 1).ToList();
            temlates.Reverse();
            var feedTemlates = db.TestTemplate.Where(w => w.Deleted != 1 && w.TypeTemplateId == 1).ToList();
            feedTemlates.Reverse();
            var templateId = 0;
            int? feedTemplateId = 0;


            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();


            if (appUser != null && access.Any())
            {
                if (id == 0)
                {
                    attest = new Attestation();
                    attest.RequestDate = DateTime.Now;
                    attest.UserID = appUser.UserId;
                    attest.CreatedBy = appUser.UserId;
                    attest.IsSubmit = false;
                    attest.IsDeleted = false;

                    db.Attestations.Add(attest);
                    db.SaveChanges();

                    return RedirectToAction("CreateCertification", new { id = attest.AttestationId });

                }

                var request = db.Attestations.FirstOrDefault(x => x.AttestationId == id);

                if (request.IsSubmit == true)
                {
                    request.ModifiedBy = appUser.UserId;
                    request.LastModifiedDate = DateTime.Now;
                    db.SaveChanges();
                }
                if (request.TestTemplateId != 0)
                {
                    //var template = temlates.FirstOrDefault(x => x.TestTemplateId == request.TestTemplateId);
                    templateId = request.TestTemplateId;
                    //ViewBag.Template = template;
                }
                if (request.LMUserId != 0)
                {
                    var manager = db.L_User.FirstOrDefault(x => x.UserId == request.LMUserId || x.OldUserId == request.LMUserId);
                    ViewBag.Manager = manager;
                }
                if (request.FeedbackId != null)
                {
                    //var feedback = feedTemlates.FirstOrDefault(x => x.TestTemplateId == request.FeedbackId);
                    feedTemplateId = request.FeedbackId;
                    //ViewBag.Feedback = feedback;
                }

                var postions = h_db.Position;
                var divisions = h_db.Division;
                ViewBag.AllUsers = allUsers;
                ViewBag.Positions = postions;
                ViewBag.Division = divisions;

                ViewBag.TestTemplateId = new SelectList(temlates, "TestTemplateId", "NameRu", templateId);
                ViewBag.FeedTemplateId = new SelectList(feedTemlates, "FeedTemplateId", "NameRu", feedTemplateId);

                return View(request);
            }

            return View("Access");

        }

        [HttpPost]
        public ActionResult SubmitCertification(CreatedCertificationData data)
        {
            var memberAD = User.Identity.Name;
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            var usersInRole = db.UserInRole;
            var sevenSamurais = usersInRole.Where(x => x.AccessLevelId == 2).Select(s => s.UserId);
            bool isOneOfSevenGods = sevenSamurais.Contains(appUser.UserId);
            bool isEric = usersInRole.Any(a => a.AccessLevelId == 3);

            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime? start = null;
            DateTime? finish = null;
            var saveAtt = db.Attestations.Find(data.CertificationId);
            saveAtt.AttestationName = data.CertificationName;

            if (!String.IsNullOrEmpty(data.CertificationStartTime))
            {

                start = DateTime.ParseExact(data.CertificationStartTime, "dd.MM.yyyy HH:mm", null);

            }
            if (!String.IsNullOrEmpty(data.CertificationEndTime))
            {

                finish = DateTime.ParseExact(data.CertificationEndTime, "dd.MM.yyyy HH:mm", null);

            }

            saveAtt.StartTime = start;
            saveAtt.FinishTime = finish;
            saveAtt.TestTemplateId = data.TestTemplateId;
            saveAtt.FeedbackId = data.FeedTemplateId;
            saveAtt.LMUserId = data.ManagerId;
            saveAtt.IsSubmit = true;
            saveAtt.CertificationLevel = data.Level;
            saveAtt.CertificationLevelToPass = data.LeveltoPass;

            db.SaveChanges();

            if (isOneOfSevenGods || appUser.UserId == 783)
            {
                TrainindDepNotification(saveAtt);
                LMNotification(saveAtt);
                MechanicNotification(saveAtt);
            }

            if(isEric)
            {
                MechanicNotification(saveAtt);
            }

            return RedirectToAction("CertificationCalendar");

        }

        public ActionResult CreateCertificationOut(int id = 0)
        {

            var allUsers = Helper.GetAllUsers();
            var appUser = Helper.GetAppUser(User.Identity);

            Attestation attest;

            var temlates = db.TestTemplate.Where(w => w.Deleted != 1 && w.TypeTemplateId != 1).ToList();
            temlates.Reverse();
            var feedTemlates = db.TestTemplate.Where(w => w.Deleted != 1 && w.TypeTemplateId == 1).ToList();
            feedTemlates.Reverse();
            var templateId = 0;
            int? feedTemplateId = 0;

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();

            if (appUser != null && access.Any())
            {
                if (id == 0)
                {
                    attest = new Attestation();
                    attest.RequestDate = DateTime.Now;
                    attest.UserID = appUser.UserId;
                    attest.CreatedBy = appUser.UserId;
                    attest.IsSubmit = false;
                    attest.IsDeleted = false;

                    db.Attestations.Add(attest);
                    db.SaveChanges();

                    return RedirectToAction("CreateCertificationOut", new { id = attest.AttestationId });

                }

                var request = db.Attestations.FirstOrDefault(x => x.AttestationId == id);

                if (request.IsSubmit == true)
                {
                    request.ModifiedBy = appUser.UserId;
                    request.LastModifiedDate = DateTime.Now;
                    db.SaveChanges();
                }
                if (request.TestTemplateId != 0)
                {
                    //var template = temlates.FirstOrDefault(x => x.TestTemplateId == request.TestTemplateId);
                    templateId = request.TestTemplateId;
                    //ViewBag.Template = template;
                }
                if (request.LMUserId != 0)
                {
                    var manager = db.L_User.FirstOrDefault(x => x.UserId == request.LMUserId || x.OldUserId == request.LMUserId);
                    ViewBag.Manager = manager;
                }
                if (request.FeedbackId != null)
                {
                    //var feedback = feedTemlates.FirstOrDefault(x => x.TestTemplateId == request.FeedbackId);
                    feedTemplateId = request.FeedbackId;
                    //ViewBag.Feedback = feedback;
                }

                var postions = h_db.Position;
                var divisions = h_db.Division;
                var companies = db.Tree.Where(w => w.TypeId == 6 && w.TreeId != 117);

                ViewBag.AllUsers = allUsers;
                ViewBag.Positions = postions;
                ViewBag.Division = divisions;

                ViewBag.TestTemplateId = new SelectList(temlates, "TestTemplateId", "NameRu", templateId);
                ViewBag.FeedTemplateId = new SelectList(feedTemlates, "FeedTemplateId", "NameRu", feedTemplateId);
                ViewBag.Companies = new SelectList(companies, "TreeId", "NameRu", 134);

                return View(request);
            }

            return View("Access");

        }

        [HttpPost]
        public ActionResult SubmitCertificationOut(CreatedCertificationData data)
        {
            var memberAD = User.Identity.Name;
            var allUsers = Helper.GetAllUsers();
            var appUser = Helper.GetAppUser(User.Identity);

            var usersInRole = db.UserInRole;
            var sevenSamurais = usersInRole.Where(x => x.AccessLevelId == 2).Select(s => s.UserId);
            bool isOneOfSevenGods = sevenSamurais.Contains(appUser.UserId);
            bool isEric = usersInRole.Any(a => a.AccessLevelId == 3);

            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime? start = null;
            DateTime? finish = null;
            var saveAtt = db.Attestations.Find(data.CertificationId);
            saveAtt.AttestationName = data.CertificationName;

            if (!String.IsNullOrEmpty(data.CertificationStartTime))
            {

                start = DateTime.ParseExact(data.CertificationStartTime, "dd.MM.yyyy HH:mm", null);

            }
            if (!String.IsNullOrEmpty(data.CertificationEndTime))
            {

                finish = DateTime.ParseExact(data.CertificationEndTime, "dd.MM.yyyy HH:mm", null);

            }

            saveAtt.StartTime = start;
            saveAtt.FinishTime = finish;
            saveAtt.TestTemplateId = data.TestTemplateId;
            saveAtt.FeedbackId = data.FeedTemplateId;
            saveAtt.LMUserId = data.ManagerId;
            saveAtt.IsSubmit = true;
            saveAtt.CertificationLevel = data.Level;
            saveAtt.CertificationLevelToPass = data.LeveltoPass;

            db.SaveChanges();

            //if (isOneOfSevenGods || appUser.UserId == 783)
            //{
            //    TrainindDepNotification(saveAtt);
            //    LMNotification(saveAtt);
            //    MechanicNotification(saveAtt);
            //}

            //if (isEric)
            //{
            //    MechanicNotification(saveAtt);
            //}

            return RedirectToAction("CertificationCalendar");

        }


        #region Для оповещения трениниг отдела
        private void TrainindDepNotification(Attestation att = null)
        {
            var allUsers = Helper.GetAllUsers().ToList();
            var sevenSamurais = db.UserInRole.Where(x => x.AccessLevelId == 2).Select(s => s.UserId).ToArray();
            var userForNotification = allUsers.Where(x => sevenSamurais.Contains(x.UserId)).ToArray();//sevenSamurais.Contains(x.UserId)

            var LM = allUsers.FirstOrDefault(x => x.UserId == att.LMUserId);
            var creator = allUsers.FirstOrDefault(x => x.UserId == att.CreatedBy);
            var cnt = att.empItems.Count();

            try
            {
                List<string> toList = new List<string>();
                foreach (var mail in Helper.GetListOfMail(userForNotification))
                {

                    toList.Add(mail);
                }
                if (toList.Count() > 0)
                {
                    var configMail = ConfigurationManager.GetSection("MailBox") as NameValueCollection;
                    string path =
                        System.Web.HttpContext.Current.Server.MapPath("~/Templates/CertificationNotifications/NotificationOfTrainingDepartment.html");
                    string message = System.IO.File.ReadAllText(path);
                    message = message.Replace("{AttestationName}", att.AttestationName);
                    message = message.Replace("{StartDate}", ((DateTime)att.StartTime).ToString("dd.MM.yyyy HH:mm"));
                    message = message.Replace("{FinishDate}", ((DateTime)att.FinishTime).ToString("dd.MM.yyyy HH:mm"));
                    message = message.Replace("{Creator}", creator.NameRu);
                    message = message.Replace("{LMUser}", LM.NameRu);
                    message = message.Replace("{Status}", "Активная");
                    message = message.Replace("{Id}", att.AttestationId.ToString());
                    message = message.Replace("{AmountOfParticipants}", cnt.ToString());
                    Helper.SendMail("", toList, "Создание новой аттестации", message);
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Для оповещения линейных менеджеров
        private void LMNotification(Attestation att = null)
        {
            var allUsers = Helper.GetAllUsers().ToArray();
            var LM = allUsers.FirstOrDefault(w => w.UserId == att.LMUserId).AccountAD;
            var creator = allUsers.FirstOrDefault(x => x.UserId == att.CreatedBy);
            var cnt = att.empItems.Count();

            //var usrNames = att.empItems.Select(s=>s.UserID)
            try
            {
                List<string> toList = new List<string>();
                toList.Add(Helper.GetMailByAccountAD(LM));
                if (toList.Count() > 0)
                {
                    var configMail = ConfigurationManager.GetSection("MailBox") as NameValueCollection;
                    string path =
                    System.Web.HttpContext.Current.Server.MapPath("~/Templates/CertificationNotifications/NotificationOfLM.html");
                    string message = System.IO.File.ReadAllText(path);                   
                    string tableItems = "";
                    var requestUrl = configMail["URL"] + "TTS/CertificationCalendar/";
                                        
                    message = message.Replace("{AttestationName}", att.AttestationName);
                    message = message.Replace("{Creator}", creator.NameRu);
                    message = message.Replace("{StartTime}", ((DateTime)att.StartTime).ToString("dd.MM.yyyy HH:mm"));
                    message = message.Replace("{FinishTime}", ((DateTime)att.FinishTime).ToString("dd.MM.yyyy HH:mm"));
                    message = message.Replace("{Status}", "Активная");
                    message = message.Replace("{Count}", cnt.ToString());
                    message = message.Replace("{RequestUrl}", configMail["URL"] + "http://training.bmst-kz.borusan.com/TTS/ApproveCertificationParticipants/" + att.AttestationId);
                    message += "<table class='table table-bordered' style='border - collapse: collapse; border: 1px solid #57697e; width: 50%; margin-left:40px;'>"
                        + "<thead><tr><th style='padding: 10px;'><font face='Arial, Helvetica, sans-serif'>ФИО</font></th>"
                        + "<th style='padding: 10px;'><font face='Arial, Helvetica, sans-serif'>Уровень</font></th></tr></thead><tbody>";
                    
                    foreach (var item in att.empItems)
                    {
                        var userName = allUsers.FirstOrDefault(f => f.UserId == item.UserID).NameRu;
                        var t = att.CertificationLevelToPass.ToString();
                        
                        tableItems += $"<tr style='text-align:center; border-bottom:0 none; border-top:0 none; border-left-width: 0;'><td style='padding: 10px;'>" +
                                                                $"<font face='Arial, Helvetica, sans - serif'>{userName}</font></td>" +
                                                                $"<td style='padding: 10px;'> " +
                                                                $"<font face='Arial, Helvetica, sans - serif'>{t}</font></td></tr>";
 
                    }
                    message += tableItems;
                    message += "</tbody></table>";                    
                    Helper.SendMail("", toList, "Создание новой аттестации для сотрудников", message);
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        #region Оповещение для механиков
        private void MechanicNotification(Attestation att = null)
        {
            var allUsers = Helper.GetAllUsers().ToArray();
            var mechanics = att.empItems.Select(s => s.UserID).ToArray();
            var userForNotification = allUsers.Where(x => mechanics.Contains(x.UserId)).ToArray();

            var LM = allUsers.FirstOrDefault(x => x.UserId == att.LMUserId);
            var cnt = att.empItems.Count();

            try
            {
                List<string> toList = new List<string>();

                string url = configMail["URL"] + "/TTS/DisplayAtt?id=" + att.AttestationId; //

                foreach (var mail in Helper.GetListOfMail(userForNotification))
                {

                    toList.Add(mail);
                }
                if (toList.Count() > 0)
                {
                    var configMail = ConfigurationManager.GetSection("MailBox") as NameValueCollection;
                    string path =
                    System.Web.HttpContext.Current.Server.MapPath("~/Templates/CertificationNotifications/NotificationOfMechanic.html");
                    string message = System.IO.File.ReadAllText(path);
                    message = message.Replace("{аттестация}", "<a href=" + url + ">аттестация</a>");
                    message = message.Replace("{AttestationName}", att.AttestationName);
                    message = message.Replace("{StartDate}", ((DateTime)att.StartTime).ToString("dd.MM.yyyy HH:mm"));
                    message = message.Replace("{FinishDate}", ((DateTime)att.FinishTime).ToString("dd.MM.yyyy HH:mm"));
                    message = message.Replace("{LMUser}", LM.NameRu);
                    message = message.Replace("{Level}", att.CertificationLevelToPass.ToString());
                    message = message.Replace("{Status}", "Активная");
                    message = message.Replace("{RequestUrl}", "http://training.bmst-kz.borusan.com/TTS/DisplayAtt/" + att.AttestationId);

                    Helper.SendMail("", toList, "Назначение аттестации", message);
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        [HttpPost]
        public string SaveCertification(CreatedCertificationData data)
        {
            CultureInfo provider = CultureInfo.InvariantCulture;

            DateTime? start = null;
            DateTime? finish = null;


            var saveAtt = db.Attestations.Find(data.CertificationId);
            if (saveAtt != null)
            {
                saveAtt.AttestationName = data.CertificationName;
                if (!String.IsNullOrEmpty(data.CertificationStartTime))
                {

                    start = DateTime.ParseExact(data.CertificationStartTime, "dd.MM.yyyy HH:mm", null);

                }
                if (!String.IsNullOrEmpty(data.CertificationEndTime))
                {

                    finish = DateTime.ParseExact(data.CertificationEndTime, "dd.MM.yyyy HH:mm", null);

                }
                saveAtt.StartTime = start;
                saveAtt.FinishTime = finish;
                saveAtt.IsSubmit = false;
                saveAtt.TestTemplateId = data.TestTemplateId;
                saveAtt.FeedbackId = data.FeedTemplateId;
                saveAtt.LMUserId = data.ManagerId;
                saveAtt.CertificationLevel = data.Level;
                saveAtt.CertificationLevelToPass = data.LeveltoPass;

                db.SaveChanges();

                return "ok";
            }


            return "error";
        }

        public ActionResult CertificationCalendar(int page = 1, int tabs = 0,
            int AttestationId = 0, int CreatedBy = 0, int TestTemplateId = 0, int ManagerId = 0, string StartDate = null, string FinishDate = null,
            string StartEndDate = null, string FinishEndDate = null)
        {

            // Удаление недосозданных аттестаций
            var DelNull = db.Attestations.Where(w => w.AttestationName == null).ToList();
            db.Attestations.RemoveRange(DelNull);
            db.SaveChanges();

            var memberAD = User.Identity.Name;
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            var templates = db.TestTemplate.ToList();
            CultureInfo provider = CultureInfo.InvariantCulture;
            DateTime? start;
            DateTime? end;

            #region Init identifiers
            var usersInRole = db.UserInRole;
            IEnumerable<Attestation> attestations = null;
            var SuperAdmin = usersInRole.FirstOrDefault(x => x.AccessLevelId == 1);
            var sevenSamurais = usersInRole.Where(x => x.AccessLevelId == 2).Select(s => s.UserId);
            var Erik = usersInRole.FirstOrDefault(x => x.AccessLevelId == 3);
            var AllCertifications = db.Attestations.ToList();
            DateTime current = DateTime.Now;
            var AttestationLMUsers = AllCertifications.Select(x => x.LMUserId).ToArray();
            #endregion

            #region isSomeOneCool
            bool isSuperAdmin = SuperAdmin.UserId == appUser.UserId;
            bool isOneOfSevenGods = sevenSamurais.Contains(appUser.UserId);
            bool isSuperErik = appUser.UserId == Erik.UserId || appUser.OldUserId == Erik.UserId;
            bool oneOfLMUsers = AttestationLMUsers.Contains(appUser.UserId);
            #endregion

            var isHasAccess = false;

            if (isSuperAdmin || isOneOfSevenGods || isSuperErik || oneOfLMUsers)
            {
                isHasAccess = true;
            }


            if (appUser != null && isHasAccess)
            {

                switch (tabs)
                {
                    case 0:
                        {
                            attestations = AllCertifications.Where(x => x.IsSubmit == true && x.FinishTime > current && x.IsDeleted != true).ToList(); // ( x.LMUserId ==  appUser.UserId || isHasAccess == true) 
                            break;
                        }
                    case 1:
                        {
                            attestations = AllCertifications.Where(x => x.IsSubmit == false && x.CreatedBy == appUser.UserId && x.IsDeleted != true).ToList();
                            break;
                        }
                    case 2:
                        {
                            attestations = AllCertifications.Where(x => x.FinishTime < current && x.IsSubmit == true && x.IsDeleted != true).ToList();
                            break;
                        }
                }

                if (SuperAdmin.UserId != appUser.UserId)
                {
                    if (isOneOfSevenGods)
                    {
                        attestations = attestations.Where(x => sevenSamurais.Contains(x.CreatedBy) || sevenSamurais.Contains(x.LMUserId)).ToList();
                    }
                    if (isSuperErik)
                    {
                        attestations = attestations.Where(x => x.CreatedBy == appUser.UserId).ToList();
                    }
                    if (oneOfLMUsers && !isOneOfSevenGods && !isSuperErik)
                    {
                        attestations = attestations.Where(x => x.LMUserId == appUser.UserId).ToList();
                    }
                }

                if (AttestationId != 0)
                {
                    attestations = attestations.Where(w => w.AttestationId == AttestationId).ToList();
                    var attestObj = db.Attestations.FirstOrDefault(f => f.AttestationId == AttestationId);
                    if (attestObj != null)
                    {
                        ViewBag.Attestation = attestObj.AttestationName;
                    }
                }

                if (CreatedBy != 0)
                {
                    attestations = attestations.Where(w => w.CreatedBy == CreatedBy).ToList();
                    var attestObj = db.L_User.FirstOrDefault(f => f.UserId == CreatedBy || f.OldUserId == CreatedBy).NameRu;
                    if (attestObj != null)
                    {
                        ViewBag.CreatedBy = attestObj;
                    }
                }

                if (TestTemplateId != 0)
                {
                    attestations = attestations.Where(w => w.TestTemplateId == TestTemplateId).ToList();
                    var attestObj = db.TestTemplate.FirstOrDefault(f => f.TestTemplateId == TestTemplateId);
                    if (attestObj != null)
                    {
                        ViewBag.TestTemplate = attestObj.NameRu;
                    }
                }

                if (ManagerId != 0)
                {
                    attestations = attestations.Where(w => w.LMUserId == ManagerId).ToList();
                    var attestObj = db.L_User.FirstOrDefault(f => f.UserId == ManagerId || f.OldUserId == ManagerId);
                    if (attestObj != null)
                    {
                        ViewBag.ManagerName = attestObj.NameRu;
                    }
                }

                bool isStartRange = !String.IsNullOrEmpty(StartDate);
                bool isFinishRange = !String.IsNullOrEmpty(FinishDate);
                bool isStartEndRange = !String.IsNullOrEmpty(StartEndDate);
                bool isFinishEndRange = !String.IsNullOrEmpty(FinishEndDate);


                if (isStartRange || isFinishRange)
                {
                    start = isStartRange ? DateTime.ParseExact(StartDate, "dd.MM.yyyy", provider) : attestations.Select(x => x.StartTime).Min().Value.Date;
                    end = isFinishRange ? DateTime.ParseExact(FinishDate, "dd.MM.yyyy", provider) : attestations.Select(x => x.StartTime).Max().Value.Date;
                    attestations = attestations.Where(x => x.StartTime.HasValue && x.StartTime.Value.Date >= start.Value.Date && x.StartTime.Value.Date <= end.Value.Date).ToList();
                }

                if (isStartEndRange || isFinishEndRange)
                {
                    start = isStartEndRange ? DateTime.ParseExact(StartEndDate, "dd.MM.yyyy", provider) : attestations.Select(x => x.FinishTime).Min().Value.Date;
                    end = isFinishEndRange ? DateTime.ParseExact(FinishEndDate, "dd.MM.yyyy", provider) : attestations.Select(x => x.FinishTime).Max().Value.Date;
                    attestations = attestations.Where(x => x.FinishTime.HasValue && x.FinishTime.Value.Date >= start.Value.Date && x.FinishTime.Value.Date <= end.Value.Date).ToList();
                }


                ViewBag.Templates = templates;
                ViewBag.tabs = tabs;
                ViewBag.Users = allUsers;
                ViewBag.AppUser = appUser;
                ViewBag.IsLM = oneOfLMUsers;
                ViewBag.IsOneOfSeven = isOneOfSevenGods;
                ViewBag.sa = isSuperAdmin;

                return View(attestations.ToPagedList(page, 20));
            }


            return View("Access");
        }

        [HttpPost]
        public string DublicateCertification(int attestId, int tabs)
        {
            if (attestId != 0)
            {
                Attestation copy = new Attestation();
                int NewId = 0;
                var Att = db.Attestations.FirstOrDefault(x => x.AttestationId == attestId);
                copy.RequestDate = DateTime.Now;
                copy.StartTime = Att.StartTime;
                copy.FinishTime = Att.FinishTime;
                copy.UserID = Att.UserID;
                copy.CreatedBy = Att.CreatedBy;
                if (tabs == 0)
                {
                    copy.IsSubmit = true;
                }
                else
                {
                    copy.IsSubmit = false;
                }
                copy.AttestationName = Att.AttestationName;
                copy.LMUserId = Att.LMUserId;
                copy.IsDeleted = false;
                copy.TestTemplateId = Att.TestTemplateId;
                copy.FeedbackId = Att.FeedbackId;
                copy.CertificationLevel = Att.CertificationLevel;
                copy.CertificationLevelToPass = Att.CertificationLevelToPass;
                copy.empItems = new List<AttestationEmployeeItems>();


                db.Attestations.Add(copy);
                db.SaveChanges();

                NewId = copy.AttestationId;

                int cnt = db.AttestationEmployeeItems.Where(s => s.AttestationId == Att.AttestationId).Count();

                if (cnt > 0)
                {

                    foreach (var item in Att.empItems)
                    {
                        AttestationEmployeeItems employee = new AttestationEmployeeItems();
                        employee.AttestationId = NewId;
                        employee.UserID = item.UserID;
                        employee.ManagerApprove = false;
                        employee.Passed = 0;
                        employee.PassedDate = null;
                        copy.empItems.Add(employee);
                        db.AttestationEmployeeItems.Add(employee);

                    }

                    db.SaveChanges();
                }



                return "Success";
            }


            return "error";
        }

        [HttpPost]
        public string DeleteCertification(int attId)
        {
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser != null)
            {
                var DeletedAttestation = db.Attestations.FirstOrDefault(s => s.AttestationId == attId);
                DeletedAttestation.IsDeleted = true;
                DeletedAttestation.DeletedBy = appUser.UserId;
                DeletedAttestation.DeletedDate = DateTime.Now;
                db.SaveChanges();

                return "Success";
            }

            return "error";
        }

        public ActionResult ApproveCertificationParticipants(int id)
        {
            var allUsers = Helper.GetAllUsers();
            var allUsersOut = db.L_User.Where(w => w.PrivateEmail != null).ToList();

            var appUser = Helper.GetAppUser(User.Identity);

            var usersInRole = db.UserInRole;
            var SuperAdmin = usersInRole.FirstOrDefault(x => x.AccessLevelId == 1);
            var sevenSamurais = usersInRole.Where(x => x.AccessLevelId == 2).Select(s => s.UserId);
            var Erik = usersInRole.FirstOrDefault(x => x.AccessLevelId == 3);
            var AllCertifications = db.Attestations.ToList();
            var AttestationLMUsers = AllCertifications.Select(x => x.LMUserId);

            bool isSuperAdmin = SuperAdmin.UserId == appUser.UserId;
            bool isOneOfSevenGods = sevenSamurais.Contains(appUser.UserId);
            bool isSuperErik = appUser.UserId == Erik.UserId;
            bool oneOfLMUsers = AttestationLMUsers.Contains(appUser.UserId);

            var isHasAccess = false;

            if (isSuperAdmin || isOneOfSevenGods || isSuperErik || oneOfLMUsers)
            {
                isHasAccess = true;
            }

            if (appUser != null && isHasAccess)
            {
               
                var attestation = db.Attestations.Find(id);
                int participantsQuantity = attestation.empItems.Count;
                var postions = h_db.Position;
                var divisions = h_db.Division;

                ViewBag.AllUsers = allUsers;
                ViewBag.AllUsersOut = allUsersOut;

                ViewBag.Positions = postions;
                ViewBag.Division = divisions;
                ViewBag.CurrentAttestation = attestation;
                ViewBag.quantity = participantsQuantity;
                return View();
            }

            return View("Access");
        }


        [HttpPost]
        public ActionResult ApproveUsersToCertification(int[] userId, int CertificationId)
        {
            var CertificationItems = db.AttestationEmployeeItems.Where(x => x.AttestationId == CertificationId && userId.Contains(x.UserID)).ToArray();
            foreach (var item in CertificationItems)
            {
                item.ManagerApprove = true;
            }
            db.SaveChanges();

            return RedirectToAction("CertificationCalendar");
        }

        public ActionResult Questions(FilterQuestions filter, int page = 1, int idSub = 0)
        {
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();

            // Удаление недосозданных вопросов
            var DelNull = db.Questions.Where(w => w.QuestionRu == null || w.QuestionRu == "Введите ваш вопрос").ToList();
            db.Questions.RemoveRange(DelNull);
            db.SaveChanges();

            var upload = db.Uploads.ToList();
            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();
            var category = db.TreeQuestion.ToList();
            //List<Question> questions = new List<Question>();

            var questions = db.Questions.Where(w => w.Deleted != 1 && w.ParentQuestionId == null).ToList();
            var questionsCheck = db.Questions.Where(w => w.Deleted != 1 && w.TypeId != 105 && w.Published == 1).ToList();
            var questionsNotPublished = db.Questions.Where(w => w.Deleted != 1 && w.Published != 1).ToList();

            var haveFileQuestionId = upload.Where(w=> w.Extension != ".pdf").Select(s => s.DocumentId).ToArray();
            var haveFileQuestion = questions.Where(w => haveFileQuestionId.Contains(w.QuestionId)).ToList();

            // Удаление ответов без вопросов
            //var answers = db.Answers.Where(w => w.Deleted != 1).ToList();

            List<Question> questionsWrong = new List<Question>();

            foreach (var q in questionsCheck)
            {
                var answersToQuestion = db.Answers.Where(w => w.QuestionId == q.QuestionId && w.Deleted != 1).ToList();
                var summAnswers = answersToQuestion.Select(s=> s.Weight).Sum(); 
                if (summAnswers < 100)
                {
                    questionsWrong.Add(new Question() { QuestionId = q.QuestionId, });
                }

            }


            if (filter != null)
            {
                if (filter.DepartmentId > 0)
                {
                    var departmentListArr = db.TreeQuestion.Where(w => w.TreeId == filter.DepartmentId && w.TypeId == 2).Select(s => s.QuestionId).ToList();
                    questions = questions.Where(w => departmentListArr.Contains(w.QuestionId)).ToList();
                }
                if (filter.QuestionId > 0)
                {
                    questions = questions.Where(x => x.QuestionId == filter.QuestionId).ToList();
                }

                if (filter.LevelId > 0)
                {
                    questions = questions.Where(x => x.LevelId == filter.LevelId).ToList();
                }
                if (filter.SubjectId > 0)
                {
                    var categoryListArr = db.TreeQuestion.Where(w => w.TreeId == filter.SubjectId && w.TypeId == 1).Select(s => s.QuestionId).ToList();
                    questions = questions.Where(w => categoryListArr.Contains(w.QuestionId)).ToList();
                    //questions = questions.Where(x => x.LevelId == filter.LevelId).ToList();
                }
                if (filter.HavePic > 0)
                {
                    if (filter.HavePic == 1)
                    {
                        questions = haveFileQuestion;
                    }
                    else
                    {
                        questions = haveFileQuestion.Where(w => w.QuestionId == filter.HavePic).ToList();
                    }
                    
                }
            }

            List<ViewQuestion> HaveFileQuestion = new List<ViewQuestion>();

            foreach (var q in haveFileQuestion)
            {
                var pics = upload.Where(x => x.ModuleId == 1 && x.DocumentId == q.QuestionId).Select(s => s.UploadId).ToList();
                HaveFileQuestion.Add(new ViewQuestion() { QuestionId = q.QuestionId, PictureList = pics,});

            }

            questions.Reverse();
            ViewBag.Questions = questions.ToPagedList(page, 50);
            ViewBag.Tree = tree;
            ViewBag.Category = category;
            ViewBag.QuestionsWrong = questionsWrong;
            ViewBag.QuestionsNotPublished = questionsNotPublished;
            ViewBag.HaveFileQuestion = HaveFileQuestion;
            ViewBag.AppUser = appUser;
            ViewBag.Access = access;

            return View();
        }

        public ActionResult CopyQuestion(int id)
        {
            var categoryList = db.TreeQuestion.Where(w => w.QuestionId == id && w.TypeId == 1).ToList();
            var departmentList = db.TreeQuestion.Where(w => w.QuestionId == id && w.TypeId == 2).ToList();
            var levelsList = db.TreeQuestion.Where(w => w.QuestionId == id && w.TypeId == 3).ToList();

            var question = db.Questions.SingleOrDefault(w => w.QuestionId == id);

            var q = new Question() { };
            var newId = 0;

            q.CreateDate = DateTime.Now;
            q.TypeId = question.TypeId;
            q.QuestionRu = question.QuestionRu + " (Копия)";
            q.Published = 0;
            db.Questions.Add(q);
            db.SaveChanges();
            newId = q.QuestionId;

            db.Answers.Add(new Answer() { QuestionId = newId, AnswerRu = "Да", Weight = 0, CreateDate = q.CreateDate });
            db.Answers.Add(new Answer() { QuestionId = newId, AnswerRu = "Нет", Weight = 0, CreateDate = q.CreateDate });
            db.SaveChanges();

            if (categoryList != null)
            {
                foreach (var cat in categoryList)
                {
                    TreeQuestion tq = new TreeQuestion()
                    {
                        QuestionId = newId,
                        TreeId = cat.TreeId,
                        TypeId = cat.TypeId
                    };
                    db.TreeQuestion.Add(tq);
                }
                db.SaveChanges();
            }

            if (departmentList != null)
            {
                foreach (var cat in departmentList)
                {
                    TreeQuestion tq = new TreeQuestion()
                    {
                        QuestionId = newId,
                        TreeId = cat.TreeId,
                        TypeId = cat.TypeId
                    };
                    db.TreeQuestion.Add(tq);
                }
                db.SaveChanges();
            }

            if (levelsList != null)
            {
                foreach (var lev in levelsList)
                {
                    TreeQuestion tq = new TreeQuestion()
                    {
                        QuestionId = newId,
                        TreeId = lev.TreeId,
                        TypeId = lev.TypeId
                    };
                    db.TreeQuestion.Add(tq);
                }
                db.SaveChanges();
            }

            return RedirectToAction("CreateQuestions", new { id = newId });
        }

        public ActionResult CreateQuestions(int id = 0)
        {
            var answers = db.Answers.Where(w => w.Deleted != 1).ToList();
            var q = new Question() { };

            if (id == 0)
            {
                q.CreateDate = DateTime.Now;
                q.TypeId = 12;
                q.QuestionRu = "Введите ваш вопрос";
                q.Published = 0;
                q.LevelId = 0;
                db.Questions.Add(q);
                db.SaveChanges();
                id = q.QuestionId;

                db.Answers.Add(new Answer() { QuestionId = id, AnswerRu = "Да", Weight = 0, CreateDate = q.CreateDate });
                db.Answers.Add(new Answer() { QuestionId = id, AnswerRu = "Нет", Weight = 0, CreateDate = q.CreateDate });
                db.SaveChanges();

                return RedirectToAction("CreateQuestions", new { id = id });
            }

            var allQuestions = db.Questions.Where(w => w.Deleted != 1).ToList();
            q = allQuestions.FirstOrDefault(w => w.QuestionId == id);
            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();

            var uploads = db.Uploads.ToList();

            var treeToQuestion = db.TreeQuestion.ToList();

            List<ViewQuestion> questionList = new List<ViewQuestion>();

            var categoryListArr = treeToQuestion.Where(w => w.QuestionId == q.QuestionId && ( w.TypeId == 1 || w.TypeId == 5)).Select(s => s.TreeId).ToList();
            var pictureList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == q.QuestionId && x.Extension != ".pdf").Select(s => s.UploadId).ToList();
            var uploadsList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == q.QuestionId && (x.Extension == ".pdf")).ToList();
            // var categoryList = tree.Where(w => categoryListArr.Contains(w.TreeId)).Select(s => s.TreeId).ToArray();
            var departmentsListArr = treeToQuestion.Where(w => w.QuestionId == q.QuestionId && w.TypeId == 2).Select(s => s.TreeId).ToList();
            var levelListArr = treeToQuestion.Where(w => w.QuestionId == q.QuestionId && w.TypeId == 3).Select(s => s.TreeId).ToList();
            ///var departmentsList = tree.Where(w => departmentsListArr.Contains(w.TreeId)).ToList();
            var a = answers.Where(x => x.QuestionId == id).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight, isEdit = true }).ToList();

            var subQuestions = allQuestions.Where(w => w.ParentQuestionId == id).ToList();

            List<ViewQuestion> SubQestionList = new List<ViewQuestion>();

            if (q.TypeId == 13 && subQuestions.Count() > 0 )
            {
                foreach (var sq in subQuestions)
                {
                    var thisSubQuestions  = db.Questions.Find(id);
                    var subAnswer = answers.SingleOrDefault(s => s.QuestionId == sq.QuestionId);
                    var textAnswer = 0;
                    var textAnswer2 = "Ответ";
                    if (subAnswer != null)
                    {
                        textAnswer = subAnswer.AnswerId;
                        textAnswer2 = subAnswer.AnswerRu;
                    }
                    SubQestionList.Add(new ViewQuestion()
                        {
                             QuestionId = sq.QuestionId,
                             Question = sq,
                             TextAnswer = textAnswer,
                             TextAnswer2 = textAnswer2,
                    });
                }
            }

            var qq = new ViewQuestion()
            {
                Question = q,
                FilesList = uploadsList,
                PictureList = pictureList,
                CategoryList = categoryListArr,
                DepartmentList = departmentsListArr,
                isEdit = true,
                Answers = a,
                LevelsList = levelListArr,
                SubQuestion = SubQestionList,
            };

            ViewBag.Tree = tree;
            ViewBag.Id = id;

            return View(qq);
        }

        public ActionResult EditTree()
        {
            var treeRoot = db.Tree.Where(w => w.TypeId == 2).ToList();
            List<TreeRootView> treeRootList = new List<TreeRootView>();

            foreach (var item in treeRoot)
            {
                treeRootList.Add(new TreeRootView() { id = item.TreeId, isBatch = true, text = item.NameRu });
            }

            return View(treeRootList);
        }

        public ActionResult EditTest(int id = 0)
        {
            var allUsers = Helper.GetAllUsers();
            //var appUser = allUsers.FirstOrDefault(x => x.AccountAD.ToLower() == User.Identity.Name.ToLower());
            L_User appUser = Helper.GetAppUser(User.Identity);
            var testTemplates = db.TestTemplate.Where(w => w.Deleted != 1).ToList();
            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();

            TestTemplate testTemplate;

            if (id == 0)
            {
                testTemplate = new TestTemplate()
                {
                    ModifedById = appUser.UserId,
                    CreatedByUserId = appUser.UserId,
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    Duration = 5,
                    TargetProcent = 1,
                    TryCount = 1,
                    
                };
                db.TestTemplate.Add(testTemplate);
                db.SaveChanges();

                testTemplate.FeedTemplateId = testTemplate.TestTemplateId;
                db.Entry(testTemplate).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("EditTest", new { id = testTemplate.TestTemplateId });
            }

            testTemplate = db.TestTemplate.Find(id);

            var testTemplateItem = db.TestTemplateItem.ToList();

            var departmentsListArr = testTemplateItem.Where(w => w.TestTemplateItemId == testTemplate.TestTemplateId && w.CategoryType == 2).Select(s => s.CategoryId).ToList();
            var departmentsList = tree.Where(w => departmentsListArr.Contains(w.TreeId)).ToList();

            var selectedCategoryArr = db.TestTemplateItem.Where(w => w.TestTemplateId == id && w.CategoryType == 1).Select(w => w.CategoryId).ToList();
            var selectedCategory = tree.Where(w => selectedCategoryArr.Contains(w.TreeId)).ToList();

            if (testTemplate != null)
            {
                //testTemplateItem.NameRu = testTemplate.NameRu;
                //testTemplateItem.DepartmentList = 
            }

            var allCategoryList = db.Tree.Where(w => (w.TypeId == 1 || w.TypeId == 5) && w.Deleted != 1).ToList();
            var categoryListArr = testTemplateItem.Where(w => w.TestTemplateId == id && w.CategoryType == 1).Select(w => w.CategoryId).ToList();
            var categoryList = tree.Where(w => categoryListArr.Contains(w.TreeId)).ToList();
            allCategoryList = allCategoryList.Except(categoryList).ToList();

            List<SelectedTreeQuestion> selectedTreeQuestion = new List<SelectedTreeQuestion>();

            foreach (var item in selectedCategory)
            {
                var name = tree.SingleOrDefault(s => s.TreeId == item.TreeId).NameRu;
                var questionQuantity = testTemplateItem.SingleOrDefault(s => s.TestTemplateId == id && s.CategoryId == item.TreeId && s.CategoryType == 1).QuestionQuantity;
                selectedTreeQuestion.Add(new SelectedTreeQuestion()
                {
                    NameRu = name,
                    TestTemplateId = id,
                    QuestionQuantity = questionQuantity,
                    TreeId = testTemplateItem.SingleOrDefault(s => s.TestTemplateId == id && s.CategoryId == item.TreeId && s.CategoryType == 1).CategoryId
                });

            }

            ViewBag.Tree = tree;
            ViewBag.AllCategoryList = allCategoryList;
            ViewBag.SelectedTreeQuestion = selectedTreeQuestion;

            ViewBag.SelectedCategory = selectedCategory;

            var selectDepartmensId = db.TestTemplateItem.Where(w => w.TestTemplateId == id && w.CategoryType == 2).Select(w => w.CategoryId).ToArray();
            var selectLevelsId = db.TestTemplateItem.Where(w => w.TestTemplateId == id && w.CategoryType == 3).Select(w => w.CategoryId).ToArray();

            ViewBag.DepartmentsList = new MultiSelectList(db.Tree.Where(w => w.TypeId == 2).ToList(), "TreeId", "NameRu", selectDepartmensId);
            ViewBag.LevelsList = new MultiSelectList(db.Tree.Where(w => w.TypeId == 3).ToList(), "TreeId", "NameRu", selectLevelsId);

            return View(testTemplate);
        }

        public ActionResult PassTest(int testTemplateId, int attestationId, int testId = 0)
        {
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            var thisTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId);
            var thisTest = db.Tests.Any(a => a.AttestationId == attestationId && a.TemplateId == testTemplateId && a.UserId == appUser.UserId && a.Finished == 0);

            if (thisTest == true)
            {
                testId = db.Tests.SingleOrDefault(a => a.AttestationId == attestationId && a.TemplateId == testTemplateId && a.UserId == appUser.UserId && a.Finished == 0).TestId;
            }

            var maxCount = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId).TryCount;
            var tryCount = db.Tests.Where(w => w.AttestationId == attestationId && w.UserId == appUser.UserId && w.Finished == 1).Count();
            var tryCountNow = maxCount - tryCount;

            if (tryCountNow == 0)
            {
                return RedirectToAction("TryEnd");
            }
            else if (thisTest != true)
            {
                var tId = GenerateMyTest(testTemplateId, attestationId);
                return RedirectToAction("PassTest", new { testTemplateId = thisTemplate.TestTemplateId, attestationId, testId = tId });
            }

            var checkFinished = db.Tests.SingleOrDefault(s => s.TestId == testId);

            if (checkFinished.Finished == 1)
            {
                return RedirectToAction("Index");
            }

            var time = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId).Duration;

            DateTime startTime = new DateTime();

            startTime = db.Tests.SingleOrDefault(s => s.TestId == testId).CreateDate.Value;
            var endTime = startTime.AddMinutes(time);

            TimeSpan interval = endTime - DateTime.Now;
            time = interval.Hours * 60;
            time = time + interval.Minutes;

            if (time <= 0)
            {
                return RedirectToAction("DisplayAtt",new { id = attestationId} );
            }

            Session.Timeout = time;

            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();
            var testTemplateName = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId).NameRu;

            var subjectsId = db.TestTemplateItem.Where(w => w.TestTemplateId == testTemplateId && w.CategoryType == 1).Select(s => s.CategoryId).ToList();
            var subjects = tree.Where(w => subjectsId.Contains(w.TreeId)).ToList();

            var levelsId = db.TestTemplateItem.Where(w => w.TestTemplateId == testTemplateId && w.CategoryType == 3).Select(s => s.CategoryId).ToList();
            var levels = tree.Where(w => levelsId.Contains(w.TreeId)).ToList();

            var allQuestions = db.Questions.Where(w => w.Deleted != 1 && w.Published == 1).ToList();

            var Questions = allQuestions.Where(w => w.Deleted != 1 && w.Published == 1 && w.ParentQuestionId == null).ToList();
            var SubQuestions = allQuestions.Where(w => w.Deleted != 1 && w.Published == 1 && w.ParentQuestionId != null).ToList();

            var questionsIdArr = db.TestsItem.Where(w => w.TestsId == testId).Select(s => s.QuestionId).Distinct().ToList();
            var questions = Questions.Where(w => questionsIdArr.Contains(w.QuestionId)).ToList();

            var thisTestsItems = db.TestsItem.Where(w => w.TestsId == testId).ToList();

            var answers = db.Answers.ToList();
            var type = db.Tree.Where(w => w.TypeId == 4).ToList();
            var uploads = db.Uploads.ToList();

            List<ViewPassTest> questionList = new List<ViewPassTest>();

            foreach (var q in questions)
            {
                var a = answers.Where(x => x.QuestionId == q.QuestionId).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId }).ToList();              
                //a.Shuffle();

                var typeId = type.SingleOrDefault(w => w.TreeId == q.TypeId).TreeId;

                var typeName = type.SingleOrDefault(w => w.TreeId == q.TypeId).NameRu;

                var selectedAnswerId = db.TestsItem.Where(w => w.QuestionId == q.QuestionId && w.Selected != null && w.TestsId == testId).Select(s => s.AnswerId).ToArray();
                var selectedAnswers = answers.Where(w => selectedAnswerId.Contains(w.AnswerId)).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId }).ToList();
                var picturesList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == q.QuestionId).Select(s => s.UploadId).ToList();

                var textAnswer = thisTestsItems.First(s => s.QuestionId == q.QuestionId && s.TestsId == testId).Answers;

                var questionSubjectsId = db.TreeQuestion.Where(w => w.QuestionId == q.QuestionId && w.TypeId == 1).Select(s => s.TreeId).ToArray();
                var thisSubjects = subjects.Where(w => questionSubjectsId.Contains(w.TreeId)).ToList();

                var subQuestions = SubQuestions.Where(w=> w.Deleted != 1 && w.ParentQuestionId == q.QuestionId).ToList();

                var subQuestionsId = subQuestions.Select(s => s.QuestionId).ToArray();
                var subA = answers.Where(w => subQuestionsId.Contains(w.QuestionId)).ToList();

                List<ViewPassTest> subQuestionList = new List<ViewPassTest>();

                List<AnswerView> subAnswerAll = new List<AnswerView>();

                foreach (var sa in subA)
                {
                    subAnswerAll.Add(new AnswerView()
                    {
                        AnswerId = sa.AnswerId,
                        AnswerRu = sa.AnswerRu,
                    });

                }

                foreach (var sub in subQuestions)
                {
                    var answerSelected = answers.SingleOrDefault(s => s.QuestionId == sub.QuestionId);
                    var subAnswerItemSelected = db.TestsItem.SingleOrDefault(s => s.AnswerId == answerSelected.AnswerId && s.TestsId == testId && s.Answers == null); 
                    var subAnswerSelected = answers.SingleOrDefault(s => s.AnswerId == subAnswerItemSelected.AnswerId);

                    List<AnswerView> subAnswerThis = new List<AnswerView>();

                    subAnswerThis.Add(new AnswerView()
                    {
                        AnswerId = subAnswerSelected.AnswerId, 
                        AnswerRu = subAnswerSelected.AnswerRu,
                        value = subAnswerSelected.AnswerId,
                    });

                    subQuestionList.Add(new ViewPassTest()
                    {   
                        Question = sub,
                        Answers = subAnswerThis,
                        SubAnswers = subAnswerThis,
                        AnswersSelected = subAnswerThis,
                    });
                }

                AnswerView answerSingle = null;

                if (typeId == 11)
                {
                    answerSingle = selectedAnswers.SingleOrDefault();
                }

                questionList.Add(new ViewPassTest()
                {
                    TestTemplateId = testTemplateId,
                    NameRu = testTemplateName,
                    Question = q,
                    SubQuestion = subQuestionList,
                    Answers = a,
                    TypeId = typeId,
                    TypeName = typeName,
                    TestsId = testId,
                    Tree = thisSubjects,
                    LevelsList = levels,
                    AnswersSelected = selectedAnswers,
                    AnswerSingle = answerSingle,
                    PictureList = picturesList,
                    TextAnswer = textAnswer,
                    SubAnswers = subAnswerAll,
                });
            }

            ViewBag.TestTemplateId = testTemplateId;
            ViewBag.Progress = db.Tests.SingleOrDefault(s => s.TestId == testId).Progress;
            ViewBag.Finished = db.Tests.SingleOrDefault(s => s.TestId == testId).Finished;
            ViewBag.Time = time * 60000;
            ViewBag.TryCountNow = tryCount + 1;
            ViewBag.MaxCount = maxCount;


            return View(questionList);
        }

        public ActionResult FinTest(int testId = 0)
        {
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);           

            var thisTest = db.Tests.SingleOrDefault(s => s.TestId == testId);
            var thisUser = allUsers.SingleOrDefault(s => s.UserId == thisTest.UserId);
            var thisAttestation = db.Attestations.Find(thisTest.AttestationId);

            var thisTemplate = db.TestTemplate.FirstOrDefault(s => s.TestTemplateId == thisAttestation.TestTemplateId);

            var tree = db.Tree.Where(w => w.TypeId == 1 && w.Deleted != 1).ToList();
            var categoriesId = db.TestTemplateItem.Where(s => s.TestTemplateId == thisTemplate.TestTemplateId && s.CategoryType == 1).Select(s => s.CategoryId).ToArray();
            var categories = tree.Where(w => categoriesId.Contains(w.TreeId)).ToList();
            var categoriesParentsId = categories.Select(s => s.ParentId).ToArray();
            var categoriesParents0 = tree.Where(w => categoriesParentsId.Contains(w.TreeId)).ToList();

            var categoriesParents = categories.Where(w => w.ParentId == null).ToList();
            categoriesParents = categoriesParents.Concat(categoriesParents0).ToList();
            var categoriesChild = categories.Where(w => w.ParentId != null).ToList();

            var questionIdArr = db.TestsItem.Where(w => w.TestsId == testId && w.Answers == null).Select(s => s.QuestionId).ToArray();
            questionIdArr = questionIdArr.Distinct().ToArray();
            var allQuestions = db.Questions.Where(w => w.Deleted != 1 && questionIdArr.Contains(w.QuestionId)).ToList();

            var answers = db.Answers.ToList();

            var allRightAnswersCount = allQuestions.Count();// db.TestsItem.Where(w => w.TestsId == testId && w.Weight > 0).Count(); 
            var rightAnswersToQuestionIdArr = db.TestsItem.Where(w => w.TestsId == testId && w.AnswerId == w.Selected && w.Weight > 0).Select(s => s.QuestionId).ToArray();
            var rightquestionsIdCount = rightAnswersToQuestionIdArr.Distinct().Count();
            var rightQuestions = db.Questions.Where(w => rightAnswersToQuestionIdArr.Contains(w.QuestionId)).ToList();
            var wrongQuestions = allQuestions.Except(rightQuestions).ToList();

            var actualParentQuestionsId = allQuestions.Where(w => w.ParentQuestionId != null).Select(s => s.ParentQuestionId).Distinct().ToArray();
            var actualParentQuestions = allQuestions.Where(w => actualParentQuestionsId.Contains(w.QuestionId)).ToList();

            wrongQuestions = wrongQuestions.Except(actualParentQuestions).ToList();

            var uploads = db.Uploads.ToList();

            var testsItems = db.TestsItem.Where(w => w.TestsId == testId).ToList();
            var rightWeight = 0;

            var thisTargetPercent = db.TestTemplate.Find(thisAttestation.TestTemplateId).TargetProcent;

            var id = thisTest.TemplateId;

            var maxCount = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == id).TryCount;
            var tryCount = db.Tests.Where(w => w.TemplateId == id && w.UserId == appUser.UserId && w.Finished == 1).Count();
            var tryCountNow = maxCount - tryCount;

            var maxWeight = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == id).TargetProcent;

            List<SubCategoriesForDetailedReport> subCategories1 = new List<SubCategoriesForDetailedReport>();

           int? totalTestSumm = 0;
 
            foreach (var item1 in categoriesParents)
            {
                int? parentSummWeight = 0;
                var questionSumm1 = Sub(thisUser, thisAttestation.AttestationId, thisAttestation.CertificationLevelToPass, item1.TreeId, testId);
                List<SubCategoriesForDetailedReport> subCategories2 = new List<SubCategoriesForDetailedReport>();
                if (categories.Any(x => x.ParentId == item1.TreeId))
                {
                    var i = 0;
                    foreach (var item2 in categories.Where(x => x.ParentId == item1.TreeId).OrderBy(o => o.TreeId))
                    {
                        i++;
                        var questionSumm2 = Sub(thisUser, thisAttestation.AttestationId, thisAttestation.CertificationLevelToPass, item2.TreeId, testId);
                        parentSummWeight += questionSumm2.subCategoryResult;
                        List<SubCategoriesForDetailedReport> subCategories3 = new List<SubCategoriesForDetailedReport>();
                        if (categories.Any(x => x.ParentId == item2.TreeId))
                        {
                            foreach (var item3 in categories.Where(x => x.ParentId == item2.TreeId).OrderBy(o => o.TreeId))
                            {
                                var questionSumm3 = Sub(thisUser, thisAttestation.AttestationId, thisAttestation.CertificationLevelToPass, item2.TreeId);
                                subCategories3.Add(new SubCategoriesForDetailedReport() { subCategoryId = item3.TreeId, subCategoryName = item3.NameRu, subCategoryResult = questionSumm3.subCategoryResult, ViewQuestion = questionSumm3.ViewQuestion });
                            }
                            subCategories2.Add(new SubCategoriesForDetailedReport() { subCategoryId = item2.TreeId, subCategoryName = item2.NameRu, SubCategories = subCategories3, subCategoryResult = questionSumm2.subCategoryResult, ViewQuestion = questionSumm2.ViewQuestion });
                        }
                        else
                        {
                            subCategories2.Add(new SubCategoriesForDetailedReport() { subCategoryId = item2.TreeId, subCategoryName = item2.NameRu, subCategoryResult = questionSumm2.subCategoryResult, ViewQuestion = questionSumm2.ViewQuestion });
                        }
                    }
                    if (parentSummWeight > 0) {
                        parentSummWeight = parentSummWeight / i;
                        totalTestSumm +=  parentSummWeight;
                    };
                   
                    subCategories1.Add(new SubCategoriesForDetailedReport() { subCategoryId = item1.TreeId, subCategoryName = item1.NameRu, SubCategories = subCategories2, subCategoryResult = parentSummWeight, ViewQuestion = questionSumm1.ViewQuestion });
                }
                else
                {
                    subCategories1.Add(new SubCategoriesForDetailedReport() { subCategoryId = item1.TreeId, subCategoryName = item1.NameRu, subCategoryResult = parentSummWeight, ViewQuestion = questionSumm1.ViewQuestion });
                }
            }

            if(totalTestSumm > 0 ) { totalTestSumm = totalTestSumm / categoriesParents.Count(); }

            rightWeight = Convert.ToInt32(totalTestSumm);
            var testResult = rightWeight >= thisTargetPercent ? 1 : 0;
            var thisEmployeeTestMark = thisAttestation.empItems.FirstOrDefault(f => f.UserID == thisTest.UserId);
            thisEmployeeTestMark.Passed = testResult;

            // Записываем результат прохождения теста
            thisTest.Finished = 1;
            thisTest.WeightResult = rightWeight;
            thisTest.SuccessAnswerCount = rightquestionsIdCount;
            thisTest.WrongAnswerCount = allQuestions.Count() - rightquestionsIdCount;
            thisTest.QuestionCount = allQuestions.Count();
            db.Entry(thisTest).State = EntityState.Modified;
            db.SaveChanges();

            // Засчитываем прохождение аттестации если один из тестов набрал нужный процент
            //var thisAttestation = db.Attestations ==
            //db.AttestationEmployeeItems.SingleOrDefault(s => s.AttestationId ==  );

            ViewResultTest viewResultTest;

            viewResultTest = new ViewResultTest()
            {
                rightAnswersCount = rightquestionsIdCount,
                rightWeight = rightWeight,
                TryCount = tryCountNow,  
            };

            ViewBag.AllRightAnswersCount = allRightAnswersCount;
            ViewBag.ViewResultTest = viewResultTest;
            ViewBag.MaxWeight = maxWeight;

            // Формирование списка вопросов на которые были даны неправильные ответы
            List<ViewQuestion> ViewWrongQuestions = new List<ViewQuestion>();
            foreach (var q in wrongQuestions)
            {
                var picsId = uploads.Where(w => w.DocumentId == q.QuestionId).Select(s=> s.UploadId).ToList();
                ViewWrongQuestions.Add(new ViewQuestion() { QuestionId = q.QuestionId, Question = q, PictureList = picsId,  });
            }
            ViewBag.WrongQuestions = ViewWrongQuestions; 

            return View(subCategories1);
        }

        public  ActionResult FeedBack(int testTemplateId, int attestationId, int feedId = 0)
        {
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            var thisTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId);
            var thisTest = db.Feedback.Any(a => a.TemplateId == testTemplateId && a.UserId == appUser.UserId && a.Finished == 0);

            if (thisTest == true)
            {
                feedId = db.Feedback.SingleOrDefault(a => a.TemplateId == testTemplateId && a.UserId == appUser.UserId && a.Finished == 0).FeedbackId;
            }

            if (thisTest != true)
            {
                var tId = GenerateMyFeedBack(testTemplateId, attestationId);
                return RedirectToAction("FeedBack", new { testTemplateId = thisTemplate.TestTemplateId, attestationId, feedId = tId });
            }

            var checkFinished = db.Feedback.SingleOrDefault(s => s.FeedbackId == feedId);

            if (checkFinished.Finished == 1)
            {
                return RedirectToAction("Index");
            }

            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();
            var testTemplateName = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId).NameRu;

            var subjectsId = db.TestTemplateItem.Where(w => w.TestTemplateId == testTemplateId && w.CategoryType == 1).Select(s => s.CategoryId).ToList();
            var subjects = tree.Where(w => subjectsId.Contains(w.TreeId)).ToList();

            var Questions = db.Questions.Where(w => w.Deleted != 1 && w.Published == 1 && w.ParentQuestionId == null).ToList();
            var SubQuestions = Questions.Where(w => w.Deleted != 1 && w.Published == 1 && w.ParentQuestionId != null).ToList();

            var questionsIdArr = db.FeedbackItems.Where(w => w.FeedbackId == feedId).Select(s => s.QuestionId).ToArray();
            var questionsId = questionsIdArr.Distinct();
            var questions = Questions.Where(w => questionsId.Contains(w.QuestionId)).ToList();

            var thisFeedsItems = db.FeedbackItems.Where(w => w.FeedbackId == feedId).ToList();

            var answers = db.Answers.ToList();
            var type = db.Tree.Where(w => w.TypeId == 4).ToList();
            var uploads = db.Uploads.ToList();

            List<ViewPassTest> questionList = new List<ViewPassTest>();

            foreach (var q in questions)
            {
                var a = answers.Where(x => x.QuestionId == q.QuestionId).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId }).ToList();

                var typeId = type.SingleOrDefault(w => w.TreeId == q.TypeId).TreeId;

                var typeName = type.SingleOrDefault(w => w.TreeId == q.TypeId).NameRu;

                var selectedAnswerId = db.FeedbackItems.Where(w => w.QuestionId == q.QuestionId && w.Selected != null && w.FeedbackId == feedId).Select(s => s.AnswerId).ToArray();
                var selectedAnswers = answers.Where(w => selectedAnswerId.Contains(w.AnswerId)).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId }).ToList();

                var textAnswer = thisFeedsItems.First(s => s.QuestionId == q.QuestionId && s.FeedbackId == feedId).Text;

                var questionSubjectsId = db.TreeQuestion.Where(w => w.QuestionId == q.QuestionId && w.TypeId == 1).Select(s => s.TreeId).ToArray();
                var thisSubjects = subjects.Where(w => questionSubjectsId.Contains(w.TreeId)).ToList();

                AnswerView answerSingle = null;

                if (typeId == 11 || typeId == 12)
                {
                    answerSingle = selectedAnswers.SingleOrDefault();
                }

                questionList.Add(new ViewPassTest()
                {
                    TestTemplateId = testTemplateId,
                    NameRu = testTemplateName,
                    Question = q,
                    Answers = a,
                    TypeId = typeId,
                    TypeName = typeName,
                    TestsId = feedId,
                    Tree = thisSubjects,
                    AnswersSelected = selectedAnswers,
                    AnswerSingle = answerSingle,
                    TextAnswer = textAnswer,
                    UserId = appUser.UserId,
                });
            }

            ViewBag.TestTemplateId = testTemplateId;

            return View(questionList);
        }

        public int GenerateMyTest(int testTemplateId, int attestationId)
        {
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();
            var levelId = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId).LevelId;

            var subjectsId = db.TestTemplateItem.Where(w => w.TestTemplateId == testTemplateId && w.CategoryType == 1).Select(s => s.CategoryId).ToList();
            var subjects = tree.Where(w => subjectsId.Contains(w.TreeId)).ToList();
            var maxDuration = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == testTemplateId).Duration;

            var allSubQwestion = db.Questions.Where(w => w.Deleted != 1 && w.ParentQuestionId != null).ToList();
            var allAnswers = db.Answers.Where(w => w.Deleted != 1).ToList();

            var questionCount = 0;

            // Создание записи в которой будут сохраняться результаты прохождения теста
            var tests = new Tests()
            {
                CreateDate = DateTime.Now,
                UserId = appUser.UserId,
                TemplateId = testTemplateId,
                Duration = maxDuration,
                Finished = 0,
                AttestationId = attestationId,
                NameRu = appUser.NameRu,
                CheckedText = 0,
                PracticalExamination = 0,
                VerbalExamination = 0,
                WeightResult = 0,
            };
            db.Tests.Add(tests);
            db.SaveChanges();

            var testsId = tests.TestId;

            foreach (var sub in subjects)
            {
                var questionQuantity = db.TestTemplateItem.SingleOrDefault(s => s.CategoryId == sub.TreeId && s.TestTemplateId == testTemplateId).QuestionQuantity;
                var questionIdArr = db.TreeQuestion.Where(w => w.TreeId == sub.TreeId).Select(s => s.QuestionId).ToArray();
                var questions = db.Questions.Where(w => w.Deleted != 1 && w.Published == 1 && w.ParentQuestionId == null && questionIdArr.Contains(w.QuestionId)).ToList();
                int subjectLehgth = db.TreeQuestion.Where(w => w.TreeId == sub.TreeId).Count();

                subjectLehgth = questions.Count();

                questions.Shuffle();

                if (subjectLehgth >= questionQuantity)
                {
                    var r = subjectLehgth - questionQuantity.Value;
                    questions.RemoveRange(questionQuantity.Value - 1, r);
                }

                foreach (var question in questions)
                {
                    var subQwestions = allSubQwestion.Where(w => w.ParentQuestionId == question.QuestionId).ToList();
                    var answers = allAnswers.Where(w => w.QuestionId == question.QuestionId).ToList();
                    answers.Shuffle();

                    foreach (var answer in answers)
                    {
                        var testsItem = new TestsItem()
                        {
                            TestsId = testsId,
                            AnswerId = answer.AnswerId,
                            QuestionId = question.QuestionId,
                            UserId = appUser.UserId,
                            Weight = answer.Weight,
                            CreateDate = DateTime.Now,
                            CategoryId = sub.TreeId,

                        };
                        db.TestsItem.Add(testsItem);
                        db.SaveChanges();

                        if (question.TypeId == 105)
                        {
                            testsItem.Answers = "Ваш ответ на вопрос, в пределах 1500 символов";
                            db.SaveChanges();
                        }

                    }

                    // Добавление субвопросов
                    foreach (var subQwestion in subQwestions)
                    {
                        var subAnswers = allAnswers.SingleOrDefault(w => w.QuestionId == subQwestion.QuestionId);
                        var testsItem = new TestsItem()
                        {
                            TestsId = testsId,
                            AnswerId = subAnswers.AnswerId,
                            QuestionId = subQwestion.QuestionId,
                            UserId = appUser.UserId,
                            Weight = subAnswers.Weight,
                            CreateDate = DateTime.Now,
                            CategoryId = sub.TreeId,
                            ParentId = question.ParentQuestionId,
                        };
                        db.TestsItem.Add(testsItem);
                        db.SaveChanges();
                    }
                }
            }


            tests.QuestionCount = questionCount;
            db.SaveChanges();
            return testsId;
        }

        public int GenerateMyFeedBack(int testTemplateId, int attestationId)
        {
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();

            var subjectsId = db.TestTemplateItem.Where(w => w.TestTemplateId == testTemplateId && w.CategoryType == 1).Select(s => s.CategoryId).ToList();
            var subjects = tree.Where(w => subjectsId.Contains(w.TreeId)).ToList();

            var allAnswers = db.Answers.Where(w => w.Deleted != 1).ToList();

            //var questionCount = 0;

            // Создание записи в которой будут сохраняться результаты прохождения теста
            var feed = new Feedback()
            {
                CreateDate = DateTime.Now,
                UserId = appUser.UserId,
                TemplateId = testTemplateId,
                Finished = 0,
                AttestationId = attestationId,
                NameRu = appUser.NameRu,
            };
            db.Feedback.Add(feed);
            db.SaveChanges();

            var feedId = feed.FeedbackId;

            foreach (var sub in subjects)
            {
                var questionQuantity = db.TestTemplateItem.SingleOrDefault(s => s.CategoryId == sub.TreeId && s.TestTemplateId == testTemplateId).QuestionQuantity;
                var questionIdArr = db.TreeQuestion.Where(w => w.TreeId == sub.TreeId).Select(s => s.QuestionId).ToArray();
                var questions = db.Questions.Where(w => w.Deleted != 1 && w.Published == 1 && w.ParentQuestionId == null && questionIdArr.Contains(w.QuestionId)).ToList();
                int subjectLehgth = db.TreeQuestion.Where(w => w.TreeId == sub.TreeId).Count();

                subjectLehgth = questions.Count();

                questions.Shuffle();

                if (subjectLehgth >= questionQuantity)
                {
                    var r = subjectLehgth - questionQuantity.Value;
                    questions.RemoveRange(questionQuantity.Value - 1, r);
                }

                foreach (var question in questions)
                {

                    var answers = allAnswers.Where(w => w.QuestionId == question.QuestionId).ToList();
                    answers.Shuffle();

                    foreach (var answer in answers)
                    {
                        var feedbackItem = new FeedbackItems()
                        {
                            FeedbackId = feedId,
                            AnswerId = answer.AnswerId,
                            QuestionId = question.QuestionId,
                            UserId = appUser.UserId,
                            Weight = answer.Weight,
                            CreateDate = DateTime.Now,

                        };
                        db.FeedbackItems.Add(feedbackItem);
                        db.SaveChanges();

                        if (question.TypeId == 105)
                        {
                            feedbackItem.Text = "Ваш ответ на вопрос, в пределах 1500 символов";
                            db.SaveChanges();
                        }

                    }

                }
            }

            db.SaveChanges();
            return feedId;
        }

        public ActionResult TryEnd()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetUser(string search, int id = 0)
        {
            if (!String.IsNullOrEmpty(search) && id == 0)
            {
                var empArr = db.L_User.Where(s => s.NameRu.ToLower().Contains(search.ToLower())).ToList();
                var emp = empArr.Select(s => new { UserId = s.UserId, Name = s.NameRu }).ToList();

                return Json(new { emp }, JsonRequestBehavior.AllowGet);
            }

            if(id != 0)
            {
                var emp = db.L_User.Where(s => s.UserId == id || s.OldUserId == id).Select(s=> new { Id = s.UserId, Name = s.NameRu });
                return Json(new { emp }, JsonRequestBehavior.AllowGet);
            }

            return Json(String.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetUserOut(string search, int id = 0)
        {
            if (!String.IsNullOrEmpty(search) && id == 0)
            {
                var empArr = db.L_User.Where(s => s.NameRu.ToLower().Contains(search.ToLower()) || s.PrivateEmail.ToLower().Contains(search.ToLower())).ToList();
                var emp = empArr.Select(s => new { UserId = s.UserId, Name = s.NameRu }).ToList();
                return Json(new { emp }, JsonRequestBehavior.AllowGet);
            }

            if (id != 0)
            {
                var emp = db.L_User.Where(s => s.UserId == id || s.OldUserId == id).Select(s => new { Id = s.UserId, Name = s.NameRu });
                return Json(new { emp }, JsonRequestBehavior.AllowGet);
            }

            return Json(String.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddNewEmployeeItemToCertification(int UserId, int attestId)
        {
            AttestationEmployeeItems item = new AttestationEmployeeItems();

            var attestation = db.Attestations.Find(attestId);
            var employee = db.L_User.FirstOrDefault(s => s.UserId == UserId || s.OldUserId == UserId);

            if (attestation != null && employee != null)
            {
                item.AttestationId = attestation.AttestationId;
                item.UserID = employee.UserId;
                db.AttestationEmployeeItems.Add(item);
                db.SaveChanges();

                var t = new EmployeeInfo();
                t.rowNumber = item.AttestationItemId;
                t.userName = employee.NameRu;
                t.LMUserName = db.L_User.FirstOrDefault(x => x.UserId == employee.LineManagerUserId || x.OldUserId == employee.LineManagerUserId).NameRu;
                t.positionName = h_db.Position.FirstOrDefault(x => x.PositionId == employee.JobTitleId.Value).NameRu;
                t.divisionName = h_db.Division.FirstOrDefault(x => x.DivisionId == employee.DivisionId).NameRu;
                return Json(t, JsonRequestBehavior.AllowGet);

            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddNewEmployeeItemToCertificationOut(int UserId, int attestId)
        {
            AttestationEmployeeItems item = new AttestationEmployeeItems();

            var attestation = db.Attestations.Find(attestId);
            var employee = db.L_User.FirstOrDefault(s => s.UserId == UserId);

            if (attestation != null && employee != null)
            {
                item.AttestationId = attestation.AttestationId;
                item.UserID = employee.UserId;
                item.SUserId = employee.Id;
                db.AttestationEmployeeItems.Add(item);
                db.SaveChanges();

                var t = new EmployeeInfo();
                t.rowNumber = item.AttestationItemId;
                t.userName = employee.NameRu;
                //t.LMUserName = h_db.User.FirstOrDefault(x => x.UserId == employee.LineManagerUserId).NameRu;
                //t.positionName = h_db.Position.FirstOrDefault(x => x.PositionId == employee.JobTitleId.Value).NameRu;
                return Json(t, JsonRequestBehavior.AllowGet);

            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult AddAllLMEmployees(int LMId, int attestId)
        {
            //var employeesHRM = h_db.User.Where(w => w.Fired != true && w.AccountAD != null).ToList();

            //L_User lUser = new L_User() { };
            //foreach (var hrmUser in employeesHRM)
            //{
            //    bool checkLocalUser = db.L_User.Any(a => a.OldUserId == hrmUser.UserId);

            //    if (!checkLocalUser)
            //    {
            //        lUser.AccountAD = hrmUser.AccountAD;
            //        lUser.Admit = 1;
            //        lUser.BranchId = hrmUser.BranchId;
            //        lUser.CompanyId = 0;
            //        lUser.DepartmentId = hrmUser.DepartmentId;
            //        lUser.DivisionId = hrmUser.DivisionId;
            //        lUser.JobTitleId = hrmUser.JobTitleId;
            //        lUser.OldUserId = hrmUser.UserId;
            //        lUser.PrivateEmail = hrmUser.PrivateEmail;
            //        lUser.LineManagerUserId = hrmUser.LineManagerUserId;
            //        lUser.NameRu = hrmUser.NameRu;

            //        db.L_User.Add(lUser);
            //        db.SaveChanges();
            //    }


            //}


            AttestationEmployeeItems item = new AttestationEmployeeItems();
            var attestation = db.Attestations.Find(attestId);
            var LM = db.L_User.FirstOrDefault(f => f.UserId == LMId);
            var employees = db.L_User.Where(s => s.LineManagerUserId == LM.OldUserId).ToArray();

            if (employees.Count() == 0)
            {

                var employeesHRM = h_db.User.Where(w => w.Fired != true && w.LineManagerUserId == LM.OldUserId).ToList();
                L_User lUser = new L_User() { };

                foreach (var hrmUser in employeesHRM)
                {
                    lUser.AccountAD = hrmUser.AccountAD;
                    lUser.Admit = 1;
                    lUser.BranchId = hrmUser.BranchId;
                    lUser.CompanyId = 0;
                    lUser.DepartmentId = hrmUser.DepartmentId;
                    lUser.DivisionId = hrmUser.DivisionId;
                    lUser.JobTitleId = hrmUser.JobTitleId;
                    lUser.OldUserId = hrmUser.UserId;
                    lUser.PrivateEmail = hrmUser.PrivateEmail;
                    lUser.LineManagerUserId = hrmUser.LineManagerUserId;
                    lUser.NameRu = hrmUser.NameRu;

                    db.L_User.Add(lUser);
                    db.SaveChanges();
                }

                employees = db.L_User.Where(s => s.LineManagerUserId == LMId).ToArray();

            }

            List<EmployeeInfo> info = new List<EmployeeInfo>();

            if (attestation != null && employees != null)
            {
                foreach (var empItem in employees)
                {
                    item = new AttestationEmployeeItems();
                    item.AttestationId = attestation.AttestationId;
                    item.UserID = empItem.UserId;
                    db.AttestationEmployeeItems.Add(item);
                    db.SaveChanges();

                    var t = new EmployeeInfo();
                    t.rowNumber = item.AttestationItemId;
                    t.userName = empItem.NameRu;
                    t.LMUserName = LM.NameRu;
                    t.positionName = h_db.Position.FirstOrDefault(x => x.PositionId == empItem.JobTitleId.Value).NameRu;
                    t.divisionName = h_db.Division.FirstOrDefault(x => x.DivisionId == empItem.DivisionId).NameRu;
                    info.Add(t);

                }

                return Json(info, JsonRequestBehavior.AllowGet);
            }

            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public string DeleteEmployeeItem(int id)
        {
            if (id != 0)
            {
                var empItem = db.AttestationEmployeeItems.Find(id);
                db.AttestationEmployeeItems.Remove(empItem);
                db.SaveChanges();

                return "ok";
            }

            return "error";
        }

        [HttpGet]
        public JsonResult GetTemplate(string search)
        {
            if (!String.IsNullOrEmpty(search))
            {
                var temp = db.TestTemplate.Where(x => x.NameRu.ToLower().Contains(search.ToLower()) && x.Saved != -1 && x.TypeTemplateId == null);
                var tempArr = temp.Select(s => new { TestTemplateId = s.TestTemplateId, Name = s.NameRu, Level = s.LevelId }).ToList();
                return Json(new { tempArr }, JsonRequestBehavior.AllowGet);
            }

            return Json(String.Empty, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public JsonResult GetFeedbackTemplate(string search)
        {
            if (!String.IsNullOrEmpty(search))
            {
                var temp = db.TestTemplate.Where(x => x.NameRu.ToLower().Contains(search.ToLower()) && x.Saved != -1 && x.TypeTemplateId == 1);
                var tempArr = temp.Select(s => new { FeedTemplateId = s.FeedTemplateId, Name = s.NameRu, Level = s.LevelId }).ToList();

                return Json(new { tempArr }, JsonRequestBehavior.AllowGet);
            }

            return Json(String.Empty, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public JsonResult GetCertifacation(string search)
        {
            if (!String.IsNullOrEmpty(search))
            {
                var att = db.Attestations.Where(x => x.AttestationName.ToLower().Contains(search.ToLower()) && x.IsDeleted != true);
                var attArr = att.Select(s => new { AttestationId = s.AttestationId, Name = s.AttestationName }).ToList();
                return Json(new { attArr }, JsonRequestBehavior.AllowGet);
            }

            return Json(String.Empty, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public JsonResult GetCertificationCreator(string search, int tabs)
        {
            if (!String.IsNullOrEmpty(search))
            {
                DateTime current = DateTime.Now;

                if (tabs == 0)
                {
                    var certifications = db.Attestations.Where(x => x.IsSubmit == true && x.FinishTime > current && x.IsDeleted != true).Select(s=>s.CreatedBy).ToArray();
                    var creators = db.L_User.Where(w => certifications.Contains(w.UserId) || certifications.Contains(w.OldUserId)).Select(s=> new { Id = s.UserId, Name = s.NameRu }).ToArray();
                    var creatorArr = creators.Where(w => w.Name.ToLower().Contains(search.ToLower())).ToArray();

                    return Json(new { creatorArr }, JsonRequestBehavior.AllowGet);
                }

                if (tabs == 1)
                {
                    var certifications = db.Attestations.Where(x => x.IsSubmit == false && x.IsDeleted != true).Select(s => s.CreatedBy).ToArray();
                    var creators = db.L_User.Where(w => certifications.Contains(w.UserId) || certifications.Contains(w.OldUserId)).Select(s => new { Id = s.UserId, Name = s.NameRu }).ToArray();
                    var creatorArr = creators.Where(w => w.Name.ToLower().Contains(search.ToLower())).ToArray();

                    return Json(new { creatorArr }, JsonRequestBehavior.AllowGet);
                }

                if (tabs == 2)
                {
                    var certifications = db.Attestations.Where(x => x.FinishTime < current && x.IsSubmit == true && x.IsDeleted != true).Select(s => s.CreatedBy).ToArray();
                    var creators = db.L_User.Where(w => certifications.Contains(w.UserId) || certifications.Contains(w.OldUserId)).Select(s => new { CreatorId = s.UserId, Name = s.NameRu }).ToArray();
                    var creatorArr = creators.Where(w => w.Name.ToLower().Contains(search.ToLower())).ToArray();

                    return Json(new { creatorArr }, JsonRequestBehavior.AllowGet);
                }
               
            }

            return Json(String.Empty, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public JsonResult GetCertificationForReports(string search)
        {
            if(!String.IsNullOrEmpty(search))
            {
                var att = db.Attestations.Where(x => x.AttestationName.ToLower().Contains(search.ToLower()) && x.IsDeleted != true && x.IsSubmit == true);
                var attArr = att.Select(s=> new { AttestationId = s.AttestationId, Name = s.AttestationName }).ToList();
                return Json(new { attArr }, JsonRequestBehavior.AllowGet);
            }
            return Json(String.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetSelectedManager(int managerId)
        {
            if (managerId != 0)
            {
                var temp = db.L_User.FirstOrDefault(f => f.UserId == managerId || f.OldUserId == managerId);
                var t = new t();
                t.Id = temp.UserId;
                t.NameRu = temp.NameRu;
                return Json(new { t = t }, JsonRequestBehavior.AllowGet);
            }
            return Json(String.Empty, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public string  DeleteAllEmployee(int attestId)
        {
            if(attestId != 0)
            {
                var items = db.AttestationEmployeeItems.Where(w => w.AttestationId == attestId).ToArray();
                db.AttestationEmployeeItems.RemoveRange(items);
                db.SaveChanges();

                return "ok";
            }

            return "error";
        }

        //Потом сделать глобальную функцию
        public SubCategoriesForDetailedReport Sub(L_User user, int CertificationId = 0, int? Level = 0, int categoryId = 0, int testid = 0)
        {
            var questions = db.Questions.Where(w => w.Deleted != 1).ToList();

            var uploads = db.Uploads.ToList();
            var attestation = db.Attestations.SingleOrDefault(w => w.AttestationId == CertificationId);
            var catTree = db.Tree.SingleOrDefault(s => s.TreeId == categoryId);

            var actualTestsItems = db.TestsItem.Where(w => w.TestsId == testid).ToList();
            var actualTestsItemsQuestionsId = actualTestsItems.Where(w => w.Answers == null).Select(s => s.QuestionId).Distinct().ToArray();

            var actualQuestions = questions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

            var actualParentQuestionsId = actualQuestions.Where(w=> w.ParentQuestionId != null).Select(s => s.ParentQuestionId).Distinct().ToArray();
            var actualParentQuestions = actualQuestions.Where(w => actualParentQuestionsId.Contains(w.QuestionId)).ToList();

            actualQuestions = actualQuestions.Except(actualParentQuestions).ToList();

            var actualTestsItemsTextQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
            var actualTextQuestions = db.Questions.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).ToList();
            var answers = db.Answers.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

            var bestTest = db.Tests.SingleOrDefault(s => s.TestId == testid);//attestationTests.Where(w => w.UserId == user.UserId && w.Finished == 1).OrderByDescending(o => o.WeightResult).First();

            var bestTestItems = actualTestsItems.Where(w => w.CategoryId == catTree.TreeId && w.TestsId == bestTest.TestId).ToList();
            var bestTestQuestionsId = bestTestItems.Select(s => s.QuestionId).Distinct().ToArray();

            var bestTestQuestions = actualQuestions.Where(w => bestTestQuestionsId.Contains(w.QuestionId)).ToList();
            var bestTestTextQuestionsItems = actualTestsItems.Where(w => bestTestQuestionsId.Contains(w.QuestionId)).ToList();

            List<ViewQuestion> thisQuestions = new List<ViewQuestion>();
            var categoryRightProcent = 0;
            foreach (var q in bestTestQuestions)
            {
                var questionRightProcent = 0; //Сборщик правильности
                var answersForQuestion = answers.Where(x => x.QuestionId == q.QuestionId).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();
                var picturesList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == q.QuestionId).Select(s => s.UploadId).ToList();

                var selectedAnswerId = bestTestItems.Where(w => w.QuestionId == q.QuestionId && w.TestsId == bestTest.TestId && w.Selected != null).Select(s => s.Selected).ToArray();
                //Отмеченные вопросы по этому вопросу
                var selectedAnswers = answers.Where(w => selectedAnswerId.Contains(w.AnswerId)).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();
                var selectedAnswersCount = selectedAnswers.Count();

                var questionAnswers = answers.Where(w => w.QuestionId == q.QuestionId).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();
                var rightAnswers = questionAnswers.Where(w => w.Weight > 0).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();

                // Делим 100% на количество вариантов ответов если вопрос где несколько вариантов ответов
                var weightItem = 0;
                if (selectedAnswersCount > 1)
                {
                    weightItem = 100 / answersForQuestion.Count;
                }

                // Подсчет правильности
                var ii = 0;
                foreach (var a1 in answersForQuestion)
                {
                    foreach (var a2 in selectedAnswers)
                    {
                        if (a1.AnswerId == a2.AnswerId) // Сравниваем ответ пользователя с ответами в базе
                        {
                            ii++;
                            if (a1.Weight > 0)
                            {
                                //a1.Weight = 100;
                                questionRightProcent = questionRightProcent + a1.Weight;
                            }
                            else
                            {
                                questionRightProcent = questionRightProcent - weightItem;
                            }

                        }
                    }
                }
                // Блокировка ухода в отрицательное значение
                if (questionRightProcent < 0)
                {
                    questionRightProcent = 0;
                }

                categoryRightProcent = categoryRightProcent + questionRightProcent;

                thisQuestions.Add(new ViewQuestion()
                {
                    QuestionId = q.QuestionId,
                    Question = q,
                    AnswersSelected = selectedAnswers,
                    Answers = questionAnswers,
                    RightAnswers = rightAnswers,
                    PictureList = picturesList,
                    QuestionRightProcent = questionRightProcent,
                });

            }

            if (categoryRightProcent > 0) { categoryRightProcent = categoryRightProcent / bestTestQuestions.Count(); }

            SubCategoriesForDetailedReport subCategoriesForDetailedReport = new SubCategoriesForDetailedReport()
            {
                ViewQuestion = thisQuestions,
                subCategoryResult = categoryRightProcent
            };

            return subCategoriesForDetailedReport;
        }

    }

    public static class Cc
    {
        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }




}