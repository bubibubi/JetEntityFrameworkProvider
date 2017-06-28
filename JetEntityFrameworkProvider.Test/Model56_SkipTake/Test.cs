using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model56_SkipTake
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void SkipTakeDate()
        {
            using (var context = new Context(GetConnection()))
            {
                for (int i = 0; i < 30; i++)
                    context.Entities.Add(new Entity() { Description = i.ToString(), Date = new DateTime(1969, 09, 15).AddDays(i % 2 == 0 ? i * 30 : - i * 30)});

                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                foreach (Entity entity in context.Entities.ToList())
                    Console.WriteLine(entity.Description);
            }

            using (var context = new Context(GetConnection()))
            {
                var entities = context.Entities.OrderBy(_ => _.Date).Skip(10).Take(5).ToList();
                Assert.AreEqual(5, entities.Count);
                for (int i = 0; i < entities.Count - 1; i++)
                {
                    Entity entity = entities[i];
                    Assert.IsTrue(entity.Date < entities[i + 1].Date);
                }
            }

            RemoveAllEntities();
        }

        private void RemoveAllEntities()
        {
            using (var context = new Context(GetConnection()))
            {
                context.Entities.RemoveRange(context.Entities.ToList());
                context.SaveChanges();
            }
        }

        [TestMethod]
        public virtual void SkipTakeDuplicatedDate()
        {
            using (var context = new Context(GetConnection()))
            {
                for (int i = 0; i < 30; i++)
                    context.Entities.Add(new Entity() { Description = i.ToString(), Date = new DateTime(1969, 09, 15)});

                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                var entities = context.Entities.OrderBy(_ => _.Date).Skip(10).Take(5).ToList();
                Assert.AreEqual(5, entities.Count);
            }

            RemoveAllEntities();
        }

        [TestMethod]
        public void SkipTakeString()
        {
            using (var context = new Context(GetConnection()))
            {
                for (int i = 0; i < 30; i++)
                    context.Entities.Add(new Entity() { Description = i % 2 == 0 ? i.ToString(): (-i).ToString() });

                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                foreach (Entity entity in context.Entities.ToList())
                    Console.WriteLine(entity.Description);
            }

            using (var context = new Context(GetConnection()))
            {
                var entities = context.Entities.OrderBy(_ => _.Description).Skip(10).Take(5).ToList();
                Assert.AreEqual(5, entities.Count);
                for (int i = 0; i < entities.Count - 1; i++)
                {
                    Entity entity = entities[i];
                    Assert.AreEqual(-1, String.Compare(entity.Description , entities[i + 1].Description));
                }
            }

            RemoveAllEntities();
        }



        [TestMethod]
        public void SkipTakeDuplicatedString()
        {
            using (var context = new Context(GetConnection()))
            {
                for (int i = 0; i < 30; i++)
                    context.Entities.Add(new Entity() { Description = "This is the same old song" });

                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                var entities = context.Entities.OrderBy(_ => _.Description).Skip(10).Take(5).ToList();
                Assert.AreEqual(5, entities.Count);
                foreach (Entity entity in context.Entities.ToList())
                    Assert.AreEqual("This is the same old song", entity.Description);
            }

            RemoveAllEntities();
        }


        [TestMethod]
        public void SkipTakeDouble()
        {
            using (var context = new Context(GetConnection()))
            {
                for (int i = 0; i < 30; i++)
                    context.Entities.Add(new Entity() { Value = i % 2 == 0 ? i : -i });

                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                foreach (Entity entity in context.Entities.ToList())
                    Console.WriteLine(entity.Value);
            }

            using (var context = new Context(GetConnection()))
            {
                var entities = context.Entities.OrderBy(_ => _.Value).Skip(10).Take(5).ToList();
                Assert.AreEqual(5, entities.Count);
                for (int i = 0; i < entities.Count - 1; i++)
                {
                    Entity entity = entities[i];
                    Assert.IsTrue(entity.Value < entities[i + 1].Value);
                }
            }

            RemoveAllEntities();
        }
    }
}
