using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model43_PKasFK
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (var context = new Context(connection))
            {
                context.AddOrUpdate("Test",
                    new Parent
                    {
                        Name = "Test",
                        Children = new List<Child>
                        {
                            new Child {ChildName = "TestChild"},
                            new Child {ChildName = "NewChild"}
                        }
                    });

            }
        }
    }
}
