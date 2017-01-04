using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model49_Inheritance_EagerlyLoad
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        // Actually there are some bugs on ef related to inheritance and eagerly loading

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "The Include path expression")]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (var context = new Context(connection))
            {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                context.A.ToList();
                
                string id = "25";

                context.A
                    .Where(x => x.Id.Equals(id))
                    .Include(_ => _.Bases)
                    .SelectMany(_ => _.Bases)
                    .OfType<Base1>()
                    .Select(y => y.SomeClass)
                    .ToList();

                context.A
                    .Include(_ => _.Bases.OfType<Base1>()
                    .Select(y => y.SomeClass))
                    .Where(x => x.Id.Equals(id))
                    .ToList();
            }
        }
    }
}
