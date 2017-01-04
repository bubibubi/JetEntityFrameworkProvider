using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model05_WithIndex
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {
            using(DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.Bars.ToList();
            }
        }

        protected abstract DbConnection GetConnection();

    }
}
