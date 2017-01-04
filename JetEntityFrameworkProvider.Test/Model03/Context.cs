using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model03
{
    public class Context : DbContext
    {
        // For migration test
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<PanelLookup> PanelLookups { get; set; }
        public DbSet<PanelTexture> PanelTextures { get; set; }
        public DbSet<Texture> Textures { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Properties<int>().Where(p => p.Name == "Id").Configure(p => p.IsKey());
            modelBuilder.Entity<Texture>().Map(m => m.Requires("IsDeleted").HasValue(false)).Ignore(m => m.IsDeleted);
            modelBuilder.Entity<PanelTexture>().HasKey(e => new { e.PanelId, e.TextureId, e.IsInterior });
            modelBuilder.Entity<PanelTexture>().HasRequired(e => e.Texture).WithMany(e => e.PanelTextures).HasForeignKey(e => e.TextureId);
            modelBuilder.Entity<PanelTexture>().HasRequired(e => e.PanelLookup).WithMany(e => e.PanelTextures).HasForeignKey(e => e.PanelId);
        }
    }
}
