using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;




namespace TTS.Models
{


    [Table("User")]
    public class User
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.None)]
        public int UserId { get; set; }// +

        [StringLength(350)]
        public string NameRu { get; set; } //+

        [StringLength(350)]
        public string NameRuGenitive { get; set; }

        [StringLength(350)]
        public string NameEn { get; set; }

        [StringLength(350)]
        public string NameKz { get; set; }

        [StringLength(250)]
        public string AccountAD { get; set; } //Active directory

        [StringLength(250)]
        public string LineManagerAccountAD { get; set; }

        public int? DepartmentId { get; set; }
        public int? DivisionId { get; set; }

        public string WorkStatus { get; set; }

        [StringLength(350)]
        public string PrivateEmail { get; set; }
        public int? BranchId { get; set; }
        public bool Fired { get; set; }

        public int? LineManagerUserId { get; set; }
        public int? JobTitleId { get; set; }

        //public static implicit operator User(L_User v)
        //{
        //    throw new NotImplementedException();
        //}
        //public string Sid { get; set; }

        //public virtual Department Department { get; set; }

        //public DateTime? ProbationStartDate { get; set; }
        //public DateTime? ProbationFinishDate { get; set; }
        //public DateTime? StartWorkDate { get; set; }
        //public DateTime? EndWorkDate { get; set; }
        //public DateTime? CreatedDate { get; set; }

        //public virtual Branch Branch { get; set; }
        //public virtual Region Region { get; set; }
        //public virtual Division Division { get; set; }
    }

    [Table("dic_Region")]
    public class Region
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int RegionId { get; set; }
        [StringLength(450)]
        public string NameRu { get; set; }
        [StringLength(450)]
        public string NameEn { get; set; }
        [StringLength(450)]
        public string NameKz { get; set; }
        public DateTime? LastChangeDate { get; set; }
    }

    [Table("dic_Branch")]
    public class Branch
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int BranchId { get; set; }
        public string NameRu { get; set; }
        public string NameEn { get; set; }
        public string NameKz { get; set; }
        public string AddressRu { get; set; }
        public string AddressEn { get; set; }
        public string AddressKz { get; set; }
        public DateTime? LastChangeDate { get; set; }
    }

    [Table("dic_Position")]
    public class Position
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int PositionId { get; set; }
        [StringLength(450)]
        public string NameRu { get; set; }
        [StringLength(450)]
        public string NameEn { get; set; }
        [StringLength(450)]
        public string NameKz { get; set; }
        public DateTime? LastChangeDate { get; set; }
    }

    [Table("dic_Division")]
    public class Division
    {
        public int? DivisionId { get; set; }
        public string NameRu { get; set; }
    }
}
