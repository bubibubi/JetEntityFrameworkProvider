using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model59_StackOverflow_TPT_TPH
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (var context = new Context(GetConnection()))
            {
                context.DataCaptureActivities.Add(new DataCaptureActivity() {Description = "Description"});
                context.SaveChanges();
            }
        }
    }
}
