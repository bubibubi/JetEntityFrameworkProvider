using System;
using System.Data.Common;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model45_InheritTPHWithSameRelationship
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        // Actually this test does not work
        [TestMethod]
        [ExpectedException(typeof(ModelValidationException))]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                context.M1s.FirstOrDefault();
            }
        }
    }
}
