using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model53_TableSplitting
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (var context = new Context(connection))
            {
                context.Persons.Add(
                    new Person() {Name = "Bubi", Address = new Address() {Province = "MO", City = new City() {Name = "Maranello"}}}
                );
                context.SaveChanges();

                var person = context.Persons.FirstOrDefault();
                var cityName = person.Address.City.Name;
                Console.WriteLine(cityName);

                var address = context.Addresses.FirstOrDefault();
                var personName = address.Person.Name;
                Console.WriteLine(personName);
            }
        }
    }
}
