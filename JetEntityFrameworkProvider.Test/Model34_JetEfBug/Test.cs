using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model34_JetEfBug
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (var db = new DataContext(connection))
            {
                var categoriesList = db.Categories.Select(c => new { c.ID, c.Name, TotalItems = db.Items.Count(i => i.Category.ID == c.ID) });
                Console.WriteLine(categoriesList);
            }
        }
    }
}
