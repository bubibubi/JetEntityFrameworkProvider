using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using JetEntityFrameworkProvider.Test.Model37_2Contexts_1;
using JetEntityFrameworkProvider.Test.Model37_2Contexts_2;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model37_2Contexts
{
    public abstract class Test
    {

        protected abstract DbConnection GetConnection();

        [TestMethod]
public void Run()
{
            using (DbConnection connection = GetConnection())
            {
                using (var context = new Context1(connection))
                {
                    context.MyEntities.Count();
                }
                using (var context = new Context2(connection))
                {
                    context.MyEntities.Count();
                    context.MyEntities.Where(_ => _.Description2.Contains("a")).Count();
                }
                using (var context = new Context2(connection))
                {
                    context.MyEntities.Where(_ => _.Description2.Contains("a")).Count();
                }
                
            }
}

        private Context1 context;

        public interface IMyEntity
        {
            int Id { get; set; }
        }

        public int SaveItem<T>(T item) where T : class, IMyEntity
        {
            DbSet dbSet = context.Set(typeof(T));
            dbSet.Attach(item);
            context.Entry(item).State = item.Id == 0 ? EntityState.Added : EntityState.Modified;
            context.SaveChanges();
            return item.Id;
        }

    }
}
