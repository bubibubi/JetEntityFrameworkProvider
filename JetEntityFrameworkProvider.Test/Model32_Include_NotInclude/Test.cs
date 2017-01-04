using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model32_Include_NotInclude
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            {
                Visit visit;

                using (TestContext db = new TestContext(connection))
                {
                    visit = new Visit
                    {
                        Description = "Visit",
                        Address = new Address() { Description = "AddressDescription" }
                    };

                    db.Visits.Add(visit);

                    db.SaveChanges();
                }

                using (TestContext ctx = new TestContext(connection))
                {
                    visit = ctx.Visits.ToList()[0];
                    Console.WriteLine(visit.Address);
                }

                using (TestContext ctx = new TestContext(connection))
                {
                    // ReSharper disable once RedundantAssignment
                    visit = ctx.Visits
                        .Include(v => v.Address)
                        .ToList()[0];
                }
            }

        }
    }
}
