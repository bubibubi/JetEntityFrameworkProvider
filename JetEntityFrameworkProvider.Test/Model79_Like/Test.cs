using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model79_Like
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {

            using (var context = new Context(GetConnection()))
            {
                context.Users.Add(new User() { Description = "Desc1" });
                context.Users.Add(new User() { Description = "Desc2" });
                context.Users.Add(new User() { Description = "Desc3" });
                context.Users.Add(new User() { Description = "Desc4" });
                context.Users.Add(new User() { Description = "Desc5" });
                context.Users.Add(new User() { Description = "Desc6" });
                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                var users = context.Users.Where(_ => DbFunctions.Like(_.Description, "Des%")).ToList();
                Assert.IsTrue(users.Count > 0);
            }
        }
    }
}