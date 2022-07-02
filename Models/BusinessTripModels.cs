using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models
{

    [Table("BusinessTripRoute")]
    public class BusinessTripRoute
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int BusinessTripRouteId { get; set; }
        public int BusinessTripId { get; set; }
        public string Name { get; set; }
        public decimal Cost { get; set; }
        public int CurrencyId { get; set; }
    }

    [Table("BusinessTripCity")]
    public class BusinessTripCity
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int BusinessTripCityId { get; set; }
        public int BusinessTripId { get; set; }
        public string Name { get; set; }
        public decimal DailyAllowancesPerDay { get; set; }
        public decimal DailyAllowancesAmount { get; set; }
        public int DailyAllowancesDaysCount { get; set; }
        public int DailyAllowancesCurrencyId { get; set; }
        public bool Local { get; set; }
    }

    [Table("BusinessTrip")]
    public class BusinessTrip
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int BusinessTripId { get; set; }

        [Display(Name = "RequestDate", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? RequestDate { get; set; }

        [Display(Name = "PersonName", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public string PersonName { get; set; }

        [Display(Name = "Position", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public string Position { get; set; }
        public int PositionId { get; set; }

        [Display(Name = "Division", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public string Division { get; set; }
        public int DivisionId { get; set; }



        [Display(Name = "City", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public int RegionId { get; set; }

        [Display(Name = "CityFrom", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public string CityFrom { get; set; }

        [Display(Name = "CityTo", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public string CityTo { get; set; }

        [Display(Name = "Local", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public bool Local { get; set; }

        [Display(Name = "Purpose", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public int PurposeId { get; set; }


        [Display(Name = "DepartureDate", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}",
               ApplyFormatInEditMode = true)]
        public DateTime? DepartureDate { get; set; }

        [Display(Name = "ArrivalDate", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? ArrivalDate { get; set; }




        /* Hotel */
        [DisplayFormat(DataFormatString = "{0:0,0.00}", ApplyFormatInEditMode = true)]
        public decimal HotelAmount { get; set; }
        public int HotelDeysCount { get; set; }
        public decimal HotelCostPerDay { get; set; }
        public int HotelCurrencyId { get; set; }


        /*Daily Allowances*/

        public decimal DailyAllowancesAmount { get; set; }
        public decimal DailyAllowancesPerDay { get; set; }
        public int DailyAllowancesCurrencyId { get; set; }
        [Display(Name = "DaysCount", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public int DaysCount { get; set; }


        /* Car Rent  */
        public decimal CarRentCostPerDay { get; set; }
        public int CarRentDeysCount { get; set; }
        public decimal CarRentAmount { get; set; }
        public int CarRentCurrencyId { get; set; }


        [Display(Name = "ExpensesRepresentation", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public decimal ExpensesRepresentationAmount { get; set; }

        public int ExpensesRepresentationCurrencyId { get; set; }

        [Display(Name = "OtherExpenses", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public decimal OtherExpensesAmount { get; set; }

        public int OtherExpensesCurrencyId { get; set; }

        public DateTime? OrderDate { get; set; }
        public string OrderNumber { get; set; }
        public string OrderPath { get; set; }
        public int? OrderIndex { get; set; }

        public bool? Saved { get; set; }


        //User
        public int UserId { get; set; }
        public int CreatedByUserId { get; set; }        //Если создавал не сам пользователь

        public int HRUserId { get; set; }
        public DateTime? HRStatusDate { get; set; }     //Дата установки статуса
        public int HRStatus { get; set; }               //статус одобрения


        [Display(Name = "LMUserId", ResourceType = typeof(Language.Modules.BusinessTrip.BusinessTrip))]
        public int LMUserId { get; set; }
        public DateTime? LMStatusDate { get; set; }     //Дата установки статуса
        public int LMStatus { get; set; }               //статус одобрения


        //Department Manager
        public int DMUserId { get; set; }
        public DateTime? DMStatusDate { get; set; }     //Дата установки статуса
        public int DMStatus { get; set; }               //статус одобрения



        public int? ParentBusinessTripId { get; set; }


        public int? CopyParentId { get; set; }
        public DateTime? CopyDate { get; set; }
        public int? CopyUserId { get; set; }

        public string RejectComment { get; set; }




        public virtual ICollection<BusinessTripRoute> BusinessTripRoute { get; set; }
        public virtual ICollection<BusinessTripCity> BusinessTripCity { get; set; }
    }

    public class FilterBusinessTrips
    {
        public int UserId { get; set; }
        public int PurposeofTripId { get; set; }
        public int? IsLocal { get; set; }
        public int BusinessTripId { get; set; }
    }

    #region dictionary
    [Table("dic_PurposeofTrip")]
    public class PurposeofTrip
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int PurposeofTripId { get; set; }
        public string NameRu { get; set; }
        public string NameEn { get; set; }
        public string NameKz { get; set; }
        public bool Active { get; set; }
    }
    #endregion  

}