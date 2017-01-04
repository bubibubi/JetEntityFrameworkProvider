using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model40_HardMapping
{
    class Context : DbContext
    {
        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<Dog> Dogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Car> Cars { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasMany<Dog>(user => user.OwnedDogs)
                .WithMany(dog => dog.Owners)
                .Map(mapping =>
                {
                    mapping.MapLeftKey("OwnerId");
                    mapping.MapRightKey("DogId");
                    mapping.ToTable("Owner_Dog");
                });

            modelBuilder.Entity<User>()
                .HasMany<Car>(user => user.OwnedCars)
                .WithMany(car => car.Owners)
                .Map(mapping =>
                {
                    mapping.MapLeftKey("OwnerId");
                    mapping.MapRightKey("CarId");
                    mapping.ToTable("Owner_Car");
                });

            modelBuilder.Entity<Car>()
            .HasOptional(car => car.Owner)
            .WithOptionalDependent()
            .Map(_ => _.MapKey("OwnerId"));

            modelBuilder.Entity<User>()
                .HasMany(u => u.OwnedCars)
                .WithRequired(c => c.Owner)
                .HasForeignKey(_ => _.OwnerId);

        }
    }
}
