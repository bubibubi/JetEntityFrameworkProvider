using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model30
{
    class Context : DbContext
    {

        public Context(DbConnection connection)
            : base(connection, false)
        {
        }

        public DbSet<Answer> Answers { get; set; }
        public DbSet<Question> Questions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOptional(x => x.Question);

            base.OnModelCreating(modelBuilder);

        }
    }
}
