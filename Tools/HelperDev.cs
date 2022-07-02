using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Web;
using TTS.Models;
using System.Security.Principal;
using Microsoft.Extensions.Caching.Memory;

namespace TTS.Tools
{

    public class Helper
    {

        public static void SendMail(string from, List<string> toList, string subject, string messageText)
        {
            var configMail = ConfigurationManager.GetSection("MailBox") as NameValueCollection;

            if (from == "")
            {
                from = configMail["From"];
            }

            var msg = new MailMessage { From = new MailAddress(from) };

            //var to = "armyideas@gmail.com";
            var to = toList.FirstOrDefault();
            msg.To.Add(new MailAddress(to));
            msg.Subject = subject;

            foreach (var item in toList)
            {
                if (to != item.Trim())
                {
                    msg.To.Add(new MailAddress(item.Trim()));
                    //msg.CC.Add(new MailAddress(item.Trim()));
                }
            }
            msg.Body = messageText;
            msg.Priority = MailPriority.High;
            msg.IsBodyHtml = true;

            if (configMail != null)
            {
                var smtpClient = new SmtpClient(configMail["SMTP"], Int32.Parse(configMail["SMTPPort"])) { EnableSsl = false };
                try
                {
                    smtpClient.Send(msg);
                }
                catch (Exception ex)
                {
                    var cc = ex.Message;
                }
            }
        }

        public static IEnumerable<string> GetListOfMail(IEnumerable<L_User> users)
        {
            foreach (var item in users)
            {              
                yield return item.AccountAD.ToLower().Replace("bmst-kz\\", "") + "@borusan.com";
            }
        }

        public static string GetMailByAccountAD(string AccountAD)
        {
            
            return AccountAD.ToLower().Replace("bmst-kz\\", "") + "@borusan.com";
        }

        public static L_User GetAppUser(IIdentity identy)
        {
            //Временная синхронизация

            User hrmUser = new User() { };
            L_User lUser = new L_User() { };

            string test = "Y"; // Y = Yes

            using (TTSEntities db = new TTSEntities())
            {
                lUser = db.L_User.Where(x => x.AccountAD != null || x.PrivateEmail != null).FirstOrDefault(x => x.AccountAD.ToLower() == identy.Name.ToLower());
                if (lUser != null && test != "Y")
                {
                    if (lUser.AccountAD.ToLower() == "bmst-kz\\vchepurova" || identy.Name.ToLower() == "bmak1009\\administrator") {
                        return db.L_User.SingleOrDefault(x => x.OldUserId == 39);
                    }
                    else
                    {
                        return lUser;
                    }
                }
                else if (test == "Y" || identy.Name.ToLower() == "bmst-kz\\vchepurova" || identy.Name.ToLower() == "bmak1009\\administrator") //
                {
                    return db.L_User.SingleOrDefault(x => x.OldUserId == 39); //39/70 Abeldinov  || x.OldUserId == 39  Смыкалов -34  x.UserId
                    //return db.L_User.SingleOrDefault(x => x.UserId == 1931);
                }

            }

            using (HRMEntities db_h = new HRMEntities())
            {
                hrmUser =  db_h.User.Where(x => x.AccountAD != null || x.PrivateEmail != null).FirstOrDefault(x => x.AccountAD.ToLower() == identy.Name.ToLower());
                if (hrmUser != null)
                {
                    using (TTSEntities db = new TTSEntities())
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

                    return lUser;

                }

            }

            return null;
        }

        public static List<L_User> GetAllUsers()
        {
            using (TTSEntities db = new TTSEntities())
            {

                var alluser = db.L_User.Where(w=> w.AccountAD != null || w.PrivateEmail != null).ToList();

                return alluser;

            }
        }

    }
}