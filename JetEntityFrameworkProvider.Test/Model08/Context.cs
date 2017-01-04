using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model08
{
    public class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<File> Files { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<PageImage> PageImages { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Configurations.Add(new FileMap());
            modelBuilder.Configurations.Add(new GalleryImageMap());
            modelBuilder.Configurations.Add(new PageImageMap());
        }
    }


}
