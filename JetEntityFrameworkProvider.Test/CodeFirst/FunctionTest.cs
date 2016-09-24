using System;
using System.Data.Entity.Core.Objects;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.CodeFirst
{
    [TestClass]
    class FunctionTest
    {
        [TestMethod]
        public void Run()
        {
            using (var context = new Context(SetUpCodeFirst.Connection))
            {
                TableWithSeveralFieldsType table = new TableWithSeveralFieldsType()
                {
                    MyInt = 10,
                    MyString = " My current string with leading and trailing spaces ",
                    MyDateTime = new DateTime(1969, 09, 15, 20, 03, 19)
                };

                context.TableWithSeveralFieldsTypes.Add(table);
                context.SaveChanges();



                Console.WriteLine(context.TableWithSeveralFieldsTypes.Select(c => new { c.MyDateTime.Day }).First().Day);
                Console.WriteLine(context.TableWithSeveralFieldsTypes.Select(c => new { Date = EntityFunctions.AddDays(c.MyDateTime, 4) }).First());
                Console.WriteLine(context.TableWithSeveralFieldsTypes.Select(c => new { ElapsedDays = EntityFunctions.DiffDays(c.MyDateTime, c.MyDateTime) }).First().ElapsedDays.Value);

                Console.WriteLine(context.TableWithSeveralFieldsTypes.Select(c => c.MyString.IndexOf(CanonicalFunctionsTest.MYSTRINGVALUE.Substring(5, 4))).First());


                Console.WriteLine(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.Contains(CanonicalFunctionsTest.MYSTRINGVALUE.Substring(3, 5))).First());
                Console.WriteLine(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.StartsWith(CanonicalFunctionsTest.MYSTRINGVALUE.Substring(3, 5))).FirstOrDefault());
                Console.WriteLine(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.StartsWith(CanonicalFunctionsTest.MYSTRINGVALUE.Substring(0, 5))).First());
                string stringEnd = CanonicalFunctionsTest.MYSTRINGVALUE.Substring(CanonicalFunctionsTest.MYSTRINGVALUE.Length - 5, 5);
                //Console.WriteLine(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.EndsWith(CanonicalFunctionsTest.MYSTRINGVALUE.Substring(CanonicalFunctionsTest.MYSTRINGVALUE.Length - 5, 5))).First());
                Console.WriteLine(context.TableWithSeveralFieldsTypes.Where(c => c.MyString.EndsWith(stringEnd)).First());

                context.Students.Where(s => !(new int[] { 1, 2, 3, 4 }).Contains(s.StudentID)).FirstOrDefault();
                
            }
        }
    }
}
