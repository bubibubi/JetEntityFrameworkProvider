using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model69_PrivateBackingField
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (var context = new Context(GetConnection()))
            {
                context.Infos.Add(new Info() {SByte = 12});
                context.SaveChanges();
            }
        }
    }
}