using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model44_CaseSensitivity
{
    public abstract class Test
    {

        protected abstract DbConnection GetConnection();


        public virtual void Run()
        {
            using (DbConnection connection = GetConnection())
            using (var context = new Context(connection))
            {
                context.Entities.Add(new Entity() { Name = "Duplicated" });
                context.Entities.Add(new Entity() { Name = "DUPLICATED" });
                context.SaveChanges();
            }
        }
    }
}
