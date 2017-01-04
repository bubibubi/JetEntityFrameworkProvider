using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model08
{
    public class PageImageMap : EntityTypeConfiguration<PageImage>
    {
        public PageImageMap()
        {
            // Primary Key
            this.HasKey(t => t.Id);

            // Table & Column Mappings
            this.ToTable("PageImages");
            this.Property(t => t.Id).HasColumnName("Id");
            // Other properties go here
        }
    }
}