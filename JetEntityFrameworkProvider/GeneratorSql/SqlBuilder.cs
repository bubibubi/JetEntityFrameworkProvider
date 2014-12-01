using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Data.OleDb;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// This class is like StringBuilder.  While traversing the tree for the first time, 
    /// we do not know all the strings that need to be appended e.g. things that need to be
    /// renamed, nested select statements etc.  So, we use a builder that can collect
    /// all kinds of sql fragments.
    /// </summary>
    internal sealed class SqlBuilder : ISqlFragment
    {
        public SqlBuilder()
        { }

        public SqlBuilder(object s)
        { Append(s); }

        public SqlBuilder(params object[] s)
        { Append(s); }


        private List<object> _sqlFragments;
        
        private List<object> sqlFragments
        {
            get
            {
                if (_sqlFragments == null)
                {
                    _sqlFragments = new List<object>();
                }
                return _sqlFragments;
            }
        }


        /// <summary>
        /// Add an object to the list - we do not verify that it is a proper sql fragment
        /// since this is an internal method.
        /// </summary>
        /// <param name="s"></param>
        public void Append(object s)
        {
            Debug.Assert(s != null);
            sqlFragments.Add(s);
        }

        /// <summary>
        /// Add a set of objects to the list - we do not verify that it is a proper sql fragment
        /// since this is an internal method.
        /// </summary>
        /// <param name="s"></param>
        public void Append(params object[] s)
        {
            Debug.Assert(s != null);
            foreach (object o in s)
                sqlFragments.Add(o);
        }

        /// <summary>
        /// This is to pretty print the SQL.  The writer <see cref="SqlWriter.Write"/>
        /// needs to know about new lines so that it can add the right amount of 
        /// indentation at the beginning of lines.
        /// </summary>
        public void AppendLine()
        {
            sqlFragments.Add("\r\n");
        }

        /// <summary>
        /// Whether the builder is empty.  This is used by the <see cref="SqlGenerator.Visit(DbProjectExpression)"/>
        /// to determine whether a sql statement can be reused.
        /// </summary>
        public bool IsEmpty
        {
            get { return ((_sqlFragments == null) || (_sqlFragments.Count == 0)); }
        }

        #region ISqlFragment Implementation

        /// <summary>
        /// We delegate the writing of the fragment to the appropriate type.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="sqlGenerator"></param>
        public void WriteSql(SqlWriter writer, SqlGenerator sqlGenerator)
        {
            if (_sqlFragments != null)
            {
                foreach (object o in _sqlFragments)
                {
                    string str = (o as String);
                    if (str != null)
                        writer.Write(str);
                    else
                    {
                        ISqlFragment sqlFragment = (o as ISqlFragment);
                        if (sqlFragment != null)
                            sqlFragment.WriteSql(writer, sqlGenerator);
                        else
                            throw new InvalidOperationException();
                    }
                }
            }
        }

        #endregion
    }
}
