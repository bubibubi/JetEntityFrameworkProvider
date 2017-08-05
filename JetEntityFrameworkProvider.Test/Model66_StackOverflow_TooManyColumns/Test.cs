using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model66_StackOverflow_TooManyColumns
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (var context = new Context(GetConnection()))
            {
                context.FasleManJdls.ToList();
            }
        }

    }
}
