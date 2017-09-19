using System.Data.Entity.Migrations;

namespace JetEntityFrameworkProvider.Test.MigrationUpDownTest
{
    public class MigrationMakeNullable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "MigrationMakeNullable",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Description = c.String(true, 50)
                })
                .PrimaryKey(t => t.Id);

            AlterColumn("dbo.MigrationMakeNullable", "Description", c => c.String(nullable: false, maxLength: 50));


            CreateTable(
                    "MigrationMakeNullableInt",
                    c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Counter = c.Int(nullable: true)
                    })
                .PrimaryKey(t => t.Id);

            AlterColumn("dbo.MigrationMakeNullableInt", "Counter", c => c.Int(nullable: false));


        }
    }
}