using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model08
{
    public class GalleryImageMap : EntityTypeConfiguration<GalleryImage>
    {
        public GalleryImageMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);
            this.ToTable("GalleryImages");
            this.Property(t => t.Id).HasColumnName("Id");
            // Other properties go here
        }
    }
}