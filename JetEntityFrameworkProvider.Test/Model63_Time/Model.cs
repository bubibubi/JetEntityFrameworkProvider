using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model63_Time
{
    [Table("Items63")]
    public class Item
    {
        public int Id { get; set; }
        public TimeSpan? TimeSpan { get; set; }
        public DateTime? DateTime { get; set; }
    }
}
