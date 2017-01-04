using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model12_ComplexType
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (TestContext db = new TestContext(connection))
            {
                var friend = new Friend
                {
                    Name = "Bubi",
                    Address =
                    {
                        Cap = "40100",
                        Street = "The street"
                    }
                };

                db.Friends.Add(friend);

                db.SaveChanges();
            }
        }

        protected abstract DbConnection GetConnection();

    }
}
