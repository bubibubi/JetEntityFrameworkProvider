using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace JetEntityFrameworkProvider.Test.CodeFirst
{
    [TestFixture]
    public class DmlTest
    {
        [Test]
        public void Insert()
        {

            Context context = new Context(SetUpCodeFirst.Connection);

            Student student;
            student = new Student() { StudentName = "New Student 1" };
            context.Students.Add(student);
            student = new Student() { StudentName = "New Student 2" };
            context.Students.Add(student);
            context.SaveChanges();

            context.Dispose();

        }

        [Test]
        public void AddUpdateDelete()
        {
            Context context;

            Student student;

            // Add a student to update
            context = new Context(SetUpCodeFirst.Connection);
            student = new Student() { StudentName = "Student to update" };
            context.Students.Add(student);
            context.SaveChanges();
            int studentId = student.StudentID;
            context.Dispose();

            context = new Context(SetUpCodeFirst.Connection);

            // Retrieve the student
            student = context.Students.Where(s => s.StudentID == studentId).First();
            
            // Update the student
            student.StudentName = "Student updated";
            context.SaveChanges();
            context.Dispose();

            // Retrieve the student and check that is the right student
            context = new Context(SetUpCodeFirst.Connection);
            student = context.Students.Where(s => s.StudentName == "Student updated").First();
            Assert.AreEqual(student.StudentID, studentId);

            // Delete the student
            context.Students.Remove(student);
            context.SaveChanges();
            context.Dispose();

            // Try to retrieve the student
            context = new Context(SetUpCodeFirst.Connection);
            student = context.Students.Where(s => s.StudentName == "Student updated" || s.StudentID == studentId).FirstOrDefault();
            Assert.AreEqual(student, null);


        }

        [Test]
        public void AddOnRelationAndList()
        {
            Context context = new Context(SetUpCodeFirst.Connection);
            Standard standard = new Standard() { StandardName = "Standard used in student" };
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

        }
    }
}
