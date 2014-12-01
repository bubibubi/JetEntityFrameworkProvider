using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace JetEntityFrameworkProvider.Utilities
{
    /// <summary>
    /// Class that gets metadata about the functions and generates the function stubs.
    /// </summary>
    public class JetProviderLinqFunctionStubsCodeGen
    {
        /// <summary>
        /// Main driver function.
        /// </summary>
        /// <param name="args"></param>
        public static void Generate(string outputFileName)
        {   
            //The following functions are omitted because they have counterparts in the BCL
            string[] omittedFunctions = new[]
            {
                "Sum", "Min", "Max", "Average", "Avg",
                "Count", "BigCount", 
                "Trim", "RTrim", "LTrim",
                "Concat", "Length", "Substring",
                "Replace", "IndexOf", "ToUpper", "ToLower",
                "Contains", "StartsWith", "EndsWith", "Year", "Month", "Day",
                "DayOfYear", "Hour", "Minute", "Second", "Millisecond", "CurrentDateTime", "CurrentDateTimeOffset",
                "CurrentUtcDateTime",
                "BitwiseAnd", "BitwiseOr", "BitwiseXor", "BitwiseNot",
                "Round", "Abs", "Power", "NewGuid",
                "Floor", "Ceiling",
            };

            //The following functions are omitted from SqlFunctions because they already exist in EntityFunctions
            string[] omittedSqlFunctions = new[]
            {
                "STDEV", "STDEVP", "VAR", "VARP", "COUNT_BIG",
                "Left", "Right", "Reverse", "GetTotalOffsetMinutes", 
                "TruncateTime", "CreateDateTime",
                "CreateDateTimeOffset", "CreateTime", "Add", "Diff",
                "Truncate", "SYSDATETIME", "SYSUTCDATETIME", "SYSDATETIMEOFFSET",
                "LEN", "LOWER", "UPPER", "NEWID", 
            };
                

            DbProviderServices dbProviderServices = (DbProviderServices)((IServiceProvider)JetProviderFactory.Instance).GetService(typeof(DbProviderServices));
            DbProviderManifest dbProviderManifest = dbProviderServices.GetProviderManifest("Jet");
            var storeFunctions = dbProviderManifest.GetStoreFunctions();

            IEnumerable<EdmFunction> sqlFunctions = storeFunctions
                .Where(f => f.NamespaceName == "Jet")
                .Where(f => !(omittedFunctions.Concat(omittedSqlFunctions)).Contains(f.Name, StringComparer.OrdinalIgnoreCase));

            FunctionStubFileWriter sqlStubsFileWriter = new FunctionStubFileWriter(sqlFunctions, GetFunctionNamingDictionary(), GetParameterNamingDictionary());
            sqlStubsFileWriter.GenerateToFile(outputFileName, "JetEntityFrameworkProvider", "JetFunctions", "Jet", true);
        }

        /// <summary>
        /// These help generate better function and argument names in following cases:
        /// 1. Generates an fxcop agreeable name
        /// 2. PasalCases two-worded identifier names.
        /// </summary>
        private static Dictionary<string, string> GetFunctionNamingDictionary()
        {
            Dictionary<string, string> sqlFunctionNames = new Dictionary<string, string>();
            sqlFunctionNames.Add("FIRST", "JetFirst");
            sqlFunctionNames.Add("LAST", "JetLast");
            sqlFunctionNames.Add("STDEV", "StDev");
            sqlFunctionNames.Add("STDEVP", "StDevP");
            sqlFunctionNames.Add("VARP", "VarP");
            sqlFunctionNames.Add("CHARINDEX", "CharIndex");
            sqlFunctionNames.Add("LTRIM", "LTrim");
            sqlFunctionNames.Add("RTRIM", "RTrim");
            sqlFunctionNames.Add("LCASE", "LCase");
            sqlFunctionNames.Add("UCASE", "UCase");
            sqlFunctionNames.Add("DATEADD", "DateAdd");
            sqlFunctionNames.Add("DATEDIFF", "DateDiff");
            sqlFunctionNames.Add("DATENAME", "DateName");
            sqlFunctionNames.Add("DATEPART", "DatePart");
            sqlFunctionNames.Add("ISNUMERIC", "IsNumeric");
            sqlFunctionNames.Add("ISDATE", "IsDate");
            sqlFunctionNames.Add("ISNULL", "IsNull");
            sqlFunctionNames.Add("SQRT", "SquareRoot");
            sqlFunctionNames.Add("STR", "StringConvert");

            return sqlFunctionNames;
        }

        private static Dictionary<string, string> GetParameterNamingDictionary()
        {
            Dictionary<string, string> sqlFunctionParameterNames = new Dictionary<string, string>();
            sqlFunctionParameterNames.Add("strSearch", "toSearch");
            sqlFunctionParameterNames.Add("strTarget", "target");
            sqlFunctionParameterNames.Add("datepart", "datePartArg");
            sqlFunctionParameterNames.Add("enddate", "endDate");
            sqlFunctionParameterNames.Add("startdate", "startDate");
            sqlFunctionParameterNames.Add("decimal", "decimalArg");
            sqlFunctionParameterNames.Add("str1", "string1");
            sqlFunctionParameterNames.Add("str2", "string2");
            sqlFunctionParameterNames.Add("str", "stringArg");
            sqlFunctionParameterNames.Add("string_expression", "stringExpression");
            sqlFunctionParameterNames.Add("x", "baseArg");
            sqlFunctionParameterNames.Add("y", "exponentArg");
            sqlFunctionParameterNames.Add("character_string", "stringArg");
            sqlFunctionParameterNames.Add("quote_character", "quoteCharacter");
            sqlFunctionParameterNames.Add("numeric_expression", "numericExpression");
            sqlFunctionParameterNames.Add("strInput", "stringInput");
            sqlFunctionParameterNames.Add("strReplacement", "stringReplacement");
            sqlFunctionParameterNames.Add("strPattern", "stringPattern");
            sqlFunctionParameterNames.Add("datetimeoffset", "dateTimeOffsetArg");

            return sqlFunctionParameterNames;
        }

        public static int Start(string[] args)
        {

            Generate("c:\\Test.txt");

            return 0;
        }
    }
}
