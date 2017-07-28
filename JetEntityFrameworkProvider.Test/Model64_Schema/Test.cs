using System;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model64_Schema
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (var context = new Context(GetConnection()))
            {
                context.Items.AddRange(
                    new[]
                    {
                        new Item() {Description = "Description1"},
                        new Item() {Description = "Description2"},
                        new Item() {Description = "Description3"},
                        new Item() {Description = "Description4"},
                    });
                context.SaveChanges();
            }

            using (var connection = GetConnection())
            {
                connection.Open();
                DbCommand command = connection.CreateCommand();
                command.CommandText = "Select * from TableWithSchema";
                command.ExecuteReader().Dispose();
            }
        }
    }
}
