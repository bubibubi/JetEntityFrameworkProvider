using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Configuration;

namespace JetEntityFrameworkProvider.Test.Model38_OneEntity2Tables
{
    class Context : DbContext
    {
        public Context(DbConnection connection) : base(connection, false)
        {
            
        }

        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>().Map(delegate(EntityMappingConfiguration<Student> studentConfig)
            {
                studentConfig.Properties(p => new { p.Id, p.StudentName });
                studentConfig.ToTable("StudentInfo");
            });

            Action<EntityMappingConfiguration<Student>> studentMapping = m =>
            {
                m.Properties(p => new { p.Id, p.Height, p.Weight, p.Photo, p.DateOfBirth });
                m.ToTable("StudentInfoDetail");
            };
            modelBuilder.Entity<Student>().Map(studentMapping);

        }
    }
}
