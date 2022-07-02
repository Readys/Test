using TTS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Newtonsoft.Json;
using TTS.Tools;
using System.Web.Script.Serialization;

namespace PartsRegistration.Controllers
{
    public class AJAXController : Controller
    {
        private TTSEntities db = new TTSEntities();
        private HRMEntities h_db = new HRMEntities();
        public const int testUser = 0; //Тестовый вход под любым пользователем//598 181 211 783 Мазанов - 471 37 Капацин Антон-174 39

        #region  Questions
        [HttpGet]
        public FileStreamResult DownloadFile(int partId)
        {
            var u = db.Uploads.Where(x => x.DocumentId == partId).ToList();
            var upload = u.Last();
            Response.Headers.Add("content-disposition", "attachment; filename=" + upload.Name.Replace(" ", "_"));
            return File(new FileStream(upload.Path, FileMode.Open),
                        "application/octet-stream"); // or "application/x-rar-compressed"
        }

        public JsonResult SaveUploadedFile(int partId = 0)
        {

            //var appUser = _db.Users.FirstOrDefault(x => x.AccountAD.ToLower() == User.Identity.Name.ToLower());
            bool isSavedSuccessfully = true;
            string fName = "";
            int uploadId = 0;
            try
            {
                foreach (string fileName in Request.Files)
                {
                    HttpPostedFileBase file = Request.Files[fileName];
                    //Save file content goes here
                    fName = file.FileName;
                    if (file != null && file.ContentLength > 0)
                    {
                        var originalDirectory = new DirectoryInfo(string.Format("{0}Uploads\\", Server.MapPath(@"\")));
                        string pathString = System.IO.Path.Combine(originalDirectory.ToString(), partId.ToString());
                        var fileName1 = Path.GetFileName(file.FileName);
                        bool isExists = System.IO.Directory.Exists(pathString);
                        if (!isExists)
                            System.IO.Directory.CreateDirectory(pathString);

                        var path = string.Format("{0}\\{1}", pathString, file.FileName);
                        file.SaveAs(path);

                        FileInfo fi = new FileInfo(file.FileName);
                        string ext = fi.Extension;

                        var upload = new Upload();
                        upload.DateUpload = DateTime.Now;
                        upload.DocumentId = partId;
                        upload.Extension = ext;
                        upload.ModuleId = 1;
                        upload.Name = fileName1;
                        upload.SizeKB = file.InputStream.Length / 1024;
                        upload.UserId = 0; //appUser.UserId;
                        upload.Path = path;
                        upload.QuestionId = partId;
                        db.Uploads.Add(upload);
                        db.SaveChanges();
                        uploadId = upload.UploadId;

                    }

                }

            }
            catch (Exception ex)
            {
                var m = ex.Message;
                isSavedSuccessfully = false;
            }


            if (isSavedSuccessfully)
            {
                return Json(new { Message = fName, UploadId = uploadId });
            }
            else
            {
                return Json(new { Message = "Error in saving file" });
            }
        }

        [HttpPost]
        public string UpdateCategory(int id, List<int> CategoryList, int typeId)
        {
            var rel = db.TreeQuestion.Where(w => w.QuestionId == id && w.TypeId == typeId);
            db.TreeQuestion.RemoveRange(rel);
            db.SaveChanges();
            if (CategoryList != null)
            {
                foreach (var cat in CategoryList)
                {
                    TreeQuestion tq = new TreeQuestion()
                    {
                        QuestionId = id,
                        TreeId = cat,
                        TypeId = typeId
                    };
                    db.TreeQuestion.Add(tq);
                }
                db.SaveChanges();
            }
            if (typeId == 3)
            {
                var firstLevelofQuestionList = 0;
                if (CategoryList != null)
                {
                    firstLevelofQuestionList = CategoryList[0];
                }
                var thisQuestion = db.Questions.SingleOrDefault(s => s.QuestionId == id);
                thisQuestion.LevelId = firstLevelofQuestionList;
                db.Entry(thisQuestion).State = EntityState.Modified;
                db.SaveChanges();
            }
            return "OK";
        }

        [HttpPost]
        public int AddQuestion(Question QuestionAdd)
        {
            Question question;
            question = new Question()
            {
                QuestionRu = QuestionAdd.QuestionRu,
                CreateDate = DateTime.Now,
                ModifedDate = DateTime.Now
            };
            db.Questions.Add(question);
            db.SaveChanges();
            return question.QuestionId;
        }

        [HttpPost]
        public JsonResult addSubQuestion(string QuestionRu, int parentQuestionId, int typeId, int QuestionId = 0)
        {
            Question question;
            Answer answer;
            TreeQuestion treeQuestion;

            var checkAnswer = db.Answers.Any(a => a.QuestionId == parentQuestionId);

            if (parentQuestionId > 0 && !checkAnswer)
            {
                answer = new Answer()
                {
                    AnswerRu = "Субответ",
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    QuestionId = parentQuestionId,
                };
                db.Answers.Add(answer);
                db.SaveChanges();
            }

            if (QuestionId == 0)
            {
                question = new Question()
                {
                    QuestionRu = QuestionRu,
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    ParentQuestionId = parentQuestionId,
                    Published = 1,
                    TypeId = typeId,
                };
                db.Questions.Add(question);
                db.SaveChanges();

                var thisParentQuestion = db.Questions.Find(question.ParentQuestionId);
                var thisSubjects = db.TreeQuestion.Where(w => w.QuestionId == question.ParentQuestionId && w.TypeId == 1).ToList();

                foreach (var subj in thisSubjects)
                {
                    TreeQuestion tq = new TreeQuestion()
                    {
                        QuestionId = question.QuestionId,
                        TreeId = subj.TreeId,
                        TypeId = 1
                    };
                    db.TreeQuestion.Add(tq);
                }
                db.SaveChanges();

                answer = new Answer()
                {
                    AnswerRu = "Ответ",
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    QuestionId = question.QuestionId,
                };
                db.Answers.Add(answer);
                db.SaveChanges();

                return Json(new { qId = question.QuestionId, answerId = answer.AnswerId });

            }
            else
            {
                var q = db.Questions.Find(QuestionId);
                q.QuestionRu = QuestionRu;
                q.ModifedDate = DateTime.Now;
                db.Entry(q).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { qId = 0 });
            }



        }

        [HttpPost]
        public string EditQuestion(int id, Question QuestionEdit)
        {
            var q = db.Questions.Find(id);
            q.QuestionRu = QuestionEdit.QuestionRu;
            q.ModifedDate = DateTime.Now;
            db.Entry(q).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public string DeleteQuestion(int id)
        {
            var qToTest = db.Rel_ToTest.Where(w => w.QuestionId == id).ToList();
            db.Rel_ToTest.RemoveRange(qToTest);
            db.SaveChanges();

            var qToAnswer = db.Answers.Where(w => w.QuestionId == id).ToList();
            db.Answers.RemoveRange(qToAnswer);
            db.SaveChanges();

            var qToTree = db.TreeQuestion.Where(w => w.QuestionId == id).ToList();
            db.TreeQuestion.RemoveRange(qToTree);
            db.SaveChanges();

            var uploads = db.Uploads.Where(w => w.DocumentId == id).ToList();
            db.Uploads.RemoveRange(uploads);
            db.SaveChanges();

            var q = db.Questions.SingleOrDefault(w => w.QuestionId == id);
            q.ModifedDate = DateTime.Now;
            q.Deleted = 1;
            db.Entry(q).State = EntityState.Modified;
            db.SaveChanges();

            return "ОК";
        }

        [HttpPost]
        public string EditType(int id, Question QuestionEdit)
        {
            Answer answer;

            var q = db.Questions.Find(id);
            q.TypeId = QuestionEdit.TypeId;
            q.ModifedDate = DateTime.Now;
            db.Entry(q).State = EntityState.Modified;
            db.SaveChanges();

            if (QuestionEdit.TypeId == 105)
            {
                var a = db.Answers.Where(w => w.QuestionId == id && w.Deleted == 0);
                db.Answers.RemoveRange(a);
                db.SaveChanges();

                answer = new Answer()
                {
                    AnswerRu = "Напишите ваш ответ не более 1500 знаков",
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    QuestionId = id,
                };
                db.Answers.Add(answer);
                db.SaveChanges();
            }

            if (QuestionEdit.TypeId == 13)
            {
                var a = db.Answers.Where(w => w.QuestionId == id && w.Deleted == 0);
                db.Answers.RemoveRange(a);
                db.SaveChanges();

                answer = new Answer()
                {
                    AnswerRu = "Напишите ваш ответ не более 1500 знаков",
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    QuestionId = QuestionEdit.QuestionId,
                };
                db.Answers.Add(answer);
                db.SaveChanges();
            }

            if (QuestionEdit.TypeId == 12)
            {
                var a = db.Answers.Where(w => w.QuestionId == id && w.Deleted == 0).ToList();
                db.Answers.RemoveRange(a);
                db.SaveChanges();

                answer = new Answer()
                {
                    AnswerRu = "Да",
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    QuestionId = id,
                };
                db.Answers.Add(answer);
                db.SaveChanges();

                answer = new Answer()
                {
                    AnswerRu = "Нет",
                    CreateDate = DateTime.Now,
                    ModifedDate = DateTime.Now,
                    QuestionId = id,
                };
                db.Answers.Add(answer);
                db.SaveChanges();

            }

            return "OK";

        }

        [HttpPost]
        public string EditLevel(int id, List<int> levelId)
        {
            var q = db.TreeQuestion.Where(w => w.QuestionId == id && w.TypeId == 3);
            db.TreeQuestion.RemoveRange(q);
            db.SaveChanges();

            foreach (var l in levelId)
            {
                TreeQuestion level;
                level = new TreeQuestion()
                {
                    QuestionId = id,
                    TreeId = l,
                    TypeId = 3

                };
                db.TreeQuestion.Add(level);
            }

            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public string EditSubject(int id, int SubjectId)
        {
            var s = db.Questions.Find(id);

            s.ModifedDate = DateTime.Now;
            db.Entry(s).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public string EditAnswer(int id, Answer AnswerEdit)
        {
            var a = db.Answers.Find(id);
            a.AnswerRu = AnswerEdit.AnswerRu;
            a.Weight = AnswerEdit.Weight;
            a.ModifedDate = DateTime.Now;
            db.Entry(a).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public int AddAnswer(int id, Answer AnswerAdd)
        {
            if (AnswerAdd.AnswerRu == null) { AnswerAdd.AnswerRu = ""; }
            Answer answer;
            answer = new Answer()
            {
                AnswerRu = AnswerAdd.AnswerRu,
                Weight = AnswerAdd.Weight,
                QuestionId = id,
                CreateDate = DateTime.Now,
                ModifedDate = DateTime.Now
            };
            db.Answers.Add(answer);
            db.SaveChanges();

            int aId = answer.AnswerId;
            return aId;
        }

        [HttpPost]
        public string DeleteAnswer(int id)
        {
            var a = db.Answers.Find(id);
            db.Answers.Remove(a);
            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public string DeleteAllAnswer(int id, List<int> AnswerList)
        {
            var aa = db.Answers.Where(w => w.QuestionId == id).ToList();
            db.Answers.RemoveRange(aa);
            db.SaveChanges();
            return "OK";
        }

        [HttpPost]
        public string DeleteCategory(int id)
        {
            var t = db.Tree.Find(id);
            t.ModifedDate = DateTime.Now;
            t.Deleted = 1;
            db.Entry(t).State = EntityState.Modified;


            var t_child = db.Tree.Where(w => w.ParentId == id).ToList();

            foreach (var cat in t_child)
            {
                cat.ModifedDate = DateTime.Now;
                cat.Deleted = 1;
                db.Entry(cat).State = EntityState.Modified;
            }
            db.SaveChanges();
            return "OK";
        }

        [HttpPost]
        public string EditCategory(int id, string name, int parentId = 0)
        {
            var t = db.Tree.Find(id);
            t.NameRu = name;
            t.ParentId = parentId;
            if (parentId == 0)
            {
                t.ParentId = null;
            };
            t.ModifedDate = DateTime.Now;
            db.Entry(t).State = EntityState.Modified;
            db.SaveChanges();
            return "OK";
        }

        [HttpPost]
        public string CreateCategory(int ParentId, string NameCategory, int TreeTypeId)
        {

            var tree = new Tree();
            if (NameCategory == null)
            {
                NameCategory = "Новая категория";
            }

            if (ParentId != 0)
            {
                tree.ParentId = ParentId;
            }

            tree.TypeId = TreeTypeId;
            tree.NameRu = NameCategory;
            tree.CreateDate = DateTime.Now;

            db.Tree.Add(tree);
            db.SaveChanges();

            return "OK";
        }

        [HttpPost]
        public int GetLastAnswerId(int id)
        {
            var AnswerLastId = db.Questions.Max(w => w.QuestionId);
            //var answerLastId = db.Answers.Max(w => w.AnswerId);

            return AnswerLastId;
        }

        [HttpPost]
        public string Published(int id)
        {
            var q = db.Questions.SingleOrDefault(s => s.QuestionId == id);
            if (q.Published == 1)
            {
                q.Published = 0;
            }
            else
            {
                q.Published = 1;
            }
            db.Entry(q).State = EntityState.Modified;
            db.SaveChanges();
            return "OK";
        }

        #endregion

        #region Test

        [HttpPost]
        public string EditName(int id, string Name)
        {
            var en = db.TestTemplate.Find(id);
            en.NameRu = Name;
            db.Entry(en).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public string Feedback(int id, int opros)
        {
            var en = db.TestTemplate.Find(id);
            en.TypeTemplateId = opros;
            db.Entry(en).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        public string EditDuration(int id, int Duration)
        {
            var d = db.TestTemplate.Find(id);
            d.Duration = Duration;
            db.Entry(d).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        public string EditTryCount(int id, int TryCount)
        {
            var t = db.TestTemplate.Find(id);
            t.TryCount = TryCount;
            db.Entry(t).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public string EditLevelForTestTemplateItem(int id, int levelId)
        {
            var ltt = db.TestTemplate.Find(id);
            ltt.LevelId = levelId;
            db.Entry(ltt).State = EntityState.Modified;
            db.SaveChanges();
            return "";
        }

        [HttpPost]
        public string UpdateDepartmentsForTestTemplate(int id, List<int> CategoryList)
        {
            var rel = db.TestTemplateItem.Where(w => w.TestTemplateId == id && w.CategoryType == 2);
            db.TestTemplateItem.RemoveRange(rel);
            db.SaveChanges();
            if (CategoryList != null)
            {
                foreach (var cat in CategoryList)
                {
                    TestTemplateItem testTemplateItem = new TestTemplateItem()
                    {
                        CategoryId = cat,
                        CategoryType = 2,
                        TestTemplateId = id,
                        CreateDate = DateTime.Now,
                    };
                    db.TestTemplateItem.Add(testTemplateItem);
                }
                db.SaveChanges();
            }
            return "OK";
        }

        [HttpPost]
        public string UpdateLevelsForTestTemplate(int id, List<int> CategoryList)
        {
            var rel = db.TestTemplateItem.Where(w => w.TestTemplateId == id && w.CategoryType == 3);
            db.TestTemplateItem.RemoveRange(rel);
            db.SaveChanges();
            if (CategoryList != null)
            {
                foreach (var cat in CategoryList)
                {
                    TestTemplateItem testTemplateItem = new TestTemplateItem()
                    {
                        CategoryId = cat,
                        CategoryType = 3,
                        TestTemplateId = id,
                        CreateDate = DateTime.Now,
                    };
                    db.TestTemplateItem.Add(testTemplateItem);
                }
                db.SaveChanges();

                var thisTestTemplate = db.TestTemplate.Find(id);
                thisTestTemplate.LevelId = CategoryList.First();
                db.Entry(thisTestTemplate).State = EntityState.Modified;
                db.SaveChanges();
            }
            return "OK";
        }

        [HttpPost]
        public string UpdateQuestionQuantityForTestTemplate(int Id, int QuestionQuantity, int TreeId)
        {
            var rel = db.TestTemplateItem.SingleOrDefault(w => w.TestTemplateId == Id && w.CategoryId == TreeId && w.CategoryType == 1);
            rel.QuestionQuantity = QuestionQuantity;
            db.SaveChanges();

            return "OK";
        }

        [HttpPost]
        public string AddSubjectForTestTemplate(int Id, int TreeId, int QuestionQuantity)
        {
            TestTemplateItem testTemplateItem;
            testTemplateItem = new TestTemplateItem()
            {
                CategoryType = 1,
                CategoryId = TreeId,
                QuestionQuantity = QuestionQuantity,
                TestTemplateId = Id,
                CreateDate = DateTime.Now,
            };
            db.TestTemplateItem.Add(testTemplateItem);
            db.SaveChanges();
            return "OK";
        }

        [HttpPost]
        public string DeleteSubject(int id, int TreeId)
        {
            var sToTest = db.TestTemplateItem.SingleOrDefault(w => w.TestTemplateId == id && w.CategoryId == TreeId);
            db.TestTemplateItem.Remove(sToTest);
            db.SaveChanges();

            return "ОК";
        }

        [HttpPost]
        public string DeleteTestTemplate(int id)
        {
            var tt = db.TestTemplate.Find(id);
            tt.ModifedDate = DateTime.Now;
            tt.Saved = -1;
            db.Entry(tt).State = EntityState.Modified;
            db.SaveChanges();
            return "OK";
        }

        [HttpPost]
        public string SaveTestTemplate(int id, int targetProcent)
        {
            var stt = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == id);
            stt.Saved = 1;
            stt.TargetProcent = targetProcent;
            db.Entry(stt).State = EntityState.Modified;
            db.SaveChanges();
            return "Шаблон сохранен";
        }

        // Сохранение результатов прохождения теста
        [HttpPost]
        public string SaveResult(int id, int idAnswer, int index, List<Answer> AnswersList, int qId)
        {
            var allAnswers = db.TestsItem.Where(w => w.TestsId == id && w.QuestionId == qId).ToList();

            if (allAnswers != null)
            {
                foreach (var a in allAnswers)
                {
                    var thisAnswer = db.TestsItem.SingleOrDefault(s => s.TestsId == id && s.QuestionId == qId && s.AnswerId == a.AnswerId);
                    thisAnswer.Selected = null;
                    db.Entry(thisAnswer).State = EntityState.Modified;

                }
                db.SaveChanges();
            }

            if (AnswersList != null)
            {
                foreach (var a in AnswersList)
                {
                    var thisAnswer = db.TestsItem.SingleOrDefault(s => s.TestsId == id && s.QuestionId == qId && s.AnswerId == a.AnswerId);
                    thisAnswer.Selected = a.AnswerId;
                    db.Entry(thisAnswer).State = EntityState.Modified;

                }
                db.SaveChanges();
            }

            var test = db.Tests.SingleOrDefault(s => s.TestId == id);
            test.Progress = index;
            db.Entry(test).State = EntityState.Modified;
            db.SaveChanges();

            return "Ответ принят!";
        }

        [HttpPost]
        public string SaveResultSingle(int id, int idAnswer, int index, int idQuestion)
        {
            var allAnswersByQuestion = db.TestsItem.Where(w => w.TestsId == id && w.QuestionId == idQuestion).ToList();
            foreach (var item in allAnswersByQuestion)
            {
                item.Selected = null;
                db.Entry(item).State = EntityState.Modified;
            }
            db.SaveChanges();

            var ti = db.TestsItem.SingleOrDefault(s => s.TestsId == id && s.AnswerId == idAnswer);
            ti.Selected = idAnswer;
            db.Entry(ti).State = EntityState.Modified;
            db.SaveChanges();

            var test = db.Tests.SingleOrDefault(s => s.TestId == id);
            test.Progress = index;
            db.Entry(test).State = EntityState.Modified;
            db.SaveChanges();

            return "Ответ принят!";
        }

        [HttpPost]
        public string SaveSubAnswer(int page, int testId, int qId, int AnswerId)
        {
            var ti = db.TestsItem.SingleOrDefault(s => s.TestsId == testId && s.QuestionId == qId);
            ti.Selected = AnswerId;
            db.Entry(ti).State = EntityState.Modified;
            db.SaveChanges();

            var test = db.Tests.SingleOrDefault(s => s.TestId == testId);
            test.Progress = page;
            db.Entry(test).State = EntityState.Modified;
            db.SaveChanges();

            return "Ответ принят!";
        }

        [HttpPost]
        public string SaveText(int id, string text, int index, int idQuestion)
        {
            var ta = db.TestsItem.Where(s => s.TestsId == id && s.QuestionId == idQuestion).ToList();

            foreach (var t in ta)
            {
                t.Answers = text;
                db.Entry(t).State = EntityState.Modified;
            }
            db.SaveChanges();

            var test = db.Tests.SingleOrDefault(s => s.TestId == id);
            test.Progress = index;
            db.Entry(test).State = EntityState.Modified;
            db.SaveChanges();

            return "Ответ принят!";
        }

        [HttpPost]
        public string SaveTextMark(int qid, int mark, int attestationId, int testId)
        {
            var tm = db.TestsItem.Where(s => s.TestsId == testId && s.QuestionId == qid).ToList();

            foreach (var t in tm)
            {
                t.ManagerMark = mark;
                db.Entry(t).State = EntityState.Modified;
            }
            db.SaveChanges();

            return "Оценка сохранена";
        }

        [HttpPost]
        public string SummTextAnswers(double summ, int attestationId, int testId)
        {
            var t = db.Tests.SingleOrDefault(s => s.TestId == testId);

            t.CheckedText = summ;
            db.Entry(t).State = EntityState.Modified;
            db.SaveChanges();

            return "Оценка сохранена";
        }

        public string SaveProgress(int id, int progress)
        {
            var ti = db.Tests.SingleOrDefault(s => s.TestId == id);
            ti.Progress = progress;
            db.Entry(ti).State = EntityState.Modified;
            db.SaveChanges();

            return "OK";
        }

        // Проверка завершенности теста
        [HttpPost]
        public int? ChekFIn(int id)
        {
            int? fin = db.Tests.Find(id).Finished;

            return fin;
        }

        #endregion

        #region Feedback

        [HttpPost]
        public string SaveFeedback(int userId, int FeedbackId, int QuestionId, int AnswerId, List<Answer> Selected, string Text, int TypeId)
        {
            if (TypeId == 12 || TypeId == 11)
            {
                var allAnswers = db.FeedbackItems.Where(w => w.FeedbackId == FeedbackId && w.QuestionId == QuestionId).ToList();

                if (allAnswers != null)
                {
                    foreach (var a in allAnswers)
                    {
                        var thisAnswer = db.FeedbackItems.SingleOrDefault(s => s.FeedbackId == FeedbackId && s.QuestionId == QuestionId && s.AnswerId == a.AnswerId);
                        thisAnswer.Selected = null;
                        db.Entry(thisAnswer).State = EntityState.Modified;

                    }
                    db.SaveChanges();
                }

                var fbi = db.FeedbackItems.FirstOrDefault(w => w.UserId == userId && w.FeedbackId == FeedbackId && w.QuestionId == QuestionId && w.AnswerId == AnswerId);
                fbi.Selected = AnswerId;
                db.Entry(fbi).State = EntityState.Modified;
                db.SaveChanges();
                return "";
            }

            if (TypeId == 105)
            {
                var fbi = db.FeedbackItems.FirstOrDefault(w => w.UserId == userId && w.FeedbackId == FeedbackId && w.QuestionId == QuestionId);
                fbi.Text = Text;
                db.Entry(fbi).State = EntityState.Modified;
                db.SaveChanges();
                return "";
            }
            if (TypeId == 53)
            {
                var allAnswers = db.FeedbackItems.Where(w => w.FeedbackId == FeedbackId && w.QuestionId == QuestionId).ToList();

                if (allAnswers != null)
                {
                    foreach (var a in allAnswers)
                    {
                        var thisAnswer = db.FeedbackItems.SingleOrDefault(s => s.FeedbackId == FeedbackId && s.QuestionId == QuestionId && s.AnswerId == a.AnswerId);
                        thisAnswer.Selected = null;
                        db.Entry(thisAnswer).State = EntityState.Modified;

                    }
                    db.SaveChanges();
                }

                if (Selected != null)
                {
                    foreach (var a in Selected)
                    {
                        var thisAnswer = db.FeedbackItems.SingleOrDefault(s => s.FeedbackId == FeedbackId && s.QuestionId == QuestionId && s.AnswerId == a.AnswerId);
                        thisAnswer.Selected = a.AnswerId;
                        db.Entry(thisAnswer).State = EntityState.Modified;
                    }
                    db.SaveChanges();
                }

                return "";
            }



            return "";
        }

        #endregion

        [HttpPost]
        public string SavePracticTest(int userId, int attestationId, int testId, int practicalExamination)
        {
            var ae = db.AttestationEmployeeItems.SingleOrDefault(s => s.UserID == userId && s.AttestationId == attestationId);
            ae.PracticalExamination = practicalExamination;
            db.Entry(ae).State = EntityState.Modified;
            db.SaveChanges();

            var t = db.Tests.SingleOrDefault(s => s.UserId == userId && s.AttestationId == attestationId && s.TestId == testId);
            t.PracticalExamination = practicalExamination;
            db.Entry(t).State = EntityState.Modified;
            db.SaveChanges();

            return "Оценка сохранена";
        }

        [HttpPost]
        public string SaveVerbalTest(int userId, int attestationId, int testId, int verbalExamination)
        {
            var ae = db.AttestationEmployeeItems.SingleOrDefault(s => s.UserID == userId && s.AttestationId == attestationId);

            ae.VerbalExamination = verbalExamination;
            db.Entry(ae).State = EntityState.Modified;
            db.SaveChanges();

            var t = db.Tests.SingleOrDefault(s => s.UserId == userId && s.AttestationId == attestationId && s.TestId == testId);
            t.VerbalExamination = verbalExamination;
            db.Entry(t).State = EntityState.Modified;
            db.SaveChanges();

            return "Оценка сохранена";
        }

        [HttpPost]
        public string UpdateAchievementsLevel(int userId, int level, int appUserId)
        {
            var thisUserAchievements = db.UsersAchievements.Where(w => w.UserId == userId).ToList();
            var ua = thisUserAchievements.LastOrDefault();

            ua.LevelId = level;
            ua.ModifiedBy = appUserId;
            ua.ModifiedDate = DateTime.Now;
            db.Entry(ua).State = EntityState.Modified;
            db.SaveChanges();

            return "Уровень изменен!";
        }

        [HttpPost]
        public string SaveRecomendLevel(int userId, int attestationId, int recommendedLevel, int appUserId, int summation, int LMConfirm, int HRConfirm)
        {
            var ae = db.AttestationEmployeeItems.SingleOrDefault(s => s.UserID == userId && s.AttestationId == attestationId);
            var roleAdmin = db.UserInRole.FirstOrDefault(f => f.UserId == appUserId);
            var acces = db.UserInRole.Where(w => w.UserId == appUserId).ToList();

            if (ae.RecommendedLevel != null) // Редактирование и подверждение рекомендованного уровня
            {
                if (/*roleAdmin.RoleId == 1 ||*/ roleAdmin.AccessLevelId == 2) // Подверждение уровня руководителем аттестации
                {
                    ae.LMConfirmRL = LMConfirm;
                    ae.LMConfirmRLById = appUserId;
                    //ae.LMConfirmRLDate = DateTime.Now;
                    db.Entry(ae).State = EntityState.Modified;
                    db.SaveChanges();
                    return "Уровень подвержден LM";
                }
                else if (/*roleAdmin.RoleId == 2 || */roleAdmin.AccessLevelId == 2) // Подверждение уровня HR
                {
                    ae.HRConfirmRL = HRConfirm;
                    ae.HRConfirmRLById = appUserId;
                    //ae.HRConfirmRLDate = DateTime.Now;
                    db.Entry(ae).State = EntityState.Modified;
                    db.SaveChanges();
                    return "Уровень подвержден HR";
                }
                // После утверждения уровня его нельзя редактировать
                else if ((roleAdmin.RoleId == null && acces.Count > 0 && ae.LMConfirmRL == null && ae.HRConfirmRL == null) || roleAdmin.AccessLevelId == 1)
                {
                    ae.RecommendedLevel = recommendedLevel;
                    ae.RecommendedByUserId = appUserId;
                    //ae.RecommendedDate = DateTime.Now;
                    ae.AttestationMark = summation;
                    db.Entry(ae).State = EntityState.Modified;
                    db.SaveChanges();
                    return "Рекомендованный уровень сохранен";
                }
            }
            // 
            else if ((roleAdmin.RoleId == null && acces.Count > 0 && ae.LMConfirmRL == null && ae.HRConfirmRL == null) || roleAdmin.AccessLevelId == 1)
            {
                ae.RecommendedLevel = recommendedLevel;
                ae.RecommendedByUserId = appUserId;
                ae.RecommendedDate = DateTime.Now;
                ae.AttestationMark = summation;
                db.Entry(ae).State = EntityState.Modified;
                db.SaveChanges();
                return "Рекомендованный уровень сохранен";
            }
            return "ОК";
        }

        [HttpPost]
        public string UpgradeLevel(int userId, int attestationId, int verbalExamination)
        {
            var ae = db.AttestationEmployeeItems.SingleOrDefault(s => s.UserID == userId && s.AttestationId == attestationId);

            ae.VerbalExamination = verbalExamination;
            db.Entry(ae).State = EntityState.Modified;
            db.SaveChanges();

            return "Оценка сохранена";
        }

        #region InfoBaze

        [HttpPost]
        public string SaveCategory(int id, int userId, string name)
        {
            var c = db.Tree.SingleOrDefault(s => s.TreeId == id);

            c.NameRu = name;
            c.ModifiedBy = userId;
            c.ModifedDate = DateTime.Now;
            db.Entry(c).State = EntityState.Modified;
            db.SaveChanges();

            return "Название сохранено";
        }

        [HttpPost]
        public int addCategory(int id, int userId, string name, int type)
        {
            //var lastChild = db.Tree.Where(w => w.ParentId == id).ToList();
            //var rightKey = lastChild.Last().Ns_right;

            var category = new Tree();
            // Защита от глюка с множественным созданием категорий
            if (name == "Новый объект")
            {

                category.CreateDate = DateTime.Now;
                category.CreatedByUserId = userId;
                category.ParentId = id;
                category.NameRu = name;
                category.TypeId = type;
                category.Deleted = 0;

                db.Tree.Add(category);
                db.SaveChanges();
            }


            //var parentRightKey = db.t_Categories.SingleOrDefault(s => s.Categories_id == id).Ns_right;
            //var rightSideKeys = db.t_Categories.Where(w => w.Ns_right >= parentRightKey).ToList();

            //rightSideKeys.ForEach(a => a.Ns_left = a.Ns_left +2);
            //rightSideKeys.ForEach(a => a.Ns_right = a.Ns_right + 2);
            //db.SaveChanges();

            return category.TreeId;
        }

        #endregion

        #region Other 

        [HttpPost]
        public string SaveTemplateForAtt(int idAtt, int idTemplate)
        {
            var a = db.Attestations.SingleOrDefault(s => s.AttestationId == idAtt);

            a.TestTemplateId = idTemplate;
            a.LastModifiedDate = DateTime.Now;
            db.Entry(a).State = EntityState.Modified;
            db.SaveChanges();

            return "Ok";
        }

        #endregion

        //GetMarkAttestationUsers
        [HttpGet]
        public JsonResult GetUserMark(int id = 0)
        {
            // Подключение пользователя
            #region SetUser
            var allUsers = Helper.GetAllUsers();
            var appUser = Helper.GetAppUser(User.Identity);

            if (appUser == null)
            {
                return Json(String.Empty, JsonRequestBehavior.AllowGet);
            }

            //var access = db.UserInRole.Where(x => x.UserId == appUser.UserId && (x.AccessLevelId == 1 || x.RoleId == 2)).ToList();
            //var admin = db.UserInRole.FirstOrDefault(x => x.AccessLevelId == 1);
            //var sevenGods = db.UserInRole.Where(x => x.AccessLevelId == 2).Select(x => x.UserId);
            //var AttestationLMUsers = db.Attestations.Select(x => x.LMUserId);
            #endregion

            if (id != 0)
            {
                var attestation = db.Attestations.SingleOrDefault(s => s.AttestationId == id);
                var usersmarks = attestation.empItems.ToList();

                var tests = db.Tests.Where(w => w.AttestationId == id).ToList();

                List<ViewUsersAttestationReport> userList = new List<ViewUsersAttestationReport>();

                var thisTestTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == attestation.TestTemplateId);
                var actualCategoryId = db.TestTemplateItem.Where(w => w.TestTemplateId == thisTestTemplate.TestTemplateId).Select(s => s.CategoryId).ToArray();
                var actualCategory = db.Tree.Where(w => w.TypeId == 1 && actualCategoryId.Contains(w.TreeId)).ToList();
                var testsToAttestation = tests.Where(w => w.AttestationId == attestation.AttestationId).ToList();

                var attestationTestsId = testsToAttestation.Select(s => s.TestId).ToArray();
                var actualTestsItems = db.TestsItem.Where(w => attestationTestsId.Contains(w.TestsId)).ToList();
                var actualTestsItemsQuestionsId = actualTestsItems.Where(w => w.Answers == null).Select(s => s.QuestionId).Distinct().ToArray();
                var actualQuestions = db.Questions.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();
                var actualTestsItemsTextQuestionsId = actualTestsItems.Where(w => w.Answers != null).Select(s => s.QuestionId).Distinct().ToArray();
                var actualTextQuestions = db.Questions.Where(w => actualTestsItemsTextQuestionsId.Contains(w.QuestionId)).ToList();

                var answers = db.Answers.Where(w => actualTestsItemsQuestionsId.Contains(w.QuestionId)).ToList();

                int? targetProcent = 0;
                var thisTemplate = db.TestTemplate.SingleOrDefault(s => s.TestTemplateId == attestation.TestTemplateId);
                if (thisTemplate != null)
                {
                    targetProcent = thisTemplate.TargetProcent;
                }

                var rezak = 0;
                //2.Уровень участников аттестации
                foreach (var user in usersmarks)
                {
                    rezak++;
                    //if (rezak < 20) { 
                    var testsList = db.Tests.Where(w => w.UserId == user.UserID && w.Finished == 1 && w.AttestationId == attestation.AttestationId).ToList();//(w.UserId == user.UserID || w.SUserId == user.SUserId)
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
                        TargetProcent = targetProcent,
                        RecomendLevel = thisUserRecomendLevel,
                        LMConfirmRL = user.LMConfirmRL,
                        HRConfirmRL = user.HRConfirmRL,
                        UsersAchievements = usersAchievements,
                    });
                    //}
                }


                var javascriptserializer = new JavaScriptSerializer();
                javascriptserializer.MaxJsonLength = Int32.MaxValue;

                return Json(new { userList }, JsonRequestBehavior.AllowGet);
            }

            return Json(String.Empty, JsonRequestBehavior.AllowGet);

        }
    }
}