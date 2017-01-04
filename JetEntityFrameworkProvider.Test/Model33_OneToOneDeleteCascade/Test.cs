using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model33_OneToOneDeleteCascade
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
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                db.Adresses.ToList();
            }

        }
    }
}
