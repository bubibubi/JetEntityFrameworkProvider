using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model28
{
    public abstract class Test
    {

        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            var ad = new Advertisement
            {
                AdImages = new List<AdImage>
                {
                    new AdImage {Image = "MyImage"}
                },

                Message = "MyMessage",
                Title = "MyTitle",
                User = new User()
            };

            using (DbConnection connection = GetConnection())
            using (Context context = new Context(connection))
            {
                context.Advertisements.Add(ad);
                context.SaveChanges();
            }
        }
    }
}
