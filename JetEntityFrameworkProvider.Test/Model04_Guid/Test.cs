using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model04_Guid
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {


            using(DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                context.Cars.Add(new Car {Name = "Maserati"});
                context.Cars.Add(new Car {Name = "Ferrari"});
                context.Cars.Add(new Car {Name = "Lamborghini"});

                context.SaveChanges();

                var myQuery = context.Persons.Where(p => p.OwnedCar.Id == Guid.Empty);
                var result = myQuery.ToList();

            }

        }

        protected abstract DbConnection GetConnection();

    }
}
