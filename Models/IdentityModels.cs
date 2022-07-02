//using System.Data.Entity;
//using Microsoft.AspNet.Identity.EntityFramework;
//using Microsoft.AspNet.Identity;
//using Microsoft.AspNet.Identity.Owin;
//using Microsoft.Owin;

//using System;
//using System.ComponentModel.DataAnnotations;

//namespace AspNetIdentityApp.Models
//{

//    public class RegisterModel
//    {
//        [Required]
//        public string Email { get; set; }

//        [Required]
//        [DataType(DataType.Password)]
//        public string Password { get; set; }

//        [Required]
//        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
//        [DataType(DataType.Password)]
//        public string PasswordConfirm { get; set; }
//    }

//    public class LoginModel
//    {
//        [Required]
//        public string Email { get; set; }
//        [Required]
//        [DataType(DataType.Password)]
//        public string Password { get; set; }
//    }
//}

//public class ApplicationContext : IdentityDbContext<ApplicationUser>
//{
//    public ApplicationContext() : base("DefaultConnection") { }

//    public static ApplicationContext Create()
//    {
//        return new ApplicationContext();
//    }
//}

//public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
//{
//    public ApplicationDbContext()
//        : base("DefaultConnection", throwIfV1Schema: false)
//    {
//    }

//    public static ApplicationDbContext Create()
//    {
//        return new ApplicationDbContext();
//    }
//}


//public class ApplicationUser : IdentityUser
//{
//    //public int Year { get; set; }
//    public ApplicationUser()
//    {
//    }
//}

//public class ApplicationUserManager : UserManager<ApplicationUser>
//{
//    public ApplicationUserManager(IUserStore<ApplicationUser> store)
//            : base(store)
//    {
//    }
//    public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options,
//                                            IOwinContext context)
//    {
//        ApplicationDbContext db_i = context.Get<ApplicationDbContext>();
//        ApplicationUserManager manager = new ApplicationUserManager(new UserStore<ApplicationUser>(db_i));
//        return manager;
//    }
//}

