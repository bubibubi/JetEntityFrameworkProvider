using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model28
{
    [TestClass]
    public class Model28_SimpleTestJetTest : Test
    {
        protected override DbConnection GetConnection()
        {
            return JetEntityFrameworkProvider.Test.Helpers.GetJetConnection();
        }
    }
}
