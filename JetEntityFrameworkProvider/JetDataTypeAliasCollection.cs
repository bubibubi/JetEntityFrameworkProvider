using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    class JetDataTypeAliasCollection : System.Collections.ObjectModel.KeyedCollection<string, JetDataTypeAlias>
    {
        static JetDataTypeAliasCollection _instance = null;

        public static JetDataTypeAliasCollection Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new JetDataTypeAliasCollection();

                return _instance;
            }
        }

        private JetDataTypeAliasCollection() : base(System.StringComparer.InvariantCultureIgnoreCase)
        {
            Add("nchar", "char");
            Add("char");

            Add("nvarchar", "varchar");
            Add("varchar");

            Add("nvarchar(max)", "text");
            Add("varchar(max)", "text");
            Add("ntext", "text");
            Add("text");

            Add("binary");
            Add("varbinary");

            Add("varbinary(max)", "image");
            Add("image");

            Add("datetime");
            Add("time", "datetime");

            Add("bit");

            Add("float");
            Add("double", "float");

            Add("single", "real");
            Add("real");
            
            Add("numeric");
            Add("decimal");
            Add("money", "decimal");

            Add("tinyint");
            Add("smallint");
            Add("integer", "int");
            Add("bigint", "int"); // JET does not support bigint at all
            Add("guid");
            Add("uniqueidentifier", "guid");
            Add("int");
                
        }

        public void Add(string name, string alias)
        {
            Add(new JetDataTypeAlias { Name = name, Alias = alias });
        }

        public void Add(string name)
        {
            Add(new JetDataTypeAlias { Name = name, Alias = name});
        }

        public bool TryGetValue(string key, out JetDataTypeAlias item)
        {
            try
            {
                item = this[key];
                return true;
            }
            catch
            {
                item = null;
                return false;
            }
        }

        protected override string GetKeyForItem(JetDataTypeAlias item)
        {
            return item.Name;
        }
    }
}
