using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model35_OneToZeroOneDeleteCascade
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (var db = new Context(connection))
            {
                var categoriesList = db.Dependents.Count();
                Console.WriteLine(categoriesList);
            }
        }
    }
}
