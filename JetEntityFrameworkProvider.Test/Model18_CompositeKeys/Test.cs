using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model18_CompositeKeys
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (TestContext context = new TestContext(connection))
            {
                context.Products.Add(
                    new Product()
                    {
                        ArticleNumber = "ABCD"
                    });
                context.SaveChanges();
            }
        }

    }
}
