using System;
using System.Data.Common;
using System.Data.Entity;

// https://stackoverflow.com/questions/44923674/ef-code-first-multi-level-inheritence-issues

namespace JetEntityFrameworkProvider.Test.Model59_StackOverflow_TPT_TPH
{
    public class Context : DbContext
    {
        public Context()
        { }


        public Context(DbConnection connection)
            : base(connection, false)
        { }

        public DbSet<Activity> Activity { get; set; }   // To have a table named Activity as required
        public DbSet<DataCaptureActivity> DataCaptureActivities { get; set; }
        public DbSet<MasterDataCaptureActivity> MasterDataCaptureActivities { get; set; }

    }

}
