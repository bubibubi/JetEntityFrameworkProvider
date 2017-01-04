using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model04
{
    public abstract class Test
    {
        [TestMethod]
        public void SaveTest()
        {

            using(DbConnection connection = GetConnection())
            using(CarsContext context = new CarsContext(connection))
            {
                context.Cars.Add(new Car { Name = "Maserati" });
                context.Cars.Add(new Car { Name = "Ferrari" });
                context.Cars.Add(new Car { Name = "Lamborghini" });

                context.SaveChanges();
            }
        }

        [TestMethod]
        public void SkipTakeTest()
        {

            using(DbConnection connection = GetConnection())
            using (CarsContext context = new CarsContext(connection))
            {
                Seed.SeedPersons(context);
                List<Person> persons = context.Persons.OrderBy(p => p.Name).Skip(3).Take(5).ToList();
                Assert.AreEqual(5, persons.Count);
                foreach (Person person in persons)
                    Console.WriteLine(person.Name);
                Console.WriteLine("=====================");
                foreach (Person person in context.Persons.OrderBy(p => p.Name).ToList())
                    Console.WriteLine(person.Name);
            }
        }

        protected abstract DbConnection GetConnection();
    }
}
