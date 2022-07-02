//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Security.Claims;
//using System.Threading.Tasks;
//using System.Web;
//using System.Web.Mvc;
//using AspNetIdentityApp.Models;
//using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.Owin;
//using Microsoft.Owin.Security;

//namespace AspNetIdentityApp.Controllers
//{
//    public class AccountController : Controller
//    {
//        private ApplicationUserManager UserManager
//        {
//            get
//            {
//                return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
//            }
//        }

//        public ActionResult Register()
//        {
//            return View();
//        }

//        [HttpPost]
//        public async Task<ActionResult> Register(RegisterModel model)
//        {
//            if (ModelState.IsValid)
//            {
//                ApplicationUser user = new ApplicationUser { UserName = model.Email, Email = model.Email, };
//                IdentityResult result = await UserManager.CreateAsync(user, model.Password);
//                if (result.Succeeded)
//                {
//                    return RedirectToAction("Login", "Account");
//                }
//                else
//                {
//                    foreach (string error in result.Errors)
//                    {
//                        ModelState.AddModelError("", error);
//                    }
//                }
//            }
//            return View(model);
//        }

//        private IAuthenticationManager AuthenticationManager
//        {
//            get
//            {
//                return HttpContext.GetOwinContext().Authentication;
//            }
//        }

//        public ActionResult Login(string returnUrl)
//        {
//            ViewBag.returnUrl = returnUrl;
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<ActionResult> Login(LoginModel model, string returnUrl)
//        {
//            if (ModelState.IsValid)
//            {
//                ApplicationUser user = await UserManager.FindAsync(model.Email, model.Password);
//                if (user == null)
//                {
//                    ModelState.AddModelError("", "Неверный логин или пароль.");
//                }
//                else
//                {
//                    ClaimsIdentity claim = await UserManager.CreateIdentityAsync(user,
//                                            DefaultAuthenticationTypes.ApplicationCookie);
//                    AuthenticationManager.SignOut();
//                    AuthenticationManager.SignIn(new AuthenticationProperties
//                    {
//                        IsPersistent = true
//                    }, claim);
//                    if (String.IsNullOrEmpty(returnUrl))
//                        return RedirectToAction("Index", "Home");
//                    return Redirect(returnUrl);
//                }
//            }
//            ViewBag.returnUrl = returnUrl;
//            return View(model);
//        }
//        public ActionResult Logout()
//        {
//            AuthenticationManager.SignOut();
//            return RedirectToAction("Login");
//        }

//        #region Удаление пользователей
//        [HttpGet]
//        public ActionResult Delete()
//        {
//            return View();
//        }

//        [HttpPost]
//        [ActionName("Delete")]
//        public async Task<ActionResult> DeleteConfirmed()
//        {
//            ApplicationUser user = await UserManager.FindByEmailAsync(User.Identity.Name);
//            if (user != null)
//            {
//                IdentityResult result = await UserManager.DeleteAsync(user);
//                if (result.Succeeded)
//                {
//                    return RedirectToAction("Logout", "Account");
//                }
//            }
//            return RedirectToAction("Index", "Home");
//        }
//        #endregion

//    }
//}