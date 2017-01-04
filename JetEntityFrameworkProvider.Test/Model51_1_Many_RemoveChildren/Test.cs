using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model51_1_Many_RemoveChildren
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            {
                using (var context = new Context(connection))
                {
                    Blog blog = new Blog()
                    {
                        Name = "MyBlog"
                    };

                    blog.Posts.Add(new Post() { Title = "Title1" });
                    blog.Posts.Add(new Post() { Title = "Title2" });

                    context.Blogs.Add(blog);
                    context.SaveChanges();
                }

                using (var context = new Context(connection))
                {
                    Blog blog = context.Blogs.First();
                    Console.WriteLine(blog.Posts.Count);
                    context.Posts.RemoveRange(blog.Posts);
                    Console.WriteLine(blog.Posts.Count);
                }
            }
        }
    }
}
