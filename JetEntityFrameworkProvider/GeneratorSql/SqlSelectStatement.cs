using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// A SqlSelectStatement represents a canonical SQL SELECT statement.
    /// It has fields for the 5 main clauses
    /// <list type="number">
    /// <item>SELECT</item>
    /// <item>FROM</item>
    /// <item>WHERE</item>
    /// <item>GROUP BY</item>
    /// <item>ORDER BY</item>
    /// </list>
    /// We do not have HAVING, since it does not correspond to anything in the DbCommandTree.
    /// Each of the fields is a SqlBuilder, so we can keep appending SQL strings
    /// or other fragments to build up the clause.
    ///
    /// We have a IsDistinct property to indicate that we want distict columns.
    /// This is given out of band, since the input expression to the select clause
    /// may already have some columns projected out, and we use append-only SqlBuilders.
    /// The DISTINCT is inserted when we finally write the object into a string.
    /// 
    /// Also, we have a Top property, which is non-null if the number of results should
    /// be limited to certain number. It is given out of band for the same reasons as DISTINCT.
    ///
    /// The FromExtents contains the list of inputs in use for the select statement.
    /// There is usually just one element in this - Select statements for joins may
    /// temporarily have more than one.
    ///
    /// If the select statement is created by a Join node, we maintain a list of
    /// all the extents that have been flattened in the join in AllJoinExtents
    /// <example>
    /// in J(j1= J(a,b), c)
    /// FromExtents has 2 nodes JoinSymbol(name=j1, ...) and Symbol(name=c)
    /// AllJoinExtents has 3 nodes Symbol(name=a), Symbol(name=b), Symbol(name=c)
    /// </example>
    ///
    /// If any expression in the non-FROM clause refers to an extent in a higher scope,
    /// we add that extent to the OuterExtents list.  This list denotes the list
    /// of extent aliases that may collide with the aliases used in this select statement.
    /// It is set by <see cref="SqlGenerator.Visit(DbVariableReferenceExpression)"/>.
    /// An extent is an outer extent if it is not one of the FromExtents.
    ///
    ///
    /// </summary>
    internal sealed class SqlSelectStatement : ISqlFragment
    {
        /// <summary>
        /// Do we need to add a DISTINCT at the beginning of the SELECT
        /// </summary>
        /// <value>
        /// <c>true</c> if this SELECT is SELECT DISTINCT; otherwise, <c>false</c>.
        /// </value>
        internal bool IsDistinct { get; set; }

        internal List<Symbol> AllJoinExtents { get; set; }

        private List<Symbol> _fromExtents;
        internal List<Symbol> FromExtents
        {
            get
            {
                if (_fromExtents == null)
                    _fromExtents = new List<Symbol>();

                return _fromExtents;
            }
        }

        private Dictionary<Symbol, bool> _outerExtents;
        internal Dictionary<Symbol, bool> OuterExtents
        {
            get
            {
                if (_outerExtents == null)
                    _outerExtents = new Dictionary<Symbol, bool>();

                return _outerExtents;
            }
        }

        internal TopClause Top { get; set; }
        internal SkipClause Skip { get; set; }

        private SqlBuilder _select = new SqlBuilder();
        internal SqlBuilder Select
        {
            get { return _select; }
        }

        private SqlBuilder _from = new SqlBuilder();
        internal SqlBuilder From
        {
            get { return _from; }
        }


        private SqlBuilder _where;
        internal SqlBuilder Where
        {
            get
            {
                if (_where == null)
                    _where = new SqlBuilder();

                return _where;
            }
        }

        private SqlBuilder _groupBy;
        internal SqlBuilder GroupBy
        {
            get
            {
                if (_groupBy == null)
                    _groupBy = new SqlBuilder();

                return _groupBy;
            }
        }

        private SqlBuilder _orderBy;
        public SqlBuilder OrderBy
        {
            get
            {
                if (_orderBy == null)
                    _orderBy = new SqlBuilder();

                return _orderBy;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this select is the top most.
        /// if not Order By should be omitted unless there is a corresponding TOP
        /// </summary>
        /// <value>
        /// <c>true</c> if this select is the top most; otherwise, <c>false</c>.
        /// </value>
        internal bool IsTopMost { get; set; }

        #region ISqlFragment Implementation

        /// <summary>
        /// Write out a SQL select statement as a string.
        /// We have to
        /// <list type="number">
        /// <item>Check whether the aliases extents we use in this statement have
        /// to be renamed.
        /// We first create a list of all the aliases used by the outer extents.
        /// For each of the FromExtents( or AllJoinExtents if it is non-null),
        /// rename it if it collides with the previous list.
        /// </item>
        /// <item>Write each of the clauses (if it exists) as a string</item>
        /// </list>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {

            // Create a list of the aliases used by the outer extents
            // JoinSymbols have to be treated specially.
            List<string> outerExtentAliases = null;
            if (_outerExtents != null && _outerExtents.Count > 0)
            {
                foreach (Symbol outerExtent in _outerExtents.Keys)
                {
                    JoinSymbol joinSymbol = outerExtent as JoinSymbol;
                    if (joinSymbol != null)
                    {
                        foreach (Symbol symbol in joinSymbol.FlattenedExtentList)
                        {
                            if (outerExtentAliases == null)
                                outerExtentAliases = new List<string>();
                            outerExtentAliases.Add(symbol.NewName);
                        }
                    }
                    else
                    {
                        if (outerExtentAliases == null)
                            outerExtentAliases = new List<string>();
                        outerExtentAliases.Add(outerExtent.NewName);
                    }
                }
            }

            // An then rename each of the FromExtents we have
            // If AllJoinExtents is non-null - it has precedence.
            // The new name is derived from the old name - we append an increasing int.
            List<Symbol> extentList = this.AllJoinExtents ?? this._fromExtents;
            if (extentList != null)
            {
                foreach (Symbol fromAlias in extentList)
                {
                    if ((outerExtentAliases != null) && outerExtentAliases.Contains(fromAlias.Name))
                    {
                        int i = sqlGenerator.AllExtentNames[fromAlias.Name];
                        string newName;
                        do
                        {
                            ++i;
                            newName = fromAlias.Name + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        }
                        while (sqlGenerator.AllExtentNames.ContainsKey(newName));
                        sqlGenerator.AllExtentNames[fromAlias.Name] = i;
                        fromAlias.NewName = newName;

                        // Add extent to list of known names (although i is always incrementing, "prefix11" can
                        // eventually collide with "prefix1" when it is extended)
                        sqlGenerator.AllExtentNames[newName] = 0;
                    }

                    // Add the current alias to the list, so that the extents
                    // that follow do not collide with me.
                    if (outerExtentAliases == null) { outerExtentAliases = new List<string>(); }
                    outerExtentAliases.Add(fromAlias.NewName);
                }
            }





            // Increase the indent, so that the Sql statement is nested by one tab.
            writer.Indent += 1; // ++ can be confusing in this context

            writer.Write("SELECT ");
            if (IsDistinct)
                writer.Write("DISTINCT ");

            if (this.Top != null)
            {
                if (this.Skip != null)
                    this.Top.Skip = this.Skip.SkipCount;

                this.Top.WriteSql(writer, sqlGenerator);
            }

            if (this._select == null || this.Select.IsEmpty)
            {
                Debug.Assert(false);  // we have removed all possibilities of SELECT *.
                writer.Write("*");
            }
            else
                this.Select.WriteSql(writer, sqlGenerator);

            writer.WriteLine();
            writer.Write("FROM ");
            this.From.WriteSql(writer, sqlGenerator);

            if (this._where != null && !this.Where.IsEmpty)
            {
                writer.WriteLine();
                writer.Write("WHERE ");
                this.Where.WriteSql(writer, sqlGenerator);
            }

            if (this._groupBy != null && !this.GroupBy.IsEmpty)
            {
                writer.WriteLine();
                writer.Write("GROUP BY ");
                this.GroupBy.WriteSql(writer, sqlGenerator);
            }

            if (this._orderBy != null && !this.OrderBy.IsEmpty && (this.IsTopMost || this.Top != null || this.Skip != null))
            {
                writer.WriteLine();
                writer.Write("ORDER BY ");
                this.OrderBy.WriteSql(writer, sqlGenerator);
            }

            if (this.Skip != null)
                this.Skip.WriteSql(writer, sqlGenerator);

            --writer.Indent;
        }

        #endregion
    }
}
