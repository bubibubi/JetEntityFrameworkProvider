using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model09
{
    public class ThreeMap : EntityTypeConfiguration<Three>
    {
        public ThreeMap()
        {
            HasKey(t => new { t.Id, t.OneId, t.TwoId });
            ToTable("Three");

            HasRequired(t => t.Two).WithMany(t => t.ThreeList).HasForeignKey(t => new {t.OneId, t.TwoId});
        }
    }
}