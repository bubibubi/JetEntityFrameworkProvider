using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetEntityFrameworkProvider.Test.MigrationUpDownTest
{


    public partial class MigrationOnDeleteStar : DbMigration
    {
        public override void Up()
        {

            CreateTable(
                    "dbo.Parents",
                    c => new
                    {
                        parentId = c.Int(nullable: false, identity: true),
                    })
                .PrimaryKey(t => t.parentId);

            CreateTable(
                    "dbo.Children",
                    c => new
                    {
                        childId = c.Int(nullable: false, identity: true),
                        parentId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.childId)
                .ForeignKey("dbo.Parents", t => t.parentId, cascadeDelete: true)
                .Index(t => t.parentId);

        }

        public override void Down()
        {
            DropForeignKey("dbo.Children", "parentId", "dbo.Parents");
            DropIndex("dbo.Children", new[] { "parentId" });
            DropTable("dbo.Parents");
            DropTable("dbo.Children");
        }
    }
}
