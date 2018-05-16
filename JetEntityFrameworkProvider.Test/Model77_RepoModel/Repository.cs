using System;
using System.Data.Common;

namespace JetEntityFrameworkProvider.Test.Model77_RepoModel
{
    class Repository
    {
        private DbConnection _dbConnection;

        private EfGenericRepository<Item> _itemRepository;

        public Repository(DbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public EfGenericRepository<Item> ItemRepository
        {
            get
            {
                if (_itemRepository == null)
                    _itemRepository = new EfGenericRepository<Item>(ApplicationDbContext.Create(_dbConnection));
                return _itemRepository;
            }
        }
    }
}
