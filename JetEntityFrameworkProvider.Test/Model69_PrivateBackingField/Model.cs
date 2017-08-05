using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model69_PrivateBackingField
{

    public class Info
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string Description { get; set; }

        public sbyte SByte
        {
            get
            {
                return (sbyte) SByteBackingField;
            }
            set
            {
                SByteBackingField = value;
            }
        }

        private int SByteBackingField { get; set; }


        public class InfoMap : EntityTypeConfiguration<Info>
        {
            public InfoMap()
            {
                ToTable("Infoes69");
                Property(_ => _.SByteBackingField).HasColumnName("SByte");
                Ignore(_ => _.SByte);
            }
        }

    }
}
