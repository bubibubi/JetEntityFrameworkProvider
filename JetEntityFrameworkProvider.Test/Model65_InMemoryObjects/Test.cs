using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model65_InMemoryObjects
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {

            Item item;


            // Seed =============================
            using (var context = new Context(GetConnection()))
            {
                context.Items.AddRange(
                    new[]
                    {
                        item = new Item() {Description = "Description1"},
                        new Item() {Description = "Description2"},
                        new Item() {Description = "Description3"},
                        new Item() {Description = "Description4"},
                    });
                context.SaveChanges();
            }

            // Upsert (object without a proxy) =============================
            item.Description = "DescriptionChanged";
            Upsert(item);

            // Upsert (new) =============================
            Upsert(new Item() {Description = "Item from upsert"});

            using (var context = new Context(GetConnection()))
            {
                item = context.Items.First(_ => _.Description == "Description2");
            }

            // Upsert (object with a proxy) =============================
            Assert.AreNotEqual(item.GetType(), typeof(Item));
            item.Description = "Description changed to object with proxy";
            Upsert(item);

            using (var context = new Context(GetConnection()))
            {
                Assert.IsNull(context.Items.FirstOrDefault(_ => _.Description == "Description2"));
            }

        }

        public void Upsert(Item item)
        {
            using (var context = new Context(GetConnection()))
            {
                if (item.Id == 0)
                {
                    context.Items.Add(item);
                }
                else
                {
                    context.Entry(item).State = EntityState.Modified;
                }

                context.SaveChanges();
            }
        }

    }
}
