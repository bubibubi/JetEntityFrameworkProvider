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

            var timeSpan = new TimeSpan(15, 12, 6);

            using (var context = new Context(GetConnection()))
            {
                context.Items.AddRange(
                    new[]
                    {
                        item1 = new Item() {TimeSpan = null, DateTime = new DateTime(1969, 09, 15)},
                        item2 = new Item() {TimeSpan = timeSpan}
                    });
                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                Assert.AreNotEqual(0, context.Items.Count(_ => _.TimeSpan == timeSpan));
            }

            using (var context = new Context(GetConnection()))
            {
                Assert.IsNull(context.Items.Find(item1.Id).TimeSpan);
                var item = context.Items.Find(item2.Id);
                Assert.AreEqual(timeSpan, item.TimeSpan);
            }

        }
    }
}
