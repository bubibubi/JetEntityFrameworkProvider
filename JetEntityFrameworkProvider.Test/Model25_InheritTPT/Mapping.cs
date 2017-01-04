using System;
using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model25_InheritTPT
{
    public class CompanyMap : EntityTypeConfiguration<Company>
    {
        public CompanyMap()
        {
            // Primary Key
            HasKey(c => c.Id);

            //Table  
            ToTable("Company");

        }
    }

    public class SupplierMap : EntityTypeConfiguration<Supplier>
    {
        public SupplierMap()
        {
            // Primary Key
            HasKey(s => s.Id);

            // Properties

            //Relationship
            HasRequired(s => s.Company)
                .WithMany().HasForeignKey(c => c.CompanyId);

            //Table  
            ToTable("Supplier");

        }
    }

}
