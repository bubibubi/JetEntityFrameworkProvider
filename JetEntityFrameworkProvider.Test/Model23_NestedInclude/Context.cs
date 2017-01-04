using System;
using System.Data.Common;
using System.Data.Entity;

namespace JetEntityFrameworkProvider.Test.Model23_NestedInclude
{
    class Context : DbContext
    {

        public Context(DbConnection connection)
            : base(connection, false)
        {
        }

        // For migration test
        public Context()
        { }

        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentMetadata> DocumentMetadatas { get; set; }
        public DbSet<DocumentMetadataExpression> DocumentMetadataExps { get; set; }
        public DbSet<VariableMetadata> VariableMetadatas { get; set; }
        public DbSet<Expression> Expressions { get; set; }
    }
}
