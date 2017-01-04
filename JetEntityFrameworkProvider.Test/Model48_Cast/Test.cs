using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model48_Cast
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        // Actually no canonical function to parse an integer (cast cant compile)

        [TestMethod]
        [ExpectedException(typeof(System.NotSupportedException))]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            {
                using (var context = new Context(connection))
                {
                    context.Entities.Add(new Entity() { Id = (new Random()).Next(100000).ToString() });
                    context.SaveChanges();
                }

                using (var context = new Context(connection))
                {
                    context.Entities.Where(_ => _.Number.ToString() == "A").ToList();
                    //context.Entities.Where(_ => Context.Rnd() == 10).ToList();
                    context.Entities.Where(_ => Double.Parse(_.Id) > 5).ToList();
                }
            }
        }
    }
}
