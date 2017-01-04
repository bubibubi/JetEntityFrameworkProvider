using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model47_200
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
                context.Depts.Count();
            }
        }
    }
}
