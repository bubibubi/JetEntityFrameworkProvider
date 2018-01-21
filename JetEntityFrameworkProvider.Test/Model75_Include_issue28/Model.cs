using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model75_Include_issue28
{
    [Table("ReferredClass75")]
    public class ReferredClass
    {
        public ReferredClass()
        {
            ReferringClasses1 = new List<ReferringClass1>();
            ReferringClasses2 =new List<ReferringClass2>();
        }

        public int Id { get; set; }
        public virtual ICollection<ReferringClass1> ReferringClasses1 { get; set; }
        public virtual ICollection<ReferringClass2> ReferringClasses2 { get; set; }
    }

    [Table("ReferringClass1_75")]
    public class ReferringClass1 : ReferringClassBase
    {
    }

    public class ReferringClassBase
    {
        public int Id { get; set; }
        public virtual ReferredClass ReferredClass { get; set; }
        public float? MyFloat { get; set; }
        public double? MyDouble { get; set; }
        //public decimal? MyDecimal { get; set; }
        public int? MyInt { get; set; }
        public Int16? MySmallInt { get; set; }
    }

    [Table("ReferringClass2_75")]
    public class ReferringClass2 : ReferringClassBase
    {
    }
}
