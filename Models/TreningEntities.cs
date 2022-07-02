using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using TTS.Models;

//using HRM.Models;

namespace TTS.Models
{
    public class TTSEntities : DbContext
    {
        public TTSEntities() : base("DefaultConnection") { }

        public DbSet<Question> Questions { get; set; }
        public DbSet<Tree> Tree { get; set; }
        public DbSet<Answer> Answers { get; set; }

        public DbSet<Department> Department { get; set; }
        public DbSet<TreeQuestion> TreeQuestion { get; set; }
        public DbSet<RelToTest> Rel_ToTest { get; set; }
        public DbSet<TestTemplate> TestTemplate { get; set; }
        public DbSet<TestTemplateItem> TestTemplateItem { get; set; }

        public DbSet<TreeType> TreeType { get; set; }
        public DbSet<TestsItem> TestsItem { get; set; }
        public DbSet<Tests> Tests { get; set; }
        //public DbSet<TryCounts> TryCounts { get; set; }

        public DbSet<Category> Categories { get; set; }
        public DbSet<UserInRole> UserInRole { get; set; }


        public DbSet<Upload> Uploads { get; set; }
        public DbSet<Attestation> Attestations { get; set; }
        public DbSet<AttestationEmployeeItems> AttestationEmployeeItems { get; set; }

        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<FeedbackItems> FeedbackItems { get; set; }

        public DbSet<UsersAchievements> UsersAchievements { get; set; }

        public DbSet<t_Categories> t_Categories { get; set; }

        public DbSet<L_User> L_User { get; set; }
        public DbSet<NetUsers> NetUsers { get; set; }

    }

    public class TTS2Entities : DbContext
    {
        public TTS2Entities() : base("DefaultConnection2") { }

        public DbSet<L_User> L_User { get; set; }
        public DbSet<NetUsers> NetUsers { get; set; }

    }

    public class HRMEntities : DbContext
    {
        public HRMEntities() : base("HRMConnection") { }

        public DbSet<Region> Region { get; set; }
        public DbSet<Branch> Branch { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Position> Position { get; set; }
        public DbSet<Division> Division { get; set; }


    }

}