using System;
using System.ComponentModel.DataAnnotations;

namespace JetEntityFrameworkProvider.Test.Model56_SkipTake
{
    public class Entity
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string Description { get; set; }
        public DateTime? Date { get; set; }
        public double? Value { get; set; }
    }

}
