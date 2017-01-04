using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model36_DetachedScenario
{
    public abstract class Test
    {

        protected abstract DbConnection GetConnection();

        [TestMethod]
        [ExpectedException(typeof(DbEntityValidationException))]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            {
                Seed(connection);

                ShowHoldersId(connection);

                ShowHolders(connection);

                using (var context = new Context(connection))
                {
                    Holder holder = new Holder()
                    {
                        Id = 1,
                        Some = "Holder updated",
                        Thing = new Thing() { Id = 2 }
                    };

                    Repository.Update(context, holder);
                }

                ShowHolders(connection);

                Console.WriteLine("========== ATTACHED UPDATE =============");

                using (var context = new Context(connection))
                {
                    Holder holder = context.Holders.First();
                    holder.Thing = new Thing() { Id = 4 };
                    context.SaveChanges();
                }

                ShowHolders(connection);
            }
        }

        private static void ShowHolders(DbConnection connection)
        {
            using (var context = new Context(connection))
            {
                foreach (var holder in context.Holders.AsQueryable().Include(_ => _.Thing).ToList())
                    Console.WriteLine(holder);
            }
        }

        private static void ShowHoldersId(DbConnection connection)
        {
            using (var context = new Context(connection))
            {
                foreach (var holder in context.Holders.Select(_ => _.Id).ToList())
                    Console.WriteLine(holder);
            }
        }


        public static void Seed(DbConnection connection)
        {
            using (var context = new Context(connection))
            {
                Holder holder = new Holder()
                {
                    Some = "Holder 1"
                };

                context.Holders.Add(holder);

                for (int i = 0; i < 3; i++)
                {
                    Thing thing = new Thing()
                    {
                        Name = string.Format("Thing {0}", i + 1)
                    };
                    context.Things.Add(thing);
                }

                context.SaveChanges();

            }
        }

    }
}
