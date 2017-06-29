using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model57_StackOverflow
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            CreatePages();

            UpdateBookNumber();
        }

        private void CreatePages()
        {
            using (var projectDB = new Context(GetConnection()))
            {
                for (int i = 0; i < 50; i++)
                    projectDB.Pages.Add(new Page() {BookNumber = i, PageNumber = 100});

                projectDB.SaveChanges();
            }
        }

        private void UpdateBookNumber()
        {
            using (var projectDB = new Context(GetConnection()))
            {
    var pagesList = projectDB.Pages.OrderBy(x => x.PageNumber).ToList();

    for (int i = 0; i < pagesList.Count; i++)
        pagesList[i].BookNumber = null;
                
    projectDB.SaveChanges();

    for (int i = 0; i < pagesList.Count; i++)
        pagesList[i].BookNumber = i + 30;

    foreach (var blah2 in pagesList.OrderBy(x => x.Id))
        Console.WriteLine("{{ID:{0}, BN:{1}, PN:{2}}}", blah2.Id, blah2.BookNumber, blah2.PageNumber);

    projectDB.SaveChanges();
            }
        }
    }
}
