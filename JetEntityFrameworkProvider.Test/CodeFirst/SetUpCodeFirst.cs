using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.CodeFirst
{
    [TestClass]
    public class SetUpCodeFirst
    {

        public static DbConnection Connection;

        [AssemblyInitialize]
        static public void AssemblyInitialize(TestContext testContext)
        {

            // This is the only reason why we include the Provider
            JetEntityFrameworkProvider.JetConnection.ShowSqlStatements = true;

            Connection = Helpers.GetConnection();

            Context context = new Context(SetUpCodeFirst.Connection);

            // Need to do more than just a connection
            // We could also call             context.Database.Initialize(false);
            Student student = new Student() { StudentName = "db creation" };
            context.Students.Add(student);
            context.SaveChanges();



            context.Dispose();

        }


        [AssemblyCleanup]
        static public void AssemblyCleanup()
        {
            Connection.Dispose();
        }


    }
}
