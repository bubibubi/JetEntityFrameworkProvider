using System;
using System.Globalization;
using System.Text;

namespace JetEntityFrameworkProvider
{
    static class LiteralHelpers
    {
        static private readonly char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        public static string ToSqlString(int value)
        {
            return value.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static string ToSqlString(byte[] value)
        {
            return " 0x" + ByteArrayToBinaryString((Byte[])value) + " ";
        }

        public static string ToSqlString(bool value)
        {
            return value ? "true" : "false";
        }

        static string ByteArrayToBinaryString(Byte[] binaryArray)
        {
            StringBuilder sb = new StringBuilder(binaryArray.Length * 2);
            for (int i = 0; i < binaryArray.Length; i++)
            {
                sb.Append(hexDigits[(binaryArray[i] & 0xF0) >> 4]).Append(hexDigits[binaryArray[i] & 0x0F]);
            }
            return sb.ToString();
        }

        public static string ToSqlString(string value)
        {
            // In Jet everything's unicode
            return "'" + value.Replace("'", "''") + "'";
        }

        public static string ToSqlString(Guid value)
        {
            // In Jet everything's unicode
            return "'P" + value.ToString() + "'";
        }

        /// <summary>
        /// Transform the given <see cref="System.DateTime"/> value in a string formatted in a valid format, 
        /// that represents a date and a time
        /// </summary>
        /// <param name="time">The <see cref="System.DateTime"/> value to format</param>
        /// <returns>The string that represents the today time, to include in a where clause</returns>
        public static string SqlDateTime(System.DateTime time)
        {
            return string.Format("#{0:MM/dd/yyyy} {0:HH.mm.ss}#", time);
        }

        /// <summary>
        /// Transform the given <see cref="System.TimeSpan"/> value in a string formatted in a valid Jet format, 
        /// that represents the today time
        /// </summary>
        /// <param name="time">The <see cref="System.TimeSpan"/> value to format</param>
        /// <returns>The string that represents the today time, to include in a where clause</returns>
        public static string SqlDayTime(System.TimeSpan time)
        {
            return SqlDateTime(JetConnection.TimeSpanOffset + time);
        }


    }
}
