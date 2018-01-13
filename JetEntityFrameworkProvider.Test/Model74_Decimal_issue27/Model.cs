using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model74_Decimal_issue27
{

    [Table("Infoes74")]
    public class Info
    {
        public int Id { get; set; }

        public decimal Number { get; set; }
    }
}
