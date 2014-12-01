using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace JetEntityFrameworkProvider.Test.CodeFirst
{
    public class CanonicalFunctionsTest
    {

        internal const string MYSTRINGVALUE = " My current string with leading and trailing spaces ";
        internal const double MYDOUBLEVALUE = -123.456789;
        int insertedRecordId;

        [SetUp]
        public void Init()
        {

            Context context = new Context(SetUpCodeFirst.Connection);

            TableWithSeveralFieldsType table = new TableWithSeveralFieldsType()
            {
                MyInt = 10,
                MyDouble = MYDOUBLEVALUE,
                MyString = MYSTRINGVALUE,
                MyDateTime = new DateTime(1969, 09, 15, 20, 03, 19)
            };

            context.TableWithSeveralFieldsTypes.Add(table);
            context.SaveChanges();
            context.Dispose();

            insertedRecordId = table.Id;

        }

        [Test]
        public void DateTimeFunction()
        {

            Context context = new Context(SetUpCodeFirst.Connection);
            IQueryable<TableWithSeveralFieldsType> insertedRecord = GetInsertedRecordQueryable(context);

            Assert.AreEqual(insertedRecord.Select(c => new { c.MyDateTime.Year }).First().Year, 1969);
            Assert.AreEqual(insertedRecord.Select(c => new { c.MyDateTime.Month }).First().Month, 09);
            Assert.AreEqual(insertedRecord.Select(c => new { c.MyDateTime.Day }).First().Day, 15);

            Assert.AreEqual(insertedRecord.Select(c => new { c.MyDateTime.Hour }).First().Hour, 20);
            Assert.AreEqual(insertedRecord.Select(c => new { c.MyDateTime.Minute }).First().Minute, 3);
            Assert.AreEqual(insertedRecord.Select(c => new { c.MyDateTime.Second }).First().Second, 19);

            Assert.AreEqual(insertedRecord.Select(c => new { Date = EntityFunctions.AddDays(c.MyDateTime, 4) }).First().Date.Value.Day, 19);
            Assert.AreEqual(insertedRecord.Select(c => new { ElapsedDays = EntityFunctions.DiffDays(c.MyDateTime, c.MyDateTime) }).First().ElapsedDays.Value, 0);

            context.Dispose();

        }

        private IQueryable<TableWithSeveralFieldsType> GetInsertedRecordQueryable(Context context)
        {
            return context.TableWithSeveralFieldsTypes.Where(c => c.Id == insertedRecordId);
        }

        [Test]
        public void StringFunction()
        {
            Context context = new Context(SetUpCodeFirst.Connection);
            
            IQueryable<TableWithSeveralFieldsType> insertedRecord = GetInsertedRecordQueryable(context);

            Assert.AreEqual(insertedRecord.Select(c => c.MyString.ToLower()).First(), MYSTRINGVALUE.ToLower());
            Assert.AreEqual(insertedRecord.Select(c => c.MyString.ToUpper()).First(), MYSTRINGVALUE.ToUpper());
            Assert.AreEqual(insertedRecord.Select(c => c.MyString.Trim()).First(), MYSTRINGVALUE.Trim());
            Assert.AreEqual(insertedRecord.Select(c => c.MyString.TrimEnd()).First(), MYSTRINGVALUE.TrimEnd());
            Assert.AreEqual(insertedRecord.Select(c => c.MyString.TrimStart()).First(), MYSTRINGVALUE.TrimStart());
            Assert.AreEqual(insertedRecord.Select(c => c.MyString.IndexOf(MYSTRINGVALUE.Substring(5, 4))).First(), MYSTRINGVALUE.IndexOf(MYSTRINGVALUE.Substring(5, 4)));
            
            context.Dispose();
        }

        [Test]
        public void LikeFunction()
        {
            Context context = new Context(SetUpCodeFirst.Connection);

            Assert.IsNotNull(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.Contains(MYSTRINGVALUE.Substring(3,5))).First());
            Assert.IsNull(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.StartsWith(MYSTRINGVALUE.Substring(3, 5))).FirstOrDefault());
            Assert.IsNotNull(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.StartsWith(MYSTRINGVALUE.Substring(0, 5))).First());
            string stringEnd = MYSTRINGVALUE.Substring(MYSTRINGVALUE.Length - 5, 5);
            Assert.IsNotNull(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.EndsWith(stringEnd)).First());

            context.Dispose();
        }

        [Test]
        public void ConcatFunction()
        {
            Context context = new Context(SetUpCodeFirst.Connection);

            IQueryable<TableWithSeveralFieldsType> insertedRecord = GetInsertedRecordQueryable(context);

            Assert.AreEqual(insertedRecord.Select(c => string.Concat(c.MyString, "abc")).First(), string.Concat(MYSTRINGVALUE, "abc"));

            context.Dispose();
        }

        [Test]
        public void FloatingPointFunctions()
        {
            Context context = new Context(SetUpCodeFirst.Connection);

            IQueryable<TableWithSeveralFieldsType> insertedRecord = GetInsertedRecordQueryable(context);

            Assert.AreEqual(insertedRecord.Select(c => Math.Abs(c.MyDouble)).First(), Math.Abs(MYDOUBLEVALUE));
            Assert.AreEqual(insertedRecord.Select(c => Math.Round(c.MyDouble, 3)).First(), Math.Round(MYDOUBLEVALUE, 3));
            Assert.AreEqual(insertedRecord.Select(c => Math.Truncate(c.MyDouble)).First(), Math.Truncate(MYDOUBLEVALUE));
            Assert.AreEqual(insertedRecord.Select(c => Math.Truncate(c.MyDouble)).First(), Math.Truncate(MYDOUBLEVALUE));
            context.Dispose();
        }

        [Test]
        public void CastToBool()
        {
            Context context = new Context(SetUpCodeFirst.Connection);
            Standard standard = new Standard() { StandardName = "Another Standard" };
            context.Standards.Add(standard);
            context.SaveChanges();

            Assert.Greater(context.Standards.Select(c => new {MyNewProperty = (bool)true }).ToList().Count, 0);
            context.Dispose();
        }

        [Test]
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

            Assert.NotNull(context.Students.Where(s => context.Standards.Contains(s.Standard)).First());
            Assert.NotNull(context.Students.Where(s => (new int[] {1,2,3,4}).Contains(s.StudentID)).First());
            context.Dispose();
        }


    }
}
