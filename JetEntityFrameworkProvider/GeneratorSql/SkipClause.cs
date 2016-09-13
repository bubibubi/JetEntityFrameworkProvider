using System;
using System.Globalization;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// SkipClause represents the a SKIP expression in a SqlSelectStatement. 
    /// SKIP expression is not valid in Jet so it just skips records from the DbDataReader
    /// It has a count property, which indicates how many rows should be skipped.
    /// </summary>
    class SkipClause : ISqlFragment
    {
        readonly int _skipCount;

        /// <summary>
        /// How many skip rows should be selected.
        /// </summary>
        public int SkipCount
        {
            get { return _skipCount; }
        }

        public SkipClause(int skipCount)
        {
            _skipCount = skipCount;
        }

        #region ISqlFragment Members

        /// <summary>
        /// Write out the SKIP part of sql select statement 
        /// It basically writes SKIP (X)
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            writer.Write(" SKIP ");
            writer.Write(_skipCount.ToString(CultureInfo.InvariantCulture));
            writer.Write(" ");
        }

        #endregion

        public override string ToString()
        {
            return string.Format("SKIP {0}", SkipCount);
        }
    }
}
