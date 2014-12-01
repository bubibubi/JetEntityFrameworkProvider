using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    public class Standard
    {
        public Standard()
        {
            Students = new List<Student>();
        }

        [Index("MultipleColumnIndex", 2)]
        public int StandardId { get; set; }
        [Index("MultipleColumnIndex", 1)]
        public string StandardName { get; set; }
        public string Description { get; set; }

        public virtual ICollection<Student> Students { get; set; }
    }
}
