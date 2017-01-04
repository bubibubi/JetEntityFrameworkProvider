using System;
using System.Data.Common;
using System.Data.Entity.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model14_StackOverflow31992383
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        // With EF 6.1.1 this test case worked

        [TestMethod]
        [ExpectedException(typeof(DbEntityValidationException))]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (TestContext db = new TestContext(connection))
            {
                // MyProperty not specified
                db.C1s.Add(new Class1());
                db.SaveChanges();   // Here in EF 6.1.3 is raised an exception because MyProperty is required

                // MyProperty not specified
                db.C3s.Add(new Class3());
                db.SaveChanges();   // Here in EF 6.1.3 is raised an exception because MyProperty is required
            }
        }
    }
}
