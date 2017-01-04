using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model25_InheritTPH
{
    public abstract class Test
    {

        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.M1s.FirstOrDefault();
            }
        }
    }
}
