using System;
using System.Data.Common;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model77_RepoModel
{
    [TestClass]
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            var random = new Random();
            var buffer = new byte[16];
            
            random.NextBytes(buffer);
            Guid id = new Guid(buffer);

            random.NextBytes(buffer);
            string topBidderId = (new Guid(buffer)).ToString();
            using (var applicationDbContext = new ApplicationDbContext(GetConnection()))
            {
                applicationDbContext.Items.Add(new Item() {Id = id, TopBidderId = topBidderId});
                applicationDbContext.SaveChanges();
            }


            var context = new Repository(GetConnection());
            var auction = context.ItemRepository.Get().FirstOrDefault(i => i.Id == id);
            Assert.IsNotNull(auction);
            Assert.AreEqual(topBidderId, auction.TopBidderId);
        }
    }
}
