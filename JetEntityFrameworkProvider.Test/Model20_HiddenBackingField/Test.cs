using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model20_HiddenBackingField
{
    public abstract class Test
    {
        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                context.Companies.Add(new Company());
            }
        }

        protected abstract DbConnection GetConnection();


    }
}
