using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model40_HardMapping
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
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.Cars.Count();
            }
        }
    }
}
