using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model63_Time
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            Item item1;
            Item item2;

            using (var context = new Context(GetConnection()))
            {
                context.Items.AddRange(
                    new[]
                    {
                        item1 = new Item() {TimeSpan = null},
                        item2 = new Item() {TimeSpan = new TimeSpan(123456)}
                    });
                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                Assert.IsNull(context.Items.Find(item1.Id).TimeSpan);
                Assert.AreEqual(new TimeSpan(123456), context.Items.Find(item2.Id).TimeSpan);
            }

        }
    }
}
