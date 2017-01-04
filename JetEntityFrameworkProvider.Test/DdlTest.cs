using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test
{
    [TestClass]
    public class DdlTest
    {
        [TestMethod]
        public void CheckIfTablesExists()
        {
            bool exists = ((JetConnection)SetUpCodeFirst.Connection).TableExists("Students");
            Assert.IsTrue(exists);
        }
    }
}
