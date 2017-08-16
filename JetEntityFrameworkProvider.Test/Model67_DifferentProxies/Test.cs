using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model67_DifferentProxies
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (var context = new Context(GetConnection()))
            {
                var joe = new Person() {Name = "Joe"};
                var joesDad = new Person() {Name = "Joe's Dad"};
                joesDad.Children.Add(joe);
                joe.Info = new Info() { Description = "The Joe's Dad Info"};

                context.People.Add(joesDad);
                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                var joes1 = context.People.Single(p => p.Name == "Joe");
                var joes2 = context.People.Single(p => p.Name == "Joe's Dad").Children.Single(p => p.Name == "Joe");

                Assert.IsTrue(object.ReferenceEquals(joes1, joes2));
                Assert.IsTrue(object.ReferenceEquals(joes1.Info.GetType(), joes2.Info.GetType()));
                Assert.IsTrue(object.ReferenceEquals(joes1.Info, joes2.Info));
            }


            List<Person> allPeople;

            using (var context = new Context(GetConnection()))
            {
                allPeople = context.People
                    .Include(_ => _.Info)
                    .Include(_ => _.Children)
                    .ToList();
            }

            // This is an in memory query because to the previous ToList
            // Take care of == because is an in memory case sensitive query!
            Assert.IsNotNull(allPeople.Single(p => p.Name == "Joe").Info);
            Assert.IsNotNull(allPeople.Single(p => p.Name == "Joe's Dad").Children.Single(p => p.Name == "Joe").Info);
            Assert.IsTrue(object.ReferenceEquals(allPeople.Single(p => p.Name == "Joe").Info, allPeople.Single(p => p.Name == "Joe's Dad").Children.Single(p => p.Name == "Joe").Info));




            using (var context = new Context(GetConnection()))
            {
                allPeople = context.People
                    .Include(_ => _.Info)
                    .Include(_ => _.Children)
                    .AsNoTracking()
                    .ToList();
            }


            Exception exception = null;
            try
            {
                // The entities are not in the context so this shoud not work
                Assert.IsNotNull(allPeople.Single(p => p.Name == "Joe's Dad").Children.Single(p => p.Name == "Joe").Info);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Assert.IsNotNull(exception);
            Console.WriteLine(exception.Message);




        }
    }
}