using System;
using System.Data.Entity.ModelConfiguration;

namespace JetEntityFrameworkProvider.Test.Model11
{
    public class VersionMap : EntityTypeConfiguration<Version>
    {
        public VersionMap()
        {
            // Relationships
            HasMany(t => t.Models)
                .WithMany(t => t.Versions);

            HasMany(t => t.DataReleaseLevels)
                .WithMany(t => t.Versions);
        }
    }
}
