using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model13_TableSplit_1_1rel
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (TestContext db = new TestContext(connection))
            {
                var visit = new Visit
                {
                    Description = "Visit",
                    Address = { Description = "AddressDescription" }
                };

                db.Visits.Add(visit);

                db.SaveChanges();
            }
        }
    }
}
