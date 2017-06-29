using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model58_TruncateTime
{
    [Table("Model58")]
    public class Entity
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }

}
