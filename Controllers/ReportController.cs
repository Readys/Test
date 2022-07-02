using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TTS.Models;
using System.IO;
using TTS.Tools;
using PagedList;

namespace TTS.Controllers
{
    public class ReportController : Controller
    {
        private TTSEntities db = new TTSEntities();
        private HRMEntities h_db = new HRMEntities();
        public const int testUser = 0; //Тестовый вход под любым пользователем//598 181 211 783 Мазанов - 471 37 Капацин Антон-174
        // GET: Report
        public ActionResult Index(int page = 1, int LevelId = -1)
        {
            var allUsers = Helper.GetAllUsers();
            var appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return View("Access");
            }

            var access = db.UserInRole.Where(x => x.UserId == appUser.UserId && (x.AccessLevelId == 1 || x.RoleId == 2)).ToList();

            var admin = db.UserInRole.FirstOrDefault(x => x.AccessLevelId == 1);
            var sevenGods = db.UserInRole.Where(x => x.AccessLevelId == 2).Select(x => x.UserId);
            var AttestationLMUsers = db.Attestations.Select(x => x.LMUserId);

            var accessCheck = access.Count();
            var tests = db.Tests.ToList();

            bool Admin = admin.UserId == appUser.UserId;
            bool oneOfSeven = sevenGods.Contains(appUser.UserId);
            bool oneOfLMUsers = AttestationLMUsers.Contains(appUser.UserId);

            List<Attestation> allAttestationsForManager = new List<Attestation>();

            allAttestationsForManager = db.Attestations.Where(w => (w.TestTemplateId > 0 && w.LMUserId == appUser.UserId) || accessCheck > 0).ToList();

            if (Admin)
            {
                allAttestationsForManager = db.Attestations.Where(w => w.IsSubmit == true && w.IsDeleted != true).ToList();
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

            if (LevelId >= 0)
            {
                allAttestationsForManager = allAttestationsForManager.Where(w=> w.CertificationLevelToPass == LevelId).ToList();
            }

            ViewBag.Template = db.TestTemplate.Where(w=> w.TypeTemplateId != 1).ToList();
            ViewBag.Tree = db.Tree.ToList();

            allAttestationsForManager.Reverse();

            return View(allAttestationsForManager.ToPagedList(page, 20));

        }

        public ActionResult MechanicReports()
        {
            return View();
        }

        [HttpGet]
        public ActionResult CreateShortExcel(int CertificationId = 0, int Level = 0)
        {
            if (CertificationId != 0 && Level >= 0)
            {
                List<L_User> users = db.L_User.ToList();
                var CertificationToExcelReport = db.Attestations.FirstOrDefault(s => s.AttestationId == CertificationId && s.CertificationLevelToPass == Level);
                string lvl = CertificationToExcelReport.CertificationLevelToPass.ToString();

                ExcelPackage package = new ExcelPackage();
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Краткий отчет по аттестации");

                worksheet.PrinterSettings.LeftMargin = 0.25M;
                worksheet.PrinterSettings.RightMargin = 0.25M;

                setYellowHeaderStyle(worksheet.Cells[1, 1, 1, 4], CertificationToExcelReport.AttestationName + "(Уровень" + lvl + ")", 8);

                var namedStyle = package.Workbook.Styles.CreateNamedStyle("HyperLink");
                namedStyle.Style.Font.UnderLine = true;
                namedStyle.Style.Font.Color.SetColor(Color.Blue);

                int i = 2;

                foreach (var item in CertificationToExcelReport.empItems)
                {
                    worksheet.Cells[i, 2].Value = users.SingleOrDefault(s => s.UserId == item.UserID).NameRu;
                    worksheet.Cells[i, 3].Value = CertificationToExcelReport.CertificationLevelToPass;
                    if (item.Passed == 0)
                    {
                        worksheet.Cells[i, 4].Value = "Не сдал";
                        worksheet.Cells[i, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[i, 4].Style.Fill.BackgroundColor.SetColor(Color.Red);
                    }
                    else
                    {
                        worksheet.Cells[i, 4].Value = "Сдал";
                        worksheet.Cells[i, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[i, 4].Style.Fill.BackgroundColor.SetColor(Color.ForestGreen);
                    }

                    i++;
                }


                worksheet.Cells[2, 1, --i, 1].Merge = true;
                worksheet.Cells[2, 1, --i, 1].Value = "ФИО";
                //worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                //worksheet.Cells[2, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                worksheet.Cells["A2"].AutoFitColumns(15);
                worksheet.Row(1).Height = 30;
                worksheet.Cells.AutoFitColumns(0);
                worksheet.Cells[2, 1].Style.Border.BorderAround(ExcelBorderStyle.Medium, Color.Black);
                worksheet.Cells[2, 2, i, 4].Style.Border.BorderAround(ExcelBorderStyle.Medium, Color.Black);


                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=" + CertificationToExcelReport.AttestationName  + ".xlsx");
                Response.BinaryWrite(package.GetAsByteArray());
                Response.End();

                return Content("OK");
            }
            return RedirectToAction("/TTS/CertificationCalendar");
        }

        public ActionResult CreateDetailedExcel(int CertificationId = 0, int Level = 0)
        {
            char[] alphabet = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
            var allUsers = Helper.GetAllUsers();
            var findAttestation = db.Attestations.FirstOrDefault(s => s.AttestationId == CertificationId && s.CertificationLevelToPass == Level);
            var findTemplate = db.TestTemplate.FirstOrDefault(s => s.TestTemplateId == findAttestation.TestTemplateId);

            var tree = db.Tree.Where(w => w.TypeId == 1 && w.Deleted != 1).ToList();
            var questions = db.Questions.Where(w => w.Deleted != 1 && w.Published == 1);

            var categoriesId = db.TestTemplateItem.Where(s => s.TestTemplateId == findTemplate.TestTemplateId && s.CategoryType == 1).Select(s => s.CategoryId).ToArray();           
            var categories = tree.Where(w => categoriesId.Contains(w.TreeId)).ToList();
            var categoriesParentsId = categories.Select(s => s.ParentId).ToArray();

            var categoriesParents0 = tree.Where(w => categoriesParentsId.Contains(w.TreeId)).ToList();
            var categoriesParents = categories.Where(w => w.ParentId == null).ToList();
                categoriesParents = categoriesParents.Concat(categoriesParents0).ToList();
            var categoriesChild = categories.Where(w => w.ParentId != null).ToList();

            //var categoriesParentsId = categories.Select( s=> s.ParentId).ToArray();
           // var categoriesParents = tree.Where(w => categoriesParentsId.Contains(w.TreeId)).ToList();

            //if (categoriesParents.Count > 0) { categories = categoriesParents; }
            //categories = categoriesChild;

            var attestationTests = db.Tests.Where(w => w.AttestationId == findAttestation.AttestationId && w.Finished == 1).ToList();
            var attestationTestsId = attestationTests.Select(s => s.TestId).ToArray();
            var usersPassTestsId = attestationTests.Select(s => s.UserId).ToArray();
            var usersPassTests = findAttestation.empItems.Where(w => w.ManagerApprove == true && usersPassTestsId.Contains(w.UserID)).ToList();

            var actualTestsItems = db.TestsItem.Where(w => attestationTestsId.Contains(w.TestsId)).ToList();
            var actualTestsItemsQuestionsId = actualTestsItems.Where(w => w.Answers == null).Select(s => s.QuestionId).Distinct().ToArray();
            var actualQuestions = questions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();
            var actualTestsItemsTextQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
            var actualTextQuestions = questions.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).ToList();

            var answers = db.Answers.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

            List<ViewResultTest> actualTests = new List<ViewResultTest>();
            List<ForDetailedReport> forDetailedReport = new List<ForDetailedReport>();

            foreach (var user in usersPassTests)
            {
                //var catParentSumm = 0;
                //var catSumm = 0;
                var thisUser = allUsers.FirstOrDefault(s => s.UserId == user.UserID || s.OldUserId == user.UserID);

                List<SubCategoriesForDetailedReport> subCategories1 = new List<SubCategoriesForDetailedReport>();

                foreach (var item1 in categoriesParents)
                {
                    var questionSumm1 = Sub(thisUser, questions.ToList(), actualTextQuestions, answers, CertificationId, Level, item1.TreeId);
                    List<SubCategoriesForDetailedReport> subCategories2 = new List<SubCategoriesForDetailedReport>();
                    if (categories.Any(x => x.ParentId == item1.TreeId))
                    {
                        foreach (var item2 in categories.Where(x => x.ParentId == item1.TreeId).OrderBy(o => o.TreeId))
                        {
                            var questionSumm2 = Sub(thisUser, questions.ToList(), actualTextQuestions, answers, CertificationId, Level, item2.TreeId);
                            List<SubCategoriesForDetailedReport> subCategories3 = new List<SubCategoriesForDetailedReport>();
                            if (categories.Any(x => x.ParentId == item2.TreeId))
                            {
                                foreach (var item3 in categories.Where(x => x.ParentId == item2.TreeId).OrderBy(o => o.TreeId))
                                {
                                    var questionSumm3 = Sub(thisUser, questions.ToList(), actualTextQuestions, answers, CertificationId, Level, item2.TreeId);
                                    subCategories3.Add(new SubCategoriesForDetailedReport() { subCategoryId = item3.TreeId, subCategoryName = item3.NameRu, subCategoryResult = questionSumm3.subCategoryResult, ViewQuestion = questionSumm3.ViewQuestion });
                                }                                
                                subCategories2.Add(new SubCategoriesForDetailedReport() { subCategoryId = item2.TreeId, subCategoryName = item2.NameRu, SubCategories = subCategories3, subCategoryResult = questionSumm2.subCategoryResult, ViewQuestion = questionSumm2.ViewQuestion, TextQuestion = questionSumm2.TextQuestion, subCategoryTextResult = questionSumm2.subCategoryTextResult });
                            }
                            else
                            {
                                subCategories2.Add(new SubCategoriesForDetailedReport() { subCategoryId = item2.TreeId, subCategoryName = item2.NameRu, subCategoryResult = questionSumm2.subCategoryResult, ViewQuestion = questionSumm2.ViewQuestion, TextQuestion = questionSumm2.TextQuestion, subCategoryTextResult = questionSumm2.subCategoryTextResult });
                            }
                        }
                        subCategories1.Add(new SubCategoriesForDetailedReport() { subCategoryId = item1.TreeId, subCategoryName = item1.NameRu, SubCategories = subCategories2, subCategoryResult = questionSumm1.subCategoryResult, ViewQuestion = questionSumm1.ViewQuestion });
                    }
                    else
                    {
                        subCategories1.Add(new SubCategoriesForDetailedReport() { subCategoryId = item1.TreeId, subCategoryName = item1.NameRu, subCategoryResult = questionSumm1.subCategoryResult, ViewQuestion = questionSumm1.ViewQuestion });
                    }
                }

                forDetailedReport.Add(new ForDetailedReport()
                {
                    UserName = thisUser.NameRu,
                    SubCategories = subCategories1,
                });

            }

            var totalTree = forDetailedReport;

            CreateExcel(totalTree, CertificationId, Level);

            return RedirectToAction("/TTS/Index");
        }

        public SubCategoriesForDetailedReport Sub(L_User user, List <Question> actualQuestions, List <Question> actualTextQuestions, List <Answer> answers, int CertificationId = 0, int Level = 0, int categoryId = 0, int testid = 0) 
        {
            var uploads = db.Uploads.ToList();
            var attestation = db.Attestations.SingleOrDefault(w => w.AttestationId == CertificationId);
            var catTree = db.Tree.SingleOrDefault(s => s.TreeId == categoryId);

            var attestationTests = db.Tests.Where(w => w.AttestationId == attestation.AttestationId && w.Finished == 1).ToList();
            var attestationTestsId = attestationTests.Select(s => s.TestId).ToArray();

            var actualTestsItems = db.TestsItem.Where(w => attestationTestsId.Contains(w.TestsId)).Distinct().ToList();

            var actualTestsItemsQuestionsId = actualTestsItems.Where(w => w.Answers == null).Select(s => s.QuestionId).Distinct().ToArray();
            actualQuestions = actualQuestions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

            //var actualTestsTextItemsQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
            //actualTextQuestions = actualTextQuestions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

            var actualParentQuestionsId = actualQuestions.Where(w => w.ParentQuestionId != null).Select(s => s.ParentQuestionId).Distinct().ToArray();
            var actualParentQuestions = actualQuestions.Where(w => actualParentQuestionsId.Contains(w.QuestionId)).ToList();

            actualQuestions = actualQuestions.Except(actualParentQuestions).ToList();

            var actualTestsItemsTextQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
            //actualTextQuestions = db.Questions.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).Distinct().ToList();
            var actualTextTestItems = actualTestsItems.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).Distinct().ToList();

            answers = db.Answers.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

            var bestTest = attestationTests.Where(w => (w.UserId == user.UserId || w.UserId == user.OldUserId) && w.Finished == 1).OrderByDescending(o => o.WeightResult).First();

            var bestTestItems = actualTestsItems.Where(w => w.CategoryId == catTree.TreeId && w.TestsId == bestTest.TestId).ToList();
            var bestTestQuestionsId = bestTestItems.Select(s => s.QuestionId).Distinct().ToArray();
            var bestTestQuestions = actualQuestions.Where(w => bestTestQuestionsId.Contains(w.QuestionId)).ToList();

            var bestTestTextItems = actualTextTestItems.Where(w => w.CategoryId == catTree.TreeId && w.TestsId == bestTest.TestId).ToList();
            var bestTestTextQuestionsId = bestTestTextItems.Select(s => s.QuestionId).Distinct().ToArray();
            var bestTestTextQuestionsItems = actualTestsItems.Where(w => bestTestTextQuestionsId.Contains(w.QuestionId)).ToList();

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
                                questionRightProcent +=  a1.Weight;
                            }
                            else
                            {
                                questionRightProcent -= weightItem;
                            }

                        }
                    }
                }
                // Блокировка ухода в отрицательное значение
                if (questionRightProcent < 0)
                {
                    questionRightProcent = 0;
                }

                categoryRightProcent +=  questionRightProcent;

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

            List<ViewTextAnswers> thisTextQuestions = new List<ViewTextAnswers>();
            int? questionTextRightProcent = 0;

            foreach (var qt in bestTestTextItems)
            {
                var thisTestItems = bestTestTextQuestionsItems.FirstOrDefault(w => (w.UserId == user.UserId || w.UserId == user.OldUserId) && w.QuestionId == qt.QuestionId);
                var qName = actualTextQuestions.SingleOrDefault(s => s.QuestionId == qt.QuestionId).QuestionRu;

                var picturesList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == qt.QuestionId).Select(s => s.UploadId).ToList();

                thisTextQuestions.Add(new ViewTextAnswers()
                {
                    QuestionId = qt.QuestionId,
                    QuestionRu = qName,
                    Answers = thisTestItems.Answers,
                    PictureList = picturesList,
                    ManagerMark = thisTestItems.ManagerMark * 20,
                });

                questionTextRightProcent += thisTestItems.ManagerMark * 20;
            }

            if (categoryRightProcent > 0) { categoryRightProcent /=  bestTestQuestions.Count(); }
            if (questionTextRightProcent > 0) { questionTextRightProcent /= actualTextQuestions.Count(); }

            SubCategoriesForDetailedReport subCategoriesForDetailedReport = new SubCategoriesForDetailedReport() { ViewQuestion = thisQuestions,
             subCategoryResult = categoryRightProcent, TextQuestion = thisTextQuestions, subCategoryTextResult = questionTextRightProcent
            };

            return subCategoriesForDetailedReport;
        }

        public int CalcWeightForCategory (L_User user, int CertificationId = 0, int Level = 0, int categoryId = 0, int testid = 0)
        {
            var uploads = db.Uploads.ToList();
            var attestation = db.Attestations.SingleOrDefault(w=> w.AttestationId == CertificationId);
            var catTree = db.Tree.SingleOrDefault(s => s.TreeId == categoryId);

            var attestationTests = db.Tests.Where(w => w.AttestationId == attestation.AttestationId && w.Finished == 1).ToList();
            var attestationTestsId = attestationTests.Select(s => s.TestId).ToArray();

            var actualTestsItems = db.TestsItem.Where(w => attestationTestsId.Contains(w.TestsId)).ToList();
            var actualTestsItemsQuestionsId = actualTestsItems.Where(w => w.Answers == null).Select(s => s.QuestionId).Distinct().ToArray();
            var actualQuestions = db.Questions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();
            var actualTestsItemsTextQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
            var actualTextQuestions = db.Questions.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).ToList();
            var answers = db.Answers.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

            var bestTest = attestationTests.Where(w => w.UserId == user.UserId && w.Finished == 1).OrderByDescending(o => o.WeightResult).First();

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

                var selectedAnswerId = bestTestItems.Where(w => w.QuestionId == q.QuestionId && w.TestsId == bestTest.TestId && w.Selected != null).Select(s => s.AnswerId).ToArray();
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
                                questionRightProcent += a1.Weight;
                            }
                            else
                            {
                                questionRightProcent -= weightItem;
                            }

                        }
                    }
                }
                // Блокировка ухода в отрицательное значение
                if (questionRightProcent < 0)
                {
                    questionRightProcent = 0;
                }

                categoryRightProcent += questionRightProcent;

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

            return categoryRightProcent;
        }

        public void CreateExcel(List<ForDetailedReport> tree, int CertificationId = 0, int Level = 0)
        {
            var CertificationToExcelReport = db.Attestations.FirstOrDefault(s => s.AttestationId == CertificationId && s.CertificationLevelToPass == Level);

            string lvl = CertificationToExcelReport.CertificationLevelToPass.ToString();

            ExcelPackage package = new ExcelPackage();
            string pathForSaving = Server.MapPath("~/Templates");
            FileInfo template = new FileInfo(pathForSaving + "\\SimpleTreningReport.xlsx");
            //ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Отчет по аттестации");
            using (package = new ExcelPackage(template, true))
            {
                var worksheet = package.Workbook.Worksheets[1];

                worksheet.Cells[2, 1].Value = CertificationToExcelReport.AttestationName + " (Уровень " + lvl + ")";
                worksheet.Cells[2, 2].Value = CertificationToExcelReport.StartTime;
                worksheet.Cells[2, 3].Value = CertificationToExcelReport.FinishTime;

                int y = 5;
                int x = 2;
                //int yParent = 0;
                //int xParent = 0;
                //var aaa = 0;
                //var b = 0;
                var x_title = 3;
                var y_title = 4;

                foreach (var parentCat in tree.First().SubCategories)
                {
                    worksheet.Cells[y_title, x_title].Value = parentCat.subCategoryName;
                    x_title++;
                    if(parentCat.SubCategories != null) { 
                        foreach (var Cat in parentCat.SubCategories)
                        {
                            worksheet.Cells[y_title, x_title].Value = Cat.subCategoryName;
                            x_title++;
                        }
                    }

                }
                worksheet.Cells[4, 2].Value = "Итого в среднем";

                foreach (var forDetailedReport in tree)
                {
                    int? totalSumm = 0;
                    int? totalTextSumm = 0;
                    worksheet.Cells[y, x-1].Value = forDetailedReport.UserName;

                    foreach (var parentCat in forDetailedReport.SubCategories)
                    {
                        int? summParent = parentCat.subCategoryResult;
                        int? summTextParent = parentCat.subCategoryTextResult;
                        if (summParent == null){ summParent = 0; }
                        if (summTextParent == null) { summTextParent = 0; }
                        worksheet.Cells[y+1, x+1].Value = summParent;
                        x++;
                        var summIndex = 1;
                        if (parentCat.SubCategories != null) { 
                            foreach (var Cat in parentCat.SubCategories)
                            {
                                worksheet.Cells[y+1, x + 1].Value = Cat.subCategoryResult;
                                worksheet.Cells[y+2, x + 1].Value = Cat.subCategoryTextResult;
                                summParent += Cat.subCategoryResult;
                                summTextParent += Cat.subCategoryTextResult;
                                x++;
                                summIndex++;
                            }
                            if (summParent > 0 ) { summParent /= parentCat.SubCategories.Count(); }
                            if (summTextParent > 0) { summTextParent /= parentCat.SubCategories.Count(); }
                            worksheet.Cells[y+1, x - summIndex + 1].Value = summParent;
                            worksheet.Cells[y+2, x - summIndex + 1].Value = summTextParent;
                        }
                        totalSumm += summParent;
                        totalTextSumm += summTextParent;
                    }
                    totalSumm /=  forDetailedReport.SubCategories.Count();
                    totalTextSumm /= forDetailedReport.SubCategories.Count();
                    worksheet.Cells[y+1, 2].Value = totalSumm;
                    worksheet.Cells[y+2, 2].Value = totalTextSumm;
                    y++;
                    x = 1;
                    worksheet.Cells[y, x].Value = "Автотест:";
                    y++;
                    worksheet.Cells[y, x].Value = "Письменные ответы:";
                    y++;
                    x = 2;
                }

                worksheet.Cells.AutoFitColumns(0);

                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=" + "Report" + ".xlsx");
                Response.BinaryWrite(package.GetAsByteArray());
                Response.End();

            }
        }
            //            List<ViewQuestion> thisTextQuestions = new List<ViewQuestion>();
            //            foreach (var qt in bestTestTextQuestions)
            //            {
            //                var picturesList = uploads.Where(x => x.ModuleId == 1 && x.DocumentId == qt.QuestionId).Select(s => s.UploadId).ToList();
            //                var textAnswer = actualTestsItems.Where(s => s.QuestionId == qt.QuestionId).First().Answers;
            //                thisTextQuestions.Add(new ViewQuestion()
            //                {
            //                    QuestionId = qt.QuestionId,
            //                    Question = qt,
            //                    PictureList = picturesList,
            //                    TextAnswer2 = textAnswer,
            //                });

            //            }

            //            if (categoryRightProcent > 0) { categoryRightProcent = categoryRightProcent / bestTestQuestions.Count; }

            //            catSumm = catSumm + categoryRightProcent;
            //            if (catTree.ParentId != null)
            //            {
            //                var thisSubcategories = categories.Where(w=> w.ParentId == catTree.ParentId).ToList();

            //                foreach (var sub in thisSubcategories)
            //                {
            //                    subCategoriesForDetailedReport2.Add(new SubCategoriesForDetailedReport()
            //                    {
            //                        subCategoryName = catName,
            //                        subCategoryId = cat.TreeId,
            //                        subCategoryResult = categoryRightProcent,
            //                        ViewQuestion = thisQuestions,
            //                        TextQuestion = thisTextQuestions,
            //                    });

            //                }

            //                    catParentSumm = catParentSumm + catSumm;
            //            }

            //            subCategoriesForDetailedReport.Add(new SubCategoriesForDetailedReport()
            //            {
            //                subCategoryName = catName,
            //                subCategoryId = cat.TreeId,
            //                subCategoryResult = categoryRightProcent,
            //                ViewQuestion = thisQuestions,
            //                TextQuestion = thisTextQuestions,
            //                SubCategories = subCategoriesForDetailedReport2
            //            });


            //        }

            //        if (catSumm > 0) { catSumm = catSumm / categories.Count(); }


            //        forDetailedReport.Add(new ForDetailedReport()
            //        {
            //            UserName = thisUser.NameRu,
            //            SubCategories = subCategoriesForDetailedReport,
            //            TotalParentMark = catParentSumm, // Далее извлеч среднее
            //            TotalMark = catSumm,
            //        });

            //        //subCategoriesForDetailedReport.Clear();
            //        catSumm = 0;
            //    }

            //    if (CertificationId != 0 && Level != 0)
            //    {
            //      

            //            //Создание динамических страниц
            //            var u = 1;
            //            foreach (var user in forDetailedReport)
            //            {
            //                u++;
            //                var worksheet2 = package.Workbook.Worksheets[u];
            //                worksheet2.Name = user.UserName;
            //                worksheet2.Column(1).Width = 20;
            //                worksheet2.Column(2).Width = 80;
            //                worksheet2.Column(3).Width = 60;
            //                worksheet2.Column(4).Width = 50;
            //                worksheet2.Column(5).Width = 50;
            //                worksheet2.Column(6).Width = 10;

            //                worksheet2.Cells[1, 1].Value = "Изображение"; worksheet2.Cells[1, 2].Value = "Вопросы"; worksheet2.Cells[1, 3].Value = "Варианты ответов";
            //                worksheet2.Cells[1, 4].Value = "Выбранные ответы"; worksheet2.Cells[1, 5].Value = "Правильные ответы"; //worksheet2.Cells[1, 6].Value = "Вес";

            //                var row = 2;

            //                var summTotal = 0;
            //                var count_c = 0;
            //                foreach (var cat in user.SubCategories)
            //                {
            //                    count_c++;
            //                    var summCategories = 0;
            //                    worksheet2.Cells[row, 2].Value = cat.subCategoryName;

            //                    row++;
            //                    var count_q = 0;
            //                    foreach (var q in cat.ViewQuestion)
            //                    {
            //                        count_q++;
            //                        worksheet2.Cells[row, 2].Value = q.Question.QuestionRu;

            //                        foreach (var p in q.PictureList)
            //                        {
            //                            string link = "http://training.bmst-kz.borusan.com/images?id=" + p.ToString();
            //                            // link = "http://training.bmst-kz.borusan.com/images?id=62";
            //                            worksheet2.Cells[row, 1].Hyperlink = new Uri(link);           
            //                            worksheet2.Cells[row, 1].Value = "image";
            //                        }

            //                        var aa = 0;
            //                        var s = "";
            //                        foreach (var a in q.Answers)
            //                        {                                      
            //                            s = s + alphabet[aa] + ". " + a.AnswerRu + "; ";
            //                            aa++;
            //                        }
            //                        //worksheet2.Row(row).Height =  aa * 10;
            //                        worksheet2.Cells[row, 3].Value = s;

            //                        aa = 1;
            //                        s = "";
            //                        var w = 0;
            //                        foreach (var a_s in q.AnswersSelected)
            //                        {
            //                            s = s + aa + ") " + a_s.AnswerRu + "; ";
            //                            w = w + a_s.Weight;
            //                            aa++;
            //                        }
            //                        worksheet2.Cells[row, 4].Value = s;
            //                        //worksheet2.Cells[row, 6].Value = w;
            //                        summCategories = summCategories + w;

            //                        aa = 1;
            //                        s = "";

            //                        foreach (var r in q.RightAnswers)
            //                        {
            //                            s = s + aa + ") " + r.AnswerRu + "; ";
            //                            aa++;
            //                        }
            //                        worksheet2.Cells[row, 5].Value = s;                                    

            //                        row++;
            //                    }

            //                        row++;
            //                    worksheet2.Cells[row, 2].Value = "";
            //                    //summCategories = summCategories;
            //                    //worksheet2.Cells[row -1, 6].Value = summCategories;
            //                    //worksheet2.Cells[row - 1, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightSalmon);
            //                    //summTotal = summTotal + summCategories;
            //                }

            //                worksheet2.Cells[row, 1].Value = "Изображение";
            //                worksheet2.Cells[row, 2].Value = "Вопросы";
            //                worksheet2.Cells[row, 3].Value = "Ответы";

            //                foreach (var cat in user.SubCategories)
            //                {
            //                    if (cat.TextQuestion.Count != 0)
            //                    {
            //                        worksheet2.Cells[row, 2].Value = cat.subCategoryName;
            //                        row++;
            //                        foreach (var qt in cat.TextQuestion)
            //                        {
            //                            worksheet2.Cells[row, 2].Value = qt.Question.QuestionRu;
            //                            worksheet2.Cells[row, 3].Value = qt.TextAnswer2;
            //                            row++;
            //                        }

            //                    }
            //                    //var textSubCategoriesId = user.SubCategories
            //                   // var textQuestionId = db.TreeQuestion.Where(w=> )                                                     

            //                }
            //                    //worksheet2.Cells[row, 6].Value = "ИТОГО:";
            //                    // worksheet2.Cells[row+1, 6].Value = summTotal / count_c;

            //                    // worksheet2.Cells.AutoFitColumns(0);
            //                }

            //            




            //            return Content("OK");
            //        }
            //}

        public static ExcelRange setYellowHeaderStyle(ExcelRange cellRange, string value, int fontSize = 10)
        {
            cellRange.Merge = true;
            cellRange.Value = value;
            cellRange.Style.Font.SetFromFont(new Font("Arial", fontSize, FontStyle.Regular));
            cellRange.Style.WrapText = true;
            cellRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cellRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cellRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cellRange.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 205, 17));
            cellRange.Style.Border.BorderAround(ExcelBorderStyle.Medium, Color.Black);
            return cellRange;
        }



    }
}