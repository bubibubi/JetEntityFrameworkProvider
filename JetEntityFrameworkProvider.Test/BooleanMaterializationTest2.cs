using System;
using System.Linq;
using JetEntityFrameworkProvider.Test.CodeFirst;
using JetEntityFrameworkProvider.Test.Model02;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test
{
    [TestClass]
    public class BooleanMaterializationTest2
    {
        [TestMethod]
        public void Run()
        {
            using (var context = new Context(SetUpCodeFirst.Connection))
            {
                Console.WriteLine(context.TableWithSeveralFieldsTypes.Select(c => new {MyNewProperty = (bool) true}).ToList().Count);
            }
        }
    }
}
