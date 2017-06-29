using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model57_StackOverflow
{
    [Table("StackOverflow44809921")]
    public class Page
    {
        public int Id { get; set; }
        public int? PageNumber { get; set; }
        [Index(IsUnique = true)]
        public int? BookNumber { get; set; }
    }

}
