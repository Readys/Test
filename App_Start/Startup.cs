//using Microsoft.Owin;
//using Owin;
//using Microsoft.Owin.Security.Cookies;
//using Microsoft.AspNet.Identity;

//[assembly: OwinStartup(typeof(AspNetIdentityApp.Startup))]

//namespace AspNetIdentityApp
//{
//    public class Startup
//    {
//        public void Configuration(IAppBuilder app)
//        {
//            // настраиваем контекст и менеджер
//            app.CreatePerOwinContext<ApplicationDbContext>(ApplicationDbContext.Create);
//            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
//            app.UseCookieAuthentication(new CookieAuthenticationOptions
//            {
//                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
//                LoginPath = new PathString("/Account/Login"),
//            });
//        }
//    }
//}

