using System;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model44_CaseSensitivity
{
    [TestClass]
    public class Model44_CaseSensitivityJetTest : Test
    {
        protected override DbConnection GetConnection()
        {
            return Helpers.GetJetConnection();
        }

        [TestMethod]
        [ExpectedException(typeof(DbUpdateException))]
        public override void Run()
        {
            base.Run();
        }
    }
}
