using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    public class Student
    {
        public Student()
        {
        }

        public int StudentID { get; set; }
        
        [Required]
        [MaxLength(50)]
        [Index]
        public string StudentName { get; set; }
        
        public string Notes { get; set; }

        public virtual Standard Standard { get; set; }

        public override string ToString()
        {
            return string.Format("{2}: {0} - {1}", StudentID, StudentName, base.ToString());
        }
    }

}
