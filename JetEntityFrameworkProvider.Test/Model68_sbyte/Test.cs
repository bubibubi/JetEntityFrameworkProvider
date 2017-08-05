using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model68_sbyte
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (var context = new Context(GetConnection()))
            {
                context.Infos.Add(new Info() {Sbyte = 12});
                context.SaveChanges();
            }
        }
    }
}