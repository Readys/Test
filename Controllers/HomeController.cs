using Microsoft.Extensions.Caching.Memory;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTS.Models;
using TTS.Tools;


//using OfficeOpenXml;

namespace TTS.Controllers
{
    public class HomeController : Controller
    {
        private TTSEntities db = new TTSEntities();
        private HRMEntities h_db = new HRMEntities();
        public const int testUser = 0; //Тестовый вход под любым пользователем//598 181 211 783 Мазанов - 471 37 Капацин Антон-174 39
        //private IMemoryCache _cache;


        //public HomeController(IMemoryCache memoryCache)
        //{
        //    _cache = memoryCache;
        //}

        public ActionResult Index(int tabs = 3, int page = 1)
        {
            //Кеширование юзеров

            //List<L_User> usersList = new List<L_User>();

            //// Look for cache key.
            //if (!_cache.TryGetValue(CacheKeys.Users, out usersList))
            //{
            //    // Key not in cache, so get data.
            //    usersList = db.L_User.Where(w => w.AccountAD != null || w.PrivateEmail != null).ToList();

            //    // Set cache options.
            //    var cacheEntryOptions = new MemoryCacheEntryOptions()
            //        // Keep in cache for this time, reset time if accessed.
            //        .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            //    // Save data in cache.
            //    _cache.Set(CacheKeys.Users, usersList, cacheEntryOptions);
            //}

            // Подключение пользователя
            #region SetUser
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return RedirectToAction("Register", "Account", new { area = "" });
                //return RedirectToAction("Login");
                //return View("/Account/Login");
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();
            #endregion

            var CurrentDateTime = DateTime.Now;

            var tree = db.Tree.Where(w => w.Deleted != 1).ToList();

            List<Attestation> allAttestations = db.Attestations.Where(w => w.AttestationName != null && w.TestTemplateId > 0 && w.IsDeleted != true).ToList();

            List<ViewAttestation> attestationList = new List<ViewAttestation>();

            var testTemplateItem = db.TestTemplateItem.ToList();
            var feedback = db.Feedback.ToList();
            var allAttestationEmployeeItems = db.AttestationEmployeeItems.ToList();

            foreach (var item in allAttestations)
            {
                #region OldUserImpot
                //Импорт пользователей с HRM когда либо участвовавших в аттестациях
                //var olduser = h_db.User.Where(w => w.Fired != true && w.AccountAD != null).ToList();

                //var allAttUsersId = db.AttestationEmployeeItems.Where(w => w.AttestationId == item.AttestationId).Select(s => s.UserID).ToArray();
                //var allActualUsers = olduser.Where(w => allAttUsersId.Contains(w.UserId)).ToList();

                //List<L_User> userList = new List<L_User>();

                //foreach (var user in allActualUsers)
                //{
                //    var checkAccount = db.L_User.FirstOrDefault(x => x.AccountAD.ToUpper() == user.AccountAD.ToUpper());
                //    if (checkAccount == null)
                //    {
                //        db.L_User.Add(new L_User()
                //        {
                //            Admit = 1,
                //            NameRu = user.NameRu,
                //            JobTitleId = user.JobTitleId,
                //            PrivateEmail = user.PrivateEmail,
                //            AccountAD = user.AccountAD,
                //            BranchId = user.BranchId,
                //            DepartmentId = user.DepartmentId,
                //            DivisionId = user.DivisionId,
                //            LineManagerUserId = user.LineManagerUserId,
                //            OldUserId = user.UserId,
                //        });
                //        db.SaveChanges();
                //    }
                //}
                //Импорт пользователей с HRM когда либо участвовавших в аттестациях
                #endregion

                var thisTestTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == item.TestTemplateId);

                var allCategoryList = tree.Where(w => w.TypeId == 1 && w.Deleted != 1).ToList();
                var categoryListArr = testTemplateItem.Where(w => item.TestTemplateId == w.TestTemplateId && w.CategoryType == 1).Select(w => w.CategoryId).ToList();
                var departmentsListArr = testTemplateItem.Where(w => w.TestTemplateId == item.TestTemplateId && w.CategoryType == 2).Select(s => s.CategoryId).ToList();
                var levelsListArr = testTemplateItem.Where(w => item.TestTemplateId == w.TestTemplateId && w.CategoryType == 3).Select(w => w.CategoryId).ToList();

                var categoryList = tree.Where(w => categoryListArr.Contains(w.TreeId)).ToList();
                var departmentsList = tree.Where(w => departmentsListArr.Contains(w.TreeId)).ToList();
                var levels = tree.Where(w => levelsListArr.Contains(w.TreeId)).ToList();
                var duration = thisTestTemplate.Duration;
                var attestationEmployeeItems = allAttestationEmployeeItems.Where(w => w.AttestationId == item.AttestationId).ToList();

                var managerApprove = allAttestationEmployeeItems.Any(s => s.AttestationId == item.AttestationId && s.UserID == appUser.UserId && s.ManagerApprove == true);

                var testsListArr = db.Tests.Where(w => w.AttestationId == item.AttestationId).Select(s => s.TestId).ToArray();
                var textanswersList = db.TestsItem.Where(w => w.Answers != null && testsListArr.Contains(w.TestsId)).ToList();
                var needMark = textanswersList.Count();

                attestationList.Add(new ViewAttestation()
                {
                    Attestation = item,
                    TestTemplateId = item.TestTemplateId,
                    AttestationName = item.AttestationName,
                    TestTemplateName = thisTestTemplate.NameRu,
                    Duration = duration,
                    LevelsList = levels,
                    DepartmentList = departmentsList,
                    SelectCategoryList = categoryList,
                    TryCount = thisTestTemplate.TryCount,
                    AllCategoryList = allCategoryList,
                    AttestationEmployeeItems = attestationEmployeeItems,
                    TestTemplate = thisTestTemplate,
                    ManagerApprove = managerApprove,
                    NeedMark = needMark,
                    Feedback = feedback,
                    FeedTemplateId = item.FeedbackId,
                });
            }

            var allAttestationList = attestationList;

            switch (tabs)
            {
                // Все аттестации
                case 1:
                    if (access.Any())
                    {
                        attestationList = attestationList.ToList();
                    }
                    break;
                case 2: // //Персональный архивный список личных аттестаций
                    var attestationIdArr3 = db.AttestationEmployeeItems.Where(w => w.UserID == appUser.UserId).Select(s => s.AttestationId).ToArray();
                    attestationList = attestationList.Where(w => attestationIdArr3.Contains(w.Attestation.AttestationId) && w.Attestation.FinishTime < DateTime.Now).ToList();
                    break;
                case 3: //Персональный список активных личных аттестаций
                    var attestationIdArr4 = db.AttestationEmployeeItems.Where(w => w.UserID == appUser.UserId).Select(s => s.AttestationId).ToArray();
                    attestationList = attestationList.Where(w => attestationIdArr4.Contains(w.Attestation.AttestationId) && w.Attestation.FinishTime > DateTime.Now).ToList();
                    break;
                default:


                    break;
            }

            ViewBag.Title = "Главная";

            ViewBag.currentTime = CurrentDateTime;
            ViewBag.Tree = tree;
            ViewBag.AppUserId = appUser.UserId;
            ViewBag.Tests = db.Tests.ToList();
            ViewBag.Access = access.ToList();
            ViewBag.tabs = tabs;
            ViewBag.Attestation = allAttestations;
            ViewBag.AllAttestationList = allAttestationList;
            ViewBag.AllAttestationEmployeeItems = allAttestationEmployeeItems;



            return View(attestationList);
        }

        //bool Admin = admin.UserId == appUser.UserId;
        //bool oneOfSeven = sevenGods.Contains(appUser.UserId);
        //bool oneOfLMUsers = AttestationLMUsers.Contains(appUser.UserId);

        //List<Attestation> allAttestationsForManager = new List<Attestation>();

        //if (Admin)
        //{
        //    allAttestationsForManager = db.Attestations.Where(w => w.IsSubmit == true && w.IsDeleted != true).ToList();
        //}

        //if(oneOfSeven)
        //{
        //    allAttestationsForManager = db.Attestations.Where(x => sevenGods.Contains(x.CreatedBy)).ToList();
        //}
        //if(oneOfLMUsers && !oneOfSeven)
        //{
        //    allAttestationsForManager = db.Attestations.Where(x => x.LMUserId == appUser.UserId).ToList();
        //}
        public ActionResult TextTest(int page = 1)
        {
            // Подключение пользователя
            #region SetUser
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => (x.UserId == appUser.UserId || x.UserId == appUser.OldUserId) && (x.AccessLevelId == 1 || x.RoleId == 2)).ToList();
            var admin = db.UserInRole.FirstOrDefault(x => x.AccessLevelId == 1);
            var sevenGods = db.UserInRole.Where(x => x.AccessLevelId == 2).Select(x => x.UserId);
            var AttestationLMUsers = db.Attestations.Select(x => x.LMUserId);
            #endregion

            var accessCheck = access.Count();
            var tests = db.Tests.ToList();

            bool Admin = admin.UserId == appUser.UserId || admin.UserId == appUser.OldUserId;
            bool oneOfSeven = sevenGods.Contains(appUser.UserId) || sevenGods.Contains(appUser.OldUserId);
            bool oneOfLMUsers = AttestationLMUsers.Contains(appUser.UserId) || AttestationLMUsers.Contains(appUser.OldUserId);

            List<Attestation> allAttestationsForManager = new List<Attestation>();

            if (Admin)
            {
                allAttestationsForManager = db.Attestations.Where(w => w.IsSubmit == true && w.IsDeleted != true).ToList();
                //allAttestationsForManager = 
            }

            if (oneOfSeven)
            {
                allAttestationsForManager = db.Attestations.Where(x => sevenGods.Contains(x.CreatedBy) && x.IsSubmit == true && x.IsDeleted != true).ToList();
            }
            if (oneOfLMUsers && !oneOfSeven && !Admin)
            {
                allAttestationsForManager = db.Attestations.Where(x => x.LMUserId == appUser.UserId && x.IsDeleted != true).ToList();
            }
            if (access.Where(w => w.RoleId == 2).Count() > 0)
            {
                allAttestationsForManager = db.Attestations.Where(w => w.IsSubmit == true && w.IsDeleted != true).ToList();
            }


            //List<Attestation> allAttestationsForManager = db.Attestations.Where(w => w.AttestationName != null && w.IsDeleted != true).ToList();
            allAttestationsForManager = allAttestationsForManager.Where(w => (w.TestTemplateId > 0 && w.LMUserId == appUser.UserId) || accessCheck > 0).ToList();
            List<ViewAttestation> attestationList = new List<ViewAttestation>();
            List<ViewAttestationReport> attestationList2 = new List<ViewAttestationReport>();

            List<L_User> allActualUsers = new List<L_User>();
            //var ThisTestList = db.Tests.Where(w => w.AttestationId == attestationId && w.UserId == appUser.UserId).ToList();

            //1. Уровень аттестации
            foreach (var item in allAttestationsForManager)
            {

                var attestationId = item.AttestationId;

                var attestationsUsers = db.AttestationEmployeeItems.Where(w => w.ManagerApprove == true && w.AttestationId == item.AttestationId).ToList();
                //var thisAttestation = db.Attestations.SingleOrDefault(s => s.AttestationId == item.AttestationId);

                int? targetProcent = 0;

                var thisTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == item.TestTemplateId);
                if (thisTemplate != null)
                {
                    targetProcent = thisTemplate.TargetProcent;
                }

                var attestationUsersId = attestationsUsers.Select(s => s.UserID).ToArray();
                var testsUsersId = tests.Where(w => w.AttestationId == item.AttestationId).Select(s => s.UserId).ToArray();
                var lazyUserId = attestationUsersId.Except(testsUsersId);
                var lazyUser = allUsers.Where(w => lazyUserId.Contains(w.UserId)).ToList();
                var testsToAttestation = tests.Where(w => w.AttestationId == item.AttestationId).ToList();
                var thisTestTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == item.TestTemplateId);

                var actualCategoryId = db.TestTemplateItem.Where(w => w.TestTemplateId == thisTestTemplate.TestTemplateId).Select(s => s.CategoryId).ToArray();
                var actualCategory = db.Tree.Where(w => w.TypeId == 1 && actualCategoryId.Contains(w.TreeId)).ToList();

                var attestationTestsId = testsToAttestation.Select(s => s.TestId).ToArray();
                var actualTestsItems = db.TestsItem.Where(w => attestationTestsId.Contains(w.TestsId)).ToList();
                var actualTestsItemsQuestionsId = actualTestsItems.Where(w => w.Answers == null).Select(s => s.QuestionId).Distinct().ToArray();
                var actualQuestions = db.Questions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();
                var actualTestsItemsTextQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
                var actualTextQuestions = db.Questions.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).ToList();

                var answers = db.Answers.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

                List<ViewUsersAttestationReport> userList = new List<ViewUsersAttestationReport>();

                //2.Уровень участников аттестации
                foreach (var user in item.empItems)
                {
                    var testsList = db.Tests.Where(w => w.UserId == user.UserID && w.Finished == 1 && w.AttestationId == item.AttestationId).ToList();
                    var thisUser = user.UserID;
                    //var thisUserFinalLevel = db.UsersAchievements.SingleOrDefault(s => s.UserId == user.UserID);
                    var thisUserRecomendLevel = user.RecommendedLevel;

                    UsersAchievements usersAchievements = new UsersAchievements();

                    var thisUserAchievements = db.UsersAchievements.Where(w => w.UserId == user.UserID).ToList();
                    if (thisUserAchievements.Count > 0)
                    {
                        var thisUserAchievementsId = thisUserAchievements.Where(w => w.UserId == user.UserID).Max(m => m.UsersAchievementsId);
                        var thisUserAchievement = db.UsersAchievements.SingleOrDefault(w => w.UsersAchievementsId == thisUserAchievementsId);
                        usersAchievements = thisUserAchievement;
                    }
                    else
                    {
                        usersAchievements.UserId = thisUser;
                        usersAchievements.CreateDate = DateTime.Now;
                        usersAchievements.DateGetAchievement = DateTime.Now;
                        usersAchievements.CreatedBy = appUser.UserId;
                        usersAchievements.LevelId = 0;
                        usersAchievements.PicId = -1;
                        usersAchievements.AchievementsId = 0;
                        usersAchievements.Grade = 0;
                        db.UsersAchievements.Add(usersAchievements);
                        db.SaveChanges();

                    }

                    List<ViewResultTest> actualTests = new List<ViewResultTest>();

                    //3. Уровень тестов
                    foreach (var test in testsList)
                    {

                        List<SubCategoriesForDetailedReport> thisCategories = new List<SubCategoriesForDetailedReport>();

                        // 4. Уровень категорий вопросов
                        foreach (var cat in actualCategory)
                        {
                            var thisCategoryTestsItems = actualTestsItems.Where(w => w.CategoryId == cat.TreeId).Distinct().ToList();
                            var thisCategoryTestsItemsActualQuestionsId = thisCategoryTestsItems.Select(s => s.QuestionId).ToArray();
                            var thisCategoryTestsItemsQuestions = actualTextQuestions.Where(w => thisCategoryTestsItemsActualQuestionsId.Contains(w.QuestionId)).ToList();

                            List<ViewQuestion> thisQuestions = new List<ViewQuestion>();

                            //foreach (var q in thisCategoryTestsItemsQuestions)
                            //{
                            //    var a = answers.Where(x => x.QuestionId == q.QuestionId).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId }).ToList();

                            //    thisQuestions.Add(new ViewQuestion()
                            //    {
                            //        Question = q,
                            //        Answers = a,

                            //    });

                            //}

                            thisCategories.Add(new SubCategoriesForDetailedReport()
                            {
                                subCategoryId = cat.TreeId,
                                subCategoryName = cat.NameRu,
                                ViewQuestion = thisQuestions,
                            });

                        }

                        actualTests.Add(new ViewResultTest()
                        {
                            TestId = test.TestId,
                            UserId = user.UserID,
                            NameRu = test.NameRu,
                            WeightResult = test.WeightResult,
                            CheckedText = test.CheckedText,
                            VerbalExamination = test.VerbalExamination,
                            PracticalExamination = test.PracticalExamination,
                            SubCategoriesForDetailedReport = thisCategories,
                        });

                    }

                    userList.Add(new ViewUsersAttestationReport()
                    {
                        User = allUsers.FirstOrDefault(x => x.UserId == user.UserID || x.OldUserId == user.UserID),
                        ViewResultTests = actualTests,
                        UsersAchievements = usersAchievements,
                        TargetProcent = targetProcent,
                        RecomendLevel = thisUserRecomendLevel,
                        LMConfirmRL = user.LMConfirmRL,
                        HRConfirmRL = user.HRConfirmRL,
                    });

                }

                attestationList2.Add(new ViewAttestationReport()
                {
                    Attestation = item,
                    ViewUsersAttestationReport = userList,
                    LazyUser = lazyUser,
                });

            }

            ViewBag.AppUser = appUser.UserId;
            ViewBag.Access = access;


            attestationList2.Reverse();

            ViewBag.Attestation = attestationList2.ToPagedList(page, 50);
            return View(attestationList2.ToPagedList(page, 50));
        }

        public ActionResult Marks(int page = 1)
        {
            // Подключение пользователя
            #region SetUser
            var allUsers = db.L_User.Where(x => x.AccountAD != null).ToList();
            var appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => (x.UserId == appUser.UserId || x.UserId == appUser.OldUserId) && (x.AccessLevelId == 1 || x.RoleId == 2)).ToList();
            var admin = db.UserInRole.Where(x => x.AccessLevelId == 1 && (x.UserId == appUser.UserId || x.UserId == appUser.OldUserId)).Select(s=> s.UserId).ToArray();
            var sevenGods = db.UserInRole.Where(x => x.AccessLevelId == 2).Select(x => x.UserId);
            var AttestationLMUsers = db.Attestations.Where(x => x.LMUserId == appUser.UserId || x.LMUserId == appUser.OldUserId).Select(s=> s.LMUserId).Distinct().ToArray();
            #endregion

            var accessCheck = access.Count();
            var tests = db.Tests.ToList();

            bool Admin = admin.Contains(appUser.UserId);
            bool oneOfSeven = sevenGods.Contains(appUser.UserId) || sevenGods.Contains(appUser.OldUserId);
            bool oneOfLMUsers = AttestationLMUsers.Contains(appUser.UserId) || AttestationLMUsers.Contains(appUser.OldUserId);

            List<Attestation> allAttestationsForManager = new List<Attestation>();

            if (Admin)
            {
                allAttestationsForManager = db.Attestations.Where(w => w.IsSubmit == true && w.IsDeleted != true).ToList();
                //allAttestationsForManager = 
            }

            if (oneOfSeven)
            {
                allAttestationsForManager = db.Attestations.Where(x => sevenGods.Contains(x.CreatedBy) && x.IsSubmit == true && x.IsDeleted != true).ToList();
            }
            if (oneOfLMUsers && !oneOfSeven && !Admin)
            {
                allAttestationsForManager = db.Attestations.Where(x => x.LMUserId == appUser.UserId && x.IsDeleted != true).ToList();
            }
            if (access.Where(w => w.RoleId == 2).Count() > 0)
            {
                allAttestationsForManager = db.Attestations.Where(w => w.IsSubmit == true && w.IsDeleted != true).ToList();
            }
            if (oneOfLMUsers)
            {
                allAttestationsForManager = db.Attestations.Where(x => AttestationLMUsers.Contains(x.CreatedBy) && x.IsSubmit == true && x.IsDeleted != true).ToList();
            }



            //List<Attestation> allAttestationsForManager = db.Attestations.Where(w => w.AttestationName != null && w.IsDeleted != true ).ToList();
            //allAttestationsForManager = allAttestationsForManager.Where(w => (w.TestTemplateId > 0 && w.LMUserId == appUser.UserId) || accessCheck > 0).ToList();
            List<ViewAttestation> attestationList = new List<ViewAttestation>();
            List<ViewAttestationReport> attestationList2 = new List<ViewAttestationReport>();

            //List<User> allActualUsers = new List<User>();
            //var ThisTestList = db.Tests.Where(w => w.AttestationId == attestationId && w.UserId == appUser.UserId).ToList();

            //1. Уровень аттестации
            foreach (var item in allAttestationsForManager)
            {

                var attestationId = item.AttestationId;

                var attestationsUsers = db.AttestationEmployeeItems.Where(w => w.ManagerApprove == true && w.AttestationId == item.AttestationId).ToList();
                //var thisAttestation = db.Attestations.SingleOrDefault(s => s.AttestationId == item.AttestationId);

                int? targetProcent = 0;

                var thisTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == item.TestTemplateId);
                if (thisTemplate != null)
                {
                    targetProcent = thisTemplate.TargetProcent;
                }

                var attestationUsersId = attestationsUsers.Select(s => s.UserID).ToArray();
                var testsUsersId = tests.Where(w => w.AttestationId == item.AttestationId).Select(s => s.UserId).ToArray();
                var lazyUserId = attestationUsersId.Except(testsUsersId);
                var lazyUser = allUsers.Where(w => lazyUserId.Contains(w.UserId)).ToList();
                var testsToAttestation = tests.Where(w => w.AttestationId == item.AttestationId).ToList();
                var thisTestTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == item.TestTemplateId);

                //var actualCategoryId = db.TestTemplateItem.Where(w => w.TestTemplateId == thisTestTemplate.TestTemplateId).Select(s => s.CategoryId).ToArray();
                //var actualCategory = db.Tree.Where(w => w.TypeId == 1 && actualCategoryId.Contains(w.TreeId)).ToList();

                var attestationTestsId = testsToAttestation.Select(s => s.TestId).ToArray();
                var actualTestsItems = db.TestsItem.Where(w => attestationTestsId.Contains(w.TestsId)).ToList();
                var actualTestsItemsQuestionsId = actualTestsItems.Where(w => w.Answers == null).Select(s => s.QuestionId).Distinct().ToArray();
                var actualQuestions = db.Questions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();
                var actualTestsItemsTextQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
                var actualTextQuestions = db.Questions.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).ToList();

                var answers = db.Answers.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

                List<ViewUsersAttestationReport> userList = new List<ViewUsersAttestationReport>();


                attestationList2.Add(new ViewAttestationReport()
                {
                    Attestation = item,
                    ViewUsersAttestationReport = userList,
                    LazyUser = lazyUser,
                });

            }

            ViewBag.AppUser = appUser.UserId;
            ViewBag.Access = access;


            attestationList2.Reverse();

            ViewBag.Attestation = attestationList2.ToPagedList(page, 50);
            return View(attestationList2.ToPagedList(page, 50));
        }


        [HttpPost]
        public JsonResult CheckTest(int userId, int attestationId, int testId)
        {
            // Подключение пользователя
            #region SetUser
            var allUsers = Helper.GetAllUsers();
            L_User appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return Json("Error", JsonRequestBehavior.AllowGet);
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();
            #endregion

            var thisAttestation = db.Attestations.SingleOrDefault(s => s.AttestationId == attestationId);
            var thisTestTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == thisAttestation.TestTemplateId);
            //var actualCategoryId = db.TestTemplateItem.Where(w => w.TestTemplateId == thisTestTemplate.TestTemplateId).Select(s => s.CategoryId).ToArray();

            var thisCategoryId = db.TestsItem.Where(w => w.TestsId == testId && w.Answers != null).Select(s => s.CategoryId).Distinct().ToArray();

            var actualCategory = db.Tree.Where(w => w.TypeId == 1 && thisCategoryId.Contains(w.TreeId)).ToList();

            var questionIdArr = db.TestsItem.Where(w => w.TestsId == testId && w.Answers != null).Select(s => s.QuestionId).ToArray();
            questionIdArr = questionIdArr.Distinct().ToArray();
            var allQuestions = db.Questions.Where(w => questionIdArr.Contains(w.QuestionId)).ToList();

            var treeQuestions = db.TreeQuestion.Where(w => questionIdArr.Contains(w.QuestionId)).Distinct().ToList();

            var uploads = db.Uploads.ToList();

            List<SubCategoriesForDetailedReport> thisCategories = new List<SubCategoriesForDetailedReport>();

            foreach (var cat in actualCategory)
            {
                var thisQuestionsThisCategoryId = treeQuestions.Where(w => w.TreeId == cat.TreeId).Select(s => s.QuestionId).ToArray();
                var thisQuestionsThisCategory = allQuestions.Where(w => thisQuestionsThisCategoryId.Contains(w.QuestionId)).ToList();

                List<ViewTextAnswers> thisQuestions = new List<ViewTextAnswers>();
                foreach (var item in thisQuestionsThisCategory)
                {
                    var thisTestItems = db.TestsItem.Where(w => w.TestsId == testId && w.QuestionId == item.QuestionId && w.Answers != null).ToList();
                    var thisTestItem = thisTestItems.First();
                    var picturesList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == item.QuestionId).Select(s => s.UploadId).ToList();

                    thisQuestions.Add(new ViewTextAnswers()
                    {
                        QuestionId = item.QuestionId,
                        QuestionRu = item.QuestionRu,
                        Answers = thisTestItem.Answers,
                        PictureList = picturesList,
                        ManagerMark = thisTestItem.ManagerMark
                    });

                }

                thisCategories.Add(new SubCategoriesForDetailedReport()
                {
                    subCategoryId = cat.TreeId,
                    subCategoryName = cat.NameRu,
                    TextQuestion = thisQuestions,
                });
            }

            //ViewBag.AttestationName = db.Attestations.SingleOrDefault(s => s.AttestationId == attestationId).AttestationName;
            //ViewBag.AttestationId = attestationId;
            //ViewBag.TestId = testId;
            //ViewBag.UserName = allUsers.SingleOrDefault(s => s.UserId == userId).NameRu; 

            return Json(thisCategories, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ResultTest(int userId, int attestationId, int testId)
        {
            // Подключение пользователя
            #region SetUser
            var allUsers = Helper.GetAllUsers();
            var appUser = Helper.GetAppUser(User.Identity); 

            if (appUser == null)
            {
                return Json("Error", JsonRequestBehavior.AllowGet);
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();
            #endregion

            var thisAttestation = db.Attestations.SingleOrDefault(s => s.AttestationId == attestationId);
            var thisTestTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == thisAttestation.TestTemplateId);
            var actualCategoryId = db.TestTemplateItem.Where(w => w.TestTemplateId == thisTestTemplate.TestTemplateId).Select(s => s.CategoryId).ToArray();
            var actualCategory = db.Tree.Where(w => w.TypeId == 1 && actualCategoryId.Contains(w.TreeId)).ToList();

            var questionIdArr = db.TestsItem.Where(w => w.TestsId == testId && w.Answers == null).Select(s => s.QuestionId).ToArray();
            questionIdArr = questionIdArr.Distinct().ToArray();
            var allQuestions = db.Questions.Where(w => questionIdArr.Contains(w.QuestionId)).ToList();

            var answers = db.Answers.Where(w => questionIdArr.Contains(w.QuestionId)).ToList();

            var actualTestsItems = db.TestsItem.Where(w => w.TestsId == testId && w.UserId == userId).ToList();

            var uploads = db.Uploads.ToList();
            List<SubCategoriesForDetailedReport> thisCategories = new List<SubCategoriesForDetailedReport>();

            foreach (var cat in actualCategory)
            {
                var catQuestionsId = db.TreeQuestion.Where(w => w.TypeId == 1 && w.TreeId == cat.TreeId).Select(s => s.QuestionId).ToArray();
                var catQuestions = allQuestions.Where(w => w.ParentQuestionId == null && catQuestionsId.Contains(w.QuestionId)).ToList();

                List<ViewQuestion> thisQuestions = new List<ViewQuestion>();

                foreach (var q in catQuestions)
                {
                    var picturesList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == q.QuestionId).Select(s => s.UploadId).ToList();
                    var selectedAnswerId = db.TestsItem.Where(w => w.QuestionId == q.QuestionId && w.Selected != null && w.TestsId == testId).Select(s => s.AnswerId).ToArray();
                    var selectedAnswers = answers.Where(w => selectedAnswerId.Contains(w.AnswerId)).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();

                    //var questionAnswerId = db.TestsItem.Where(w => w.QuestionId == q.QuestionId && w.Selected != null).Select(s => s.AnswerId).ToArray();
                    var questionAnswers = answers.Where(w => w.QuestionId == q.QuestionId).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();
                    var rightAnswers = questionAnswers.Where(w => w.Weight > 0).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();

                    var subQuestions = allQuestions.Where(w => w.ParentQuestionId == q.QuestionId).ToList();

                    var subQuestionsId = subQuestions.Select(s => s.QuestionId).ToArray();
                    var subA = answers.Where(w => subQuestionsId.Contains(w.QuestionId)).ToList();

                    List<ViewQuestion> subQuestionList = new List<ViewQuestion>();

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
                        var thisQuestionAnswer = answers.SingleOrDefault(s => s.QuestionId == sub.QuestionId);

                        var thisQuestionAnswerSelectedId = actualTestsItems.SingleOrDefault(s => s.QuestionId == sub.QuestionId && s.TestsId == testId && s.Answers == null).Selected;
                        var thisQuestionAnswerSelected = answers.SingleOrDefault(s => s.AnswerId == thisQuestionAnswerSelectedId);
                        var subAnswerSelected = answers.SingleOrDefault(s => s.AnswerId == thisQuestionAnswerSelected.AnswerId);

                        List<AnswerView> subAnswerThis = new List<AnswerView>();
                        List<AnswerView> subAnswerThisSelected = new List<AnswerView>();

                        subAnswerThis.Add(new AnswerView()
                        {
                            AnswerId = thisQuestionAnswer.AnswerId,
                            AnswerRu = thisQuestionAnswer.AnswerRu,
                            value = thisQuestionAnswer.AnswerId,
                        });

                        subAnswerThisSelected.Add(new AnswerView()
                        {
                            AnswerId = subAnswerSelected.AnswerId,
                            AnswerRu = subAnswerSelected.AnswerRu,
                            value = subAnswerSelected.AnswerId,
                            Weight = subAnswerSelected.Weight,
                        });

                        subQuestionList.Add(new ViewQuestion()
                        {
                            Question = sub,
                            SubAnswers = subAnswerThis,
                            AnswersSelected = subAnswerThisSelected,
                        });
                    }

                    thisQuestions.Add(new ViewQuestion()
                    {
                        QuestionId = q.QuestionId,
                        Question = q,
                        PictureList = picturesList,
                        AnswersSelected = selectedAnswers,
                        Answers = questionAnswers,
                        RightAnswers = rightAnswers,
                        SubQuestion = subQuestionList,
                    });

                }

                thisCategories.Add(new SubCategoriesForDetailedReport()
                {
                    subCategoryId = cat.TreeId,
                    subCategoryName = cat.NameRu,
                    ViewQuestion = thisQuestions,
                });

            }

            return Json(thisCategories, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult FeedbackView(int userId, int attestationId, int feedId)
        {
            // Подключение пользователя
            #region SetUser
            var allUsers = Helper.GetAllUsers();
            var appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return Json("Error", JsonRequestBehavior.AllowGet);
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId).ToList();
            #endregion

            var thisAttestation = db.Attestations.SingleOrDefault(s => s.AttestationId == attestationId);
            var thisFeedTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == thisAttestation.FeedbackId && s.TypeTemplateId == 1);
            var actualCategoryId = db.TestTemplateItem.Where(w => w.TestTemplateId == thisFeedTemplate.TestTemplateId).Select(s => s.CategoryId).ToArray();
            var actualCategory = db.Tree.Where(w => w.TypeId == 5 && actualCategoryId.Contains(w.TreeId)).ToList();

            var thisFeedback = db.Feedback.SingleOrDefault(s => s.TemplateId == feedId);

            var questionIdArr = db.FeedbackItems.Where(w => w.FeedbackId == thisFeedback.FeedbackId).Select(s => s.QuestionId).ToArray();
            questionIdArr = questionIdArr.Distinct().ToArray();
            var allQuestions = db.Questions.Where(w => questionIdArr.Contains(w.QuestionId)).ToList();

            var answers = db.Answers.Where(w => questionIdArr.Contains(w.QuestionId)).ToList();

            var actualTestsItems = db.FeedbackItems.Where(w => w.FeedbackId == thisFeedback.FeedbackId && w.UserId == userId).ToList();

            List<SubCategoriesForDetailedReport> thisCategories = new List<SubCategoriesForDetailedReport>();

            foreach (var cat in actualCategory)
            {
                var catQuestionsId = db.TreeQuestion.Where(w => w.TypeId == 1 && w.TreeId == cat.TreeId).Select(s => s.QuestionId).ToArray();
                var catQuestions = allQuestions.Where(w => w.ParentQuestionId == null && catQuestionsId.Contains(w.QuestionId)).ToList();

                List<ViewQuestion> thisQuestions = new List<ViewQuestion>();

                foreach (var q in catQuestions)
                {
                    var selectedAnswerId = db.FeedbackItems.Where(w => w.QuestionId == q.QuestionId && w.Selected != null && w.FeedbackId == thisFeedback.FeedbackId).Select(s => s.AnswerId).ToArray();
                    var selectedAnswers = answers.Where(w => selectedAnswerId.Contains(w.AnswerId)).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();

                    //var questionAnswerId = db.TestsItem.Where(w => w.QuestionId == q.QuestionId && w.Selected != null).Select(s => s.AnswerId).ToArray();
                    var questionAnswers = answers.Where(w => w.QuestionId == q.QuestionId).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();
                    var rightAnswers = questionAnswers.Where(w => w.Weight > 0).Select(s => new AnswerView { AnswerId = s.AnswerId, AnswerRu = s.AnswerRu, QuestionId = s.QuestionId, Weight = s.Weight }).ToList();

                    var textanswer = actualTestsItems.Where(w => w.QuestionId == q.QuestionId).First();

                    thisQuestions.Add(new ViewQuestion()
                    {
                        QuestionId = q.QuestionId,
                        Question = q,
                        AnswersSelected = selectedAnswers,
                        Answers = questionAnswers,
                        RightAnswers = rightAnswers,
                        TextAnswer2 = textanswer.Text,
                    });

                }

                thisCategories.Add(new SubCategoriesForDetailedReport()
                {
                    subCategoryId = cat.TreeId,
                    subCategoryName = cat.NameRu,
                    ViewQuestion = thisQuestions,
                });

            }

            return Json(thisCategories, JsonRequestBehavior.AllowGet);
        }

    }

}
