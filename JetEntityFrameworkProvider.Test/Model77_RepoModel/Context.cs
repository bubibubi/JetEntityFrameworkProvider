using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model77_RepoModel
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbConnection dbConnection)
            : base(dbConnection, true) { }

        public DbSet<Item> Items { get; set; }

        public static ApplicationDbContext Create(DbConnection dbConnection)
        {
            return new ApplicationDbContext(dbConnection);
        }
    }
}
