using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model78_SimpleSample
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {

            using (var context= new Context(GetConnection()))
            {
                context.Contacts.Add(new Contact());
                context.SaveChanges();
            }

        }
    }
}