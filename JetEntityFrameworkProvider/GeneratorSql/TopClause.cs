using System;
using System.Globalization;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// TopClause represents the a TOP expression in a SqlSelectStatement. 
    /// It has a count property, which indicates how many TOP rows should be selected and a 
    /// boolen WithTies property.
    /// </summary>
    class TopClause : ISqlFragment
    {
        readonly int _topCount;
        readonly bool _withTies;

        /// <summary>
        /// Gets or sets the skip value (if there is a skip clause)
        /// </summary>
        /// <value>
        /// The skip.
        /// </value>
        public int? Skip { get; set; }

        /// <summary>
        /// Do we need to add a WITH_TIES to the top statement
        /// </summary>
        internal bool WithTies
        {
            get { return _withTies; }
        }

        /// <summary>
        /// How many top rows should be selected.
        /// </summary>
        internal int TopCount
        {
            get { return _topCount; }
        }

        /// <summary>
        /// Creates a TopClause with the given topCount and withTies.
        /// </summary>
        /// <param name="topCount"></param>
        /// <param name="withTies"></param>
        internal TopClause(int topCount, bool withTies)
        {
            this._topCount = topCount;
            this._withTies = withTies;
        }

        #region ISqlFragment Members

        /// <summary>
        /// Write out the TOP part of sql select statement 
        /// It basically writes TOP (X) [WITH TIES].
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            writer.Write("TOP ");
            writer.Write((_topCount + Skip.GetValueOrDefault(0)).ToString(CultureInfo.InvariantCulture));
            writer.Write(" ");

            if (this.WithTies)
                throw new NotImplementedException("WITH TIES not implemented in Jet");
        }

        #endregion
    }
}
