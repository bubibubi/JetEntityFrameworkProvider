# Enabling CodeFirst simple migration

In order to enable simple migration you need to do the following steps (as in SQL Server EF Provider)

* The context should configured in this way

{{
using System.Data.Common;
using System.Data.Entity;

namespace MyApp
{
    class MyContext : DbContext
    {
        public MyContext(DbConnection connection)
            : base(connection, false)
        {}

        public MyContext()
        { }

        public DbSet<MyEntity> MyEntities { get; set; }
    }
}
}}

* We need a migration configuration

{{
using System.Data.Entity.Migrations;

namespace MyApp.Migrations
{

    internal sealed class MyContextMigrationConfiguration : DbMigrationsConfiguration<MyApp.MyContext>
    {
        public MyContextMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = true;
        }
    }
}
}}

* We need to use that migration configuration (with the connection that raised the migration). Somewhere, before context access, we need to insert the following statements.

{{
Database.SetInitializer(new MigrateDatabaseToLatestVersion<MyContext,MyContextMigrationConfiguration>(true));
}}


At the first SaveChanges the database is migrated to the actual version.
