using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using JetEntityFrameworkProvider.Test.Model41.Demo.Data.EntityModels;

namespace JetEntityFrameworkProvider.Test.Model41
{
    namespace Demo.Data.EntityModels
    {
        public class Applicant
        {
            public Guid Id { get; set; }

            public string E_FirstName { get; set; }

            public string E_LastName { get; set; }

            public DateTime DateOfBirth { get; set; }

            public virtual IList<Applicant_Address> Addresses { get; set; }
        }
    }

    namespace Demo.Data.EntityModels
    {
        public class Applicant_Address
        {
            public Guid Id { get; set; }

            public Guid Applicant_Id { get; set; }

            public virtual Applicant Applicant { get; set; }

            public string E_Flat_Unit { get; set; }

            public string E_Building { get; set; }

            public string E_Street { get; set; }

            public string E_Locality { get; set; }

            public string E_Town { get; set; }

            public string E_County { get; set; }

            public string E_PostCode { get; set; }
        }
    }

    public class DemoContext : DbContext
    {
        public DemoContext()
            : base("DemoContext")
        {
        }

        public DemoContext(DbConnection connection)
            : base(connection, false)
        {
        }


        public DbSet<Applicant> Applicants { get; set; }

        public DbSet<Applicant_Address> Applicant_Addresses { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Applicant>().HasKey(k => k.Id);
            modelBuilder.Entity<Applicant_Address>().HasKey(k => k.Id);

            modelBuilder.Entity<Applicant_Address>().HasRequired(_ => _.Applicant).WithMany(_ => _.Addresses).HasForeignKey(a => a.Applicant_Id);
            //modelBuilder.Entity<Applicant>().HasMany(a => a.Addresses).WithRequired(ad => ad.Applicant).HasForeignKey(a => a.Applicant_Id);

        }
    }
}
