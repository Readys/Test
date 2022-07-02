using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;



namespace TTS.Models
{
    [Table("_User")]
    public class L_User
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        public string NameRu { get; set; } 
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string MiddleName { get; set; }

        public string AccountAD { get; set; } //Active directory

        public int? DepartmentId { get; set; }
        public int? DivisionId { get; set; }

        public string PrivateEmail { get; set; }
        public int? BranchId { get; set; }

        public int? LineManagerUserId { get; set; }
        public int? JobTitleId { get; set; }

        public int? Admit { get; set; }

        public string Id { get; set; }

        public int CompanyId { get; set; }
        public int OldUserId { get; set; }
        
    }

    [Table("AspNetUsers")]
    public class NetUsers
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }

        public string PhoneNumber { get; set; }

        public int CompanyId { get; set; }

    }


    [Table("dic_Department")]
    public class Department
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int DepartmentId { get; set; }
        public string NameRu { get; set; }
    }

    [Table("rel_ToTest")]
    public class RelToTest
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Rel_ToTestId { get; set; }
        public int TestId { get; set; }
        public int? QuestionId { get; set; }
        public int TreeId { get; set; }        
    }

    [Table("Tree")]
    public class Tree
    {
        [Key]
        public int TreeId { get; set; }
        //[DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifedDate { get; set; }
        public int? CreatedByUserId { get; set; }
        public string NameRu { get; set; }
        public int? ParentId { get; set; }
        public int? TypeId { get; set; }
        public int? Deleted { get; set; }

        public int? ModifiedBy { get; set; }
        public int? NsLeft { get; set; }
        public int? NsRight { get; set; }
        public int? NsDepth { get; set; }
        public int? LevelNumber { get; set; }
        
    }

    public class TreeRootView
    {
        public int id { get; set; }
        public string text { get; set; }
        public bool isBatch { get; set; }
    }

    public class AnswerView
    {
        public int AnswerId { get; set; }
        public int value { get; set; }
        public int QuestionId { get; set; }
        public int Weight { get; set; }
        public string AnswerRu { get; set; }
        public bool isEdit { get; set; }
    }

    [Table("Questions")]
    public class Question
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int QuestionId { get; set; }
        //[DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public string QuestionRu { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public int TypeId { get; set; }
        public int LevelId { get; set; }
        public int Deleted { get; set; }
        public int? Published { get; set; }
        public int? ParentQuestionId { get; set; }
        
    }

    public partial class ViewQuestion
    {
        public int QuestionId { get; set; }
        public Question Question { get; set; }
        public List<int> CategoryList { get; set; }
        public List<int> DepartmentList { get; set; }
        public List<int> PictureList { get; set; }
        public List<Upload> FilesList { get; set; }
        public List<int> LevelsList { get; set; }
        public bool isEdit { get; set; }
        public ICollection<AnswerView> Answers { get; set; }
        public ICollection<AnswerView> AnswersSelected { get; set; }
        public ICollection<AnswerView> RightAnswers { get; set; }
        public int QuestionRightProcent { get; set; }
        public int TextAnswer { get; set; }
        public string TextAnswer2 { get; set; }

        public ICollection<ViewQuestion> SubQuestion { get; set; }
        public ICollection<AnswerView> SubAnswers { get; set; }
    }

    public partial class ViewTextAnswers
    {
        public int QuestionId { get; set; }
        public string QuestionRu { get; set; }
        public List<int> PictureList { get; set; }
        public string Answers { get; set; }
        public int? ManagerMark { get; set; }
    }

    [Table("Answer")]
    public class Answer
    {      
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public int Weight { get; set; }
        public string AnswerRu { get; set; }
        public string ShortAnswer { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public int Deleted { get; set; }
        public int? TypeId { get; set; }
        
    }

    [Table("TreeQuestion")]
    public class TreeQuestion
    {
        [Key]
        public int TreeQuestionId { get; set; }      
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int QuestionId { get; set; }
        public int TreeId { get; set; }
        public int TypeId { get; set; }
    }

    public partial class ViewTest
    {
        public int TestTemplateId { get; set; }
        public string NameRu { get; set; }
        public List<Tree> AllCategoryList { get; set; }
        public List<Tree> SelectCategoryList { get; set; }
        public List<Tree> DepartmentList { get; set; }
        public List<Tree> LevelsList { get; set; }
        public int? TryCount { get; set; }
        //public List<int> PictureList { get; set; }
        //public ICollection<Question> Question { get; set; }
        public virtual Attestation Attestation { get; set; }
        public int Duration { get; set; }
    }

    public partial class ViewAttestation
    {
        public virtual Attestation Attestation { get; set; }
        public virtual TestTemplate TestTemplate { get; set; }
        public int TestTemplateId { get; set; }
        public int? FeedTemplateId { get; set; } 
        public string AttestationName { get; set; }
        public string TestTemplateName { get; set; }
        public List<Tree> AllCategoryList { get; set; }
        public List<Tree> SelectCategoryList { get; set; }
        public List<Tree> DepartmentList { get; set; }
        public List<Tree> LevelsList { get; set; }
        public int? TryCount { get; set; }
        public ICollection<AttestationEmployeeItems> AttestationEmployeeItems { get; set; }
        public ICollection<ViewResultTest> Tests { get; set; }
        public ICollection<Feedback> Feedback { get; set; }
        public ICollection<L_User> User { get; set; }       

        public int Duration { get; set; }
        public int NeedMark { get; set; }

        public bool ManagerApprove { get; set; }
    }

    public partial class ViewAttestationReport
    {
        public virtual Attestation Attestation { get; set; }
        public ICollection<L_User> LazyUser { get; set; }
        public ICollection<ViewUsersAttestationReport> ViewUsersAttestationReport { get; set; }   
    }

    public partial class ViewUsersAttestationReport
    {
        public virtual L_User User { get; set; }
        public virtual UsersAchievements UsersAchievements { get; set; }
        public ICollection<ViewResultTest> ViewResultTests { get; set; }

        public int? TargetProcent { get; set; }
        public int? RecomendLevel { get; set; }
        public int? LMConfirmRL { get; set; }
        public int? HRConfirmRL { get; set; }
    }

    public partial class ViewUsersTests
    {
        //public int UserId { get; set; }
        //public ICollection<ViewResultTest> Tests { get; set; }
    }

    [Table("UsersAchievements")]
    public class UsersAchievements
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UsersAchievementsId { get; set; }
        public int UserId { get; set; }
        public int LevelId { get; set; }
        public int CreatedBy { get; set; }
        public int AchievementsId { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime DateGetAchievement { get; set; }
        public int PicId { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? Grade { get; set; }
        
        //
    }


    [Table("TestTemplateItem")]
    public class TestTemplateItem
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int TestTemplateItemId { get; set; }
        public int TestTemplateId { get; set; }
        public int CategoryId { get; set; }
        public int CategoryType { get; set; }
        public int? QuestionQuantity { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifedDate { get; set; }

    }

    public partial class SelectedTreeQuestion
    {
        public int TestTemplateId { get; set; }
        public string NameRu { get; set; }
        public int TreeId { get; set; }
        public int? QuestionQuantity { get; set; }
    }

    public partial class TestRecord2
    {
        public string question { get; set; }
        public List<int> answerWeight { get; set; }
    }

    [Table("Upload")]
    public class Upload
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UploadId { get; set; }

        [Display(Name = "Название")]
        public string Name { get; set; }

        [Display(Name = "Путь")]
        public string Path { get; set; }

        [Display(Name = "TypeId")]
        public int ModuleId { get; set; }

        [Display(Name = "DocumentId")]
        public int DocumentId { get; set; }

        [Display(Name = "Extension")]
        public string Extension { get; set; }

        [Display(Name = "Размер")]
        public decimal SizeKB { get; set; }

        [Display(Name = "Дата")]
        public DateTime? DateUpload { get; set; }

        [Display(Name = "Пользователь")]
        public int UserId { get; set; }

        [Display(Name = "Id вопроса")]
        public int? QuestionId { get; set; }

    }

    [Table("TreeType")]
    public class TreeType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TreeTypeId { get; set; }
        public string Name { get; set; }
    }

    [Table("TestTemplate")]
    public class TestTemplate
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int TestTemplateId { get; set; }
        public string NameRu { get; set; }
        public int Duration { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? ModifedDate { get; set; }
        public int ModifedById { get; set; }
        public int CountQuestion { get; set; }
        public int TryCount { get; set; }
        public int Deleted { get; set; }
        public int LevelId { get; set; }
        public int? Saved { get; set; }
        public int? TargetProcent { get; set; }
        public int? TypeTemplateId { get; set; }

        public int? FeedTemplateId{ get; set; }

        
    }

    [Table("Feedback")]
    public class Feedback
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int FeedbackId { get; set; }
        public string NameRu { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreatedById { get; set; }
        public DateTime? ModifedDate { get; set; }
        public int? ModifedById { get; set; }
        public int Deleted { get; set; }
        public int AttestationId { get; set; }
        public int CategoryId { get; set; }

        public int UserId { get; set; }
        public int Finished { get; set; }
        public int TemplateId { get; set; }
        
    }

    [Table("FeedbackItems")]
    public class FeedbackItems
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int FeedbackItemId { get; set; }
        public int FeedbackId { get; set; }
        public int Value { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? CreatedById { get; set; }

        public int? QuestionId { get; set; }
        public int? AnswerId { get; set; }
        public int? UserId { get; set; }
        public int? Weight { get; set; }
        public int? Selected { get; set; }
        public string Text { get; set; }


    }


    // Прохождение экземпляра теста

    [Table("TestsItem")]
    public class TestsItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TestsItemId { get; set; }
        public int TestsId { get; set; }
        public int QuestionId { get; set; }
        public int Weight { get; set; }
        public string Answers { get; set; }
        public DateTime? ShowDate { get; set; }
        public DateTime? AnswerDate { get; set; }
        public int UserId { get; set; }
        public int AnswerId { get; set; }
        public int? Selected { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? ManagerMark { get; set; }
        public bool? TextAnswer { get; set; }
        public int? CategoryId { get; set; }

        public int? ParentId { get; set; }

        public string SUserId { get; set; }
        //public int TryCount { get; set; }        
    }

    [Table("Tests")]
    public class Tests
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TestId { get; set; }
        public int TemplateId { get; set; }
        public int QuestionCount { get; set; }
        public int UserId { get; set; }
        public string NameRu { get; set; }
        public int SuccessAnswerCount { get; set; }
        public int WrongAnswerCount { get; set; }
        public int? Duration { get; set; }
        public int? FactDuration { get; set; }
        public int? Progress { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? Finished { get; set; }
        public int? WeightResult { get; set; }
        public int? AttestationId { get; set; }
        public double? CheckedText { get; set; }
        public int? VerbalExamination { get; set; }
        public int? PracticalExamination { get; set; }
        // public virtual ICollection<TestsItem> Items { get; set; }

        public string SUserId { get; set; }

    }

    public partial class ViewPassTest
    {
        public int TestTemplateId { get; set; }
        public string NameRu { get; set; }
        public DateTime? CreateDate { get; set; }
        public int CreatedByUserId { get; set; }
        public int CountQuestion { get; set; }
        public int TryCount { get; set; }
        public List<Tree> LevelsList { get; set; }
        public List<int> PictureList { get; set; }
        public Question Question { get; set; }
        public ICollection<ViewPassTest> SubQuestion { get; set; }
        public ICollection<Tree> Tree { get; set; }
        public ICollection<AnswerView> Answers { get; set; }
        public ICollection<AnswerView> AnswersSelected { get; set; }
        public AnswerView AnswerSingle { get; set; }

        public ICollection<AnswerView> SubAnswers { get; set; }
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public int TestsId { get; set; }
        public int Duration { get; set; }
        public string TextAnswer { get; set; }

        public ICollection<TreeQuestion> TreeQuestion { get; set; }
        public int UserId { get; set; }

    }

    public partial class ViewResultTest
    {
        public int TestId { get; set; }
        public int? AttestationId { get; set; }
        public int TestTemplateId { get; set; }
        public int UserId { get; set; }
        public string NameRu { get; set; }
        public int CountQuestion { get; set; }
        public int TryCount { get; set; }
        public int LevelId { get; set; }
        public int TypeId { get; set; }
        public string TypeName { get; set; }
        public int Duration { get; set; }
        public int rightWeight { get; set; }
        public int rightAnswersCount { get; set; }
        public int? TargetProcent { get; set; }
        public int? Progress { get; set; }

        public int? WeightResult { get; set; }
        public double? CheckedText { get; set; }
        public int? VerbalExamination { get; set; }
        public int? PracticalExamination { get; set; }
        public int? CertificationLevel { get; set; }
        public int? CertificationLevelToPass { get; set; }
        public int? RecommendedLevel { get; set; }      

        public ICollection<SubCategoriesForDetailedReport> SubCategoriesForDetailedReport { get; set; }
    }

    // Фильтрация

    public class FilterQuestions
    {
        public int QuestionId { get; set; }
        public int LevelId { get; set; }
        public int SubjectId { get; set; }
        public int HavePic { get; set; }
        public int DepartmentId { get; set; }
    }

    public class FilterAttestations
    {
        public int AttestationId { get; set; }
        public int UserId { get; set; }
    }

    // Дерево категорий
    public class Category
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string NameRu { get; set; }
        public string NameEn { get; set; }
        public string NameKz { get; set; }
        public string Description { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? ModifedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public int ParentId { get; set; }
        public int TypeId { get; set; }
        public int Deleted { get; set; }
        public int Published { get; set; }

        //represnts Parent ID and it's nullable  
        public int? Pid { get; set; }
        [ForeignKey("Pid")]
        public virtual Category Parent { get; set; }
        public virtual ICollection<Category> Childs { get; set; }
    }

    // Аттестации
    [Table("Attestation")]
    public class Attestation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttestationId { get; set; }
        public DateTime? RequestDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? FinishTime { get; set; }
        public int UserID { get; set; }
        public int CreatedBy { get; set; }
        public bool? IsSubmit { get; set; }
        public string AttestationName { get; set; }
        public int LMUserId { get; set; }
        public int TestTemplateId { get; set; }
        public int? FeedbackId { get; set; }
        public int CertificationLevel { get; set; }
        public int? CertificationLevelToPass { get; set; }
        public bool? IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public virtual ICollection<AttestationEmployeeItems> empItems { get; set; }

        public int? Out { get; set; }
    }

    [Table("AttestationEmployeeItems")]
    public class AttestationEmployeeItems
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttestationItemId { get; set; }
        public int AttestationId { get; set; }
        public int UserID { get; set; }
        public bool ManagerApprove { get; set; }
        public int Passed { get; set; }
        public DateTime? PassedDate { get; set; }

        public int? VerbalExamination { get; set; }
        public int? PracticalExamination { get; set; }
        public int? RecommendedLevel { get; set; }
        public int? AttestationMark { get; set; }

        public int? RecommendedByUserId { get; set; }
        public DateTime? RecommendedDate { get; set; }
        public int? LMConfirmRL { get; set; }
        public int? LMConfirmRLById { get; set; }
        public DateTime? LMConfirmRLDate { get; set; }
        public int? HRConfirmRL { get; set; }
        public int? HRConfirmRLById { get; set; }
        public DateTime? HRConfirmRLDate { get; set; }

        public string SUserId { get; set; }

    }

    public struct EmployeeInfo
    {
        public int rowNumber { get; set; }
        public string userName { get; set; }
        public string positionName { get; set; }
        public string divisionName { get; set; }
        public string LMUserName { get; set; }
        public int CertificationLevel { get; set; }
    }

    // Пользователи с ролями
    [Table("UserInRole")]
    public class UserInRole
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int UserInRoleId { get; set; }
        public int UserId { get; set; }
        public int? RoleId { get; set; }
        public int ScreenId { get; set; }
        public int AccessLevelId { get; set; }
    }

    [Table("Signatory")]
    public class Signatory
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int SignatoryId { get; set; }
        public int UserId { get; set; }
        public int ResponsibilityId { get; set; }
    }

    public struct CreatedCertificationData
    {
        public int CertificationId { get; set; }
        public string CertificationName { get; set; }
        public string CertificationStartTime { get; set; }
        public string CertificationEndTime { get; set; }
        public int TestTemplateId { get; set; }
        public int FeedTemplateId { get; set; }
        public int ManagerId { get; set; }
        public int Level { get; set; }
        public int LeveltoPass { get; set; }
    }

    public struct t
    {
        public int Id { get; set; }
        public string NameRu { get; set; }
    }

    //Excel report
    public struct ForDetailedReport
    {
        public string UserName { get; set; }
        public string RootCategoryName { get; set; }
        public double TotalMark { get; set; }
        public double TotalParentMark { get; set; }
        public List<SubCategoriesForDetailedReport> SubCategories { get; set; }
    }

    public struct SubCategoriesForDetailedReport
    {
        public int subCategoryId { get; set; }
        public int ParentId { get; set; }
        public string subCategoryName { get; set; }
        public int? subCategoryResult { get; set; }
        public int? subCategoryTextResult { get; set; }

        public ICollection<ViewQuestion> ViewQuestion { get; set; }
        public ICollection<ViewTextAnswers> TextQuestion { get; set; }
        public ICollection<TestsItem> TestsItems { get; set; }

        public List<SubCategoriesForDetailedReport> SubCategories { get; set; }
    }



    // Тренинги с префиксом t_
    [Table("t_Categories")]
    public class t_Categories
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int Categories_id { get; set; }
        public string NameRU { get; set; }
        public int? Parent_id { get; set; }
        public bool? IsDeleted { get; set; }
        //public string Path_name { get; set; }
        //public string Path { get; set; }
        //public string Description { get; set; }

        public int Ns_left { get; set; }
        public int Ns_right { get; set; }
        //public int Ns_depth { get; set; }
        //public int? Ns_order { get; set; }

        public int? CreatedBy { get; set; }
        //public int? DeletedBy { get; set; }
        public int? ModifiedBy { get; set; }

        //public DateTime? LastModifiedDate { get; set; }
        //public DateTime? DeletedDate { get; set; }

        //public int? Children { get; set; }

        // public virtual ICollection<t_Categories> nodes { get; set; }

    }

    class category
    {
        public int Categories_id;
        public string NameRU;
        public int Parent_id; 

        public category(int Categories_id, string NameRU, int Parent_id)
        {
            this.Categories_id = Categories_id;
            this.NameRU = NameRU;
            this.Parent_id = Parent_id;
        }
    }

    public class TreeItem<T>
    {
        public T Item { get; set; }
        public IEnumerable<TreeItem<T>> children { get; set; }
    }

}
