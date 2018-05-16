using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model76_Contains
{
    [Table("Record76")]
    public class Record
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

}
