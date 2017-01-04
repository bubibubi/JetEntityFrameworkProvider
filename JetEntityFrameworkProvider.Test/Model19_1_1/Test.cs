using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model19_1_1
{
    public abstract class Test
    {

        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                ClassA classA;
                ClassB classB;


                // Very simple behaviour (as expected). You can see the queries after SaveChanges()
                classA = new ClassA { Description = "B empty" };
                context.As.Add(classA);

                classA = new ClassA { Description = "B full", ClassB = new ClassB() { Description = "ClassB full" } };
                context.As.Add(classA);

                classB = new ClassB { Description = "B empty" };
                context.Bs.Add(classB);

                context.SaveChanges();
                /*
                insert into [ClassAs]([Description])
                    values (@p0);

                    @p0 = B full

                insert into [ClassAs]([Description])
                    values (@p0);

                    @p0 = B empty

                insert into [ClassBs]([Description], [ClassA_Id])
                    values (@p0, @p1);

                    @p0 = ClassB full
                    @p1 = 1

                insert into [ClassBs]([Description], [ClassA_Id])
                    values (@p0, null);
                 
                    @p0 = B empty
                */




                // Here a new classB references an already referenced classA. But we don't want this!!!
                // EF works like we want, the classA is detached from the old classB then attached to the
                // new classB. Below you can see the queries
                classB = new ClassB { Description = "B full with the wrong A", ClassA = classA };
                context.Bs.Add(classB);
                /*
                update [ClassBs]
                    set [ClassA_Id] = null
                    where (([Id] = @p0) and ([ClassA_Id] = @p1))

                    @p0 = 1
                    @p1 = 1

                insert into [ClassBs]([Description], [ClassA_Id])
                    values (@p0, @p1);

                    @p0 = B full with the wrong A
                    @p1 = 1
                */

                context.SaveChanges();
            }

            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                foreach (var classB in context.Bs.ToList())
                {
                    if (classB.ClassA == null)
                        continue;
                    Console.WriteLine("{0} {1} {2}", classB, classB.ClassA.Id, classB.ClassA.ClassB.Id);
                }
            }
        }
    }
}
