using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace JetEntityFrameworkProvider.Test.CodeFirst
{
    [TestFixture]
    public class DdlTest
    {
        [Test]
        public void CheckIfTablesExists()
        {
            bool exists = SetUpCodeFirst.Connection.TableExists("Students");
            Assert.IsTrue(exists);
        }
    }
}
