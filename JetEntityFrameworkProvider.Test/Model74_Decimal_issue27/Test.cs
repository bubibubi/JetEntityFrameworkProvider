using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model74_Decimal_issue27
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (var context = new Context(GetConnection()))
            {
                context.Infos.Add(new Info() {Number = 12.43m});
                context.SaveChanges();
            }




            using (var context = new Context(GetConnection()))
            {
                Console.WriteLine(context.Infos.Sum(_ => _.Number));
            }


        }
    }
}