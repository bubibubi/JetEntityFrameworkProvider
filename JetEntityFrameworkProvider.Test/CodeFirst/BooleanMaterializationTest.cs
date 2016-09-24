using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.CodeFirst
{
    [TestClass]
    public class BooleanMaterializationTest
    {
        [TestMethod]
        public void Run()
        {
            using (var context = new Context(SetUpCodeFirst.Connection))
            {
                Console.WriteLine(context.Students.Select(c => new {MyNewProperty = (bool) true}).ToList().Count);
                Console.WriteLine(context.TableWithSeveralFieldsTypes.Select(c => new {MyNewProperty = (bool) true}).ToList().Count);
            }
        }
    }
}
