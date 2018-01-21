using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model75_Include_issue28
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            ReferringClass1 rc1 = new ReferringClass1() {MyDouble = 36.7, MyFloat = 44.3f, MyInt = 1};
            using (var context = new Context(GetConnection()))
            {
                var r = new ReferredClass();
                r.ReferringClasses1.Add(rc1);
                r.ReferringClasses1.Add(new ReferringClass1() { MyDouble = 36.7, MyFloat = 44.3f, MyInt = 16 });
                r.ReferringClasses2.Add(new ReferringClass2() { MyDouble = 36.7, MyFloat = 44.3f, MyInt = 16 });
                r.ReferringClasses2.Add(new ReferringClass2() { MyDouble = 36.7, MyFloat = 44.3f, MyInt = 16 });
                context.ReferredClasses.Add(r);
                context.SaveChanges();
            }




            using (var context = new Context(GetConnection()))
            {
                var rSet = context.ReferredClasses.Where(_ => _.Id < 10)
                    .Include(_ => _.ReferringClasses1)
                    .Include(_ => _.ReferringClasses2).ToList();
                var rc1Result = rSet.SelectMany(_ => _.ReferringClasses1).First(_ => _.MyInt == 1);
                //Assert.AreEqual(rc1.MyDecimal, rc1Result.MyDecimal);
            }


        }
    }
}