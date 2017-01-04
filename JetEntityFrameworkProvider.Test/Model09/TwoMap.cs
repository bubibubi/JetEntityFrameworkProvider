using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model09
{
    public class TwoMap : EntityTypeConfiguration<Two>
    {
        public TwoMap()
        {
            this.HasKey(t => new { t.Id, t.OneId });
            ToTable("Two");
            
            HasRequired(t => t.One).WithMany(t => t.TwoList).HasForeignKey(t => t.OneId);
        }
    }
}