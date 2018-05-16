using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model76_Contains
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {

            using (var context= new Context(GetConnection()))
            {
                context.Records.Add(new Record() {Description = "WordsDontComeEasyToMe"});
                context.Records.Add(new Record() { Description = "HowCanIFindTheWay" });
                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                int l3 = 3;
                int l2 = 2;
                string s = "";
                var list = context.Records
                    .Where(_ => _.Description.StartsWith(s) && _.Description.Length > l3)
                    .Select(_ => _.Description.Substring(0, l2)).Distinct().ToList();
                Assert.AreEqual(2, list.Count);
            }

        }
    }
}