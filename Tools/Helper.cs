using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using HRM.Models;
using Newtonsoft.Json;

namespace HRM.Tools
{
    public class Helper
    {

        private static PrincipalContext _ctx;
        private static UserPrincipal _user;
        private static DirectoryEntry _directoryEntry;
        private static PropertyValueCollection _collection;

        public static double GetBusinessDays(DateTime startD, DateTime endD, IEnumerable<HolidaysDaysOff> holiday = null)
        {
            double calcBusinessDays =
                1 + ((endD - startD).TotalDays * 5 -
                (startD.DayOfWeek - endD.DayOfWeek) * 2) / 7;

            if ((int)endD.DayOfWeek == 6) calcBusinessDays--;
            if ((int)startD.DayOfWeek == 0) calcBusinessDays--;

            return calcBusinessDays;
        }

        //Количество выходных дней с учетом праздников и переносов рабочих дней на выходные
        public static double GetFreeDaysCount(DateTime startDate, DateTime endDate, IEnumerable<HolidaysDaysOff> holiday)
        {

            TimeSpan dateDiff = endDate - startDate;

            var weekends = dateDiff.TotalDays + 1 - GetBusinessDays(startDate, endDate);

            //Праздник
            var holidays = holiday.Where(x => x.TypeId == 1 && (x.Date >= startDate && x.Date <= endDate)).ToList();
            foreach (var h in holidays)
            {
                if (h.Date.DayOfWeek == DayOfWeek.Saturday || h.Date.DayOfWeek == DayOfWeek.Sunday)
                {
                    //Если праздник попал на выходной убераем его из расчетов
                    weekends--;
                }
            }


            //Выходной
            var dayOff = holiday.Where(x => x.TypeId == 2 && (x.Date >= startDate && x.Date <= endDate)).ToList();
            foreach (var o in dayOff)
            {
                if (o.Date.DayOfWeek == DayOfWeek.Saturday || o.Date.DayOfWeek == DayOfWeek.Sunday)
                {
                    //Если праздник попал на выходной убераем его из расчетов
                    weekends--;
                }
            }


            //Рабочий день
            var workDays = holiday.Where(x => x.TypeId == 3 && (x.Date >= startDate && x.Date <= endDate)).ToList();
            foreach (var w in workDays)
            {
                if (w.Date.DayOfWeek == DayOfWeek.Saturday || w.Date.DayOfWeek == DayOfWeek.Sunday)
                {
                    //Если праздник попал на выходной убераем его из расчетов
                    weekends--;
                }
            }

            return weekends + holidays.Count() + dayOff.Count() - workDays.Count();


        }

        public static bool CreateFolderIfNeeded(string path)
        {
            var result = true;
            if (Directory.Exists(path)) return true;

            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public class MyDateTimeConverter : Newtonsoft.Json.JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(DateTime);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var t = long.Parse((string)reader.Value);
                return new DateTime(1970, 1, 1).AddMilliseconds(t);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }

        public static void RequestResponse(string message, int errCode = 0)
        {
            string errMessage;

            HttpResponse response = System.Web.HttpContext.Current.Response;
            System.Web.HttpContext.Current.Server.ClearError();
            try
            {
                switch (errCode)
                {
                    case 547:
                        errMessage = "Невозможно удалить запись, т.к. на нее есть ссылки в проекте";
                        break;
                    default:
                        errMessage = message;
                        break;
                }

                response.Clear();
                response.StatusCode = 500;
                response.StatusDescription = errMessage;
                response.TrySkipIisCustomErrors = true;
                response.Write(errMessage);
                response.End();
            }
#pragma warning disable CS0168 // The variable 'ex' is declared but never used
            catch (Exception ex)
#pragma warning restore CS0168 // The variable 'ex' is declared but never used
            {

            }

            //try
            //{
            //    HttpContext.Current.Response.ClearHeaders();
            //    HttpContext.Current.Response.ClearContent();
            //    if (message != null)
            //    {

            //        HttpContext.Current.Response.Status = "503 ServiceUnavailable";
            //        HttpContext.Current.Response.StatusCode = 503;
            //        HttpContext.Current.Response.StatusDescription = HttpUtility.JavaScriptStringEncode("Мой текст");
            //        //HttpContext.Current.Response.ContentType = "text/plain";
            //        //HttpContext.Current.Response.ContentEncoding = new UTF8Encoding();
            //        //HttpContext.Current.Response.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //        //HttpContext.Current.Response.ContentEncoding = Encoding.GetEncoding("Windows-1252");
            //    }
            //    else
            //    {
            //        HttpContext.Current.Response.StatusCode = 200;
            //    }
            //    HttpContext.Current.Response.Flush();
            //}
            //catch (Exception ex)
            //{
            //    writeEventLog("MCS", "requestResponse", ex.Message);
            //}
        }


        public static void SendMail(string from, List<string> toList, string subject, string messageText)
        {
            var configMail = ConfigurationManager.GetSection("MailBox") as NameValueCollection;

            if (from == "")
            {
                from = configMail["FromHr"];
            }

            var msg = new MailMessage { From = new MailAddress(from) };
            var to = toList.FirstOrDefault();
            msg.To.Add(new MailAddress(to));
            msg.Subject = subject;
            foreach (var item in toList)
            {
                if (to != item.Trim())
                {
                    //msg.To.Add(new MailAddress(item.Trim()));
                    msg.CC.Add(new MailAddress(item.Trim()));
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


        public static string getOrgChartUserName(OrgChart item, IEnumerable<User> allUsers)
        {

            var lang = System.Threading.Thread.CurrentThread.CurrentCulture.Name;


            var userName = "Пустой";
            var userId = 0;

            if (item != null && item.UserId != null) { userId = (int)item.UserId; }

            if (allUsers.SingleOrDefault(x => x.UserId == userId) != null)
            {
                if (lang == "ru-RU")
                {
                    userName = allUsers.SingleOrDefault(x => x.UserId == userId).NameRu;
                }

                if (lang == "en-US")
                {
                    userName = allUsers.SingleOrDefault(x => x.UserId == userId).NameEn;
                }


            }
            return userName;
        }


        public static string getOrgChartUserPhotoPath(OrgChart item, IEnumerable<User> allUsers)
        {
            var userId = 0;

            var photoPath = "";

            if (item != null && item.UserId != null) { userId = (int)item.UserId; }

            if (allUsers.SingleOrDefault(x => x.UserId == userId) != null)
            {
                photoPath = allUsers.SingleOrDefault(x => x.UserId == userId).PhotoPath;
            }
            return photoPath;
        }



        public static string getOrgChartUserPosition(OrgChart item, IEnumerable<Position> positions)
        {
            var lang = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            var title = "";
            var positionId = 0;
            if (item != null && item.PositionId != null) { positionId = (int)item.PositionId; }
            var position = positions.SingleOrDefault(x => x.PositionId == positionId);
            if (position != null)
            {
                if (lang == "ru-RU")
                {
                    title = position.NameRu;
                }
                if (lang == "en-US")
                {
                    title = position.NameEn;
                }
            }
            return title;
        }

        public static string getOrgChartUserDepartment(OrgChart item, IEnumerable<Department> departments)
        {
            var lang = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            var title = "";
            var depId = 0;
            if (item != null && item.DepartmentId != null) { depId = (int)item.DepartmentId; }
            var dep = departments.SingleOrDefault(x => x.DepartmentId == depId);
            if (dep != null)
            {
                if (lang == "ru-RU")
                {
                    title = dep.NameRu;
                }
                if (lang == "en-US")
                {
                    title = dep.NameEn;
                }
            }
            return title;
        }

        public static string Translit(string str)
        {
            string[] lat_up = { "A", "B", "V", "G", "D", "E", "Yo", "Zh", "Z", "I", "Y", "K", "L", "M", "N", "O", "P", "R", "S", "T", "U", "F", "Kh", "Ts", "Ch", "Sh", "Shch", "\"", "Y", "'", "E", "Yu", "Ya" };
            string[] lat_low = { "a", "b", "v", "g", "d", "e", "yo", "zh", "z", "i", "y", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "f", "kh", "ts", "ch", "sh", "shch", "\"", "y", "'", "e", "yu", "ya" };
            string[] rus_up = { "А", "Б", "В", "Г", "Д", "Е", "Ё", "Ж", "З", "И", "Й", "К", "Л", "М", "Н", "О", "П", "Р", "С", "Т", "У", "Ф", "Х", "Ц", "Ч", "Ш", "Щ", "Ъ", "Ы", "Ь", "Э", "Ю", "Я" };
            string[] rus_low = { "а", "б", "в", "г", "д", "е", "ё", "ж", "з", "и", "й", "к", "л", "м", "н", "о", "п", "р", "с", "т", "у", "ф", "х", "ц", "ч", "ш", "щ", "ъ", "ы", "ь", "э", "ю", "я" };
            for (int i = 0; i <= 32; i++)
            {
                str = str.Replace(rus_up[i], lat_up[i]);
                str = str.Replace(rus_low[i], lat_low[i]);
            }
            return str;
        }

        public static string OpenFileButton(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var config = ConfigurationManager.GetSection("CompanyInfo") as NameValueCollection;
                var posDoc = path.IndexOf(@"Documents\");
                var lengthDoc = path.Length;
                var pathOrder = path.Substring(posDoc, lengthDoc - posDoc);
                pathOrder = config["ProjectServerIp"] + @"/" + pathOrder.Replace(@"\", "/").Replace("Documents", config["SharedDocumentsFolder"]);
                return pathOrder;
            }
            return "";
        }

        public static ADUserProfile GetADUserProfile(string username = "", string sid = "")
        {
            var config = ConfigurationManager.GetSection("CompanyInfo") as NameValueCollection;
            try
            {
                _ctx = new PrincipalContext(ContextType.Domain, config["ActiveDirectoryIp"], config["ActiveDirectoryPath"]);

                if (username != "")
                    _user = UserPrincipal.FindByIdentity(_ctx, username.Replace(config["ActiveDirectoryName"] + "\\", ""));

                if (sid != "")
                    _user = UserPrincipal.FindByIdentity(_ctx, IdentityType.Sid, (sid));

                var profile = new ADUserProfile
                {
                    UserName = _user.DisplayName,
                    FirstName = _user.GivenName,
                    LastName = _user.Surname,
                    Email = _user.EmailAddress,
                    Phone = _user.GetProperty("mobile"),
                    City = _user.GetProperty("l"),
                    Sid = _user.Sid.Value
                };
                if (profile.City != null || profile.City != "")
                    profile.City = _user.GetProperty("physicalDeliveryOfficeName");
                _directoryEntry = (DirectoryEntry)_user.GetUnderlyingObject();
                _collection = _directoryEntry.Properties["thumbnailPhoto"];

                if (_collection.Value != null && _collection.Value is byte[])
                {
                    var thumbnailInBytes = (byte[])_collection.Value;
                    profile.Photo = thumbnailInBytes;
                }

                return profile;
            }
            catch (Exception)
            {
                try
                {
                    _ctx = new PrincipalContext(ContextType.Domain, config["ActiveDirectoryIp2"], config["ActiveDirectoryPath2"]);

                    if (username != "")
                        _user = UserPrincipal.FindByIdentity(_ctx, username.Replace("BORINTERNAL\\", "").Replace(config["ActiveDirectoryName2"] + "\\", ""));

                    if (sid != "")
                        _user = UserPrincipal.FindByIdentity(_ctx, IdentityType.Sid, (sid));

                    var profile = new ADUserProfile
                    {
                        UserName = _user.DisplayName,
                        FirstName = _user.GivenName,
                        LastName = _user.Surname,
                        Email = _user.EmailAddress,
                        Phone = _user.GetProperty("mobile"),
                        City = _user.GetProperty("l"),
                        Sid = _user.Sid.Value
                    };

                    if (profile.City != null || profile.City != "")
                        profile.City = _user.GetProperty("physicalDeliveryOfficeName");
                    _directoryEntry = (DirectoryEntry)_user.GetUnderlyingObject();
                    _collection = _directoryEntry.Properties["thumbnailPhoto"];

                    if (_collection.Value != null && _collection.Value is byte[])
                    {
                        byte[] thumbnailInBytes = (byte[])_collection.Value;
                        profile.Photo = thumbnailInBytes;
                    }
                    return profile;
                }
                catch (Exception)
                { }
                return null;
            }
        }

        public static IEnumerable<string> GetListOfMail(IEnumerable<User> users)
        {
            foreach (var item in users)
            {
                yield return item.AccountAD.Replace("BMST-KZ\\", "") + "@borusan.com";
            }
        }

        public static string GetMailByAccountAD(string AccountAD)
        {
            return AccountAD.Replace("BMST-KZ\\", "") + "@borusan.com";
        }

        public class MyDateTimeConvertorBc : Newtonsoft.Json.Converters.DateTimeConverterBase
        {
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                try
                {
                    return DateTime.Parse(reader.Value.ToString());
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                //yyyy-mm-dd
                writer.WriteValue(((DateTime)value).ToString("yyyy-MM-dd"));
            }
        }
    }


    public static class AccountManagementExtensions
    {
        public static String GetProperty(this Principal principal, String property)
        {
            DirectoryEntry directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry;
            if (directoryEntry.Properties.Contains(property))
                return directoryEntry.Properties[property].Value.ToString();
            else
                return String.Empty;
        }

        public static String GetCompany(this Principal principal)
        {
            return principal.GetProperty("company");
        }

        public static String GetDepartment(this Principal principal)
        {
            return principal.GetProperty("department");
        }

    }



}