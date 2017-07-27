using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model62_InnerQueryBug_DbInitializerSeed
{
    //[TestClass]
    public class Model62_InnerQueryBug_SqlServerTest : Test
    {
        protected override DbConnection GetConnection()
        {
            return Helpers.GetSqlServerConnection();
        }
    }
}
