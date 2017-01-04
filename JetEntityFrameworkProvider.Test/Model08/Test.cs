using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model08
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                context.Files.Add(new File() { });
                context.SaveChanges();
            }
        }

        protected abstract DbConnection GetConnection();
    }
}
