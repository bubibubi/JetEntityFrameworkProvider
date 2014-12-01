using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace JetEntityFrameworkProvider.Test.DbFirst
{
    [TestFixture]
    public class SqlSelectTest
    {
        [Test]
        public void SelectLikeShow()
        {
            School school = new School(SetUpDbFirst.EntityConnection);
            ShowCourses(school.Courses.Where(c => c.CourseID.StartsWith("C2")).ToList());
        }

        [Test]
        public void SelectBetweenShow()
        {
            School school = new School(SetUpDbFirst.EntityConnection);
            ShowCourses(school.Courses.Between(c => c.CourseID, "C1050", "C3141").ToList());
        }

        [Test]
        public void SelectLikeCount()
        {
            School school = new School(SetUpDbFirst.EntityConnection);
            Assert.AreEqual(1, school.Courses.Where(c => c.CourseID.StartsWith("C2")).ToList().Count);
        }

        [Test]
        public void SelectLikeWithEscapeChar1Count()
        {
            School school = new School(SetUpDbFirst.EntityConnection);
            Assert.AreEqual(1, school.Courses.Where(c => c.CourseID.StartsWith("*")).ToList().Count);
        }

        [Test]
        public void SelectLikeWithEscapeChar2Count()
        {
            School school = new School(SetUpDbFirst.EntityConnection);
            Assert.AreEqual(1, school.Courses.Where(c => c.CourseID.StartsWith("%")).ToList().Count);
        }

        [Test]
        public void SelectBetweenCount()
        {
            School school = new School(SetUpDbFirst.EntityConnection);
            Assert.AreEqual(2, school.Courses.Between(c => c.CourseID, "C1050", "C3141").ToList().Count);
        }



        /* According to Microsoft article http://support2.microsoft.com/kb/194206/en-us
         * The Microsoft ODBC Driver for Access and the Microsoft OLE DB Provider for Jet do not provide 
         * support for bitwise operations in SQL statements. Attempts to use AND, OR, and XOR with numeric 
         * fields in a SQL statement return the result of a logical operation (true or false).
         * This behavior is by design.
         */
        //[Test]
        //public void BitwiseAndOnFields()
        //{
        //    School school = new School(SetUpGlobal.EntityConnection);
        //    Course course = school.Courses.First(c => (c.DepartmentID & 3) == 3);
        //}

        internal void ShowCourses(IEnumerable<Course> courses)
        {
            foreach (Course course in courses)
            {
                Console.WriteLine("CourseID:{0,-10} CourseTitle:{1,-15} Department:{2,-10}",
                    course.CourseID, course.Title, course.DepartmentID);
            }
        }

    }
}
