using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model06_Inherit
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {
            using(DbConnection connection = GetConnection())
            {
                int userId;

                using (Context context = new Context(connection))
                {
                    User user = new User
                    {
                        Firstname = "Bubi",
                        Address = new Address
                        {
                            City = "Modena"
                        }
                    };

                    context.Users.Add(user);
                    context.SaveChanges();

                    userId = user.Id;
                }
                using (Context context = new Context(connection))
                {
                    User user = context.Users.First(u => u.Id == userId);
                    Console.WriteLine("{0} {1}", user.Firstname, user.Address.City);

                    user.Address.City = "Bologna";
                    context.SaveChanges();
                }
                using (Context context = new Context(connection))
                {
                    User user = context.Users.First(u => u.Id == userId);
                    Console.WriteLine("{0} {1}", user.Firstname, user.Address.City);
                }

            }
        }

        protected abstract DbConnection GetConnection();

    }
}
