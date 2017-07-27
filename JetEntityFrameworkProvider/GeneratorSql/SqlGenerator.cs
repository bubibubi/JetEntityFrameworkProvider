using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace JetEntityFrameworkProvider
{
    // From original example and from http://msdn.microsoft.com/cs-cz/library/ee789837(v=vs.110).aspx



    /// <summary>
    /// Translates the command object into a SQL string targeting Jet.
    /// In origin the target was SQL Server 2005 or SQL Server 2008.
    /// </summary>
    /// <remarks>
    /// The translation is implemented as a visitor <see cref="DbExpressionVisitor{T}"/>
    /// over the query tree.  It makes a single pass over the tree, collecting the sql
    /// fragments for the various nodes in the tree <see cref="ISqlFragment"/>.
    ///
    /// The major operations are
    /// <list type="bullet">
    /// <item>Select statement minimization.  Multiple nodes in the query tree
    /// that can be part of a single SQL select statement are merged. e.g. a
    /// Filter node that is the input of a Project node can typically share the
    /// same SQL statement.</item>
    /// <item>Alpha-renaming.  As a result of the statement minimization above, there
    /// could be name collisions when using correlated subqueries
    /// <example>
    /// <code>
    /// Filter(
    ///     b = Project( c.x
    ///         c = Extent(foo)
    ///         )
    ///     exists (
    ///         Filter(
    ///             c = Extent(foo)
    ///             b.x = c.x
    ///             )
    ///     )
    /// )
    /// </code>
    /// The first Filter, Project and Extent will share the same SQL select statement.
    /// The alias for the Project i.e. b, will be replaced with c.
    /// If the alias c for the Filter within the exists clause is not renamed,
    /// we will get <c>c.x = c.x</c>, which is incorrect.
    /// Instead, the alias c within the second filter should be renamed to c1, to give
    /// <c>c.x = c1.x</c> i.e. b is renamed to c, and c is renamed to c1.
    /// </example>
    /// </item>
    /// <item>Join flattening.  In the query tree, a list of join nodes is typically
    /// represented as a tree of Join nodes, each with 2 children. e.g.
    /// <example>
    /// <code>
    /// a = Join(InnerJoin
    ///     b = Join(CrossJoin
    ///         c = Extent(foo)
    ///         d = Extent(foo)
    ///         )
    ///     e = Extent(foo)
    ///     on b.c.x = e.x
    ///     )
    /// </code>
    /// If translated directly, this will be translated to
    /// <code>
    /// FROM ( SELECT c.*, d.*
    ///         FROM foo as c
    ///         CROSS JOIN foo as d) as b
    /// INNER JOIN foo as e on b.x' = e.x
    /// </code>
    /// It would be better to translate this as
    /// <code>
    /// FROM foo as c
    /// CROSS JOIN foo as d
    /// INNER JOIN foo as e on c.x = e.x
    /// </code>
    /// This allows the optimizer to choose an appropriate join ordering for evaluation.
    /// </example>
    /// </item>
    /// <item>Select * and column renaming.  In the example above, we noticed that
    /// in some cases we add <c>SELECT * FROM ...</c> to complete the SQL
    /// statement. i.e. there is no explicit PROJECT list.
    /// In this case, we enumerate all the columns available in the FROM clause
    /// This is particularly problematic in the case of Join trees, since the columns
    /// from the extents joined might have the same name - this is illegal.  To solve
    /// this problem, we will have to rename columns if they are part of a SELECT *
    /// for a JOIN node - we do not need renaming in any other situation.
    /// <see cref="SqlGenerator.AddDefaultColumns"/>.
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Renaming issues.
    /// When rows or columns are renamed, we produce names that are unique globally
    /// with respect to the query.  The names are derived from the original names,
    /// with an integer as a suffix. e.g. CustomerId will be renamed to CustomerId1,
    /// CustomerId2 etc.
    ///
    /// Since the names generated are globally unique, they will not conflict when the
    /// columns of a JOIN SELECT statement are joined with another JOIN. 
    ///
    /// </para>
    ///
    /// <para>
    /// Record flattening.
    /// SQL server does not have the concept of records.  However, a join statement
    /// produces records.  We have to flatten the record accesses into a simple
    /// <c>alias.column</c> form.  <see cref="SqlGenerator.Visit(DbPropertyExpression)"/>
    /// </para>
    ///
    /// <para>
    /// Building the SQL.
    /// There are 2 phases
    /// <list type="numbered">
    /// <item>Traverse the tree, producing a sql builder <see cref="SqlBuilder"/></item>
    /// <item>Write the SqlBuilder into a string, renaming the aliases and columns
    /// as needed.</item>
    /// </list>
    ///
    /// In the first phase, we traverse the tree.  We cannot generate the SQL string
    /// right away, since
    /// <list type="bullet">
    /// <item>The WHERE clause has to be visited before the from clause.</item>
    /// <item>extent aliases and column aliases need to be renamed.  To minimize
    /// renaming collisions, all the names used must be known, before any renaming
    /// choice is made.</item>
    /// </list>
    /// To defer the renaming choices, we use symbols <see cref="Symbol"/>.  These
    /// are renamed in the second phase.
    ///
    /// Since visitor methods cannot transfer information to child nodes through
    /// parameters, we use some global stacks,
    /// <list type="bullet">
    /// <item>A stack for the current SQL select statement.  This is needed by
    /// <see cref="SqlGenerator.Visit(DbVariableReferenceExpression)"/> to create a
    /// list of free variables used by a select statement.  This is needed for
    /// alias renaming.
    /// </item>
    /// <item>A stack for the join context.  When visiting a <see cref="DbScanExpression"/>,
    /// we need to know whether we are inside a join or not.  If we are inside
    /// a join, we do not create a new SELECT statement.</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// Global state.
    /// To enable renaming, we maintain
    /// <list type="bullet">
    /// <item>The set of all extent aliases used.</item>
    /// <item>The set of all column aliases used.</item>
    /// </list>
    ///
    /// Finally, we have a symbol table to lookup variable references.  All references
    /// to the same extent have the same symbol.
    /// </para>
    ///
    /// <para>
    /// Sql select statement sharing.
    ///
    /// Each of the relational operator nodes
    /// <list type="bullet">
    /// <item>Project</item>
    /// <item>Filter</item>
    /// <item>GroupBy</item>
    /// <item>Sort/OrderBy</item>
    /// </list>
    /// can add its non-input (e.g. project, predicate, sort order etc.) to
    /// the SQL statement for the input, or create a new SQL statement.
    /// If it chooses to reuse the input's SQL statement, we play the following
    /// symbol table trick to accomplish renaming.  The symbol table entry for
    /// the alias of the current node points to the symbol for the input in
    /// the input's SQL statement.
    /// <example>
    /// <code>
    /// Project(b.x
    ///     b = Filter(
    ///         c = Extent(foo)
    ///         c.x = 5)
    ///     )
    /// </code>
    /// The Extent node creates a new SqlSelectStatement.  This is added to the
    /// symbol table by the Filter as {c, Symbol(c)}.  Thus, <c>c.x</c> is resolved to
    /// <c>Symbol(c).x</c>.
    /// Looking at the project node, we add {b, Symbol(c)} to the symbol table if the
    /// SQL statement is reused, and {b, Symbol(b)}, if there is no reuse.
    ///
    /// Thus, <c>b.x</c> is resolved to <c>Symbol(c).x</c> if there is reuse, and to
    /// <c>Symbol(b).x</c> if there is no reuse.
    /// </example>
    /// </para>
    /// </remarks>
    internal sealed class SqlGenerator : DbExpressionVisitor<ISqlFragment>
    {
        #region Visitor parameter stacks
        /// <summary>
        /// Every relational node has to pass its SELECT statement to its children
        /// This allows them (DbVariableReferenceExpression eventually) to update the list of
        /// outer extents (free variables) used by this select statement.
        /// </summary>
        private Stack<SqlSelectStatement> selectStatementStack;

        /// <summary>
        /// The top of the stack
        /// </summary>
        private SqlSelectStatement CurrentSelectStatement
        {
            // There is always something on the stack, so we can always Peek.
            get { return selectStatementStack.Peek(); }
        }

        /// <summary>
        /// Nested joins and extents need to know whether they should create
        /// a new Select statement, or reuse the parent's.  This flag
        /// indicates whether the parent is a join or not.
        /// </summary>
        private Stack<bool> isParentAJoinStack;

        /// <summary>
        /// The top of the stack
        /// </summary>
        private bool IsParentAJoin
        {
            // There might be no entry on the stack if a Join node has never
            // been seen, so we return false in that case.
            get { return isParentAJoinStack.Count == 0 ? false : isParentAJoinStack.Peek(); }
        }

        #endregion

        #region Global lists and state
        private Dictionary<string, int> allExtentNames;
        internal Dictionary<string, int> AllExtentNames
        {
            get { return allExtentNames; }
        }

        // For each column name, we store the last integer suffix that
        // was added to produce a unique column name.  This speeds up
        // the creation of the next unique name for this column name.
        private Dictionary<string, int> allColumnNames;
        internal Dictionary<string, int> AllColumnNames
        {
            get { return allColumnNames; }
        }

        readonly SymbolTable symbolTable = new SymbolTable();

        /// <summary>
        /// VariableReferenceExpressions are allowed only as children of DbPropertyExpression
        /// or MethodExpression.  The cheapest way to ensure this is to set the following
        /// property in DbVariableReferenceExpression and reset it in the allowed parent expressions.
        /// </summary>
        private bool isVarRefSingle = false;

        #endregion

        #region Statics
        static private readonly Dictionary<string, FunctionHandler> _specialBuiltInFunctionHandlers = InitializeSpecialBuiltInFunctionHandlers();
        static private readonly Dictionary<string, FunctionHandler> _canonicalFunctionHandlers = InitializeCanonicalFunctionHandlers();
        static private readonly Dictionary<string, string> _functionNameToOperatorDictionary = InitializeFunctionNameToOperatorDictionary();
        static private readonly Dictionary<string, string> _dateAddFunctionNameToDatepartDictionary = InitializeDateAddFunctionNameToDatepartDictionary();
        static private readonly Dictionary<string, string> _dateDiffFunctionNameToDatepartDictionary = InitializeDateDiffFunctionNameToDatepartDictionary();
        static private readonly HashSet<string> _datepartKeywords = InitializeDatepartKeywords();

        private delegate ISqlFragment FunctionHandler(SqlGenerator sqlgen, DbFunctionExpression functionExpr);

        /// <summary>
        /// All special built-in functions and their handlers
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, FunctionHandler> InitializeSpecialBuiltInFunctionHandlers()
        {
            Dictionary<string, FunctionHandler> functionHandlers = new Dictionary<string, FunctionHandler>(5, StringComparer.Ordinal);
            functionHandlers.Add("concat", HandleConcatFunction);
            functionHandlers.Add("dateadd", HandleDatepartDateFunction);
            functionHandlers.Add("datediff", HandleDatepartDateFunction);
            functionHandlers.Add("datename", HandleDatepartDateFunction);
            functionHandlers.Add("datepart", HandleDatepartDateFunction);

            return functionHandlers;
        }

        /// <summary>
        /// All special non-aggregate canonical functions and their handlers
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, FunctionHandler> InitializeCanonicalFunctionHandlers()
        {
            Dictionary<string, FunctionHandler> functionHandlers = new Dictionary<string, FunctionHandler>(51, StringComparer.Ordinal);
            functionHandlers.Add("IndexOf", HandleCanonicalFunctionIndexOf);
            functionHandlers.Add("Length", HandleCanonicalFunctionLength);
            functionHandlers.Add("Truncate", HandleCanonicalFunctionTruncate);
            functionHandlers.Add("Abs", HandleCanonicalFunctionAbs);

            // String functions
            functionHandlers.Add("ToLower", HandleCanonicalFunctionToLower);
            functionHandlers.Add("ToUpper", HandleCanonicalFunctionToUpper);
            functionHandlers.Add("Substring", HandleCanonicalFunctionSubstring);
            functionHandlers.Add("Contains", HandleCanonicalFunctionContains);
            functionHandlers.Add("StartsWith", HandleCanonicalFunctionStartsWith);
            functionHandlers.Add("EndsWith", HandleCanonicalFunctionEndsWith);
            functionHandlers.Add("Reverse", HandleCanonicalFunctionReverse); // Actually reverse is supported only in DAO not in OLEDB...

            // DateTime Functions (no milli, micro and nano seconds handling)
            functionHandlers.Add("Year", HandleFunctionDefaultCastToInt);
            functionHandlers.Add("Month", HandleFunctionDefaultCastToInt);
            functionHandlers.Add("Day", HandleFunctionDefaultCastToInt);
            functionHandlers.Add("Hour", HandleFunctionDefaultCastToInt);
            functionHandlers.Add("Minute", HandleFunctionDefaultCastToInt);
            functionHandlers.Add("Second", HandleFunctionDefaultCastToInt);

            functionHandlers.Add("CurrentDateTime", HandleCanonicalFunctionCurrentDateTime);
            functionHandlers.Add("CurrentUtcDateTime", HandleCanonicalFunctionCurrentDateTime); // No UTC support

            functionHandlers.Add("CreateDateTime", HandleCanonicalFunctionCreateDateTime);
            functionHandlers.Add("CreateTime", HandleCanonicalFunctionCreateTime);

            functionHandlers.Add("TruncateTime", HandleCanonicalFunctionTruncateTime);

            functionHandlers.Add("AddYears", HandleCanonicalFunctionDateAdd);
            functionHandlers.Add("AddMonths", HandleCanonicalFunctionDateAdd);
            functionHandlers.Add("AddDays", HandleCanonicalFunctionDateAdd);
            functionHandlers.Add("AddHours", HandleCanonicalFunctionDateAdd);
            functionHandlers.Add("AddMinutes", HandleCanonicalFunctionDateAdd);
            functionHandlers.Add("AddSeconds", HandleCanonicalFunctionDateAdd);
            functionHandlers.Add("AddMilliseconds", HandleCanonicalFunctionDateAdd);
            functionHandlers.Add("DiffYears", HandleCanonicalFunctionDateDiff);
            functionHandlers.Add("DiffMonths", HandleCanonicalFunctionDateDiff);
            functionHandlers.Add("DiffDays", HandleCanonicalFunctionDateDiff);
            functionHandlers.Add("DiffHours", HandleCanonicalFunctionDateDiff);
            functionHandlers.Add("DiffMinutes", HandleCanonicalFunctionDateDiff);
            functionHandlers.Add("DiffSeconds", HandleCanonicalFunctionDateDiff);


            //Functions that translate to operators
            functionHandlers.Add("Concat", HandleConcatFunction);
            functionHandlers.Add("BitwiseAnd", HandleCanonicalFunctionBitwise);
            functionHandlers.Add("BitwiseNot", HandleCanonicalFunctionBitwise);
            functionHandlers.Add("BitwiseOr", HandleCanonicalFunctionBitwise);
            functionHandlers.Add("BitwiseXor", HandleCanonicalFunctionBitwise);

            return functionHandlers;
        }

        /// <summary>
        /// Valid datepart values
        /// </summary>
        /// <returns></returns>
        private static HashSet<string> InitializeDatepartKeywords()
        {
            #region Datepart Keywords
            //
            // valid datepart values
            //
            HashSet<string> datepartKeywords = new HashSet<string>(StringComparer.Ordinal);
            datepartKeywords.Add("d");
            datepartKeywords.Add("day");
            datepartKeywords.Add("dayofyear");
            datepartKeywords.Add("dd");
            datepartKeywords.Add("dw");
            datepartKeywords.Add("dy");
            datepartKeywords.Add("hh");
            datepartKeywords.Add("hour");
            datepartKeywords.Add("m");
            datepartKeywords.Add("mi");
            datepartKeywords.Add("millisecond");
            datepartKeywords.Add("minute");
            datepartKeywords.Add("mm");
            datepartKeywords.Add("month");
            datepartKeywords.Add("ms");
            datepartKeywords.Add("n");
            datepartKeywords.Add("q");
            datepartKeywords.Add("qq");
            datepartKeywords.Add("quarter");
            datepartKeywords.Add("s");
            datepartKeywords.Add("second");
            datepartKeywords.Add("ss");
            datepartKeywords.Add("week");
            datepartKeywords.Add("weekday");
            datepartKeywords.Add("wk");
            datepartKeywords.Add("ww");
            datepartKeywords.Add("y");
            datepartKeywords.Add("year");
            datepartKeywords.Add("yy");
            datepartKeywords.Add("yyyy");
            return datepartKeywords;
            #endregion
        }

        /// <summary>
        /// Initializes the mapping from functions to Jet operators
        /// for all functions that translate to Jet operators
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> InitializeFunctionNameToOperatorDictionary()
        {
            Dictionary<string, string> functionNameToOperatorDictionary = new Dictionary<string, string>(5, StringComparer.Ordinal);
            functionNameToOperatorDictionary.Add("Concat", "&");    //canonical
            functionNameToOperatorDictionary.Add("CONCAT", "&");    //store

            /* According to Microsoft article http://support2.microsoft.com/kb/194206/en-us
             * The Microsoft ODBC Driver for Access and the Microsoft OLE DB Provider for Jet do not provide 
             * support for bitwise operations in SQL statements. Attempts to use AND, OR, and XOR with numeric 
             * fields in a SQL statement return the result of a logical operation (true or false).
             * This behavior is by design.
             */

            functionNameToOperatorDictionary.Add("BitwiseAnd", "BAND");
            functionNameToOperatorDictionary.Add("BitwiseNot", "BNOT");
            functionNameToOperatorDictionary.Add("BitwiseOr", "BOR");
            functionNameToOperatorDictionary.Add("BitwiseXor", "BXOR");
            return functionNameToOperatorDictionary;
        }

        /// <summary>
        /// Initalizes the mapping from names of canonical function for date/time addition
        /// to corresponding dateparts
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> InitializeDateAddFunctionNameToDatepartDictionary()
        {
            Dictionary<string, string> dateAddFunctionNameToDatepartDictionary = new Dictionary<string, string>(5, StringComparer.Ordinal);
            dateAddFunctionNameToDatepartDictionary.Add("AddYears", "yyyy");
            dateAddFunctionNameToDatepartDictionary.Add("AddMonths", "m");
            dateAddFunctionNameToDatepartDictionary.Add("AddDays", "d");
            dateAddFunctionNameToDatepartDictionary.Add("AddHours", "h");
            dateAddFunctionNameToDatepartDictionary.Add("AddMinutes", "n");
            dateAddFunctionNameToDatepartDictionary.Add("AddSeconds", "s");
            return dateAddFunctionNameToDatepartDictionary;
        }

        /// <summary>
        /// Initalizes the mapping from names of canonical function for date/time difference
        /// to corresponding dateparts
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, string> InitializeDateDiffFunctionNameToDatepartDictionary()
        {
            Dictionary<string, string> dateDiffFunctionNameToDatepartDictionary = new Dictionary<string, string>(5, StringComparer.Ordinal);
            dateDiffFunctionNameToDatepartDictionary.Add("DiffYears", "yyyy");
            dateDiffFunctionNameToDatepartDictionary.Add("DiffMonths", "m");
            dateDiffFunctionNameToDatepartDictionary.Add("DiffDays", "d");
            dateDiffFunctionNameToDatepartDictionary.Add("DiffHours", "h");
            dateDiffFunctionNameToDatepartDictionary.Add("DiffMinutes", "n");
            dateDiffFunctionNameToDatepartDictionary.Add("DiffSeconds", "s");
            return dateDiffFunctionNameToDatepartDictionary;
        }


        #endregion

        /// <summary>
        /// Prevents a default instance of the <see cref="SqlGenerator"/> class from being created.
        /// </summary>
        private SqlGenerator()
        {
        }

        #region Entry points
        /// <summary>
        /// General purpose static function that can be called from System.Data assembly
        /// </summary>
        /// <param name="tree">command tree</param>
        /// <param name="parameters">Parameters to add to the command tree corresponding
        /// to constants in the command tree. Used only in ModificationCommandTrees.</param>
        /// <param name="commandType">Type of the command.</param>
        /// <returns>
        /// The string representing the SQL to be executed.
        /// </returns>
        /// <exception cref="System.NotSupportedException">Unrecognized command tree type</exception>
        internal static string GenerateSql(DbCommandTree tree, out List<DbParameter> parameters, out CommandType commandType)
        {
            commandType = CommandType.Text;

            //Handle Query
            DbQueryCommandTree queryCommandTree = tree as DbQueryCommandTree;
            if (queryCommandTree != null)
            {
                SqlGenerator sqlGenerator = new SqlGenerator();
                parameters = null;
                return sqlGenerator.GenerateSql((DbQueryCommandTree)tree);
            }

            //Handle Function
            DbFunctionCommandTree DbFunctionCommandTree = tree as DbFunctionCommandTree;
            if (DbFunctionCommandTree != null)
            {
                SqlGenerator sqlGen = new SqlGenerator();
                parameters = null;

                string sql = sqlGen.GenerateFunctionSql(DbFunctionCommandTree, out commandType);

                return sql;
            }

            //Handle Insert
            DbInsertCommandTree insertCommandTree = tree as DbInsertCommandTree;
            if (insertCommandTree != null)
                return JetDmlBuilder.GenerateInsertSql(insertCommandTree, out parameters);

            //Handle Delete
            DbDeleteCommandTree deleteCommandTree = tree as DbDeleteCommandTree;
            if (deleteCommandTree != null)
                return JetDmlBuilder.GenerateDeleteSql(deleteCommandTree, out parameters);

            //Handle Update
            DbUpdateCommandTree updateCommandTree = tree as DbUpdateCommandTree;
            if (updateCommandTree != null)
                return JetDmlBuilder.GenerateUpdateSql(updateCommandTree, out parameters);

            throw new NotSupportedException("Unrecognized command tree type");
        }
        #endregion

        #region Driver Methods
        /// <summary>
        /// Translate a command tree to a SQL string.
        ///
        /// The input tree could be translated to either a SQL SELECT statement
        /// or a SELECT expression.  This choice is made based on the return type
        /// of the expression
        /// CollectionType => select statement
        /// non collection type => select expression
        /// </summary>
        /// <param name="tree"></param>
        /// <returns>The string representing the SQL to be executed.</returns>
        private string GenerateSql(DbQueryCommandTree tree)
        {
            DbExpression targetExperssion = tree.Query;

            selectStatementStack = new Stack<SqlSelectStatement>();
            isParentAJoinStack = new Stack<bool>();

            allExtentNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            allColumnNames = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Literals will not be converted to parameters.

            ISqlFragment result;
            if (MetadataHelpers.IsCollectionType(targetExperssion.ResultType))
            {
                SqlSelectStatement sqlStatement = VisitExpressionEnsureSqlStatement(targetExperssion);
                Debug.Assert(sqlStatement != null, "The outer most sql statment is null");
                sqlStatement.IsTopMost = true;
                result = sqlStatement;
            }
            else
            {
                SqlBuilder sqlBuilder = new SqlBuilder();
                sqlBuilder.Append("SELECT ");
                sqlBuilder.Append(targetExperssion.Accept(this));

                result = sqlBuilder;
            }

            if (isVarRefSingle)
            {
                throw new NotSupportedException();
                // A DbVariableReferenceExpression has to be a child of DbPropertyExpression or MethodExpression
            }

            // Check that the parameter stacks are not leaking.
            Debug.Assert(selectStatementStack.Count == 0);
            Debug.Assert(isParentAJoinStack.Count == 0);

            return WriteSql(result);
        }

        /// <summary>
        /// Translate a function command tree to a SQL string.
        /// </summary>
        private string GenerateFunctionSql(DbFunctionCommandTree tree, out CommandType commandType)
        {
            EdmFunction function = tree.EdmFunction;

            // We expect function to always have these properties
            string userCommandText = (string)function.MetadataProperties["CommandTextAttribute"].Value;
            string userFuncName = (string)function.MetadataProperties["StoreFunctionNameAttribute"].Value;

            if (String.IsNullOrEmpty(userCommandText))
            {
                // build a quoted description of the function
                commandType = CommandType.StoredProcedure;

                // if the function store name is not explicitly given, it is assumed to be the metadata name
                string functionName = String.IsNullOrEmpty(userFuncName) ?
                    function.Name : userFuncName;

                // quote elements of function text
                string quotedFunctionName = JetProviderManifest.QuoteIdentifier(functionName);

                string quotedFunctionText = quotedFunctionName;

                return quotedFunctionText;
            }
            else
            {
                // if the user has specified the command text, pass it through verbatim and choose CommandType.Text
                commandType = CommandType.Text;
                return userCommandText;
            }
        }

        /// <summary>
        /// Convert the SQL fragments to a string.
        /// We have to setup the Stream for writing.
        /// </summary>
        /// <param name="sqlStatement"></param>
        /// <returns>A string representing the SQL to be executed.</returns>
        private string WriteSql(ISqlFragment sqlStatement)
        {
            StringBuilder builder = new StringBuilder(1024);
            using (SqlWriter writer = new SqlWriter(builder))
            {
                sqlStatement.WriteSql(writer, this);
            }

            return builder.ToString();
        }
        #endregion

        #region DbExpressionVisitor Illegal Members

        public override ISqlFragment Visit(DbDerefExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbLambdaExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbEntityRefExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbRefKeyExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbIsOfExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbOfTypeExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbRefExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbRelationshipNavigationExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }

        public override ISqlFragment Visit(DbTreatExpression e)
        {
            throw new IllegalDbExpressionException(e.GetType());
        }


        #endregion

        #region DbExpressionVisitor Members

        /// <summary>
        /// Translate(left) AND Translate(right)
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/>.</returns>
        public override ISqlFragment Visit(DbAndExpression e)
        {
            return VisitBinaryExpression(" AND ", e.Left, e.Right);
        }

        /// <summary>
        /// An apply is just like a join, so it shares the common join processing
        /// in <see cref="VisitJoinExpression"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/>.</returns>
        public override ISqlFragment Visit(DbApplyExpression e)
        {
            List<DbExpressionBinding> inputs = new List<DbExpressionBinding>();
            inputs.Add(e.Input);
            inputs.Add(e.Apply);

            string joinString;

            switch (e.ExpressionKind)
            {
                case DbExpressionKind.CrossApply:
                    joinString = "CROSS APPLY";
                    throw new NotImplementedException("CrossApply not implemented in Jet");

                case DbExpressionKind.OuterApply:
                    joinString = "OUTER APPLY";
                    throw new NotImplementedException("OuterApply not implemented in Jet");

                default:
                    throw new NotImplementedException(string.Format("Unknown DbApplyExpression {0}", e.ExpressionKind));
            }

            // Apply does not work at all so we do not need to visit anything else
            //return VisitJoinExpression(inputs, DbExpressionKind.CrossJoin, joinString, null);
        }

        public override ISqlFragment Visit(DbArithmeticExpression e)
        {
            SqlBuilder result;

            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Divide:
                    result = VisitBinaryExpression(" / ", e.Arguments[0], e.Arguments[1]);
                    break;
                case DbExpressionKind.Minus:
                    result = VisitBinaryExpression(" - ", e.Arguments[0], e.Arguments[1]);
                    break;
                case DbExpressionKind.Modulo:
                    result = VisitBinaryExpression(" % ", e.Arguments[0], e.Arguments[1]);
                    break;
                case DbExpressionKind.Multiply:
                    result = VisitBinaryExpression(" * ", e.Arguments[0], e.Arguments[1]);
                    break;
                case DbExpressionKind.Plus:
                    result = VisitBinaryExpression(" + ", e.Arguments[0], e.Arguments[1]);
                    break;

                case DbExpressionKind.UnaryMinus:
                    result = new SqlBuilder();
                    result.Append(" -(");
                    result.Append(e.Arguments[0].Accept(this));
                    result.Append(")");
                    break;

                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
            }

            return result;
        }

        /// <summary>
        /// There is an issue about functions returning boolean. Jet converts it to Int16 and EF throw "an handled" cast exception.
        /// Also checking the return type and casting it with a CBool if needed does not work
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbCaseExpression e)
        {
            SqlBuilder result = new SqlBuilder();

            Debug.Assert(e.When.Count == e.Then.Count);

#warning If e.when.count == 1 and e.else.count ==1 insert Iif


            if (e.When.Count == 1 && e.Else != null && !(e.Else is DbNullExpression))
            {
                result.Append("IIf(");

                result.Append(e.When[0].Accept(this));
                result.Append(", ");
                result.Append(e.Then[0].Accept(this));
                result.Append(", ");
                result.Append(e.Else.Accept(this));
                result.Append(")");
            }
            else
            {
                bool first = true;
                result.Append("Switch(");
                for (int i = 0; i < e.When.Count; ++i)
                {
                    if (first)
                        first = false;
                    else
                        result.Append(", ");

                    result.Append(e.When[i].Accept(this));
                    result.Append(", ");
                    result.Append(e.Then[i].Accept(this));
                }
                if (e.Else != null && !(e.Else is DbNullExpression))
                {
                    if (e.When.Count != 0)
                        result.Append(", ");
                    result.Append(" true, ");
                    result.Append(e.Else.Accept(this));
                }

                result.Append(")");
            }


            return result;
        }

        public override ISqlFragment Visit(DbCastExpression e)
        {
            return Cast(e.ResultType.GetPrimitiveTypeKind(), e.Argument.Accept(this));
        }

        private ISqlFragment Cast(PrimitiveTypeKind primitiveTypeKind, ISqlFragment argument)
        {
            switch (primitiveTypeKind)
            {
                case PrimitiveTypeKind.String:
                    return new SqlBuilder("(", argument, "&\"\")");

                case PrimitiveTypeKind.Time:
                case PrimitiveTypeKind.DateTime:
                    return new SqlBuilder("CDate(IIf(IsNull(", argument, "),0,", argument, "))");

                case PrimitiveTypeKind.Single:
                    return new SqlBuilder("CSng(IIf(IsNull(", argument, "),0,", argument, "))");
                case PrimitiveTypeKind.Double:
                    return new SqlBuilder("CDbl(IIf(IsNull(", argument, "),0,", argument, "))");

                case PrimitiveTypeKind.Byte:
                    return new SqlBuilder("CByte(IIf(IsNull(", argument, "),0,", argument, "))");

                case PrimitiveTypeKind.Int16:
                case PrimitiveTypeKind.Int32:
                    return new SqlBuilder("CInt(IIf(IsNull(", argument, "),0,", argument, "))");
                case PrimitiveTypeKind.Int64:
                    return new SqlBuilder("CLng(IIf(IsNull(", argument, "),0,", argument, "))");

                case PrimitiveTypeKind.Boolean:
                    return new SqlBuilder("CBool(IIf(IsNull(", argument, "),0,", argument, "))");
                case PrimitiveTypeKind.Guid:
                // TODO: Guid - Cast
                case PrimitiveTypeKind.DateTimeOffset:
                case PrimitiveTypeKind.Decimal:
                case PrimitiveTypeKind.SByte:
                case PrimitiveTypeKind.Binary:

                case PrimitiveTypeKind.Geography:
                case PrimitiveTypeKind.GeographyCollection:
                case PrimitiveTypeKind.GeographyLineString:
                case PrimitiveTypeKind.GeographyMultiLineString:
                case PrimitiveTypeKind.GeographyMultiPoint:
                case PrimitiveTypeKind.GeographyMultiPolygon:
                case PrimitiveTypeKind.GeographyPoint:
                case PrimitiveTypeKind.GeographyPolygon:
                case PrimitiveTypeKind.Geometry:
                case PrimitiveTypeKind.GeometryCollection:
                case PrimitiveTypeKind.GeometryLineString:
                case PrimitiveTypeKind.GeometryMultiLineString:
                case PrimitiveTypeKind.GeometryMultiPoint:
                case PrimitiveTypeKind.GeometryMultiPolygon:
                case PrimitiveTypeKind.GeometryPoint:
                case PrimitiveTypeKind.GeometryPolygon:

                default:
                    throw new InvalidOperationException(string.Format("invalid PrimitiveTypeKind for cast(): cannot handle type {0} with Jet", primitiveTypeKind));

            }
        }

        /// <summary>
        /// The parser generates Not(Equals(...)) for &lt;&gt;.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/>.</returns>
        public override ISqlFragment Visit(DbComparisonExpression e)
        {
            SqlBuilder result;
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Equals:
                    result = VisitBinaryExpression(" = ", e.Left, e.Right);
                    break;
                case DbExpressionKind.LessThan:
                    result = VisitBinaryExpression(" < ", e.Left, e.Right);
                    break;
                case DbExpressionKind.LessThanOrEquals:
                    result = VisitBinaryExpression(" <= ", e.Left, e.Right);
                    break;
                case DbExpressionKind.GreaterThan:
                    result = VisitBinaryExpression(" > ", e.Left, e.Right);
                    break;
                case DbExpressionKind.GreaterThanOrEquals:
                    result = VisitBinaryExpression(" >= ", e.Left, e.Right);
                    break;
                // The parser does not generate the expression kind below.
                case DbExpressionKind.NotEquals:
                    result = VisitBinaryExpression(" <> ", e.Left, e.Right);
                    break;

                default:
                    Debug.Assert(false);  // The constructor should have prevented this
                    throw new InvalidOperationException(String.Empty);
            }

            return result;
        }

        /// <summary>
        /// Constants will be send to the store as part of the generated SQL statement, not as parameters
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/>.  Strings are wrapped in single
        /// quotes and escaped.  Numbers are written literally.</returns>
        public override ISqlFragment Visit(DbConstantExpression e)
        {
            return VisitConstantExpression(e.ResultType, e.Value);
        }

        private ISqlFragment VisitConstantExpression(TypeUsage expressionType, object expressionValue)
        {
            SqlBuilder result = new SqlBuilder();

            PrimitiveTypeKind typeKind;
            // Model Types can be (at the time of this implementation):
            //      Binary, Boolean, Byte, DateTime, Decimal, Double, Guid, Int16, Int32, Int64,Single, String
            if (expressionType.TryGetPrimitiveTypeKind(out typeKind))
            {
                switch (typeKind)
                {
                    case PrimitiveTypeKind.Int32:
                    case PrimitiveTypeKind.Byte:
                        result.Append(expressionValue.ToString());
                        break;

                    case PrimitiveTypeKind.Binary:
                        result.Append(LiteralHelpers.ToSqlString((Byte[])expressionValue));
                        break;

                    case PrimitiveTypeKind.Boolean:
                        result.Append(LiteralHelpers.ToSqlString((bool)expressionValue));
                        break;


                    case PrimitiveTypeKind.DateTime:
                        result.Append(LiteralHelpers.SqlDateTime((System.DateTime)expressionValue));
                        break;

                    case PrimitiveTypeKind.Time:
                        result.Append(LiteralHelpers.SqlDayTime((System.TimeSpan)expressionValue));
                        break;

                    case PrimitiveTypeKind.DateTimeOffset:
                        throw new NotImplementedException("Jet does not implement DateTimeOffset");


                    case PrimitiveTypeKind.Decimal:
                        string strDecimal = ((Decimal)expressionValue).ToString(CultureInfo.InvariantCulture);
                        result.Append(strDecimal);
                        break;

                    case PrimitiveTypeKind.Double:
                        result.Append(((Double)expressionValue).ToString(CultureInfo.InvariantCulture));
                        break;
                    case PrimitiveTypeKind.Single:
                        result.Append(((Single)expressionValue).ToString(CultureInfo.InvariantCulture));
                        break;
                    case PrimitiveTypeKind.Int16:
                    case PrimitiveTypeKind.Int64:
                        result.Append(expressionValue.ToString());
                        break;
                    case PrimitiveTypeKind.String:
                        result.Append(LiteralHelpers.ToSqlString(expressionValue as string));
                        break;

                    case PrimitiveTypeKind.Guid:
                    default:
                        // all known scalar types should been handled already.
                        throw new NotSupportedException("Primitive type kind " + typeKind + " is not supported by the Jet Provider");
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            return result;
        }


        /// <summary>
        /// The DISTINCT has to be added to the beginning of SqlSelectStatement.Select,
        /// but it might be too late for that.  So, we use a flag on SqlSelectStatement
        /// instead, and add the "DISTINCT" in the second phase.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/></returns>
        public override ISqlFragment Visit(DbDistinctExpression e)
        {
            SqlSelectStatement result = VisitExpressionEnsureSqlStatement(e.Argument);

            if (!IsCompatible(result, e.ExpressionKind))
            {
                Symbol fromSymbol;
                TypeUsage inputType = MetadataHelpers.GetElementTypeUsage(e.Argument.ResultType);
                result = CreateNewSelectStatement(result, "distinct", inputType, out fromSymbol);
                AddFromSymbol(result, "distinct", fromSymbol, false);
            }

            result.IsDistinct = true;
            return result;
        }

        /// <summary>
        /// An element expression returns a scalar - so it is translated to
        /// ( Select ... )
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbElementExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append("(");
            result.Append(VisitExpressionEnsureSqlStatement(e.Argument));
            result.Append(")");

            return result;
        }

        /// <summary>
        /// <see cref="Visit(DbUnionAllExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbExceptExpression e)
        {
            return VisitSetOpExpression(e.Left, e.Right, "EXCEPT");
        }

        /// <summary>
        /// Only concrete expression types will be visited.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbExpression e)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns>If we are in a Join context, returns a <see cref="SqlBuilder"/>
        /// with the extent name, otherwise, a new <see cref="SqlSelectStatement"/>
        /// with the From field set.</returns>
        public override ISqlFragment Visit(DbScanExpression e)
        {
            EntitySetBase target = e.Target;

            if (IsParentAJoin)
            {
                SqlBuilder result = new SqlBuilder();
                result.Append(GetTargetTSql(target));

                return result;
            }
            else
            {
                SqlSelectStatement result = new SqlSelectStatement();
                result.From.Append(GetTargetTSql(target));

                return result;
            }
        }

        /// <summary>
        /// Gets escaped TSql identifier describing this entity set.
        /// </summary>
        /// <returns></returns>
        internal static string GetTargetTSql(EntitySetBase entitySetBase)
        {
            // construct escaped T-SQL referencing entity set
            StringBuilder builder = new StringBuilder(50);
            string definingQuery = MetadataHelpers.TryGetValueForMetadataProperty<string>(entitySetBase, "DefiningQuery");
            if (!string.IsNullOrEmpty(definingQuery))
            {
                builder.Append("(");
                builder.Append(definingQuery);
                builder.Append(")");
            }
            else
            {

                string tableName = MetadataHelpers.TryGetValueForMetadataProperty<string>(entitySetBase, "Table");
                if (!string.IsNullOrEmpty(tableName))
                {
                    builder.Append(JetProviderManifest.QuoteIdentifier(tableName));
                }
                else
                {
                    builder.Append(JetProviderManifest.QuoteIdentifier(entitySetBase.Name));
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// The bodies of <see cref="Visit(DbFilterExpression)"/>, <see cref="Visit(DbGroupByExpression)"/>,
        /// <see cref="Visit(DbProjectExpression)"/>, <see cref="Visit(DbSortExpression)"/> are similar.
        /// Each does the following.
        /// <list type="number">
        /// <item> Visit the input expression</item>
        /// <item> Determine if the input's SQL statement can be reused, or a new
        /// one must be created.</item>
        /// <item>Create a new symbol table scope</item>
        /// <item>Push the Sql statement onto a stack, so that children can
        /// update the free variable list.</item>
        /// <item>Visit the non-input expression.</item>
        /// <item>Cleanup</item>
        /// </list>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/></returns>
        public override ISqlFragment Visit(DbFilterExpression e)
        {
            return VisitFilterExpression(e.Input, e.Predicate, false);
        }

        /// <summary>
        /// Lambda functions are not supported.
        /// The functions supported are:
        /// <list type="number">
        /// <item>Canonical Functions - We recognize these by their dataspace, it is DataSpace.CSpace</item>
        /// <item>Store Functions - We recognize these by the BuiltInAttribute and not being Canonical</item>
        /// <item>User-defined Functions - All the rest except for Lambda functions</item>
        /// </list>
        /// We handle Canonical and Store functions the same way: If they are in the list of functions 
        /// that need special handling, we invoke the appropriate handler, otherwise we translate them to
        /// FunctionName(arg1, arg2, ..., argn).
        /// We translate user-defined functions to NamespaceName.FunctionName(arg1, arg2, ..., argn).
        /// </summary>
        /// <param name="functionExpression"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbFunctionExpression functionExpression)
        {
            //
            // check if function requires special case processing, if so, delegates to it
            //
            if (IsSpecialBuiltInFunction(functionExpression))
                return HandleSpecialBuiltInFunction(functionExpression);

            if (IsSpecialCanonicalFunction(functionExpression))
                return HandleSpecialCanonicalFunction(functionExpression);

            return HandleFunctionDefault(functionExpression);
        }


        /// <summary>
        /// <see cref="Visit(DbFilterExpression)"/> for general details.
        /// We modify both the GroupBy and the Select fields of the SqlSelectStatement.
        /// GroupBy gets just the keys without aliases,
        /// and Select gets the keys and the aggregates with aliases.
        /// 
        /// Whenever there exists at least one aggregate with an argument that is not is not a simple
        /// <see cref="DbPropertyExpression"/>  over <see cref="DbVariableReferenceExpression"/>, 
        /// we create a nested query in which we alias the arguments to the aggregates. 
        /// That is due to the following two limitations of Sql Server:
        /// <list type="number">
        /// <item>If an expression being aggregated contains an outer reference, then that outer 
        /// reference must be the only column referenced in the expression </item>
        /// <item>Sql Server cannot perform an aggregate function on an expression containing 
        /// an aggregate or a subquery. </item>
        /// </list>
        /// 
        /// The default translation, without inner query is: 
        /// 
        ///     SELECT 
        ///         kexp1 AS key1, kexp2 AS key2,... kexpn AS keyn, 
        ///         aggf1(aexpr1) AS agg1, .. aggfn(aexprn) AS aggn
        ///     FROM input AS a
        ///     GROUP BY kexp1, kexp2, .. kexpn
        /// 
        /// When we inject an innner query, the equivalent translation is:
        /// 
        ///     SELECT 
        ///         key1 AS key1, key2 AS key2, .. keyn AS keys,  
        ///         aggf1(agg1) AS agg1, aggfn(aggn) AS aggn
        ///     FROM (
        ///             SELECT 
        ///                 kexp1 AS key1, kexp2 AS key2,... kexpn AS keyn, 
        ///                 aexpr1 AS agg1, .. aexprn AS aggn
        ///             FROM input AS a
        ///         ) as a
        ///     GROUP BY key1, key2, keyn
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/></returns>
        public override ISqlFragment Visit(DbGroupByExpression e)
        {
            Symbol fromSymbol;
            SqlSelectStatement innerQuery = VisitInputExpression(e.Input.Expression,
                e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            // GroupBy is compatible with Filter and OrderBy
            // but not with Project, GroupBy
            if (!IsCompatible(innerQuery, e.ExpressionKind))
            {
                innerQuery = CreateNewSelectStatement(innerQuery, e.Input.VariableName, e.Input.VariableType, out fromSymbol);
            }

            selectStatementStack.Push(innerQuery);
            symbolTable.EnterScope();

            AddFromSymbol(innerQuery, e.Input.VariableName, fromSymbol);
            // This line is not present for other relational nodes.
            symbolTable.Add(e.Input.GroupVariableName, fromSymbol);

            // The enumerator is shared by both the keys and the aggregates,
            // so, we do not close it in between.
            RowType groupByType = MetadataHelpers.GetEdmType<RowType>(MetadataHelpers.GetEdmType<CollectionType>(e.ResultType).TypeUsage);

            // Jet does not support arbitrarily complex expressions inside aggregates, 
            // and requires keys to have reference to the input scope, 
            // so we check for the specific restrictions and if need we inject an inner query.
            var needsInnerQuery = GroupByAggregatesNeedInnerQuery(e.Aggregates, e.Input.GroupVariableName) ||
                                  GroupByKeysNeedInnerQuery(e.Keys, e.Input.VariableName);


            SqlSelectStatement result;
            if (needsInnerQuery)
            {
                //Create the inner query
                result = CreateNewSelectStatement(innerQuery, e.Input.VariableName, e.Input.VariableType, false, out fromSymbol);
                AddFromSymbol(result, e.Input.VariableName, fromSymbol, false);
            }
            else
            {
                result = innerQuery;
            }

            using (IEnumerator<EdmProperty> members = groupByType.Properties.GetEnumerator())
            {
                members.MoveNext();
                Debug.Assert(result.Select.IsEmpty);

                string separator = "";

                foreach (DbExpression key in e.Keys)
                {
                    EdmProperty member = members.Current;
                    string alias = JetProviderManifest.QuoteIdentifier(member.Name);

                    result.GroupBy.Append(separator);

                    ISqlFragment keySql = key.Accept(this);

                    if (!needsInnerQuery)
                    {
                        //Default translation: Key AS Alias
                        result.Select.Append(separator);
                        result.Select.AppendLine();
                        result.Select.Append(keySql);
                        result.Select.Append(" AS ");
                        result.Select.Append(alias);

                        result.GroupBy.Append(keySql);
                    }
                    else
                    {
                        // The inner query contains the default translation Key AS Alias
                        innerQuery.Select.Append(separator);
                        innerQuery.Select.AppendLine();
                        innerQuery.Select.Append(keySql);
                        innerQuery.Select.Append(" AS ");
                        innerQuery.Select.Append(alias);

                        //The outer resulting query projects over the key aliased in the inner query: 
                        //  fromSymbol.Alias AS Alias
                        result.Select.Append(separator);
                        result.Select.AppendLine();
                        result.Select.Append(fromSymbol);
                        result.Select.Append(".");
                        result.Select.Append(alias);
                        result.Select.Append(" AS ");
                        result.Select.Append(alias);

                        result.GroupBy.Append(alias);
                    }

                    separator = ", ";
                    members.MoveNext();
                }

                foreach (DbAggregate aggregate in e.Aggregates)
                {
                    EdmProperty member = members.Current;
                    string alias = JetProviderManifest.QuoteIdentifier(member.Name);

                    Debug.Assert(aggregate.Arguments.Count == 1);
                    ISqlFragment translatedAggregateArgument = aggregate.Arguments[0].Accept(this);

                    object aggregateArgument;

                    if (needsInnerQuery)
                    {
                        //In this case the argument to the aggratete is reference to the one projected out by the
                        // inner query
                        SqlBuilder wrappingAggregateArgument = new SqlBuilder();
                        wrappingAggregateArgument.Append(fromSymbol);
                        wrappingAggregateArgument.Append(".");
                        wrappingAggregateArgument.Append(alias);
                        aggregateArgument = wrappingAggregateArgument;

                        innerQuery.Select.Append(separator);
                        innerQuery.Select.AppendLine();
                        innerQuery.Select.Append(translatedAggregateArgument);
                        innerQuery.Select.Append(" AS ");
                        innerQuery.Select.Append(alias);
                    }
                    else
                    {
                        aggregateArgument = translatedAggregateArgument;
                    }

                    ISqlFragment aggregateResult = VisitAggregate(aggregate, aggregateArgument);

                    result.Select.Append(separator);
                    result.Select.AppendLine();
                    result.Select.Append(aggregateResult);
                    result.Select.Append(" AS ");
                    result.Select.Append(alias);

                    separator = ", ";
                    members.MoveNext();
                }
            }

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }

        /// <summary>
        /// <see cref="Visit(DbUnionAllExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbIntersectExpression e)
        {
            return VisitSetOpExpression(e.Left, e.Right, "INTERSECT");
        }

        /// <summary>
        /// Not(IsEmpty) has to be handled specially, so we delegate to
        /// <see cref="VisitIsEmptyExpression"/>.
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/>.
        /// <code>[NOT] EXISTS( ... )</code>
        /// </returns>
        public override ISqlFragment Visit(DbIsEmptyExpression e)
        {
            return VisitIsEmptyExpression(e, false);
        }

        /// <summary>
        /// Not(IsNull) is handled specially, so we delegate to
        /// <see cref="VisitIsNullExpression"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/>
        /// <code>IS [NOT] NULL</code>
        /// </returns>
        public override ISqlFragment Visit(DbIsNullExpression e)
        {
            return VisitIsNullExpression(e, false);
        }

        /// <summary>
        /// <see cref="VisitJoinExpression"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/>.</returns>
        public override ISqlFragment Visit(DbCrossJoinExpression e)
        {
            return VisitJoinExpression(e.Inputs, e.ExpressionKind, "CROSS JOIN", null);
        }

        /// <summary>
        /// <see cref="VisitJoinExpression"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/>.</returns>
        public override ISqlFragment Visit(DbJoinExpression e)
        {
            #region Map join type to a string
            string joinString;
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.FullOuterJoin:
                    joinString = "FULL OUTER JOIN";
                    break;

                case DbExpressionKind.InnerJoin:
                    joinString = "INNER JOIN";
                    break;

                case DbExpressionKind.LeftOuterJoin:
                    joinString = "LEFT OUTER JOIN";
                    break;

                default:
                    Debug.Assert(false);
                    joinString = null;
                    break;
            }
            #endregion

            List<DbExpressionBinding> inputs = new List<DbExpressionBinding>(2);
            inputs.Add(e.Left);
            inputs.Add(e.Right);

            return VisitJoinExpression(inputs, e.ExpressionKind, joinString, e.JoinCondition);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbLikeExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append(e.Argument.Accept(this));
            result.Append(" LIKE ");
            result.Append(e.Pattern.Accept(this));
            return result;
        }

        /// <summary>
        ///  Translates to TOP expression.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbLimitExpression e)
        {
            if (!(e.Limit is DbConstantExpression) &&
                !(e.Limit is DbParameterReferenceExpression))
                throw new InvalidOperationException("DbLimitExpression.Limit is of invalid expression type");

            SqlSelectStatement result = VisitExpressionEnsureSqlStatement(e.Argument, false);
            Symbol fromSymbol;

            if (!IsCompatible(result, e.ExpressionKind))
            {
                TypeUsage inputType = MetadataHelpers.GetElementTypeUsage(e.Argument.ResultType);

                result = CreateNewSelectStatement(result, "top", inputType, out fromSymbol);
                AddFromSymbol(result, "top", fromSymbol, false);
            }

            int topCount = HandleCountExpression(e.Limit);

            if (e.WithTies)
                throw new NotImplementedException("WITH TIES not implemented in Jet");

            result.Top = new TopClause(topCount, e.WithTies);
            return result;
        }

        /// <summary>
        /// DbNewInstanceExpression is allowed as a child of DbProjectExpression only.
        /// If anyone else is the parent, we throw.
        /// We also perform special casing for collections - where we could convert
        /// them into Unions
        ///
        /// <see cref="VisitNewInstanceExpression"/> for the actual implementation.
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbNewInstanceExpression e)
        {
            if (MetadataHelpers.IsCollectionType(e.ResultType))
            {
                return VisitCollectionConstructor(e);
            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// The Not expression may cause the translation of its child to change.
        /// These children are
        /// <list type="bullet">
        /// <item><see cref="DbNotExpression"/>NOT(Not(x)) becomes x</item>
        /// <item><see cref="DbIsEmptyExpression"/>NOT EXISTS becomes EXISTS</item>
        /// <item><see cref="DbIsNullExpression"/>IS NULL becomes IS NOT NULL</item>
        /// <item><see cref="DbComparisonExpression"/>= becomes&lt;&gt; </item>
        /// </list>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbNotExpression e)
        {
            // Flatten Not(Not(x)) to x.
            DbNotExpression notExpression = e.Argument as DbNotExpression;
            if (notExpression != null)
            {
                return notExpression.Argument.Accept(this);
            }

            DbIsEmptyExpression isEmptyExpression = e.Argument as DbIsEmptyExpression;
            if (isEmptyExpression != null)
            {
                return VisitIsEmptyExpression(isEmptyExpression, true);
            }

            DbIsNullExpression isNullExpression = e.Argument as DbIsNullExpression;
            if (isNullExpression != null)
            {
                return VisitIsNullExpression(isNullExpression, true);
            }

            DbComparisonExpression comparisonExpression = e.Argument as DbComparisonExpression;
            if (comparisonExpression != null)
            {
                if (comparisonExpression.ExpressionKind == DbExpressionKind.Equals)
                {
                    return VisitBinaryExpression(" <> ", comparisonExpression.Left, comparisonExpression.Right);
                }
            }

            SqlBuilder result = new SqlBuilder();
            result.Append(" NOT (");
            result.Append(e.Argument.Accept(this));
            result.Append(")");

            return result;
        }

        /// <summary>
        /// Visits the specified expression.
        /// </summary>
        /// <param name="e">The expression.</param>
        /// <returns>The sql fragment</returns>
        public override ISqlFragment Visit(DbNullExpression e)
        {
            // Need a cast on the primitive type of the result type? (GetSqlPrimitiveType(e.ResultType))
            if (JetConnection.IntegerNullValue == null || MetadataHelpers.GetEdmType<PrimitiveType>(e.ResultType).PrimitiveTypeKind != PrimitiveTypeKind.Int32)
                return new SqlBuilder("(null)");
            else
                return new SqlBuilder(((int)JetConnection.IntegerNullValue).ToString(NumberFormatInfo.InvariantInfo));
        }

        /// <summary>
        /// Visits the specified expression.
        /// </summary>
        /// <param name="e">The expression.</param>
        /// <returns>The sql fragment</returns>
        public override ISqlFragment Visit(DbOrExpression e)
        {
            return VisitBinaryExpression(" OR ", e.Left, e.Right);
        }

        /// <summary>
        /// Visits the specified expression.
        /// </summary>
        /// <param name="e">The expression.</param>
        /// <returns>The sql fragment</returns>
        public override ISqlFragment Visit(DbParameterReferenceExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            // Do not quote this name.
            // We are not checking that e.Name has no illegal characters. e.g. space
            result.Append("@" + e.ParameterName);

            return result;
        }

        /// <summary>
        /// Visits the specified expression.
        /// </summary>
        /// <param name="e">The expression.</param>
        /// <returns>The sql fragment</returns>
        public override ISqlFragment Visit(DbProjectExpression e)
        {
            Symbol fromSymbol;
            SqlSelectStatement result = VisitInputExpression(e.Input.Expression, e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            // Project is compatible with Filter
            // but not with Project, GroupBy
            if (!IsCompatible(result, e.ExpressionKind))
            {
                result = CreateNewSelectStatement(result, e.Input.VariableName, e.Input.VariableType, out fromSymbol);
            }

            selectStatementStack.Push(result);
            symbolTable.EnterScope();

            AddFromSymbol(result, e.Input.VariableName, fromSymbol);

            // Project is the only node that can have DbNewInstanceExpression as a child
            // so we have to check it here.
            // We call VisitNewInstanceExpression instead of Visit(DbNewInstanceExpression), since
            // the latter throws.
            DbNewInstanceExpression newInstanceExpression = e.Projection as DbNewInstanceExpression;
            if (newInstanceExpression != null)
                result.Select.Append(VisitNewInstanceExpression(newInstanceExpression));
            else
                result.Select.Append(e.Projection.Accept(this));

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }

        /// <summary>
        /// This method handles record flattening, which works as follows.
        /// consider an expression <c>Prop(y, Prop(x, Prop(d, Prop(c, Prop(b, Var(a)))))</c>
        /// where a,b,c are joins, d is an extent and x and y are fields.
        /// b has been flattened into a, and has its own SELECT statement.
        /// c has been flattened into b.
        /// d has been flattened into c.
        ///
        /// We visit the instance, so we reach Var(a) first.  This gives us a (join)symbol.
        /// Symbol(a).b gives us a join symbol, with a SELECT statement i.e. Symbol(b).
        /// From this point on , we need to remember Symbol(b) as the source alias,
        /// and then try to find the column.  So, we use a SymbolPair.
        ///
        /// We have reached the end when the symbol no longer points to a join symbol.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="JoinSymbol"/> if we have not reached the first
        /// Join node that has a SELECT statement.
        /// A <see cref="SymbolPair"/> if we have seen the JoinNode, and it has
        /// a SELECT statement.
        /// A <see cref="SqlBuilder"/> with {Input}.propertyName otherwise.
        /// </returns>
        public override ISqlFragment Visit(DbPropertyExpression e)
        {
            SqlBuilder result;

            ISqlFragment instanceSql = e.Instance.Accept(this);

            // Since the DbVariableReferenceExpression is a proper child of ours, we can reset
            // isVarSingle.
            DbVariableReferenceExpression DbVariableReferenceExpression = e.Instance as DbVariableReferenceExpression;
            if (DbVariableReferenceExpression != null)
                isVarRefSingle = false;

            // We need to flatten, and have not yet seen the first nested SELECT statement.
            JoinSymbol joinSymbol = instanceSql as JoinSymbol;
            if (joinSymbol != null)
            {
                Debug.Assert(joinSymbol.NameToExtent.ContainsKey(e.Property.Name));
                if (joinSymbol.IsNestedJoin)
                {
                    return new SymbolPair(joinSymbol, joinSymbol.NameToExtent[e.Property.Name]);
                }
                else
                {
                    return joinSymbol.NameToExtent[e.Property.Name];
                }
            }

            // ---------------------------------------
            // We have seen the first nested SELECT statement, but not the column.
            SymbolPair symbolPair = instanceSql as SymbolPair;
            if (symbolPair != null)
            {
                JoinSymbol columnJoinSymbol = symbolPair.Column as JoinSymbol;
                if (columnJoinSymbol != null)
                {
                    symbolPair.Column = columnJoinSymbol.NameToExtent[e.Property.Name];
                    return symbolPair;
                }
                else
                {
                    // symbolPair.Column has the base extent.
                    // we need the symbol for the column, since it might have been renamed
                    // when handling a JOIN.
                    if (symbolPair.Column.Columns.ContainsKey(e.Property.Name))
                    {
                        result = new SqlBuilder();
                        result.Append(symbolPair.Source);
                        result.Append(".");
                        result.Append(symbolPair.Column.Columns[e.Property.Name]);
                        return result;
                    }
                }
            }
            // ---------------------------------------

            result = new SqlBuilder();
            result.Append(instanceSql);
            result.Append(".");

            // At this point the column name cannot be renamed, so we do
            // not use a symbol.
            result.Append(JetProviderManifest.QuoteIdentifier(e.Property.Name));

            return result;
        }

        /// <summary>
        /// Any(input, x) => Exists(Filter(input,x))
        /// All(input, x) => Not Exists(Filter(input, not(x))
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbQuantifierExpression e)
        {
            SqlBuilder result = new SqlBuilder();

            bool negatePredicate = (e.ExpressionKind == DbExpressionKind.All);
            if (e.ExpressionKind == DbExpressionKind.Any)
            {
                result.Append("EXISTS (");
            }
            else
            {
                Debug.Assert(e.ExpressionKind == DbExpressionKind.All);
                result.Append("NOT EXISTS (");
            }

            SqlSelectStatement filter = VisitFilterExpression(e.Input, e.Predicate, negatePredicate);
            if (filter.Select.IsEmpty)
            {
                AddDefaultColumns(filter);
            }

            result.Append(filter);
            result.Append(")");

            return result;
        }

        /// <summary>
        /// The translation in here is for the JetCommand that will skip records on the DbDataReader.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlBuilder"/></returns>
        public override ISqlFragment Visit(DbSkipExpression e)
        {

            Debug.Assert(e.Count is DbConstantExpression || e.Count is DbParameterReferenceExpression, "DbSkipExpression.Count is of invalid expression type");

            //Visit the input
            Symbol fromSymbol;
            SqlSelectStatement result = VisitInputExpression(e.Input.Expression, e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            if (!(e.Count is DbConstantExpression) &&
            !(e.Count is DbParameterReferenceExpression))
                throw new InvalidOperationException("DbSkipExpression.Count is of invalid expression type");


            if (!IsCompatible(result, e.ExpressionKind))
                result = CreateNewSelectStatement(result, e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            int skipCount = HandleCountExpression(e.Count);

            result.Skip = new SkipClause(skipCount);

            selectStatementStack.Push(result);
            symbolTable.EnterScope();

            AddFromSymbol(result, e.Input.VariableName, fromSymbol);

            AddSortKeys(result.OrderBy, e.SortOrder);

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;

        }

        /// <summary>
        /// <see cref="Visit(DbFilterExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="SqlSelectStatement"/></returns>
        /// <seealso cref="Visit(DbFilterExpression)"/>
        public override ISqlFragment Visit(DbSortExpression e)
        {
            Symbol fromSymbol;
            SqlSelectStatement result = VisitInputExpression(e.Input.Expression, e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            // OrderBy is compatible with Filter
            // and nothing else
            if (!IsCompatible(result, e.ExpressionKind))
                result = CreateNewSelectStatement(result, e.Input.VariableName, e.Input.VariableType, out fromSymbol);

            selectStatementStack.Push(result);
            symbolTable.EnterScope();

            AddFromSymbol(result, e.Input.VariableName, fromSymbol);

            AddSortKeys(result.OrderBy, e.SortOrder);

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }


        /// <summary>
        /// This code is shared by <see cref="Visit(DbExceptExpression)"/>
        /// and <see cref="Visit(DbIntersectExpression)"/>
        ///
        /// <see cref="VisitSetOpExpression"/>
        /// Since the left and right expression may not be Sql select statements,
        /// we must wrap them up to look like SQL select statements.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override ISqlFragment Visit(DbUnionAllExpression e)
        {
            return VisitSetOpExpression(e.Left, e.Right, "UNION ALL");
        }

        /// <summary>
        /// This method determines whether an extent from an outer scope(free variable)
        /// is used in the CurrentSelectStatement.
        ///
        /// An extent in an outer scope, if its symbol is not in the FromExtents
        /// of the CurrentSelectStatement.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>A <see cref="Symbol"/>.</returns>
        public override ISqlFragment Visit(DbVariableReferenceExpression e)
        {
            if (isVarRefSingle)
            {
                throw new NotSupportedException();
                // A DbVariableReferenceExpression has to be a child of DbPropertyExpression or MethodExpression
                // This is also checked in GenerateSql(...) at the end of the visiting.
            }
            isVarRefSingle = true; // This will be reset by DbPropertyExpression or MethodExpression

            Symbol result = symbolTable.Lookup(e.VariableName);
            if (!CurrentSelectStatement.FromExtents.Contains(result))
            {
                CurrentSelectStatement.OuterExtents[result] = true;
            }

            return result;
        }


        #region Visits shared by multiple nodes

        /// <summary>
        /// Aggregates are not visited by the normal visitor walk.
        /// </summary>
        /// <param name="aggregate">The aggreate go be translated</param>
        /// <param name="aggregateArgument">The translated aggregate argument</param>
        /// <returns></returns>
        SqlBuilder VisitAggregate(DbAggregate aggregate, object aggregateArgument)
        {
            SqlBuilder aggregateResult = new SqlBuilder();
            DbFunctionAggregate functionAggregate = aggregate as DbFunctionAggregate;

            if (functionAggregate == null)
                throw new NotSupportedException("The aggregate function must be a DbFunctionAggregate");

            // Actually aggregate canonical functions are the following
            // Avg(expression)  
            // BigCount(expression)  
            // Count(expression)  
            // Max(expression)  
            // Min(expression)  
            // StDev(expression)  
            // StDevP(expression)  
            // Sum(expression)  
            // Var(expression)  
            // VarP(expression)  


            // Count is unsupported by Jet
            if (MetadataHelpers.IsCanonicalFunction(functionAggregate.Function) && String.Equals(functionAggregate.Function.Name, "BigCount", StringComparison.Ordinal))
                aggregateResult.Append("COUNT");
            else
                WriteFunctionName(aggregateResult, functionAggregate.Function);

            aggregateResult.Append("(");

            if (functionAggregate != null && functionAggregate.Distinct)
                aggregateResult.Append("DISTINCT ");

            aggregateResult.Append(aggregateArgument);

            aggregateResult.Append(")");
            return aggregateResult;
        }


        SqlBuilder VisitBinaryExpression(string op, DbExpression left, DbExpression right)
        {
            SqlBuilder result = new SqlBuilder();
            ParanthesizeExpressionIfNeeded(left, result);
            result.Append(op);
            ParanthesizeExpressionIfNeeded(right, result);
            return result;
        }

        /// <summary>
        /// This is called by the relational nodes.  It does the following
        /// <list>
        /// <item>If the input is not a SqlSelectStatement, it assumes that the input
        /// is a collection expression, and creates a new SqlSelectStatement </item>
        /// </list>
        /// </summary>
        /// <param name="inputExpression"></param>
        /// <param name="inputVarName"></param>
        /// <param name="inputVarType"></param>
        /// <param name="fromSymbol"></param>
        /// <returns>A <see cref="SqlSelectStatement"/> and the main fromSymbol
        /// for this select statement.</returns>
        private SqlSelectStatement VisitInputExpression(DbExpression inputExpression, string inputVarName, TypeUsage inputVarType, out Symbol fromSymbol)
        {
            SqlSelectStatement result;
            ISqlFragment sqlFragment = inputExpression.Accept(this);
            result = sqlFragment as SqlSelectStatement;

            if (result == null)
            {
                result = new SqlSelectStatement();
                WrapNonQueryExtent(result, sqlFragment, inputExpression.ExpressionKind);
            }

            if (result.FromExtents.Count == 0)
            {
                // input was an extent
                fromSymbol = new Symbol(inputVarName, inputVarType);
            }
            else if (result.FromExtents.Count == 1)
            {
                // input was Filter/GroupBy/Project/OrderBy
                // we are likely to reuse this statement.
                fromSymbol = result.FromExtents[0];
            }
            else
            {
                // input was a join.
                // we are reusing the select statement produced by a Join node
                // we need to remove the original extents, and replace them with a
                // new extent with just the Join symbol.
                JoinSymbol joinSymbol = new JoinSymbol(inputVarName, inputVarType, result.FromExtents);
                joinSymbol.FlattenedExtentList = result.AllJoinExtents;

                fromSymbol = joinSymbol;
                result.FromExtents.Clear();
                result.FromExtents.Add(fromSymbol);
            }

            return result;
        }

        /// <summary>
        /// <see cref="Visit(DbIsEmptyExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="negate">Was the parent a DbNotExpression?</param>
        /// <returns></returns>
        SqlBuilder VisitIsEmptyExpression(DbIsEmptyExpression e, bool negate)
        {
            SqlBuilder result = new SqlBuilder();
            if (!negate)
            {
                result.Append(" NOT");
            }
            result.Append(" EXISTS (");
            result.Append(VisitExpressionEnsureSqlStatement(e.Argument));
            result.AppendLine();
            result.Append(")");

            return result;
        }


        /// <summary>
        /// Translate a NewInstance(Element(X)) expression into
        ///   "select top 1 * from X"
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment VisitCollectionConstructor(DbNewInstanceExpression e)
        {
            Debug.Assert(e.Arguments.Count <= 1);

            if (e.Arguments.Count == 1 && e.Arguments[0].ExpressionKind == DbExpressionKind.Element)
            {
                DbElementExpression elementExpr = e.Arguments[0] as DbElementExpression;
                SqlSelectStatement result = VisitExpressionEnsureSqlStatement(elementExpr.Argument);

                if (!IsCompatible(result, DbExpressionKind.Element))
                {
                    Symbol fromSymbol;
                    TypeUsage inputType = MetadataHelpers.GetElementTypeUsage(elementExpr.Argument.ResultType);

                    result = CreateNewSelectStatement(result, "element", inputType, out fromSymbol);
                    AddFromSymbol(result, "element", fromSymbol, false);
                }
                result.Top = new TopClause(1, false);
                return result;
            }


            // Otherwise simply build this out as a union-all ladder
            CollectionType collectionType = MetadataHelpers.GetEdmType<CollectionType>(e.ResultType);
            Debug.Assert(collectionType != null);
            bool isScalarElement = MetadataHelpers.IsPrimitiveType(collectionType.TypeUsage);

            SqlBuilder resultSql = new SqlBuilder();
            string separator = "";

            // handle empty table
            if (e.Arguments.Count == 0)
            {
                Debug.Assert(isScalarElement);
                resultSql.Append(" SELECT ");
                Cast(collectionType.TypeUsage.GetPrimitiveTypeKind(), null);
                resultSql.Append(GetSqlPrimitiveType(collectionType.TypeUsage));
                // We need to append the "DUAL" table because of access
                resultSql.Append(" AS X FROM (SELECT 1 FROM " + JetConnection.DUAL + ") AS Y WHERE 1=0");
            }

            foreach (DbExpression arg in e.Arguments)
            {
                resultSql.Append(separator);
                resultSql.Append(" SELECT ");
                resultSql.Append(arg.Accept(this));
                // For scalar elements, we need to append alias and to append the "DUAL" table because access does not support SELECT without FROM
                if (isScalarElement)
                    resultSql.Append(" AS X FROM " + JetConnection.DUAL + " ");

                separator = " UNION ALL ";
            }

            return resultSql;
        }

        /// <summary>
        /// <see cref="Visit(DbIsNullExpression)"/>
        /// </summary>
        /// <param name="e"></param>
        /// <param name="negate">Was the parent a DbNotExpression?</param>
        /// <returns></returns>
        private SqlBuilder VisitIsNullExpression(DbIsNullExpression e, bool negate)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append(e.Argument.Accept(this));
            if (!negate)
            {
                result.Append(" IS NULL");
            }
            else
            {
                result.Append(" IS NOT NULL");
            }

            return result;
        }

        /// <summary>
        /// This handles the processing of join expressions.
        /// The extents on a left spine are flattened, while joins
        /// not on the left spine give rise to new nested sub queries.
        ///
        /// Joins work differently from the rest of the visiting, in that
        /// the parent (i.e. the join node) creates the SqlSelectStatement
        /// for the children to use.
        ///
        /// The "parameter" IsInJoinContext indicates whether a child extent should
        /// add its stuff to the existing SqlSelectStatement, or create a new SqlSelectStatement
        /// By passing true, we ask the children to add themselves to the parent join,
        /// by passing false, we ask the children to create new Select statements for
        /// themselves.
        ///
        /// This method is called from <see cref="Visit(DbApplyExpression)"/> and
        /// <see cref="Visit(DbJoinExpression)"/>.
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="joinKind"></param>
        /// <param name="joinString"></param>
        /// <param name="joinCondition"></param>
        /// <returns> A <see cref="SqlSelectStatement"/></returns>
        ISqlFragment VisitJoinExpression(IList<DbExpressionBinding> inputs, DbExpressionKind joinKind,
            string joinString, DbExpression joinCondition)
        {
            SqlSelectStatement result;
            // If the parent is not a join( or says that it is not),
            // we should create a new SqlSelectStatement.
            // otherwise, we add our child extents to the parent's FROM clause.
            if (!IsParentAJoin)
            {
                result = new SqlSelectStatement();
                result.AllJoinExtents = new List<Symbol>(); 
                selectStatementStack.Push(result);
            }
            else
            {
                result = CurrentSelectStatement;
            }

            // Process each of the inputs, and then the joinCondition if it exists.
            // It would be nice if we could call VisitInputExpression - that would
            // avoid some code duplication
            // but the Join postprocessing is messy and prevents this reuse.
            symbolTable.EnterScope();

            string separator = "";
            bool isLeftMostInput = true;
            int inputCount = inputs.Count;

            result.From.Append("(");

            for (int idx = 0; idx < inputCount; idx++)
            {
                DbExpressionBinding input = inputs[idx];

                if (separator != "")
                {
                    result.From.AppendLine();
                }
                result.From.Append(separator + " ");
                // Change this if other conditions are required
                // to force the child to produce a nested SqlStatement.
                bool needsJoinContext = (input.Expression.ExpressionKind == DbExpressionKind.Scan)
                                        || (isLeftMostInput &&
                                            (IsJoinExpression(input.Expression)
                                             || IsApplyExpression(input.Expression)))
                                        ;

                isParentAJoinStack.Push(needsJoinContext ? true : false);
                // if the child reuses our select statement, it will append the from
                // symbols to our FromExtents list.  So, we need to remember the
                // start of the child's entries.
                int fromSymbolStart = result.FromExtents.Count;

                ISqlFragment fromExtentFragment = input.Expression.Accept(this);

                isParentAJoinStack.Pop();

                ProcessJoinInputResult(fromExtentFragment, result, input, fromSymbolStart);
                separator = joinString;

                isLeftMostInput = false;
            }

            // Visit the on clause/join condition.
            switch (joinKind)
            {
                case DbExpressionKind.FullOuterJoin:
                case DbExpressionKind.InnerJoin:
                case DbExpressionKind.LeftOuterJoin:
                    // Parenthesis to group ON clause and inversion of join condition added to fix test JoinErrorTest1
                    // It seems that Microsoft Access solves more join clauses if
                    // we insert parenthesis in the ON clause.
                    //    "<join> ON (<on_clause>)"
                    // instead of 
                    //    "<join> ON <on_clause>". 
                    result.From.Append(" ON (");
                    isParentAJoinStack.Push(false);
                    if (joinCondition is DbAndExpression)
                    {
                        // fix test JoinErrorTest1
                        result.From.Append(VisitBinaryExpression(" AND ", ((DbAndExpression)joinCondition).Right, ((DbAndExpression)joinCondition).Left));
                    }
                    else
                    {
                        result.From.Append(joinCondition.Accept(this));
                    }
                    result.From.Append(")");
                    isParentAJoinStack.Pop();
                    break;
            }

            symbolTable.ExitScope();

            if (!IsParentAJoin)
            {
                selectStatementStack.Pop();
            }

            result.From.Append(")");

            return result;
        }

        /// <summary>
        /// This is called from <see cref="VisitJoinExpression"/>.
        ///
        /// This is responsible for maintaining the symbol table after visiting
        /// a child of a join expression.
        ///
        /// The child's sql statement may need to be completed.
        ///
        /// The child's result could be one of
        /// <list type="number">
        /// <item>The same as the parent's - this is treated specially.</item>
        /// <item>A sql select statement, which may need to be completed</item>
        /// <item>An extent - just copy it to the from clause</item>
        /// <item>Anything else (from a collection-valued expression) -
        /// unnest and copy it.</item>
        /// </list>
        ///
        /// If the input was a Join, we need to create a new join symbol,
        /// otherwise, we create a normal symbol.
        ///
        /// We then call AddFromSymbol to add the AS clause, and update the symbol table.
        ///
        ///
        ///
        /// If the child's result was the same as the parent's, we have to clean up
        /// the list of symbols in the FromExtents list, since this contains symbols from
        /// the children of both the parent and the child.
        /// The happens when the child visited is a Join, and is the leftmost child of
        /// the parent.
        /// </summary>
        /// <param name="fromExtentFragment"></param>
        /// <param name="result"></param>
        /// <param name="input"></param>
        /// <param name="fromSymbolStart"></param>
        private void ProcessJoinInputResult(ISqlFragment fromExtentFragment, SqlSelectStatement result,
            DbExpressionBinding input, int fromSymbolStart)
        {
            Symbol fromSymbol = null;

            if (result != fromExtentFragment)
            {
                // The child has its own select statement, and is not reusing
                // our select statement.
                // This should look a lot like VisitInputExpression().
                SqlSelectStatement sqlSelectStatement = fromExtentFragment as SqlSelectStatement;
                if (sqlSelectStatement != null)
                {
                    if (sqlSelectStatement.Select.IsEmpty)
                    {
                        List<Symbol> columns = AddDefaultColumns(sqlSelectStatement);

                        if (IsJoinExpression(input.Expression)
                            || IsApplyExpression(input.Expression))
                        {
                            List<Symbol> extents = sqlSelectStatement.FromExtents;
                            JoinSymbol newJoinSymbol = new JoinSymbol(input.VariableName, input.VariableType, extents);
                            newJoinSymbol.IsNestedJoin = true;
                            newJoinSymbol.ColumnList = columns;

                            fromSymbol = newJoinSymbol;
                        }
                        else
                        {
                            // this is a copy of the code in CreateNewSelectStatement.

                            // if the oldStatement has a join as its input, ...
                            // clone the join symbol, so that we "reuse" the
                            // join symbol.  Normally, we create a new symbol - see the next block
                            // of code.
                            JoinSymbol oldJoinSymbol = sqlSelectStatement.FromExtents[0] as JoinSymbol;
                            if (oldJoinSymbol != null)
                            {
                                // Note: sqlSelectStatement.FromExtents will not do, since it might
                                // just be an alias of joinSymbol, and we want an actual JoinSymbol.
                                JoinSymbol newJoinSymbol = new JoinSymbol(input.VariableName, input.VariableType, oldJoinSymbol.ExtentList);
                                // This indicates that the sqlSelectStatement is a blocking scope
                                // i.e. it hides/renames extent columns
                                newJoinSymbol.IsNestedJoin = true;
                                newJoinSymbol.ColumnList = columns;
                                newJoinSymbol.FlattenedExtentList = oldJoinSymbol.FlattenedExtentList;

                                fromSymbol = newJoinSymbol;
                            }
                        }

                    }
                    result.From.Append(" (");
                    result.From.Append(sqlSelectStatement);
                    result.From.Append(" )");
                }
                else if (input.Expression is DbScanExpression)
                {
                    result.From.Append(fromExtentFragment);
                }
                else // bracket it
                {
                    WrapNonQueryExtent(result, fromExtentFragment, input.Expression.ExpressionKind);
                }

                if (fromSymbol == null) // i.e. not a join symbol
                {
                    fromSymbol = new Symbol(input.VariableName, input.VariableType);
                }


                AddFromSymbol(result, input.VariableName, fromSymbol);
                result.AllJoinExtents.Add(fromSymbol);
            }
            else // result == fromExtentFragment.  The child extents have been merged into the parent's.
            {
                // we are adding extents to the current sql statement via flattening.
                // We are replacing the child's extents with a single Join symbol.
                // The child's extents are all those following the index fromSymbolStart.
                //
                List<Symbol> extents = new List<Symbol>();

                // We cannot call extents.AddRange, since the is no simple way to
                // get the range of symbols fromSymbolStart..result.FromExtents.Count
                // from result.FromExtents.
                // We copy these symbols to create the JoinSymbol later.
                for (int i = fromSymbolStart; i < result.FromExtents.Count; ++i)
                {
                    extents.Add(result.FromExtents[i]);
                }
                result.FromExtents.RemoveRange(fromSymbolStart, result.FromExtents.Count - fromSymbolStart);
                fromSymbol = new JoinSymbol(input.VariableName, input.VariableType, extents);
                result.FromExtents.Add(fromSymbol);
                // this Join Symbol does not have its own select statement, so we
                // do not set IsNestedJoin


                // We do not call AddFromSymbol(), since we do not want to add
                // "AS alias" to the FROM clause- it has been done when the extent was added earlier.
                symbolTable.Add(input.VariableName, fromSymbol);
            }
        }

        /// <summary>
        /// We assume that this is only called as a child of a Project.
        ///
        /// This replaces <see cref="Visit(DbNewInstanceExpression)"/>, since
        /// we do not allow DbNewInstanceExpression as a child of any node other than
        /// DbProjectExpression.
        ///
        /// We write out the translation of each of the columns in the record.
        /// </summary>
        /// <param name="e"></param>

        /// <returns>A <see cref="SqlBuilder"/></returns>
        ISqlFragment VisitNewInstanceExpression(DbNewInstanceExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            RowType rowType = e.ResultType.EdmType as RowType;

            if (rowType != null)
            {
                ReadOnlyMetadataCollection<EdmProperty> members = rowType.Properties;
                string separator = "";
                for (int i = 0; i < e.Arguments.Count; ++i)
                {
                    DbExpression argument = e.Arguments[i];
                    if (MetadataHelpers.IsRowType(argument.ResultType))
                    {
                        // We do not support nested records or other complex objects.
                        throw new NotSupportedException();
                    }

                    EdmProperty member = members[i];
                    result.Append(separator);
                    result.AppendLine();
                    result.Append(argument.Accept(this));
                    result.Append(" AS ");
                    result.Append(JetProviderManifest.QuoteIdentifier(member.Name));
                    separator = ", ";
                }
            }
            else
            {
                //
                // Types other then RowType (such as UDTs for instance) are not supported.
                //
                throw new NotSupportedException();
            }

            return result;
        }

        private ISqlFragment VisitSetOpExpression(DbExpression left, DbExpression right, string separator)
        {

            SqlSelectStatement leftSelectStatement = VisitExpressionEnsureSqlStatement(left);
            SqlSelectStatement rightSelectStatement = VisitExpressionEnsureSqlStatement(right);

            SqlBuilder setStatement = new SqlBuilder();
            setStatement.Append(leftSelectStatement);
            setStatement.AppendLine();
            setStatement.Append(separator); // e.g. UNION ALL
            setStatement.AppendLine();
            setStatement.Append(rightSelectStatement);

            return setStatement;
        }


        #endregion


        #region Function Handling Helpers
        /// <summary>
        /// Determines whether the given function is a built-in function that requires special handling
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool IsSpecialBuiltInFunction(DbFunctionExpression e)
        {
            return IsBuiltinFunction(e.Function) && _specialBuiltInFunctionHandlers.ContainsKey(e.Function.Name);
        }

        /// <summary>
        /// Determines whether the given function is a canonical function that requires special handling
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool IsSpecialCanonicalFunction(DbFunctionExpression e)
        {
            return MetadataHelpers.IsCanonicalFunction(e.Function) && _canonicalFunctionHandlers.ContainsKey(e.Function.Name);
        }

        /// <summary>
        /// Default handling for functions
        /// Translates them to FunctionName(arg1, arg2, ..., argn)
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment HandleFunctionDefault(DbFunctionExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            WriteFunctionName(result, e.Function);
            HandleFunctionArgumentsDefault(e, result);
            return result;
        }

        /// <summary>
        /// Default handling for functions with a given name.
        /// Translates them to functionName(arg1, arg2, ..., argn)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="functionName"></param>
        /// <returns></returns>
        private ISqlFragment HandleFunctionDefaultGivenName(DbFunctionExpression e, string functionName)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append(functionName);
            HandleFunctionArgumentsDefault(e, result);
            return result;
        }

        /// <summary>
        /// Default handling on function arguments
        /// Appends the list of arguments to the given result
        /// If the function is niladic it does not append anything,
        /// otherwise it appends (arg1, arg2, ..., argn)
        /// </summary>
        /// <param name="e"></param>
        /// <param name="result"></param>
        private void HandleFunctionArgumentsDefault(DbFunctionExpression e, SqlBuilder result)
        {
            bool isNiladicFunction = MetadataHelpers.TryGetValueForMetadataProperty<bool>(e.Function, "NiladicFunctionAttribute");
            if (isNiladicFunction && e.Arguments.Count > 0)
            {
                throw new InvalidOperationException("Niladic functions cannot have parameters");
            }

            if (!isNiladicFunction)
            {
                WriteFunctionArguments(e.Arguments, result);
            }
        }

        private void WriteFunctionArguments(IEnumerable<DbExpression> functionArguments, SqlBuilder result)
        {
            result.Append("(");
            bool first = true;
            foreach (DbExpression arg in functionArguments)
            {
                if (first)
                    first = false;
                else
                    result.Append(", ");
                result.Append(arg.Accept(this));
            }
            result.Append(")");
        }


        /// <summary>
        /// Handler for special built in functions
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment HandleSpecialBuiltInFunction(DbFunctionExpression e)
        {
            return HandleSpecialFunction(_specialBuiltInFunctionHandlers, e);
        }

        /// <summary>
        /// Handler for special canonical functions
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment HandleSpecialCanonicalFunction(DbFunctionExpression e)
        {
            return HandleSpecialFunction(_canonicalFunctionHandlers, e);
        }

        /// <summary>
        /// Dispatches the special function processing to the appropriate handler
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment HandleSpecialFunction(Dictionary<string, FunctionHandler> handlers, DbFunctionExpression e)
        {
            if (!handlers.ContainsKey(e.Function.Name))
                throw new InvalidOperationException("Special handling should be called only for functions in the list of special functions");

            return handlers[e.Function.Name](this, e);
        }

        /// <summary>
        /// Handles functions that are translated into SQL operators.
        /// The given function should have one or two arguments. 
        /// Functions with one arguemnt are translated into 
        ///     op arg
        /// Functions with two arguments are translated into
        ///     arg0 op arg1
        /// Also, the arguments can be optionaly enclosed in parethesis
        /// </summary>
        /// <param name="e"></param>
        /// <param name="parenthesiseArguments">Whether the arguments should be enclosed in parethesis</param>
        /// <returns></returns>
        private ISqlFragment HandleSpecialFunctionToOperator(DbFunctionExpression e, bool parenthesiseArguments)
        {
            SqlBuilder result = new SqlBuilder();

            if (e.Arguments.Count == 0 || e.Arguments.Count > 2)
                throw new ArgumentException("There should be 1 or 2 arguments for operator");

            if (e.Arguments.Count > 1)
            {
                if (parenthesiseArguments)
                {
                    result.Append("(");
                }
                result.Append(e.Arguments[0].Accept(this));
                if (parenthesiseArguments)
                {
                    result.Append(")");
                }
            }
            result.Append(" ");

            if (!_functionNameToOperatorDictionary.ContainsKey(e.Function.Name))
                throw new ArgumentException("The function can not be mapped to an operator");

            result.Append(_functionNameToOperatorDictionary[e.Function.Name]);
            result.Append(" ");

            if (parenthesiseArguments)
            {
                result.Append("(");
            }
            result.Append(e.Arguments[e.Arguments.Count - 1].Accept(this));
            if (parenthesiseArguments)
            {
                result.Append(")");
            }
            return result;
        }

        /// <summary>
        /// <see cref="HandleSpecialFunctionToOperator"></see>
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleConcatFunction(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleSpecialFunctionToOperator(e, false);
        }

        /// <summary>
        /// <see cref="HandleSpecialFunctionToOperator"></see>
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionBitwise(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            throw new NotSupportedException("Bitwise logical operations are not supported by Jet");
        }

        /// <summary>
        /// Handles special case in which datapart 'type' parameter is present. all the functions
        /// handles here have *only* the 1st parameter as datepart. datepart value is passed along
        /// the QP as string and has to be expanded as Jet keyword.
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleDatepartDateFunction(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            Debug.Assert(e.Arguments.Count > 0, "e.Arguments.Count > 0");

            DbConstantExpression constExpr = e.Arguments[0] as DbConstantExpression;
            if (constExpr == null)
                throw new InvalidOperationException(String.Format("DATEPART argument to function '{0}.{1}' must be a literal string", e.Function.NamespaceName, e.Function.Name));

            string datepart = constExpr.Value as string;
            if (datepart == null)
                throw new InvalidOperationException(String.Format("DATEPART argument to function '{0}.{1}' must be a literal string", e.Function.NamespaceName, e.Function.Name));

            SqlBuilder result = new SqlBuilder();

            //
            // check if datepart value is valid
            //
            if (!_datepartKeywords.Contains(datepart))
                throw new InvalidOperationException(String.Format("{0}' is not a valid value for DATEPART argument in '{1}.{2}' function", datepart, e.Function.NamespaceName, e.Function.Name));

            //
            // finaly, expand the function name
            //
            sqlgen.WriteFunctionName(result, e.Function);
            result.Append("(");

            // expand the datepart literal as tsql kword
            result.Append(datepart);
            string separator = ", ";

            // expand remaining arguments
            for (int i = 1; i < e.Arguments.Count; i++)
            {
                result.Append(separator);
                result.Append(e.Arguments[i].Accept(sqlgen));
            }

            result.Append(")");

            return result;
        }


        /// <summary>
        /// Handles the function that requires a cast to an int as return type.
        /// Jet often returns a Jet int (16 bits int) while .Net requires a 32 bits int (long int in Jet).
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns>The Sql fragment containing the cast</returns>
        private static ISqlFragment HandleFunctionDefaultCastToInt(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append("CLng(");
            result.Append(sqlgen.HandleFunctionDefault(e));
            result.Append(")");

            return result;
        }

        private static ISqlFragment HandleCanonicalFunctionCurrentDateTime(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleFunctionDefaultGivenName(e, "Now");
        }

        /// <summary>
        /// See <see cref="HandleCanonicalFunctionDateTimeTypeCreation"/> for exact translation
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionCreateDateTime(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            string typeName = "datetime";
            return sqlgen.HandleCanonicalFunctionDateTimeTypeCreation(typeName, e.Arguments, true, false);
        }


        /// <summary>
        /// Creates a datetime
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionCreateTime(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleCanonicalFunctionDateTimeTypeCreation("datetime", e.Arguments, false, false);
        }


        /// <summary>
        /// Truncates time from a datetime value
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static ISqlFragment HandleCanonicalFunctionTruncateTime(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append("CDate(Int(");
            sqlgen.HandleFunctionArgumentsDefault(e, result);
            result.Append("))");

            return result;
        }



        /// <summary>
        /// Dump out an expression - optionally wrap it with parantheses if possible
        /// </summary>
        /// <param name="e"></param>
        /// <param name="result"></param>
        private void ParanthesizeExpressionIfNeeded(DbExpression e, SqlBuilder result)
        {
            if (IsComplexExpression(e))
            {
                result.Append("(");
                result.Append(e.Accept(this));
                result.Append(")");
            }
            else
            {
                result.Append(e.Accept(this));
            }
        }

        /// <summary>
        /// Helper for all date and time types creating functions.
        /// The given expression is in general translated into:
        /// CDate(DateSerial(.) + TimeSerial(.)
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <param name="args">The arguments.</param>
        /// <param name="hasDatePart">if set to <c>true</c> the args has the date part.</param>
        /// <param name="hasTimeZonePart">if set to <c>true</c> the args has the time zone part. Actually is ignored</param>
        /// <returns>A sql fragment</returns>
        private ISqlFragment HandleCanonicalFunctionDateTimeTypeCreation(string typeName, IList<DbExpression> args, bool hasDatePart, bool hasTimeZonePart)
        {
            if (args.Count != (hasDatePart ? 3 : 0) + 3 + (hasTimeZonePart ? 1 : 0))
                throw new ArgumentException("Invalid number of parameters for a date time creating function");



            SqlBuilder result = new SqlBuilder();
            int currentArgumentIndex = 0;

            result.Append("CDate(");

            //Building the string representation
            if (hasDatePart)
            {
                result.Append("DateSerial(");
                result.Append(args[currentArgumentIndex++].Accept(this));
                result.Append(", ");
                result.Append(args[currentArgumentIndex++].Accept(this));
                result.Append(", ");
                result.Append(args[currentArgumentIndex++].Accept(this));
                result.Append(") + ");
            }

            result.Append("TimeSerial(");
            result.Append(args[currentArgumentIndex++].Accept(this));
            result.Append(", ");
            result.Append(args[currentArgumentIndex++].Accept(this));
            result.Append(", ");
            result.Append(args[currentArgumentIndex++].Accept(this));
            result.Append(")");

            result.Append(")");

            //  TZOFFSET
            if (hasTimeZonePart)
            {
                // In this case nothing to do.
                // We don't also need to increment currentArgumentIndex
            }

            return result;
        }

        /// <summary>
        /// Handler for all date/time addition canonical functions.
        /// Translation, e.g.
        /// AddYears(datetime, number) =>  DateAdd(year, number, datetime)
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionDateAdd(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            SqlBuilder result = new SqlBuilder();

            result.Append("DateAdd(");
            result.Append("\"");
            result.Append(_dateAddFunctionNameToDatepartDictionary[e.Function.Name]);
            result.Append("\", ");
            result.Append(e.Arguments[1].Accept(sqlgen));
            result.Append(", ");
            result.Append(e.Arguments[0].Accept(sqlgen));
            result.Append(") ");

            return result;
        }

        /// <summary>
        /// Handler for all date/time addition canonical functions.
        /// Translation, e.g.
        /// DiffYears(datetime, number) =>  DATEDIFF(year, datetime, datetime)
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionDateDiff(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            SqlBuilder result = new SqlBuilder();

            result.Append("DateDiff(");
            result.Append("\"");
            result.Append(_dateDiffFunctionNameToDatepartDictionary[e.Function.Name]);
            result.Append("\", ");
            result.Append(e.Arguments[1].Accept(sqlgen));
            result.Append(", ");
            result.Append(e.Arguments[0].Accept(sqlgen));
            result.Append(") ");

            return result;
        }

        /// <summary>
        ///  Function rename a.IndexOf(b) -> Instr(a, b)
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionIndexOf(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append("Instr(");
            result.Append(e.Arguments[1].Accept(sqlgen));
            result.Append(", ");
            result.Append(e.Arguments[0].Accept(sqlgen));
            result.Append(") ");

            return result;
        }

        /// <summary>
        ///  Function rename Length -> Len
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionSubstring(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleFunctionDefaultGivenName(e, "Mid");
        }


        /// <summary>
        ///  Function rename Length -> Len
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionLength(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleFunctionDefaultGivenName(e, "Len");
        }


        /// <summary>
        /// Truncate(numericExpression, digits) -> Round(numericExpression, digits, 1);
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionTruncate(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleFunctionDefaultGivenName(e, "Round");
        }

        /// <summary>
        /// Handle the canonical function Abs(). 
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionAbs(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            // Convert the call to Abs(Byte) to a no-op, since Byte is an unsigned type. 
            if (e.Arguments[0].ResultType.IsPrimitiveTypeOf(PrimitiveTypeKind.Byte))
            {
                SqlBuilder result = new SqlBuilder();
                result.Append(e.Arguments[0].Accept(sqlgen));
                return result;
            }
            else
            {
                return sqlgen.HandleFunctionDefault(e);
            }
        }

        /// <summary>
        ///  Function rename Reverse -> StrReverse
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionReverse(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            throw new NotSupportedException("Reverse is not supported in Jet");
            //return sqlgen.HandleFunctionDefaultGivenName(e, "StrReverse"); // Actually this in ADO does not work
        }


        /// <summary>
        ///  Function rename ToLower -> LCase
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionToLower(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleFunctionDefaultGivenName(e, "LCase");
        }

        /// <summary>
        ///  Function rename ToUpper -> UCase
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionToUpper(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.HandleFunctionDefaultGivenName(e, "UCase");
        }
        /// <summary>
        /// Function to translate the StartsWith, EndsWith and Contains canonical functions to LIKE expression in SQL
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="targetExpression"></param>
        /// <param name="constSearchParamExpression"></param>
        /// <param name="result"></param>
        /// <param name="insertPercentStart"></param>
        /// <param name="insertPercentEnd"></param>
        private void TranslateConstantParameterForLike(DbExpression targetExpression, DbConstantExpression constSearchParamExpression, SqlBuilder result, bool insertPercentStart, bool insertPercentEnd)
        {
            result.Append(targetExpression.Accept(this));
            result.Append(" LIKE ");

            // If it's a DbConstantExpression then escape the search parameter if necessary.
            bool escapingOccurred;

            StringBuilder searchParamBuilder = new StringBuilder();
            if (insertPercentStart == true)
                searchParamBuilder.Append("%");
            searchParamBuilder.Append(JetProviderManifest.EscapeLikeText(constSearchParamExpression.Value as string, out escapingOccurred));
            if (insertPercentEnd == true)
                searchParamBuilder.Append("%");

            result.Append(VisitConstantExpression(constSearchParamExpression.ResultType, searchParamBuilder.ToString()));

        }


        public override ISqlFragment Visit(DbInExpression expression)
        {
            Debug.Assert(expression.List.Count > 0);
            SqlBuilder result = new SqlBuilder();
            result.Append(expression.Item.Accept(this));
            result.Append(" IN (");

            bool first = true;
            foreach (DbExpression listItem in expression.List)
            {
                if (first)
                    first = false;
                else
                    result.Append(", ");
                result.Append(listItem.Accept(this));
            }
            result.Append(")");


            return result;
        }

        /// <summary>
        /// Handler for Contains. Wraps the normal translation with a case statement
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionContains(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.CastPredicateToBool(HandleCanonicalFunctionContains, e);
        }

        /// <summary>
        /// CONTAINS(arg0, arg1) => arg0 LIKE '%arg1%'
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static SqlBuilder HandleCanonicalFunctionContains(SqlGenerator sqlgen, IList<DbExpression> args, SqlBuilder result)
        {
            Debug.Assert(args.Count == 2, "Contains should have two arguments");
            // Check if args[1] is a DbConstantExpression
            DbConstantExpression constSearchParamExpression = args[1] as DbConstantExpression;
            if ((constSearchParamExpression != null) && !string.IsNullOrEmpty(constSearchParamExpression.Value as string))
            {
                sqlgen.TranslateConstantParameterForLike(args[0], constSearchParamExpression, result, true, true);
            }
            else
            {
                // We use CHARINDEX when the search param is a DbNullExpression because all of SQL Server 2008, 2005 and 2000
                // consistently return NULL as the result.
                //  However, if instead we use the optimized LIKE translation when the search param is a DbNullExpression,
                //  only SQL Server 2005 yields a True instead of a DbNull as compared to SQL Server 2008 and 2000.
                result.Append("Instr( ");
                result.Append(args[1].Accept(sqlgen));
                result.Append(", ");
                result.Append(args[0].Accept(sqlgen));
                result.Append(") > 0");
            }
            return result;
        }

        /// <summary>
        /// Handler for StartsWith. Wraps the normal translation with a case statement
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionStartsWith(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.CastPredicateToBool(HandleCanonicalFunctionStartsWith, e);
        }

        /// <summary>
        /// StartsWith(arg0, arg1) => arg0 LIKE 'arg1*'
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static SqlBuilder HandleCanonicalFunctionStartsWith(SqlGenerator sqlgen, IList<DbExpression> args, SqlBuilder result)
        {
            Debug.Assert(args.Count == 2, "StartsWith should have two arguments");
            // Check if args[1] is a DbConstantExpression
            DbConstantExpression constSearchParamExpression = args[1] as DbConstantExpression;
            if ((constSearchParamExpression != null) && (string.IsNullOrEmpty(constSearchParamExpression.Value as string) == false))
            {
                sqlgen.TranslateConstantParameterForLike(args[0], constSearchParamExpression, result, false, true);
            }
            else
            {
                // We use CHARINDEX when the search param is a DbNullExpression because all of SQL Server 2008, 2005 and 2000
                // consistently return NULL as the result.
                //      However, if instead we use the optimized LIKE translation when the search param is a DbNullExpression,
                //      only SQL Server 2005 yields a True instead of a DbNull as compared to SQL Server 2008 and 2000. This is
                //      bug 32315 in LIKE in SQL Server 2005.
                result.Append("Instr( ");
                result.Append(args[1].Accept(sqlgen));
                result.Append(", ");
                result.Append(args[0].Accept(sqlgen));
                result.Append(") = 1");
            }

            return result;
        }

        /// <summary>
        /// Handler for EndsWith. Wraps the normal translation with a case statement
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private static ISqlFragment HandleCanonicalFunctionEndsWith(SqlGenerator sqlgen, DbFunctionExpression e)
        {
            return sqlgen.CastPredicateToBool(HandleCanonicalFunctionEndsWith, e);
        }

        /// <summary>
        /// ENDSWITH(arg0, arg1) => arg0 LIKE '*arg1'
        /// </summary>
        /// <param name="sqlgen"></param>
        /// <param name="args"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static SqlBuilder HandleCanonicalFunctionEndsWith(SqlGenerator sqlgen, IList<DbExpression> args, SqlBuilder result)
        {
            Debug.Assert(args.Count == 2, "EndsWith should have two arguments");

            // Check if args[1] is a DbConstantExpression and if args [0] is a DbPropertyExpression
            DbConstantExpression constSearchParamExpression = args[1] as DbConstantExpression;
            DbPropertyExpression targetParamExpression = args[0] as DbPropertyExpression;
            if ((constSearchParamExpression != null) && (targetParamExpression != null) && (string.IsNullOrEmpty(constSearchParamExpression.Value as string) == false))
            {
                // The LIKE optimization for EndsWith can only be used when the target is a column in table and
                // the search string is a constant. This is because SQL Server ignores a trailing space in a query like:
                // EndsWith('abcd ', 'cd'), which translates to:
                //      SELECT
                //      CASE WHEN ('abcd ' LIKE '%cd') THEN cast(1 as bit) WHEN ( NOT ('abcd ' LIKE '%cd')) THEN cast(0 as bit) END AS [C1]
                //      FROM ( SELECT 1 AS X ) AS [SingleRowTable1]
                // and "incorrectly" returns 1 (true), but the CLR would expect a 0 (false) back.

                sqlgen.TranslateConstantParameterForLike(args[0], constSearchParamExpression, result, true, false);
            }
            else
            {
                result.Append("CHARINDEX( REVERSE(");
                result.Append(args[1].Accept(sqlgen));
                result.Append("), REVERSE(");
                result.Append(args[0].Accept(sqlgen));
                result.Append(")) = 1");
            }
            return result;
        }

        /// <summary>
        /// Turns a predicate into a statement returning a bit
        /// PREDICATE => CASE WHEN (PREDICATE) THEN CAST(1 AS BIT) WHEN (NOT (PREDICATE)) CAST (O AS BIT) END
        /// The predicate is produced by the given predicateTranslator.
        /// </summary>
        /// <param name="predicateTranslator"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private ISqlFragment CastPredicateToBool(Func<SqlGenerator, IList<DbExpression>, SqlBuilder, SqlBuilder> predicateTranslator, DbFunctionExpression e)
        {
            SqlBuilder result = new SqlBuilder();
            result.Append("CBool(");
            predicateTranslator(this, e.Arguments, result);
            result.Append(")");
            return result;
        }
        #endregion


        #endregion

        #region Helper methods for the DbExpressionVisitor
        /// <summary>
        /// <see cref="AddDefaultColumns"/>
        /// Add the column names from the referenced extent/join to the
        /// select statement.
        ///
        /// If the symbol is a JoinSymbol, we recursively visit all the extents,
        /// halting at real extents and JoinSymbols that have an associated SqlSelectStatement.
        ///
        /// The column names for a real extent can be derived from its type.
        /// The column names for a Join Select statement can be got from the
        /// list of columns that was created when the Join's select statement
        /// was created.
        ///
        /// We do the following for each column.
        /// <list type="number">
        /// <item>Add the SQL string for each column to the SELECT clause</item>
        /// <item>Add the column to the list of columns - so that it can
        /// become part of the "type" of a JoinSymbol</item>
        /// <item>Check if the column name collides with a previous column added
        /// to the same select statement.  Flag both the columns for renaming if true.</item>
        /// <item>Add the column to a name lookup dictionary for collision detection.</item>
        /// </list>
        /// </summary>
        /// <param name="selectStatement">The select statement that started off as SELECT *</param>
        /// <param name="symbol">The symbol containing the type information for
        /// the columns to be added.</param>
        /// <param name="columnList">Columns that have been added to the Select statement.
        /// This is created in <see cref="AddDefaultColumns"/>.</param>
        /// <param name="columnDictionary">A dictionary of the columns above.</param>
        /// <param name="separator">Comma or nothing, depending on whether the SELECT
        /// clause is empty.</param>
        private void AddColumns(SqlSelectStatement selectStatement, Symbol symbol,
            List<Symbol> columnList, Dictionary<string, Symbol> columnDictionary, ref string separator)
        {
            JoinSymbol joinSymbol = symbol as JoinSymbol;
            if (joinSymbol != null)
            {
                if (!joinSymbol.IsNestedJoin)
                {
                    // Recurse if the join symbol is a collection of flattened extents
                    foreach (Symbol sym in joinSymbol.ExtentList)
                    {
                        // if sym is ScalarType means we are at base case in the
                        // recursion and there are not columns to add, just skip
                        if (MetadataHelpers.IsPrimitiveType(sym.Type))
                        {
                            continue;
                        }

                        AddColumns(selectStatement, sym, columnList, columnDictionary, ref separator);
                    }
                }
                else
                {
                    foreach (Symbol joinColumn in joinSymbol.ColumnList)
                    {
                        // we write tableName.columnName
                        // rather than tableName.columnName as alias
                        // since the column name is unique (by the way we generate new column names)
                        //
                        // We use the symbols for both the table and the column,
                        // since they are subject to renaming.
                        selectStatement.Select.Append(separator);
                        selectStatement.Select.Append(symbol);
                        selectStatement.Select.Append(".");
                        selectStatement.Select.Append(joinColumn);

                        // check for name collisions.  If there is,
                        // flag both the colliding symbols.
                        if (columnDictionary.ContainsKey(joinColumn.Name))
                        {
                            columnDictionary[joinColumn.Name].NeedsRenaming = true; // the original symbol
                            joinColumn.NeedsRenaming = true; // the current symbol.
                        }
                        else
                        {
                            columnDictionary[joinColumn.Name] = joinColumn;
                        }

                        columnList.Add(joinColumn);

                        separator = ", ";
                    }
                }
            }
            else
            {
                // This is a non-join extent/select statement, and the CQT type has
                // the relevant column information.

                // The type could be a record type(e.g. Project(...),
                // or an entity type ( e.g. EntityExpression(...)
                // so, we check whether it is a structuralType.

                // Consider an expression of the form J(a, b=P(E))
                // The inner P(E) would have been translated to a SQL statement
                // We should not use the raw names from the type, but the equivalent
                // symbols (they are present in symbol.Columns) if they exist.
                //
                // We add the new columns to the symbol's columns if they do
                // not already exist.
                //

                foreach (EdmProperty property in MetadataHelpers.GetProperties(symbol.Type))
                {
                    string recordMemberName = property.Name;
                    // Since all renaming happens in the second phase
                    // we lose nothing by setting the next column name index to 0
                    // many times.
                    allColumnNames[recordMemberName] = 0;

                    // Create a new symbol/reuse existing symbol for the column
                    Symbol columnSymbol;
                    if (!symbol.Columns.TryGetValue(recordMemberName, out columnSymbol))
                    {
                        // we do not care about the types of columns, so we pass null
                        // when construction the symbol.
                        columnSymbol = new Symbol(recordMemberName, null);
                        symbol.Columns.Add(recordMemberName, columnSymbol);
                    }

                    selectStatement.Select.Append(separator);
                    selectStatement.Select.Append(symbol);
                    selectStatement.Select.Append(".");

                    // We use the actual name before the "AS", the new name goes
                    // after the AS.
                    selectStatement.Select.Append(JetProviderManifest.QuoteIdentifier(recordMemberName));

                    selectStatement.Select.Append(" AS ");
                    selectStatement.Select.Append(columnSymbol);

                    // Check for column name collisions.
                    if (columnDictionary.ContainsKey(recordMemberName))
                    {
                        columnDictionary[recordMemberName].NeedsRenaming = true;
                        columnSymbol.NeedsRenaming = true;
                    }
                    else
                    {
                        columnDictionary[recordMemberName] = symbol.Columns[recordMemberName];
                    }

                    columnList.Add(columnSymbol);

                    separator = ", ";
                }
            }
        }

        /// <summary>
        /// Expands Select * to "select the_list_of_columns"
        /// If the columns are taken from an extent, they are written as
        /// {original_column_name AS Symbol(original_column)} to allow renaming.
        ///
        /// If the columns are taken from a Join, they are written as just
        /// {original_column_name}, since there cannot be a name collision.
        ///
        /// We concatenate the columns from each of the inputs to the select statement.
        /// Since the inputs may be joins that are flattened, we need to recurse.
        /// The inputs are inferred from the symbols in FromExtents.
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <returns></returns>
        private List<Symbol> AddDefaultColumns(SqlSelectStatement selectStatement)
        {
            // This is the list of columns added in this select statement
            // This forms the "type" of the Select statement, if it has to
            // be expanded in another SELECT *
            List<Symbol> columnList = new List<Symbol>();

            // A lookup for the previous set of columns to aid column name
            // collision detection.
            Dictionary<string, Symbol> columnDictionary = new Dictionary<string, Symbol>(StringComparer.OrdinalIgnoreCase);

            string separator = "";
            // The Select should usually be empty before we are called,
            // but we do not mind if it is not.
            if (!selectStatement.Select.IsEmpty)
            {
                separator = ", ";
            }

            foreach (Symbol symbol in selectStatement.FromExtents)
            {
                AddColumns(selectStatement, symbol, columnList, columnDictionary, ref separator);
            }

            return columnList;
        }

        /// <summary>
        /// <see cref="AddFromSymbol(SqlSelectStatement, string, Symbol, bool)"/>
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <param name="inputVarName"></param>
        /// <param name="fromSymbol"></param>
        private void AddFromSymbol(SqlSelectStatement selectStatement, string inputVarName, Symbol fromSymbol)
        {
            AddFromSymbol(selectStatement, inputVarName, fromSymbol, true);
        }

        /// <summary>
        /// This method is called after the input to a relational node is visited.
        /// <see cref="Visit(DbProjectExpression)"/> and <see cref="ProcessJoinInputResult"/>
        /// There are 2 scenarios
        /// <list type="number">
        /// <item>The fromSymbol is new i.e. the select statement has just been
        /// created, or a join extent has been added.</item>
        /// <item>The fromSymbol is old i.e. we are reusing a select statement.</item>
        /// </list>
        ///
        /// If we are not reusing the select statement, we have to complete the
        /// FROM clause with the alias
        /// <code>
        /// -- if the input was an extent
        /// FROM = [SchemaName].[TableName]
        /// -- if the input was a Project
        /// FROM = (SELECT ... FROM ... WHERE ...)
        /// </code>
        ///
        /// These become
        /// <code>
        /// -- if the input was an extent
        /// FROM = [SchemaName].[TableName] AS alias
        /// -- if the input was a Project
        /// FROM = (SELECT ... FROM ... WHERE ...) AS alias
        /// </code>
        /// and look like valid FROM clauses.
        ///
        /// Finally, we have to add the alias to the global list of aliases used,
        /// and also to the current symbol table.
        /// </summary>
        /// <param name="selectStatement"></param>
        /// <param name="inputVarName">The alias to be used.</param>
        /// <param name="fromSymbol"></param>
        /// <param name="addToSymbolTable"></param>
        private void AddFromSymbol(SqlSelectStatement selectStatement, string inputVarName, Symbol fromSymbol, bool addToSymbolTable)
        {
            // the first check is true if this is a new statement
            // the second check is true if we are in a join - we do not
            // check if we are in a join context.
            // We do not want to add "AS alias" if it has been done already
            // e.g. when we are reusing the Sql statement.
            if (selectStatement.FromExtents.Count == 0 || fromSymbol != selectStatement.FromExtents[0])
            {
                selectStatement.FromExtents.Add(fromSymbol);
                selectStatement.From.Append(" AS ");
                selectStatement.From.Append(fromSymbol);

                // We have this inside the if statement, since
                // we only want to add extents that are actually used.
                allExtentNames[fromSymbol.Name] = 0;
            }

            if (addToSymbolTable)
            {
                symbolTable.Add(inputVarName, fromSymbol);
            }
        }

        /// <summary>
        /// Translates a list of SortClauses.
        /// Used in the translation of OrderBy 
        /// </summary>
        /// <param name="orderByClause">The SqlBuilder to which the sort keys should be appended</param>
        /// <param name="sortKeys"></param>
        private void AddSortKeys(SqlBuilder orderByClause, IList<DbSortClause> sortKeys)
        {
            string separator = "";
            foreach (DbSortClause sortClause in sortKeys)
            {
                orderByClause.Append(separator);
                orderByClause.Append(sortClause.Expression.Accept(this));
                Debug.Assert(sortClause.Collation != null);
                if (!String.IsNullOrEmpty(sortClause.Collation))
                {
                    orderByClause.Append(" COLLATE ");
                    orderByClause.Append(sortClause.Collation);
                }

                orderByClause.Append(sortClause.Ascending ? " ASC" : " DESC");

                separator = ", ";
            }
        }

        /// <summary>
        /// <see cref="CreateNewSelectStatement(SqlSelectStatement oldStatement, string inputVarName, TypeUsage inputVarType, bool finalizeOldStatement, out Symbol fromSymbol) "/>
        /// </summary>
        /// <param name="oldStatement"></param>
        /// <param name="inputVarName"></param>
        /// <param name="inputVarType"></param>
        /// <param name="fromSymbol"></param>
        /// <returns>A new select statement, with the old one as the from clause.</returns>
        private SqlSelectStatement CreateNewSelectStatement(SqlSelectStatement oldStatement,
            string inputVarName, TypeUsage inputVarType, out Symbol fromSymbol)
        {
            return CreateNewSelectStatement(oldStatement, inputVarName, inputVarType, true, out fromSymbol);
        }


        /// <summary>
        /// This is called after a relational node's input has been visited, and the
        /// input's sql statement cannot be reused.  <see cref="Visit(DbProjectExpression)"/>
        ///
        /// When the input's sql statement cannot be reused, we create a new sql
        /// statement, with the old one as the from clause of the new statement.
        ///
        /// The old statement must be completed i.e. if it has an empty select list,
        /// the list of columns must be projected out.
        ///
        /// If the old statement being completed has a join symbol as its from extent,
        /// the new statement must have a clone of the join symbol as its extent.
        /// We cannot reuse the old symbol, but the new select statement must behave
        /// as though it is working over the "join" record.
        /// </summary>
        /// <param name="oldStatement"></param>
        /// <param name="inputVarName"></param>
        /// <param name="inputVarType"></param>
        /// <param name="finalizeOldStatement"></param>
        /// <param name="fromSymbol"></param>
        /// <returns>A new select statement, with the old one as the from clause.</returns>
        private SqlSelectStatement CreateNewSelectStatement(SqlSelectStatement oldStatement,
            string inputVarName, TypeUsage inputVarType, bool finalizeOldStatement, out Symbol fromSymbol)
        {
            fromSymbol = null;

            // Finalize the old statement
            if (finalizeOldStatement && oldStatement.Select.IsEmpty)
            {
                List<Symbol> columns = AddDefaultColumns(oldStatement);

                // Thid could not have been called from a join node.
                Debug.Assert(oldStatement.FromExtents.Count == 1);

                // if the oldStatement has a join as its input, ...
                // clone the join symbol, so that we "reuse" the
                // join symbol.  Normally, we create a new symbol - see the next block
                // of code.
                JoinSymbol oldJoinSymbol = oldStatement.FromExtents[0] as JoinSymbol;
                if (oldJoinSymbol != null)
                {
                    // Note: oldStatement.FromExtents will not do, since it might
                    // just be an alias of joinSymbol, and we want an actual JoinSymbol.
                    JoinSymbol newJoinSymbol = new JoinSymbol(inputVarName, inputVarType, oldJoinSymbol.ExtentList);
                    // This indicates that the oldStatement is a blocking scope
                    // i.e. it hides/renames extent columns
                    newJoinSymbol.IsNestedJoin = true;
                    newJoinSymbol.ColumnList = columns;
                    newJoinSymbol.FlattenedExtentList = oldJoinSymbol.FlattenedExtentList;

                    fromSymbol = newJoinSymbol;
                }
            }

            if (fromSymbol == null)
            {
                // This is just a simple extent/SqlSelectStatement,
                // and we can get the column list from the type.
                fromSymbol = new Symbol(inputVarName, inputVarType);
            }

            // Observe that the following looks like the body of Visit(ExtentExpression).
            SqlSelectStatement selectStatement = new SqlSelectStatement();
            selectStatement.From.Append("( ");
            selectStatement.From.Append(oldStatement);
            selectStatement.From.AppendLine();
            selectStatement.From.Append(") ");

            return selectStatement;
        }

        /// <summary>
        /// Returns the sql primitive/native type name. 
        /// It will include size, precision or scale depending on type information present in the 
        /// type facets
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private string GetSqlPrimitiveType(TypeUsage type)
        {
            PrimitiveType primitiveType = MetadataHelpers.GetEdmType<PrimitiveType>(type);

            string typeName = primitiveType.Name;
            bool isUnicode = true;
            bool isFixedLength = false;
            int maxLength = 0;
            string length = "max";
            bool preserveSeconds = true;
            byte decimalPrecision = 0;
            byte decimalScale = 0;

            switch (primitiveType.PrimitiveTypeKind)
            {
                case PrimitiveTypeKind.Binary:
                    maxLength = type.GetMaxLength(Int32.MaxValue);
                    if (maxLength > JetProviderManifest.BINARY_MAXSIZE)
                    {
                        typeName = "image";
                        length = "max";
                    }
                    else
                    {
                        length = maxLength.ToString(CultureInfo.InvariantCulture);
                        isFixedLength = type.GetIsFixedLength(false);
                        typeName = (isFixedLength ? "binary(" : "varbinary(") + length + ")";
                    }
                    break;

                case PrimitiveTypeKind.String:
                    isUnicode = type.GetIsUnicode();
                    isFixedLength = type.GetIsFixedLength();
                    maxLength = type.GetMaxLength(Int32.MaxValue);
                    if (maxLength > JetProviderManifest.VARCHAR_MAXSIZE)
                    {
                        typeName = "text";
                        length = "max";
                    }
                    else
                    {
                        length = maxLength.ToString(CultureInfo.InvariantCulture);
                        typeName = (isFixedLength ? "char(" : "varchar(") + length + ")";
                    }
                    break;

                case PrimitiveTypeKind.DateTime:
                    typeName = "datetime";
                    break;

                case PrimitiveTypeKind.Time:
                    throw new NotSupportedException("Jet does not support Time fields.");
                    typeName = "time";
                    break;

                case PrimitiveTypeKind.DateTimeOffset:
                    throw new NotSupportedException("Jet does not support DateTimeOffset fields.");
                    typeName = "datetimeoffset";
                    break;

                case PrimitiveTypeKind.Decimal:
                    decimalPrecision = type.GetPrecision(18);
                    Debug.Assert(decimalPrecision > 0, "decimal precision must be greater than zero");
                    decimalScale = type.GetScale(0);
                    Debug.Assert(decimalPrecision >= decimalScale, "decimalPrecision must be greater or equal to decimalScale");
                    Debug.Assert(decimalPrecision <= 38, "decimalPrecision must be less than or equal to 38");
                    typeName = typeName + "(" + decimalPrecision + "," + decimalScale + ")";
                    break;

                case PrimitiveTypeKind.Int32:
                    typeName = "int";
                    break;

                case PrimitiveTypeKind.Int64:
                    typeName = "bigint";
                    break;

                case PrimitiveTypeKind.Int16:
                    typeName = "smallint";
                    break;

                case PrimitiveTypeKind.Byte:
                    typeName = "tinyint";
                    break;

                case PrimitiveTypeKind.Boolean:
                    typeName = "bit";
                    break;

                case PrimitiveTypeKind.Single:
                    typeName = "real";
                    break;

                case PrimitiveTypeKind.Double:
                    typeName = "float";
                    break;

                case PrimitiveTypeKind.Guid:
                    typeName = "guid";
                    break;

                default:
                    throw new NotSupportedException("Unsupported EdmType: " + primitiveType.PrimitiveTypeKind);
            }

            return typeName;
        }

        /// <summary>
        /// Handles the expression representing DbLimitExpression.Limit and DbSkipExpression.Count.
        /// If must be a constant expression
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private int HandleCountExpression(DbExpression e)
        {
            if (e.ExpressionKind != DbExpressionKind.Constant)
                throw new InvalidOperationException("DbLimitExpression and DbSkipExpression must be constants");

            return Convert.ToInt32(((DbConstantExpression)e).Value);
        }

        /// <summary>
        /// This is used to determine if a particular expression is an Apply operation.
        /// This is only the case when the DbExpressionKind is CrossApply or OuterApply.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static bool IsApplyExpression(DbExpression e)
        {
            return (DbExpressionKind.CrossApply == e.ExpressionKind || DbExpressionKind.OuterApply == e.ExpressionKind);
        }

        /// <summary>
        /// This is used to determine if a particular expression is a Join operation.
        /// This is true for DbCrossJoinExpression and DbJoinExpression, the
        /// latter of which may have one of several different ExpressionKinds.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static bool IsJoinExpression(DbExpression e)
        {
            return (DbExpressionKind.CrossJoin == e.ExpressionKind ||
                    DbExpressionKind.FullOuterJoin == e.ExpressionKind ||
                    DbExpressionKind.InnerJoin == e.ExpressionKind ||
                    DbExpressionKind.LeftOuterJoin == e.ExpressionKind);
        }

        /// <summary>
        /// This is used to determine if a calling expression needs to place
        /// round brackets around the translation of the expression e.
        ///
        /// Constants, parameters and properties do not require brackets,
        /// everything else does.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>true, if the expression needs brackets </returns>
        private static bool IsComplexExpression(DbExpression e)
        {
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Constant:
                case DbExpressionKind.ParameterReference:
                case DbExpressionKind.Property:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Determine if the owner expression can add its unique sql to the input's
        /// SqlSelectStatement
        /// </summary>
        /// <param name="result">The SqlSelectStatement of the input to the relational node.</param>
        /// <param name="expressionKind">The kind of the expression node(not the input's)</param>
        /// <returns></returns>
        private static bool IsCompatible(SqlSelectStatement result, DbExpressionKind expressionKind)
        {
            switch (expressionKind)
            {
                case DbExpressionKind.Distinct:
                    return result.Top == null
                        // The projection after distinct may not project all 
                        // columns used in the Order By
                        && result.OrderBy.IsEmpty;

                case DbExpressionKind.Filter:
                    return result.Select.IsEmpty
                            && result.Where.IsEmpty
                            && result.GroupBy.IsEmpty
                            && result.Top == null;

                case DbExpressionKind.GroupBy:
                    return result.Select.IsEmpty
                            && result.GroupBy.IsEmpty
                            && result.OrderBy.IsEmpty
                            && result.Top == null;

                case DbExpressionKind.Limit:
                case DbExpressionKind.Element:
                    return result.Top == null;

                case DbExpressionKind.Project:
                    return result.Select.IsEmpty
                            && result.GroupBy.IsEmpty
                            && !result.IsDistinct;

                case DbExpressionKind.Skip:
                    return result.Select.IsEmpty
                            && result.GroupBy.IsEmpty
                            && result.OrderBy.IsEmpty
                            && !result.IsDistinct;

                case DbExpressionKind.Sort:
                    return result.Select.IsEmpty
                            && result.GroupBy.IsEmpty
                            && result.OrderBy.IsEmpty
                            && !result.IsDistinct;

                default:
                    Debug.Assert(false);
                    throw new InvalidOperationException();
            }

        }

        /// <summary>
        /// Simply calls <see cref="VisitExpressionEnsureSqlStatement(DbExpression, bool)"/>
        /// with addDefaultColumns set to true
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private SqlSelectStatement VisitExpressionEnsureSqlStatement(DbExpression e)
        {
            return VisitExpressionEnsureSqlStatement(e, true);
        }

        /// <summary>
        /// This is called from <see cref="GenerateSql(DbQueryCommandTree)"/> and nodes which require a
        /// select statement as an argument e.g. <see cref="Visit(DbIsEmptyExpression)"/>,
        /// <see cref="Visit(DbUnionAllExpression)"/>.
        ///
        /// SqlGenerator needs its child to have a proper alias if the child is
        /// just an extent or a join.
        ///
        /// The normal relational nodes result in complete valid SQL statements.
        /// For the rest, we need to treat them as there was a dummy
        /// <code>
        /// -- originally {expression}
        /// -- change that to
        /// SELECT *
        /// FROM {expression} as c
        /// </code>
        /// 
        /// DbLimitExpression needs to start the statement but not add the default columns
        /// </summary>
        /// <param name="e"></param>
        /// <param name="addDefaultColumns"></param>
        /// <returns></returns>
        SqlSelectStatement VisitExpressionEnsureSqlStatement(DbExpression e, bool addDefaultColumns)
        {
            Debug.Assert(MetadataHelpers.IsCollectionType(e.ResultType));

            SqlSelectStatement result;
            switch (e.ExpressionKind)
            {
                case DbExpressionKind.Project:
                case DbExpressionKind.Filter:
                case DbExpressionKind.GroupBy:
                case DbExpressionKind.Sort:
                    result = e.Accept(this) as SqlSelectStatement;
                    break;

                default:
                    Symbol fromSymbol;
                    string inputVarName = "c";  // any name will do - this is my random choice.
                    symbolTable.EnterScope();

                    TypeUsage type = null;
                    switch (e.ExpressionKind)
                    {
                        case DbExpressionKind.Scan:
                        case DbExpressionKind.CrossJoin:
                        case DbExpressionKind.FullOuterJoin:
                        case DbExpressionKind.InnerJoin:
                        case DbExpressionKind.LeftOuterJoin:
                        case DbExpressionKind.CrossApply:
                        case DbExpressionKind.OuterApply:
                            type = MetadataHelpers.GetElementTypeUsage(e.ResultType);
                            break;

                        default:
                            Debug.Assert(MetadataHelpers.IsCollectionType(e.ResultType));
                            type = MetadataHelpers.GetEdmType<CollectionType>(e.ResultType).TypeUsage;
                            break;
                    }

                    result = VisitInputExpression(e, inputVarName, type, out fromSymbol);
                    AddFromSymbol(result, inputVarName, fromSymbol);
                    symbolTable.ExitScope();
                    break;
            }

            if (addDefaultColumns && result.Select.IsEmpty)
            {
                AddDefaultColumns(result);
            }

            return result;
        }

        /// <summary>
        /// This method is called by <see cref="Visit(DbFilterExpression)"/> and
        /// <see cref="Visit(DbQuantifierExpression)"/>
        ///
        /// </summary>
        /// <param name="input"></param>
        /// <param name="predicate"></param>
        /// <param name="negatePredicate">This is passed from <see cref="Visit(DbQuantifierExpression)"/>
        /// in the All(...) case.</param>
        /// <returns></returns>
        private SqlSelectStatement VisitFilterExpression(DbExpressionBinding input, DbExpression predicate, bool negatePredicate)
        {
            Symbol fromSymbol;
            SqlSelectStatement result = VisitInputExpression(input.Expression,
                input.VariableName, input.VariableType, out fromSymbol);

            // Filter is compatible with OrderBy
            // but not with Project, another Filter or GroupBy
            if (!IsCompatible(result, DbExpressionKind.Filter))
            {
                result = CreateNewSelectStatement(result, input.VariableName, input.VariableType, out fromSymbol);
            }

            selectStatementStack.Push(result);
            symbolTable.EnterScope();

            AddFromSymbol(result, input.VariableName, fromSymbol);

            if (negatePredicate)
                result.Where.Append("NOT (");

            result.Where.Append(predicate.Accept(this));

            if (negatePredicate)
                result.Where.Append(")");

            symbolTable.ExitScope();
            selectStatementStack.Pop();

            return result;
        }

        /// <summary>
        /// If the sql fragment for an input expression is not a SqlSelect statement
        /// or other acceptable form (e.g. an extent as a SqlBuilder), we need
        /// to wrap it in a form acceptable in a FROM clause.  These are
        /// primarily the
        /// <list type="bullet">
        /// <item>The set operation expressions - union all, intersect, except</item>
        /// <item>TVFs, which are conceptually similar to tables</item>
        /// </list>
        /// </summary>
        /// <param name="result"></param>
        /// <param name="sqlFragment"></param>
        /// <param name="expressionKind"></param>
        private static void WrapNonQueryExtent(SqlSelectStatement result, ISqlFragment sqlFragment, DbExpressionKind expressionKind)
        {
            switch (expressionKind)
            {
                case DbExpressionKind.Function:
                    // TVF
                    result.From.Append(sqlFragment);
                    break;

                default:
                    result.From.Append(" (");
                    result.From.Append(sqlFragment);
                    result.From.Append(")");
                    break;
            }
        }

        /// <summary>
        /// Is this a builtin function (ie) does it have the builtinAttribute specified?
        /// </summary>
        /// <param name="function"></param>
        /// <returns></returns>
        private static bool IsBuiltinFunction(EdmFunction function)
        {
            return MetadataHelpers.TryGetValueForMetadataProperty<bool>(function, "BuiltInAttribute");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="function"></param>
        /// <param name="result"></param>
        void WriteFunctionName(SqlBuilder result, EdmFunction function)
        {
            string storeFunctionName = MetadataHelpers.TryGetValueForMetadataProperty<string>(function, "StoreFunctionNameAttribute");

            if (string.IsNullOrEmpty(storeFunctionName))
                storeFunctionName = function.Name;

            // If the function is a builtin (ie) the BuiltIn attribute has been
            // specified, then, the function name should not be quoted; additionally,
            // no namespace should be used.
            if (IsBuiltinFunction(function))
            {
                if (function.NamespaceName == "Edm")
                    result.Append(storeFunctionName);
                else
                    result.Append(storeFunctionName);
            }
            else
                // Should we actually support this?
                result.Append(JetProviderManifest.QuoteIdentifier(storeFunctionName));
        }

        #region Group by helpers

        /// <summary>
        /// Helper method for the Group By visitor
        /// Returns true if at least one of the aggregates in the given list
        /// has an argument that is not a <see cref="DbConstantExpression" /> and is not
        /// a <see cref="DbPropertyExpression" /> over <see cref="DbVariableReferenceExpression" />,
        /// either potentially capped with a <see cref="DbCastExpression" />
        /// This is really due to the following two limitations of Sql Server:
        /// <list type="number">
        ///     <item>
        ///         If an expression being aggregated contains an outer reference, then that outer
        ///         reference must be the only column referenced in the expression (SQLBUDT #488741)
        ///     </item>
        ///     <item>
        ///         Sql Server cannot perform an aggregate function on an expression containing
        ///         an aggregate or a subquery. (SQLBUDT #504600)
        ///     </item>
        /// </list>
        /// Potentially, we could furhter optimize this.
        /// </summary>
        private static bool GroupByAggregatesNeedInnerQuery(IList<DbAggregate> aggregates, string inputVarRefName)
        {
            foreach (var aggregate in aggregates)
            {
                Debug.Assert(aggregate.Arguments.Count == 1);
                if (GroupByAggregateNeedsInnerQuery(aggregate.Arguments[0], inputVarRefName))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the given expression is not a <see cref="DbConstantExpression" /> or a
        /// <see cref="DbPropertyExpression" /> over  a <see cref="DbVariableReferenceExpression" />
        /// referencing the given inputVarRefName, either
        /// potentially capped with a <see cref="DbCastExpression" />.
        /// </summary>
        private static bool GroupByAggregateNeedsInnerQuery(DbExpression expression, string inputVarRefName)
        {
            return GroupByExpressionNeedsInnerQuery(expression, inputVarRefName, true);
        }

        /// <summary>
        /// Helper method for the Group By visitor
        /// Returns true if at least one of the expressions in the given list
        /// is not <see cref="DbPropertyExpression" /> over <see cref="DbVariableReferenceExpression" />
        /// referencing the given inputVarRefName potentially capped with a <see cref="DbCastExpression" />.
        /// This is really due to the following limitation: Sql Server requires each GROUP BY expression
        /// (key) to contain at least one column that is not an outer reference. (SQLBUDT #616523)
        /// Potentially, we could further optimize this.
        /// </summary>
        private static bool GroupByKeysNeedInnerQuery(IList<DbExpression> keys, string inputVarRefName)
        {
            foreach (var key in keys)
            {
                if (GroupByKeyNeedsInnerQuery(key, inputVarRefName))
                {
                    return true;
                }
            }
            return false;
        }




        /// <summary>
        /// Returns true if the given expression is not <see cref="DbPropertyExpression" /> over
        /// <see cref="DbVariableReferenceExpression" /> referencing the given inputVarRefName
        /// potentially capped with a <see cref="DbCastExpression" />.
        /// This is really due to the following limitation: Sql Server requires each GROUP BY expression
        /// (key) to contain at least one column that is not an outer reference. (SQLBUDT #616523)
        /// Potentially, we could further optimize this.
        /// </summary>
        private static bool GroupByKeyNeedsInnerQuery(DbExpression expression, string inputVarRefName)
        {
            return GroupByExpressionNeedsInnerQuery(expression, inputVarRefName, false);
        }


        /// <summary>
        /// Helper method for processing Group By keys and aggregates.
        /// Returns true if the given expression is not a <see cref="DbConstantExpression" />
        /// (and allowConstants is specified)or a <see cref="DbPropertyExpression" /> over
        /// a <see cref="DbVariableReferenceExpression" /> referencing the given inputVarRefName,
        /// either potentially capped with a <see cref="DbCastExpression" />.
        /// </summary>
        private static bool GroupByExpressionNeedsInnerQuery(DbExpression expression, string inputVarRefName, bool allowConstants)
        {
            //Skip a constant if constants are allowed
            if (allowConstants && (expression.ExpressionKind == DbExpressionKind.Constant))
            {
                return false;
            }

            //Skip a cast expression
            if (expression.ExpressionKind == DbExpressionKind.Cast)
            {
                var castExpression = (DbCastExpression)expression;
                return GroupByExpressionNeedsInnerQuery(castExpression.Argument, inputVarRefName, allowConstants);
            }

            //Allow Property(Property(...)), needed when the input is a join
            if (expression.ExpressionKind == DbExpressionKind.Property)
            {
                var propertyExpression = (DbPropertyExpression)expression;
                return GroupByExpressionNeedsInnerQuery(propertyExpression.Instance, inputVarRefName, allowConstants);
            }

            if (expression.ExpressionKind == DbExpressionKind.VariableReference)
            {
                var varRefExpression = expression as DbVariableReferenceExpression;
                return !varRefExpression.VariableName.Equals(inputVarRefName);
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Determines whether the given expression is a <see cref="DbPropertyExpression"/> 
        /// over <see cref="DbVariableReferenceExpression"/>
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        static bool IsPropertyOverVarRef(DbExpression expression)
        {
            DbPropertyExpression propertyExpression = expression as DbPropertyExpression;
            if (propertyExpression == null)
            {
                return false;
            }
            DbVariableReferenceExpression varRefExpression = propertyExpression.Instance as DbVariableReferenceExpression;
            if (varRefExpression == null)
            {
                return false;
            }
            return true;
        }

        #endregion

    }

}

