using System;
using System.Linq;
using JetEntityFrameworkProvider.Test.CodeFirst;
using JetEntityFrameworkProvider.Test.Model01;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test
{
    [TestClass]
    public class CanonicalFunctionsTest2
    {

        [TestMethod]
        public void CastToBool()
        {
            Context context = new Context(SetUpCodeFirst.Connection);
            Standard standard = new Standard() { StandardName = "Another Standard" };
            context.Standards.Add(standard);
            context.SaveChanges();

            Assert.IsTrue(context.Standards.Select(c => new {MyNewProperty = true }).ToList().Count > 0);
            context.Dispose();
        }

        [TestMethod]
        public void InClause()
        {
            Context context = new Context(SetUpCodeFirst.Connection);
            Standard standard = new Standard() { StandardName = "Standard used in student in clause" };
            Student student;
            context.Standards.Add(standard);
            context.SaveChanges();
            student = new Student() { StudentName = "Student 1 related to standard in clause", Standard = standard };
            context.Students.Add(student);
            student = new Student() { StudentName = "Student 2 related to standard in clause", Standard = standard };
            context.Students.Add(student);
            context.SaveChanges();

            Assert.IsNotNull(context.Students.Where(s => context.Standards.Contains(s.Standard)).First());
            Assert.IsNotNull(context.Students.Where(s => (new[] {1,2,3,4}).Contains(s.StudentId)).First());
            context.Dispose();
        }

        [TestMethod]
        public void NotInClause()
        {
            using (Context context = new Context(SetUpCodeFirst.Connection))
            {
                // ReSharper disable ReturnValueOfPureMethodIsNotUsed
                context.Students.Where(s => !(new[] {1, 2, 3, 4}).Contains(s.StudentId)).FirstOrDefault();
                // ReSharper restore ReturnValueOfPureMethodIsNotUsed
            }
        }

    }
}
