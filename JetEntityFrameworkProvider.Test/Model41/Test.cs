using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model41
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (var context = new DemoContext(connection))
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.Applicants.Count();
                //context.Applicants.AsQueryable().Where("Ciao");

            }
        }
    }
}
