using System;
using System.Data.Common;
using System.Data.Entity;

// https://stackoverflow.com/questions/44809921/duplicate-values-in-the-index-primary-key-or-relationship

namespace JetEntityFrameworkProvider.Test.Model57_StackOverflow
{
    public class Context : DbContext
    {
        // For migration test
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, false)
        { }
        public DbSet<Page> Pages { get; set; }
    }

}
