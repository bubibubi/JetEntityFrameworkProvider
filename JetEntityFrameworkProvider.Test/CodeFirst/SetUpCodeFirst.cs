using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace JetEntityFrameworkProvider.Test.CodeFirst
{
    [SetUpFixture]
    public class SetUpCodeFirst
    {

        public static DbConnection Connection;

        [SetUp]
        public void Init()
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


        [TearDown]
        public void Dispose()
        {
            Connection.Dispose();
        }


    }
}
