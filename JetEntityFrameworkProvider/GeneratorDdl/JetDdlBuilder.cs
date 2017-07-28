using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Text;

namespace JetEntityFrameworkProvider
{
    class JetDdlBuilder
    {
        private readonly StringBuilder stringBuilder = new StringBuilder();

        public string GetCommandText()
        {
            return stringBuilder.ToString();
        }


        public void AppendStringLiteral(string literalValue)
        {
            AppendSql("'" + literalValue.Replace("'", "''") + "'");
        }


        public void AppendIdentifier(string identifier)
        {
            string correctIdentifier;

            if (identifier.ToLower().StartsWith("dbo.", StringComparison.CurrentCultureIgnoreCase)) 
                correctIdentifier = identifier.Substring(4);
            else if (identifier.ToLower().StartsWith("Jet.", StringComparison.CurrentCultureIgnoreCase))
                correctIdentifier = identifier.Substring(4);
            else
                correctIdentifier = identifier;


            if (correctIdentifier.Length > JetProviderManifest.MaxObjectNameLength)
            {
                string guid = Guid.NewGuid().ToString().Replace("-", "");
                correctIdentifier = correctIdentifier.Substring(0, JetProviderManifest.MaxObjectNameLength - guid.Length) + guid;
            }


            AppendSql(JetProviderManifest.QuoteIdentifier(correctIdentifier));
        }


        public void AppendIdentifierList(IEnumerable<string> identifiers)
        {
            bool first = true;
            foreach (var identifier in identifiers)
            {
                if (first)
                    first = false;
                else
                    AppendSql(", ");
                AppendIdentifier(identifier);
            }
        }

        public void AppendType(EdmProperty column)
        {
            AppendType(column.TypeUsage, column.Nullable, column.TypeUsage.GetIsIdentity(), Convert.ToString(column.DefaultValue));
        }

        public void AppendType(TypeUsage typeUsage, bool isNullable, bool isIdentity, string defaultValueSql)
        {
            bool isTimestamp = false;

            JetDataTypeAlias alias;
            JetDataTypeAliasCollection.Instance.TryGetValue(typeUsage.EdmType.Name, out alias);

            if (alias == null)
                throw new NotSupportedException(string.Format("Type {0} unsupported", typeUsage.EdmType.Name));

            string jetTypeName = alias.Alias;
            string jetLength = "";


            switch (jetTypeName)
            {
                case "decimal":
                case "numeric":
                    jetLength = string.Format(System.Globalization.CultureInfo.InvariantCulture, "({0}, {1})", typeUsage.GetPrecision(), typeUsage.GetScale());
                    break;
                case "binary":
                case "varbinary":
                case "varchar":
                case "char":
                    jetLength = string.Format("({0})", typeUsage.GetMaxLength());
                    break;
                default:
                    break;
            }

            AppendSql(jetTypeName);
            AppendSql(jetLength);
            if (!string.IsNullOrWhiteSpace(defaultValueSql))
            {
                AppendSql(" default " + defaultValueSql);
                if (!isNullable)
                    AppendSql(" not null");
            }
            else
                AppendSql(isNullable ? " null" : " not null");

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (isTimestamp)
                ;// nothing to generate for identity

            else if (isIdentity && jetTypeName == "guid")
                AppendSql(" default GenGUID()");
            //AppendSql(" counter");
            //AppendSql(" autoincrement");
            else if (isIdentity)
                AppendSql(" identity(1,1)");
        }

        /// <summary>
        /// Appends raw SQL into the string builder.
        /// </summary>
        /// <param name="text">Raw SQL string to append into the string builder.</param>
        public void AppendSql(string text)
        {
            stringBuilder.Append(text);
        }

        /// <summary>
        /// Appends raw SQL into the string builder.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="p">The parameters.</param>
        public void AppendSql(string format, params object[] p)
        {
            stringBuilder.AppendFormat(format, p);
        }


        /// <summary>
        /// Appends new line for visual formatting or for ending a comment.
        /// </summary>
        public void AppendNewLine()
        {
            stringBuilder.Append("\r\n");
        }


        public string CreateConstraintName(string constraint, string objectName)
        {
            string correctIdentifier;

            if (objectName.ToLower().StartsWith("dbo.", StringComparison.CurrentCultureIgnoreCase))
                correctIdentifier = objectName.Substring(4);
            else if (objectName.ToLower().StartsWith("Jet.", StringComparison.CurrentCultureIgnoreCase))
                correctIdentifier = objectName.Substring(4);
            else
                correctIdentifier = objectName;

            string name = string.Format("{0}_{1}", constraint, correctIdentifier);

            if (JetConnection.AppendRandomNumberForForeignKeyNames)
            {
                if (name.Length + 9 > JetProviderManifest.MaxObjectNameLength)
                    name = name.Substring(0, JetProviderManifest.MaxObjectNameLength - 9);

                name += "_" + GetRandomString();
            }

            return name;
        }

        // Returns an eigth nibbles string
        protected string GetRandomString()
        {
            Random random = new Random();
            string randomValue = "";
            for (int n = 0; n < 8; n++)
            {
                byte b = (byte)random.Next(15);
                randomValue += string.Format("{0:x1}", b);
            }

            return randomValue;
        }


    }
}
