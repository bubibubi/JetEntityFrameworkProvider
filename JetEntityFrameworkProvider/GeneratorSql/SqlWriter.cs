using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Data.OleDb;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// This extends StringWriter primarily to add the ability to add an indent
    /// to each line that is written out.
    /// </summary>
    class SqlWriter : StringWriter
    {

        bool _atBeginningOfLine = true;


        public SqlWriter(StringBuilder b)
            : base(b, System.Globalization.CultureInfo.InvariantCulture)
        {
            // We start at -1, since the first select statement will increment it to 0.
            Indent = -1;
        }

        /// <summary>
        /// The number of tabs to be added at the beginning of each new line.
        /// </summary>
        internal int Indent{get; set;}


        /// <summary>
        /// Reset atBeginningofLine if we detect the newline string.
        /// <see cref="SqlBuilder.AppendLine"/>
        /// Add as many tabs as the value of indent if we are at the 
        /// beginning of a line.
        /// </summary>
        /// <param name="value"></param>
        public override void Write(string value)
        {
            if (value == "\r\n")
            {
                base.WriteLine();
                _atBeginningOfLine = true;
            }
            else
            {
                if (_atBeginningOfLine && JetConnection.IndentSqlStatements)
                {
                    if (Indent > 0)
                        base.Write(new string('\t', Indent));
                    _atBeginningOfLine = false;
                }
                base.Write(value);
            }
        }

        /// <summary>
        /// Writes a line terminator to the text stream.
        /// </summary>
        public override void WriteLine()
        {
            base.WriteLine();
            _atBeginningOfLine = true;
        }

    }
}
