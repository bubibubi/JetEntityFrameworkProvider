using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    public class TestMigration
    {
        public int ID { get; set; }

        [Required]
        [MaxLength(50)]
        [Index]
        public string Name { get; set; }
    }
}
