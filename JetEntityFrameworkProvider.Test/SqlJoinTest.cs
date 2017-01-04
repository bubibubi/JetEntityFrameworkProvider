using System;
using System.Linq;
using JetEntityFrameworkProvider.Test.Model01;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test
{
    [TestClass]
    public class SqlJoinTest
    {
        const string THESTANDARD = "SqlJoinTest Standard";

        [TestInitialize]
        public void Init()
        {
            Context context = new Context(SetUpCodeFirst.Connection);
            Standard standard = new Standard() { StandardName = THESTANDARD };
            Student student;
            context.Standards.Add(standard);
            context.SaveChanges();
            student = new Student() { StudentName = "Student 1 related to standard", Standard = standard };
            context.Students.Add(student);
            student = new Student() { StudentName = "Student 2 related to standard", Standard = standard };
            context.Students.Add(student);
            context.SaveChanges();

            int standardId = standard.StandardId;

            standard = context.Standards.Where(s => s.StandardId == standardId).First();

            Assert.AreEqual(standard.Students.Count, 2);

            foreach (Student student2 in standard.Students)
                Console.WriteLine(student2);
            
            context.Dispose();
        }

        [TestMethod]
        public void JoinTest()
        {
            Context context = new Context(SetUpCodeFirst.Connection);

            Assert.AreEqual(2, context.Students.Where(s => s.Standard.StandardName == THESTANDARD).ToList().Count);
            Assert.AreEqual(2, context.Students.Where(s => s.Standard.StandardName == THESTANDARD).Select(s => new { MyStudentName = s.StudentName, MyStudentStandardName = s.Standard.StandardName }).ToList().Count);


            context.Dispose();
        }



    }
}
