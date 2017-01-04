using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model26_Decompile
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (ParentChildModel context = new ParentChildModel(connection))
            {
                //var decompiled = 
                //    context.Parents.Where(p => p.Children.Any(c => c.FullName.StartsWith("A"))).Decompile();

            }
        }

    }
}
