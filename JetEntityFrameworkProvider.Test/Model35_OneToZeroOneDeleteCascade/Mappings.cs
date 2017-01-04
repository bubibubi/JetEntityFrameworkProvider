using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model35_OneToZeroOneDeleteCascade
{
    public class PrincipalMap : EntityTypeConfiguration<Principal>
    {
        public PrincipalMap()
        {
            ToTable("PRINCIPALS");

            HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("PRINCIPALID")
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        }
    }

    public class DependentMap : EntityTypeConfiguration<Dependent>
    {
        public DependentMap()
        {
            ToTable("DEPENDENTS");

            HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("DEPENDENTID")
                .IsRequired()
                .HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);

            HasRequired(x => x.Principal).WithOptional(x => x.Dependent).Map(x => x.MapKey("PRINCIPALID")).WillCascadeOnDelete();
        }
    }
}
