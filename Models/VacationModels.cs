using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Hosting;

namespace HRM.Models
{
    [Table("VacationAdditional")]
    public class VacationAdditional
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int VacationAdditionalId { get; set; }
        public int UserId { get; set; }
        public int TypeId { get; set; }
        public int DaysCount { get; set; }
    }


    [Table("Vacation")]
    public class Vacation
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int VacationId { get; set; }

        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodEnd { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }

        public int UserId { get; set; }
        public bool IsExpat { get; set; }
        public int Term { get; set; }
        public int RemainDays { get; set; }
        public int TypeId { get; set; }
        public int VacDays { get; set; }
        public string Cause { get; set; }
        public int? VacationRequestId { get; set; }
    }

    [Table("VacationRequest")]
    public class VacationRequest
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int VacationRequestId { get; set; }
        public DateTime RequestDate { get; set; }
        [Display(Name = "Type", ResourceType = typeof(Language.Modules.Vacation.Vacation))]
        public int? VacationTypeId { get; set; }
        public int PositionId { get; set; }
        public int DivisionId { get; set; }
        public int RegionId { get; set; }
        public int BranchId { get; set; }

        [Display(Name = "StartDate", ResourceType = typeof(Language.Modules.Vacation.Vacation))]
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public DateTime? CompensationStartDate { get; set; }
        public DateTime? CompensationFinishDate { get; set; }

        public int DaysCount { get; set; }
        public bool? Saved { get; set; }

        #region Order
        //Приказ
        public DateTime? OrderDate { get; set; }
        public string OrderNumber { get; set; }
        public string OrderPath { get; set; }
        public int? OrderIndex { get; set; }
        #endregion

        public int UserId { get; set; }
        public int CreatedByUserId { get; set; }
        public int HRUserId { get; set; }
        public DateTime? HRStatusDate { get; set; }
        public int HRStatus { get; set; }

        [Display(Name = "LMUserId", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public int LMUserId { get; set; }
        public DateTime? LMStatusDate { get; set; }

        public int LMStatus { get; set; }
        public string RejectComment { get; set; }

        //Если беременность и роды
        [Display(Name = "NumberSickLeave", ResourceType = typeof(Language.Modules.Vacation.Vacation))]
        public string NumberSickLeave { get; set; }

        #region Learning vacation
        //Если на обучение



        [Display(Name = "NameInstitutionRu", ResourceType = typeof(Language.Modules.Vacation.Vacation))]
        public string NameInstitutionRu { get; set; } // Учебное заведение
        [Display(Name = "NameInstitutionKz", ResourceType = typeof(Language.Modules.Vacation.Vacation))]
        public string NameInstitutionKz { get; set; }// Учебное заведение
        [Display(Name = "SpecialtyRu", ResourceType = typeof(Language.Modules.Vacation.Vacation))]
        public string SpecialtyRu { get; set; } // Специальность
        [Display(Name = "SpecialtyKz", ResourceType = typeof(Language.Modules.Vacation.Vacation))]
        public string SpecialtyKz { get; set; } // Специальность
        #endregion
    }

    public class VacationFilter
    {
        public string UserId { get; set; }
        public int? TypeOfHoliday { get; set; }
        public int PositionId { get; set; }
    }

    #region dictionary
    [Table("dic_VacationType")]
    public class VacationType
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int VacationTypeId { get; set; }
        public string ApplicationEn { get; set; }
        public string ApplicationRu { get; set; }
        public string NameEn { get; set; }
        public string NameRu { get; set; }
        public string NameKz { get; set; }
        public string Code { get; set; }
        public bool Active { get; set; }
    }
    #endregion

    public class VacationDaysCountForUser
    {

        public int balance { get; set; }  // Остаток дней на сегодня
        public int spent { get; set; } // Потрачено дней на сегодня
        public int fromStartWork { get; set; } // Количество дней с момента приема на работу
        public int spentEco { get; set; } // Потрачено дней экологии
        public int spentVred { get; set; } // Потрачено дней Вредности
        public int eco { get; set; } // Начислено дней экологии
        public int vred { get; set; }// Начислено дней вредности
        public int current { get; set; } // Начислено за этот год
        public int standart { get; set; } // Количество дней начисляемое за год

        public List<HRM.Models.Vacation> EcoList { get; set; }
        public List<HRM.Models.Vacation> VredList { get; set; }
        public List<HRM.Models.Vacation> TrudList { get; set; }

    }



}