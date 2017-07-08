using System;
using System.Data.Common;
using System.Data.Entity;

// https://stackoverflow.com/questions/44972714/ef-seed-table-with-foreign-key

namespace JetEntityFrameworkProvider.Test.Model61_StackOverflow_Seed
{
    public class Context : DbContext
    {
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<ClassSchedule> ClassSchedules { get; set; }

    }

}
