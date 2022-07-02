using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;
using TTS.Models;

namespace TTS.Controllers
{
    public class TestController : ApiController
    {
        private TTSEntities db = new TTSEntities();

            // GET api/Test
            public IEnumerable<string> Get()
        {
            var questionLastId = db.Questions.Max(w => w.QuestionId);
           
            return new string[] { "value1", "value2" };
        }

        // GET api/Test/5
        public string Get(int id)
        {
            ////Формируем задание в тесте
           // var test = db.Tests.SingleOrDefault(s => s.TestId == id);
           // var questionId = db.Rel_ToTest.SingleOrDefault(s => s.TestId == id).QuestionId;
           // var question = db.Questions.SingleOrDefault(s => s.QuestionId == questionId);

           // var answers = db.AnswerToQuestion.Where(s => s.QuestionId == id).Select(s => s.AnswerId).ToList();
          //  var answersById = db.Answers.Where(w => answers.Contains(w.AnswerId)).ToList();

            return "question.QuestionRu";
        }

        // POST api/values
        public void Post([FromBody] ViewQuestion testRecord)
        {
            //var timeNow = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss,fff");
            //DateTime myDate = DateTime.ParseExact(timeNow, "yyyy-MM-dd HH:mm:ss,fff",System.Globalization.CultureInfo.InvariantCulture);

            //  if (testId == 1)
            // {
       /*     Test test;
            test = new Test()
            {
                Grade = testRecord.Level,CreateDate = DateTime.Now,ModifedDate = DateTime.Now, CreatedByUserId = 1
            };
            db.Test.Add(test);
            db.SaveChanges();

            int tId = test.TestId;
            test = db.Test.Find(tId);
            

            //   }
            List<string> categorys = new List<string>();
            categorys = testRecord.Category;

            foreach (var catName in categorys)
            {
                Category category;
                category = new Category()
                {
                    NameRu = catName,
                    ModifedDate = DateTime.Now,
                    CreatedByUserId = 1
                };
                db.Test.Add(test);
                db.SaveChanges();

                int cId = category.CategoryId;
                category = db.Category.Find(tId);

                AttachmentToQuestion attachmentToTest;
                attachmentToTest = new AttachmentToQuestion() { AttachmentId = attId, QuestionId = tId };
                db.AttachmentToQuestion.Add(attachmentToTest);
                db.SaveChanges();

            }
            //
            Questions questions;
              questions = new Questions(){QuestionRu = testRecord.Question,CreateDate = DateTime.Now,
              ModifedDate = DateTime.Now, CreatedByUserId = 1};
              db.Questions.Add(questions);
              db.SaveChanges();

              int qId = questions.QuestionId;
              questions = db.Questions.Find(qId);

              QuestionToTest questionToTest;
              questionToTest = new QuestionToTest(){QuestionId = qId,  TestId = tId};
              db.QuestionToTest.Add(questionToTest);
              db.SaveChanges();

              List<string> answers = new List<string>();
              answers = testRecord.Answers;
              int i = 0;
              foreach (var item in answers)
              {
                  Answers answer;
                  answer = new Answers() {CreateDate = DateTime.Now, ModifedDate = DateTime.Now,
                  CreatedByUserId = 1, AnswerRu = item, ShortAnswer = ""  };
                  db.Answers.Add(answer);
                  db.SaveChanges();

                  int aId = answer.AnswerId;
                  answer = db.Answers.Find(aId);

                  AnswerToQuestion answersToTest;
                  answersToTest = new AnswerToQuestion() { AnswerId = aId, QuestionId = tId };
                  if (testRecord.AnswerWeight[i] > 0) { answersToTest.RightAnswer = true; answersToTest.Weight = testRecord.AnswerWeight[i]; }
                  db.AnswerToQuestion.Add(answersToTest);
                  db.SaveChanges();
                  i++;
              }

              List<int> attachments = new List<int>();
              attachments = testRecord.FileId;

              foreach (var item in attachments)
              {
                  Attachment attachment;
                  attachment = new Attachment()
                  {
                      CreateDate = DateTime.Now,
                      CreatedByUserId = 1,
                      Path = item + "",
                  };
                  db.Attachment.Add(attachment);
                  db.SaveChanges();

                  int attId = attachment.AttachmentId;
                  attachment = db.Attachment.Find(attId);

                  AttachmentToQuestion attachmentToTest;
                  attachmentToTest = new AttachmentToQuestion() { AttachmentId = attId, QuestionId = tId };
                  db.AttachmentToQuestion.Add(attachmentToTest);
                  db.SaveChanges();

              }

          */

        }


        // PUT api/values/5
        public void Put()
          {

        }


       



    [System.Web.Mvc.HttpPut]
        public string SaveTypeId(int typeId, int questionId)
        {

            return "ok";
        }
        // DELETE api/values/5
        public void Delete(int id)
          {
            /*
            var todo = _context.TodoItems.Find(id);
            if (todo == null)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todo);
            _context.SaveChanges();
            return NoContent();*/
        }
        }
    } 
