using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetEntityFrameworkProvider.Test.Model55_Unicode
{
    public class Entity
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string Description { get; set; }
    }

}
