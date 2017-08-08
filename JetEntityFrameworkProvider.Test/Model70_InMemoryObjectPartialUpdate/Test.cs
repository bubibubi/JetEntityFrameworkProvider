using System;
using System.Data.Common;
using System.Data.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model70_InMemoryObjectPartialUpdate
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            int itemId;

            using (var context = new Context(GetConnection()))
            {
                Item item = new Item() {ColumnA = "ColumnA", ColumnB = "ColumnB"};
                context.Items.Add(item);
                context.SaveChanges();
                itemId = item.Id;
            }

            using (var context = new Context(GetConnection()))
            {
                Item item = new Item() { Id = itemId, ColumnA = "ColumnAUpdated"};
                context.Entry(item).State = EntityState.Modified;
                context.Entry(item).Property(_ => _.ColumnB).IsModified = false;
                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                Item item = context.Items.Find(itemId);
                Assert.IsNotNull(item);
                Assert.AreEqual("ColumnAUpdated", item.ColumnA);
                Assert.AreEqual("ColumnB", item.ColumnB);
            }



        }
    }
}