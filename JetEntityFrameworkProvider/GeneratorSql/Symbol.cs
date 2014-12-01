using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Data.OleDb;
using System.Data.Entity.Core.Metadata.Edm;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// <see cref="SymbolTable"/>
    /// This class represents an extent/nested select statement,
    /// or a column.
    ///
    /// The important fields are Name, Type and NewName.
    /// NewName starts off the same as Name, and is then modified as necessary.
    ///
    ///
    /// The rest are used by special symbols.
    /// e.g. NeedsRenaming is used by columns to indicate that a new name must
    /// be picked for the column in the second phase of translation.
    ///
    /// IsUnnest is used by symbols for a collection expression used as a from clause.
    /// This allows <see cref="SqlGenerator.AddFromSymbol(SqlSelectStatement, string, Symbol, bool)"/> to add the column list
    /// after the alias.
    ///
    /// </summary>
    class Symbol : ISqlFragment
    {

        public Symbol(string name, TypeUsage type)
        {
            this.Name = name;
            this.NewName = name;
            this.Type = type;
            Columns = new Dictionary<string, Symbol>(StringComparer.CurrentCultureIgnoreCase);
        }

        internal Dictionary<string, Symbol> Columns {get; private set;}

        /// <summary>
        /// Gets or sets a value indicating whether a column should be ranamed in the second phase of translation.
        /// </summary>
        /// <value>
        ///   <c>true</c> if needs renaming; otherwise, <c>false</c>.
        /// </value>
        public bool NeedsRenaming {get; set;}

        public string Name { get; private set; }

        public string NewName {get; set;}

        public TypeUsage Type {get; set;}


        #region ISqlFragment Members

        /// <summary>
        /// Write this symbol out as a string for sql.  This is just
        /// the new name of the symbol (which could be the same as the old name).
        ///
        /// We rename columns here if necessary.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            if (this.NeedsRenaming)
            {
                string newName;
                int i = sqlGenerator.AllColumnNames[this.NewName];
                do
                {
                    ++i;
                    newName = this.Name + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                } while (sqlGenerator.AllColumnNames.ContainsKey(newName));
                sqlGenerator.AllColumnNames[this.NewName] = i;

                // Prevent it from being renamed repeatedly.
                this.NeedsRenaming = false;
                this.NewName = newName;

                // Add this column name to list of known names so that there are no subsequent
                // collisions
                sqlGenerator.AllColumnNames[newName] = 0;
            }
            writer.Write(JetProviderManifest.QuoteIdentifier(this.NewName));
        }

        #endregion
    }
}
