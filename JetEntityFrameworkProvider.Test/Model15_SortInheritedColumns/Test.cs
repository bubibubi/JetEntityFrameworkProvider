using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model15_SortInheritedColumns
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
                db.Brands.Add(new Brand());
                db.SaveChanges(); // Just to run DB and Table creation statement

                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                db.Brands.Where(b => b.addDate.ToString().Contains("abc")).ToList();
            }
        }
    }
}
