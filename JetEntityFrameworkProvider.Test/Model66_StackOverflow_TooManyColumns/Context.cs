using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model66_StackOverflow_TooManyColumns
{
    public class Context : DbContext
    {
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<MyFlashCard> MyFlashCards { get; set; }
        public DbSet<MyFlashCardPic> MyFlashCardPics { get; set; }
        public DbSet<FasleManJdl> FasleManJdls { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MyFlashCard>()
                .HasMany(e => e.MyFlashCardPics)
                .WithRequired(e => e.MyFlashCard)
                .HasForeignKey(e => e.MyFlashCardId)
                .WillCascadeOnDelete();
        }
    }

}
