using System;
using System.Linq;
using JetEntityFrameworkProvider.Test.CodeFirst;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test
{
    [TestClass]
    public class DataTypesTest
    {

        const int MYINT = 23456;

        [TestInitialize]
        public void Init()
        {

            Context context = new Context(SetUpCodeFirst.Connection);

            TableWithSeveralFieldsType table = new TableWithSeveralFieldsType()
            {
                MyInt = MYINT,
                MyDouble = 12.34,
                MyString = "Another student",
                MyDateTime = new DateTime(1969, 09, 15, 20, 03, 19),
                MyBool = true,
                MyNullableBool = false

            };

            context.TableWithSeveralFieldsTypes.Add(table);
            context.SaveChanges();
            context.Dispose();
        }

        [TestMethod]
        public void Booleans()
        {

            Context context = new Context(SetUpCodeFirst.Connection);

            context.TableWithSeveralFieldsTypes.Where(c => c.MyBool == true).Select(c => c.MyBool).First();

            context.Dispose();

        }



    }
}
