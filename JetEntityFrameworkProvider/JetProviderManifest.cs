using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// The Provider Manifest for Jet
    /// Here are implemented also database specific static methods
    /// </summary>
    class JetProviderManifest : DbXmlEnabledProviderManifest
    {

        // The Jet has only one manifest independent from the token
        public static readonly JetProviderManifest Instance = new JetProviderManifest("Jet");

        private string _token = "Jet";

        public const int VARCHAR_MAXSIZE = 255;
        public const int BINARY_MAXSIZE = 510;

        private System.Collections.ObjectModel.ReadOnlyCollection<PrimitiveType> _primitiveTypes = null;
        private System.Collections.ObjectModel.ReadOnlyCollection<EdmFunction> _functions = null;

        public const int MaxObjectNameLength = 64;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="manifestToken">A token used to infer the capabilities of the store</param>
        public JetProviderManifest(string manifestToken) : base(JetProviderManifest.GetProviderManifest())
        {                        
            // GetStoreVersion will throw ArgumentException if manifestToken is null, empty, or not recognized.
            _token = manifestToken;
        }

        private static XmlReader GetProviderManifest()
        {
            return GetXmlResource("JetEntityFrameworkProvider.Resources.JetProviderServices.ProviderManifest.xml");
        }
        
        private XmlReader GetStoreSchemaMapping()
        {
            return GetXmlResource("JetEntityFrameworkProvider.Resources.JetProviderServices.StoreSchemaMapping.msl");
        }

        private XmlReader GetStoreSchemaDescription()
        {
            return GetXmlResource("JetEntityFrameworkProvider.Resources.JetProviderServices.StoreSchemaDefinition.ssdl");
        }
        private static XmlReader GetXmlResource(string resourceName)
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Stream stream = executingAssembly.GetManifestResourceStream(resourceName);
            return XmlReader.Create(stream);
        }

        /// <summary>
        /// Function to detect wildcard characters (ANSI %, _, [ and ^ and Jet * ? [ #) and escape them
        /// This escaping is used when StartsWith, EndsWith and Contains canonical and CLR functions
        /// are translated to their equivalent LIKE expression
        /// </summary>
        /// <param name="text">Original input as specified by the user</param>
        /// <param name="alwaysEscapeEscapeChar">escape the escape character ~ regardless whether wildcard 
        /// characters were encountered </param>
        /// <param name="usedEscapeChar">true if the escaping was performed, false if no escaping was required</param>
        /// <returns>The escaped string that can be used as pattern in a LIKE expression</returns>
        internal static string EscapeLikeText(string text, out bool usedEscapeChar)
        {
            usedEscapeChar = false;
            if (
                !(text.Contains("*") || text.Contains("?") || text.Contains("[") || text.Contains("#") ) &&
                !(text.Contains("%") || text.Contains("_") || text.Contains("[") || text.Contains("^") )
                )
                return text;

            
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (
                    c == '*' || c == '?' || c == '[' || c == '#' ||
                    c == '%' || c == '_' || c == '[' || c == '^'
                    )
                {
                    sb.AppendFormat("[{0}]", c);
                    usedEscapeChar = true;
                }
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }


        /// <summary>
        /// When overridden in a derived class, this method returns provider-specific information.
        /// </summary>
        /// <param name="informationType">The name of the information to be retrieved.</param>
        /// <returns>
        /// An XmlReader at the begining of the information requested.
        /// </returns>
        /// <exception cref="System.ArgumentException">informationType</exception>
        protected override XmlReader GetDbInformation(string informationType)
        {
            if (informationType == DbProviderManifest.StoreSchemaDefinitionVersion3)
                return GetStoreSchemaDescription();

            if (informationType == DbProviderManifest.StoreSchemaMappingVersion3)
                return GetStoreSchemaMapping();

            throw new ArgumentException(String.Format("Unknown db information '{0}'.", informationType), "informationType");
        }

        /// <summary>
        /// Returns the list of primitive types supported by the storage provider.
        /// </summary>
        /// <returns>
        /// A collection of type <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> that contains the list of primitive types supported by the storage provider.
        /// </returns>
        public override System.Collections.ObjectModel.ReadOnlyCollection<PrimitiveType> GetStoreTypes()
        {
            if (this._primitiveTypes == null)
                this._primitiveTypes = base.GetStoreTypes();

            return this._primitiveTypes;
        }

        /// <summary>
        /// Returns the list of provider-supported functions.
        /// </summary>
        /// <returns>
        /// A collection of type <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> that contains the list of provider-supported functions.
        /// </returns>
        public override System.Collections.ObjectModel.ReadOnlyCollection<EdmFunction> GetStoreFunctions()
        {
            if (this._functions == null)
                this._functions = base.GetStoreFunctions();

            return this._functions;
        }

        /// <summary>
        /// This method takes a type and a set of facets and returns the best mapped equivalent type 
        /// in EDM.
        /// </summary>
        /// <param name="storeType">A TypeUsage encapsulating a store type and a set of facets</param>
        /// <returns>A TypeUsage encapsulating an EDM type and a set of facets</returns>
        public override TypeUsage GetEdmType(TypeUsage storeType)
        {
            if (storeType == null)
                throw new ArgumentNullException("storeType");

            string storeTypeName = storeType.EdmType.Name.ToLowerInvariant();
            if (!base.StoreTypeNameToEdmPrimitiveType.ContainsKey(storeTypeName))
                throw new ArgumentException(String.Format("The underlying provider does not support the type '{0}'.", storeTypeName));

            PrimitiveType edmPrimitiveType = base.StoreTypeNameToEdmPrimitiveType[storeTypeName];

            int maxLength = 0;
            bool isUnicode = true;
            bool isFixedLen = false;
            bool isUnbounded = true;

            PrimitiveTypeKind newPrimitiveTypeKind;

            switch (storeTypeName)
            {
                // for some types we just go with simple type usage with no facets
                case "tinyint":
                case "smallint":
                case "bigint":
                case "bit":
                case "uniqueidentifier":
                case "int":
                case "guid":
                    return TypeUsage.CreateDefaultTypeUsage(edmPrimitiveType);
                
                case "nvarchar":
                case "varchar":
                    newPrimitiveTypeKind = PrimitiveTypeKind.String;
                    isUnbounded = !storeType.TryGetMaxLength(out maxLength);
                    isFixedLen = false;
                    break;

                case "nchar":
                case "char":
                    newPrimitiveTypeKind = PrimitiveTypeKind.String;
                    isUnbounded = !storeType.TryGetMaxLength(out maxLength);
                    isFixedLen = true;
                    break;

                case "nvarchar(max)":
                case "varchar(max)":
                case "ntext":
                case "text":
                    newPrimitiveTypeKind = PrimitiveTypeKind.String;
                    isUnbounded = true;
                    isFixedLen = false;
                    break;

                case "binary":
                    newPrimitiveTypeKind = PrimitiveTypeKind.Binary;
                    isUnbounded = !storeType.TryGetMaxLength(out maxLength);
                    isFixedLen = true;
                    break;

                case "varbinary":
                    newPrimitiveTypeKind = PrimitiveTypeKind.Binary;
                    isUnbounded = !storeType.TryGetMaxLength(out maxLength);
                    isFixedLen = false;
                    break;

                case "varbinary(max)":
                case "image":
                    newPrimitiveTypeKind = PrimitiveTypeKind.Binary;
                    isUnbounded = true;
                    isFixedLen = false;
                    break;

                case "float":
                case "real":
                    return TypeUsage.CreateDefaultTypeUsage(edmPrimitiveType);

                case "decimal":
                case "numeric":
                    {
                        byte precision;
                        byte scale;
                        if (storeType.TryGetPrecision(out precision) && storeType.TryGetScale(out scale))
                            return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType, precision, scale);
                        else
                            return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType);
                    }

                case "money":
                    return TypeUsage.CreateDecimalTypeUsage(edmPrimitiveType, 19, 4);

                case "datetime":
                    return TypeUsage.CreateDateTimeTypeUsage(edmPrimitiveType, null);

                case "time":
                    return TypeUsage.CreateTimeTypeUsage(edmPrimitiveType, null);
                
                default:
                    throw new NotSupportedException(String.Format("Jet does not support the type '{0}'.", storeTypeName));
            }

            Debug.Assert(newPrimitiveTypeKind == PrimitiveTypeKind.String || newPrimitiveTypeKind == PrimitiveTypeKind.Binary, "at this point only string and binary types should be present");

            switch (newPrimitiveTypeKind)
            {
                case PrimitiveTypeKind.String:
                    if (!isUnbounded)
                    {
                        return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen, maxLength);
                    }
                    else
                    {
                        return TypeUsage.CreateStringTypeUsage(edmPrimitiveType, isUnicode, isFixedLen);
                    }
                case PrimitiveTypeKind.Binary:
                    if (!isUnbounded)
                    {
                        return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen, maxLength);
                    }
                    else
                    {
                        return TypeUsage.CreateBinaryTypeUsage(edmPrimitiveType, isFixedLen);
                    }
                default:
                    throw new NotSupportedException(String.Format("Jet does not support the type '{0}'.", storeTypeName));
            }
        }

        /// <summary>
        /// This method takes a type and a set of facets and returns the best mapped equivalent type 
        /// in Jet
        /// </summary>
        /// <param name="storeType">A TypeUsage encapsulating an EDM type and a set of facets</param>
        /// <returns>A TypeUsage encapsulating a store type and a set of facets</returns>
        public override TypeUsage GetStoreType(TypeUsage edmType)
        {
            if(edmType == null)
                throw new ArgumentNullException("edmType");

            System.Diagnostics.Debug.Assert(edmType.EdmType.BuiltInTypeKind == BuiltInTypeKind.PrimitiveType);

            PrimitiveType primitiveType = edmType.EdmType as PrimitiveType;
            if (primitiveType == null)
                throw new ArgumentException(String.Format("The underlying provider does not support the type '{0}'.", edmType));

            ReadOnlyMetadataCollection<Facet> facets = edmType.Facets;

            switch (primitiveType.PrimitiveTypeKind)
            {
                case PrimitiveTypeKind.Boolean:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["bit"]);

                case PrimitiveTypeKind.Byte:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["tinyint"]);

                case PrimitiveTypeKind.Int16:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["smallint"]);

                case PrimitiveTypeKind.Int32:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int"]);

                case PrimitiveTypeKind.Int64:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["int"]);

                case PrimitiveTypeKind.Guid:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["guid"]);

                case PrimitiveTypeKind.Double:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["float"]);

                case PrimitiveTypeKind.Single:
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["real"]);

                case PrimitiveTypeKind.Decimal: // decimal, numeric, smallmoney, money
                    {
                        byte precision;
                        if (!edmType.TryGetPrecision(out precision))
                            precision = 18;

                        byte scale;
                        if (!edmType.TryGetScale(out scale))
                            scale = 0;

                        return TypeUsage.CreateDecimalTypeUsage(StoreTypeNameToStorePrimitiveType["decimal"], precision, scale);
                    }

                case PrimitiveTypeKind.Binary: // binary, varbinary, image
                    {
                        bool isFixedLength = edmType.GetIsFixedLength();
                        bool isMaxLength = edmType.GetMaxLength() > BINARY_MAXSIZE;
                        int maxLength = edmType.GetMaxLength();

                        TypeUsage tu;
                        if (isFixedLength)
                            tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["binary"], true, maxLength);
                        else if (isMaxLength)
                        {
                            tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["image"], false);
                            System.Diagnostics.Debug.Assert(tu.Facets["MaxLength"].Description.IsConstant, "varbinary(max) is not constant!");
                        }
                        else
                            tu = TypeUsage.CreateBinaryTypeUsage(StoreTypeNameToStorePrimitiveType["varbinary"], false, maxLength);

                        return tu;
                    }

                case PrimitiveTypeKind.String:
                    // char, varchar, text
                    {
                        bool isUnicode = edmType.GetIsUnicode(); // We do not handle unicode (everything's unicode in Jet)
                        bool isFixedLength = edmType.GetIsFixedLength();
                        bool isMaxLength = edmType.GetMaxLength() > VARCHAR_MAXSIZE;
                        int maxLength = edmType.GetMaxLength();

                        TypeUsage tu;

                        if (isFixedLength)
                            tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["char"], false, true, maxLength);
                        else if (isMaxLength)
                            tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["text"], false, false);
                        else
                            tu = TypeUsage.CreateStringTypeUsage(StoreTypeNameToStorePrimitiveType["varchar"], false, false, maxLength);

                        return tu;
                    }

                case PrimitiveTypeKind.DateTime: // datetime
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["datetime"]);
                case PrimitiveTypeKind.Time: // time
                    return TypeUsage.CreateDefaultTypeUsage(StoreTypeNameToStorePrimitiveType["time"]);
                default:
                    throw new NotSupportedException(String.Format("There is no store type corresponding to the EDM type '{0}' of primitive type '{1}'.", edmType, primitiveType.PrimitiveTypeKind));
            }
        }


        /// <summary>
        /// Returns true, Jet supports escaping strings to be used as arguments to like
        /// The character to escape must be enclosed in square brackets
        /// </summary>
        /// <param name="escapeCharacter">The character '['</param>
        /// <returns>True</returns>
        public override bool SupportsEscapingLikeArgument(out char escapeCharacter)
        {
            escapeCharacter = '[';
            return true;
        }

        /// <summary>
        /// Escapes the wildcard characters and the escape character in the given argument.
        /// </summary>
        /// <param name="argument"></param>
        /// <returns>Equivalent to the argument, with the wildcard characters and the escape character escaped</returns>
        public override string EscapeLikeArgument(string argument)
        {
            bool usedEscapeCharacter;
            return EscapeLikeText(argument, out usedEscapeCharacter);
        }

        /// <summary>
        /// Returns a boolean that specifies whether the provider can handle expression trees
        /// containing instances of DbInExpression.
        /// The default implementation returns <c>false</c> for backwards compatibility. Derived classes can override this method.
        /// </summary>
        /// <returns>
        /// <c>false</c>
        /// </returns>
        public override bool SupportsInExpression()
        {
            return true;
        }


        /// <summary>
        /// Quotes an identifier
        /// </summary>
        /// <param name="name">Identifier name</param>
        /// <returns>The quoted identifier</returns>
        internal static string QuoteIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            return "[" + name.Replace("]", "]]") + "]";
        }

    }
}