using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model09
{
    public class OneMap : EntityTypeConfiguration<One>
    {
        public OneMap()
        {
            this.HasKey(t => t.Id);
            ToTable("One");
        }
    }
}