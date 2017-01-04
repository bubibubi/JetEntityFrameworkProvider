using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model27_SimpleTest
{
    public abstract class Test
    {

        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (MyDbContext dbContext = new MyDbContext(connection))
            {
                dbContext.Users.Add(new User { Email = "x@b.com", Name = "x" });
                dbContext.Users.Add(new User { Email = "x@b.com", Name = "x" });
                dbContext.Vehicles.Add(new Vehicle { BuildYear = 2016, Model = "BMW X4" });
                dbContext.SaveChanges();
            }
        }
    }
}
