using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;


namespace TTS.Models
{
    public class SickLeaveReport
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public bool IsSellary { get; set; }
    }

}
