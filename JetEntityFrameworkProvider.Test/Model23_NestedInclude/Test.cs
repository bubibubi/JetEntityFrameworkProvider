using System;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetEntityFrameworkProvider.Test.Model23_NestedInclude
{
    public abstract class Test
    {
        protected abstract DbConnection GetConnection();

        [TestMethod]
        public void Run()
        {
            using (DbConnection connection = GetConnection())
            {
                Context context = new Context(connection);

                for (int i = 0; i < 1; i++)
                {

                    DocumentMetadata documentMetadata = new DocumentMetadata()
                    {
                        Name = "My document metadata"
                    };
                    documentMetadata.DocumentMetadataExpressions.Add(
                        new DocumentMetadataExpression()
                        {
                            Expression = new Expression() { Name = "Expression name", Value = "Expression Value" }
                        });
                    documentMetadata.VariablesMetadata.Add(
                        new VariableMetadata() { DefaultValue = "Variable default value", Name = "Variable name", Type = "The type" });

                    Document document = new Document()
                    {
                        Name = "My new document",
                        DocumentMetadata = documentMetadata
                    };
                    context.Documents.Add(document);
                    if (i % 500 == 0)
                    {
                        context.SaveChanges();
                        context.Dispose();
                        context = new Context(connection);
                        Console.Write(".");
                    }
                }
                context.SaveChanges();
                context.Dispose();

                using (context = new Context(connection))
                {
                    var documents = context.Documents
                        .Include(db => db.DocumentMetadata)
                        .Include(db => db.DocumentMetadata.VariablesMetadata)
                        .Include(db => db.DocumentMetadata.DocumentMetadataExpressions).ToList();

                    foreach (Document document in documents)
                    {
                        Console.WriteLine(document);
                    }

                }

            }

        }
    }
}
