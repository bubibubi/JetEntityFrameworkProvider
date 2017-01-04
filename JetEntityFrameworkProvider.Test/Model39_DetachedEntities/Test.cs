using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model39_DetachedEntities
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        [ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void Run1()
        {
            using (DbConnection connection = GetConnection())
            using (var context = new MyContext(connection))
            {
                Grade grade = new Grade() {Id = 1, Quantity = 50, Name = "Dont care"};
                UpdateQuantity(context, grade);
            }
        }

        private static void UpdateQuantity(MyContext context, object f)
        {
            DbSet dbSet = context.Set(f.GetType());
            dbSet.Attach(f);
            var be = context.Entry(f);
            be.Property("Quantity").IsModified = true;

            context.SaveChanges();
        }

        [TestMethod]
        public void Run2()
        {

            using (DbConnection connection = GetConnection())
            using (var context = new MyContext(connection))
            {
                context.Grades.RemoveRange(context.Grades);
                context.GradeWidths.RemoveRange(context.GradeWidths);
                context.SaveChanges();
            }

            using (DbConnection connection = GetConnection())
            using (var context = new MyContext(connection))
            {
                context.Grades.Add(new Grade()
                {
                    Id = 1, 
                    Quantity = 50, 
                    Name = "Dont care",
                    GradeWidths = new List<GradeWidth>(new[]
                    {
                        new GradeWidth() {Width = 10},
                        new GradeWidth() {Width = 20},
                        new GradeWidth() {Width = 30}
                    })
                });
                context.SaveChanges();
            }

            using (DbConnection connection = GetConnection())
            using (var context = new MyContext(connection))
            {
                Grade grade = context.Grades.Include(g => g.GradeWidths).AsNoTracking().First();

                // We need to reset all the ids
                grade.Id = 0;
                foreach (GradeWidth gradeWidth in grade.GradeWidths)
                    gradeWidth.Id = 0;


                context.Grades.Add(grade);
                context.SaveChanges();
            }

            using (DbConnection connection = GetConnection())
            using (var context = new MyContext(connection))
            {
                Debug.Assert(context.Grades.Count() == 2);
                Debug.Assert(context.GradeWidths.Count() == 6);
            }

        }

    }
}
