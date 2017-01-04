using System;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model16_OwnCollection
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            int blogId;

            using (DbConnection connection = GetConnection())
            {
                using (BloggingContext db = new BloggingContext(connection))
                {
                    Blog blog = new Blog { Name = "MyNewBlog" };
                    blog.Posts.Add("My1stPost", "Not really interesting post");
                    blog.Posts.Add("My2ndPost", "Not really interesting post");
                    blog.Posts.Add("My3rdPost", "Not really interesting post");

                    blog = db.Blogs.Add(blog);
                    db.SaveChanges();
                    blogId = blog.BlogId;
                }

                using (BloggingContext db = new BloggingContext(connection))
                {
                    //var query = db.Blogs.Where(b => b.BlogId == 25);
                    //DataTable dataTable = ToDataTable(connection, query);

                    Blog blog = db.Blogs.Find(blogId);
                    foreach (Post post in blog.Posts)
                        Console.WriteLine("{0} - {1}", post.Title, post.Content);
                }
            }
        }

        private static DataTable ToDataTable(DbConnection connection, IQueryable query)
        {

            if (query == null)
                throw new ArgumentNullException("query");
            if (connection == null)
                throw new ArgumentNullException();

            string sqlQuery = query.ToString();

            DbCommand command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            // Get the right one or get it from a factory
            DbDataAdapter dataAdapter = new OleDbDataAdapter();
            dataAdapter.SelectCommand = (OleDbCommand)command;

            DataTable dataTable = new DataTable("sd");

            try
            {
                command.Connection.Open();
                dataAdapter.Fill(dataTable);
            }
            finally
            {
                command.Connection.Close();
            }

            return dataTable;
        }

    }
}
