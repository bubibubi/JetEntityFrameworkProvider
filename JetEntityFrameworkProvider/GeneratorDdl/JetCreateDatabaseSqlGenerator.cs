using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    sealed class JetCreateDatabaseSqlGenerator : JetDdlBuilder
    {
        private readonly HashSet<EntitySet> ignoredEntitySets = new HashSet<EntitySet>();

        internal static string CreateObjectsScript(StoreItemCollection itemCollection)
        {
            JetCreateDatabaseSqlGenerator builder = new JetCreateDatabaseSqlGenerator();

            foreach (EntityContainer container in itemCollection.GetItems<EntityContainer>())
            {
                var entitySets = container.BaseEntitySets.OfType<EntitySet>().OrderBy(s => s.Name);

                foreach (EntitySet entitySet in container.BaseEntitySets.OfType<EntitySet>().OrderBy(s => s.Name))
                {
                    builder.AppendCreateTable(entitySet);
                    builder.AppendSql(";");
                }

                foreach (AssociationSet associationSet in container.BaseEntitySets.OfType<AssociationSet>().OrderBy(s => s.Name))
                {
                    builder.AppendCreateForeignKeys(associationSet);
                    builder.AppendSql(";");
                }
            }
            return builder.GetCommandText();
        }

        internal static string CreateObjectScript(EntitySet entitySet)
        {
            JetCreateDatabaseSqlGenerator builder = new JetCreateDatabaseSqlGenerator();
            builder.AppendCreateTable(entitySet);
            return builder.GetCommandText();
        }

        internal static string CreateObjectScript(AssociationSet associationSet)
        {
            JetCreateDatabaseSqlGenerator builder = new JetCreateDatabaseSqlGenerator();
            builder.AppendCreateForeignKeys(associationSet);
            return builder.GetCommandText();
        }

        private static string GetTableName(EntitySet entitySet)
        {
            string tableName = entitySet.MetadataProperties["Table"].Value as string;
            return tableName ?? entitySet.Name;
        }

        private void AppendCreateForeignKeys(AssociationSet associationSet)
        {
            var constraint = associationSet.ElementType.ReferentialConstraints.Single();
            var principalEnd = associationSet.AssociationSetEnds[constraint.FromRole.Name];
            var dependentEnd = associationSet.AssociationSetEnds[constraint.ToRole.Name];

            // If any of the participating entity sets was skipped, skip the association too
            if (ignoredEntitySets.Contains(principalEnd.EntitySet) || ignoredEntitySets.Contains(dependentEnd.EntitySet))
            {
                // AppendSql("-- Ignoring association set with participating entity set with defining query: ");
                AppendIdentifier(associationSet.Name);
            }
            else
            {
                AppendSql("alter table ");
                AppendIdentifier(dependentEnd.EntitySet);
                AppendSql(" add constraint ");
                AppendIdentifier(associationSet.Name);
                AppendSql(" foreign key (");
                AppendIdentifierList(constraint.ToProperties.Select(p => p.Name).ToList());
                AppendSql(") references ");
                AppendIdentifier(principalEnd.EntitySet);
                AppendSql("(");
                AppendIdentifierList(constraint.FromProperties.Select(p => p.Name).ToList());
                AppendSql(")");
                if (principalEnd.CorrespondingAssociationEndMember.DeleteBehavior == OperationAction.Cascade)
                {
                    AppendSql(" on delete cascade");
                }
            }
            AppendNewLine();
        }

        private void AppendCreateTable(EntitySet entitySet)
        {
            //If the entity set has defining query, skip it
            if (entitySet.MetadataProperties["DefiningQuery"].Value != null)
            {
                AppendIdentifier(entitySet);
                ignoredEntitySets.Add(entitySet);
            }
            else
            {
                AppendSql("create table ");
                AppendIdentifier(entitySet);
                AppendSql(" (");
                AppendNewLine();

                foreach (EdmProperty column in entitySet.ElementType.Properties)
                {
                    AppendSql("    ");
                    AppendIdentifier(column.Name);
                    AppendSql(" ");
                    AppendType(column);
                    AppendSql(",");
                    AppendNewLine();
                }


                if (entitySet.ElementType.KeyMembers.Count > 0)
                {
                    // Don't know how EF could work without keys...
                    string constraintName = CreatePkConstraintName(entitySet);

                    AppendSql("  constraint {0}  primary key (", constraintName);
                    AppendIdentifierList(entitySet.ElementType.KeyMembers.Select(c => c.Name).ToList());
                    AppendSql(")");
                    AppendNewLine();
                }

                AppendSql(")");
            }
            AppendNewLine();
        }

        public void AppendIdentifier(EntitySet table)
        {
            string tableName = GetTableName(table);
            AppendIdentifier(tableName);
        }


        public string CreatePkConstraintName(EntitySet entitySet)
        {
            return CreateConstraintName("PK", entitySet.Name);
        }

    }

}
