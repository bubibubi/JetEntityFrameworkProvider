using System;
using System.Collections.Generic;
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

        public static JetConnection Connection;

        [SetUp]
        public void Init()
        {

            JetEntityFrameworkProvider.JetCommand.ShowSqlStatements = true;

            Connection = Helpers.GetJetConnection();

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
