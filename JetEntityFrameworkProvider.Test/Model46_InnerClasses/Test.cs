using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model46_InnerClasses
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
                ClassA a = new ClassA();
                a.B.b = 10;


                a.x = 10;
                a.y = 20;

                context.ClassAs.Add(a);
                context.SaveChanges();
            }

            using (DbConnection connection = GetConnection())
            using (var context = new Context(connection))
            {
                ClassA a = context.ClassAs.First();
                Debug.Assert(a.B.b == 10);
                Debug.Assert(a.C != null);
                Debug.Assert(a.C.c == null);
            }
        }
    }
}
