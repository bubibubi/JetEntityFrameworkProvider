using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model55_Unicode
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {

            string text1 = "òèù";
            string text2 = "﻿崩壊アンプリファー";

            using (var context = new Context(GetConnection()))
            {
                context.Entities.Add(new Entity() { Description = text1 });
                context.Entities.Add(new Entity() { Description = text2 });
                context.SaveChanges();
            }

            using (var context = new Context(GetConnection()))
            {
                foreach (Entity entity in context.Entities.ToList())
                    Console.WriteLine(entity.Description);
            }

            using (var context = new Context(GetConnection()))
            {
                Assert.IsTrue(context.Entities.SingleOrDefault(_ => _.Description == text1) != null);
                Assert.IsTrue(context.Entities.SingleOrDefault(_ => _.Description == text2) != null);
            }
        }
    }
}
