using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    class JetDataReader : DbDataReader
    {

        public JetDataReader(DbDataReader dataReader)
        {
            _wrappedDataReader = dataReader;
        }

        private DbDataReader _wrappedDataReader;

        public override void Close()
        {
            _wrappedDataReader.Close();
        }

        public override int Depth
        {
            get { return _wrappedDataReader.Depth; }
        }

        public override int FieldCount
        {
            get { return _wrappedDataReader.FieldCount; }
        }

        public override bool GetBoolean(int ordinal)
        {
            object booleanObject = GetValue(ordinal);
            if (booleanObject == null)
                throw new InvalidOperationException("Cannot cast null to boolean");
            if (booleanObject.GetType() == typeof(bool))
                return _wrappedDataReader.GetBoolean(ordinal);
            else if (booleanObject.GetType() == typeof(short))
                return ((short)booleanObject) != 0;
            else
                throw new InvalidOperationException(string.Format("Cannot convert {0} to boolean", booleanObject.GetType()));
        }

        public override byte GetByte(int ordinal)
        {
            return _wrappedDataReader.GetByte(ordinal);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _wrappedDataReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override char GetChar(int ordinal)
        {
            return _wrappedDataReader.GetChar(ordinal);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _wrappedDataReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return _wrappedDataReader.GetDataTypeName(ordinal);
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return _wrappedDataReader.GetDateTime(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return _wrappedDataReader.GetDecimal(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return _wrappedDataReader.GetDouble(ordinal);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return _wrappedDataReader.GetEnumerator();
        }

        public override Type GetFieldType(int ordinal)
        {
            return _wrappedDataReader.GetFieldType(ordinal);
        }

        public override float GetFloat(int ordinal)
        {
            return _wrappedDataReader.GetFloat(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return _wrappedDataReader.GetGuid(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return _wrappedDataReader.GetInt16(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return _wrappedDataReader.GetInt32(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return _wrappedDataReader.GetInt64(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _wrappedDataReader.GetName(ordinal);
        }

        public override int GetOrdinal(string name)
        {
            return _wrappedDataReader.GetOrdinal(name);
        }

        public override System.Data.DataTable GetSchemaTable()
        {
            return _wrappedDataReader.GetSchemaTable();
        }

        public override string GetString(int ordinal)
        {
            return _wrappedDataReader.GetString(ordinal);
        }

        public override object GetValue(int ordinal)
        {
            return _wrappedDataReader.GetValue(ordinal);
        }

        public override int GetValues(object[] values)
        {
            return _wrappedDataReader.GetValues(values);
        }

        public override bool HasRows
        {
            get { return _wrappedDataReader.HasRows; }
        }

        public override bool IsClosed
        {
            get { return _wrappedDataReader.IsClosed; }
        }

        public override bool IsDBNull(int ordinal)
        {
            return _wrappedDataReader.IsDBNull(ordinal);
        }

        public override bool NextResult()
        {
            return _wrappedDataReader.NextResult();
        }

        public override bool Read()
        {
            return _wrappedDataReader.Read();
        }

        public override int RecordsAffected
        {
            get { return _wrappedDataReader.RecordsAffected; }
        }

        public override object this[string name]
        {
            get { return _wrappedDataReader[name]; }
        }

        public override object this[int ordinal]
        {
            get { return _wrappedDataReader[ordinal]; }
        }
    }
}
