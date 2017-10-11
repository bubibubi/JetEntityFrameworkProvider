using System;
using System.Data.Common;
using System.Data.Entity.ModelConfiguration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model72_ComplexTypeWNavyProp
{
    public abstract class Test
    {
        [TestMethod]
        [ExpectedException(typeof(ModelValidationException))]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (TestContext db = new TestContext(connection))
            {
                var city = new City() {Name = "Modena", Cap = "40100"};

                var friend = new Friend
                {
                    Name = "Bubi",
                    Address =
                    {
                        City = city,
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
