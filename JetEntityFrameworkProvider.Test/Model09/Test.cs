using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model09
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                context.Ones.Add(new One() { });
                context.SaveChanges();
            }
        }

        protected abstract DbConnection GetConnection();
    }
}
