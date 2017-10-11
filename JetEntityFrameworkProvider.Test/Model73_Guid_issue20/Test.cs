using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model73_Guid_issue20
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {


            using(DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                context.Cars.Add(new Car { Name = "Maserati", OtherGuid = new Guid("5C60F693-BEF5-E011-A485-80EE7300C695") });
                context.Cars.Add(new Car {Name = "Ferrari"});
                context.Cars.Add(new Car {Name = "Lamborghini"});

                context.SaveChanges();
            }

            Guid idCar;

            using (DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                idCar = context.Cars.First(_ => _.Name == "Maserati").Id;
            }


            using (DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                idCar = context.Cars.Single(_ => _.Id == idCar).Id;
            }

            using (DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                context.Cars.Find(idCar);
            }

            using (DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                context.Cars.Find(new Guid("5C60F693-BEF5-E011-A485-80EE7300C695"));
            }

            using (DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                string idCarString = idCar.ToString();
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.Cars.Single(_ => _.Id == new Guid(idCarString));
            }

            using (DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.Cars.First(_ => _.OtherGuid == new Guid("5C60F693-BEF5-E011-A485-80EE7300C695"));
            }

        }

        protected abstract DbConnection GetConnection();

    }
}
