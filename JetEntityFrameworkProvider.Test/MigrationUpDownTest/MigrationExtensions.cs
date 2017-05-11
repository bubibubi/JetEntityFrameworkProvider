using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Data.Entity.SqlServer;
using System.Reflection;

namespace JetEntityFrameworkProvider.Test.MigrationUpDownTest
{
    static class MigrationExtensions
    {
        public static void RunMigration(this DbContext context, DbMigration migration)
        {
            var prop = migration.GetType().GetProperty("Operations", BindingFlags.NonPublic | BindingFlags.Instance);
            if (prop != null)
            {
                IEnumerable<MigrationOperation> operations = prop.GetValue(migration) as IEnumerable<MigrationOperation>;
                MigrationSqlGenerator generator = (new DbMigrationsConfiguration()).GetSqlGenerator("JetEntityFrameworkProvider");
                var statements = generator.Generate(operations, "Jet");
                foreach (MigrationStatement item in statements)
                    context.Database.ExecuteSqlCommand(item.Sql);
            }
        }
    }
}
