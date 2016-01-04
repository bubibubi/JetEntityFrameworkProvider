using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetEntityFrameworkProvider
{
    /// <summary>
    /// Provides configuration to customize the default behavior of the provider for a specified DbContext.
    /// </summary>
    /// <typeparam name="TContext">The type of the DbContext to configure.</typeparam>
    public class JetProviderConfiguration
    {
        public static readonly JetProviderConfiguration Instance = new JetProviderConfiguration();
        private readonly CommandGenerationConfiguration _commandGenerationConfig = new CommandGenerationConfiguration();
        private readonly DdlGenerationConfiguration _ddlGenerationConfig = new DdlGenerationConfiguration();

        private JetProviderConfiguration() { }

        /// <summary>
        /// Gets the provider invariant name for the provider.
        /// </summary>
        public string ProviderInvariantName
        {
            get { return JetProviderServices.PROVIDERINVARIANTNAME; }
        }

        /// <summary>
        /// Gets the configuration options for SQL commands generation.
        /// </summary>
        public CommandGenerationConfiguration CommandGeneration
        {
            get { return _commandGenerationConfig; }
        }

        /// <summary>
        /// Gets the configuration options for DDL generation.
        /// </summary>
        public DdlGenerationConfiguration DdlGeneration
        {
            get { return _ddlGenerationConfig; }
        }

        /// <summary>
        /// Represents SQL commands generation options.
        /// </summary>
        public class CommandGenerationConfiguration
        {
            private const string DEFAULT_DUAL_TABLE = "MSysRelationships";
            private string _dualTable = DEFAULT_DUAL_TABLE;

            internal CommandGenerationConfiguration() { }

            /// <summary>
            /// Gets or sets the name of the table to use for scalar conditional queries. The default value is MSysRelationships.
            /// </summary>
            public string DualTable
            {
                get { return _dualTable; }
                set
                {
                    if (string.IsNullOrWhiteSpace(value))
                        throw new ArgumentNullException("Invalid conditional scalar utility table name.");

                    _dualTable = value;
                }
            }
        }

        /// <summary>
        /// Represents DDL changes options.
        /// </summary>
        public class DdlGenerationConfiguration
        {
            private const bool DEFAULT_APPEND_RANDOM_NUMBER_FOR_FK = true;
            private bool _appendRandomNumberForFKs = DEFAULT_APPEND_RANDOM_NUMBER_FOR_FK;

            internal DdlGenerationConfiguration() { }

            /// <summary>
            /// Gets or sets a value indicating whether to append a random number 
            /// at the end of the foreign key name. The default value is true.
            /// </summary>
            public bool AppendRandomNumberForForeignKeyNames
            {
                get { return _appendRandomNumberForFKs; }
                set { _appendRandomNumberForFKs = value; }
            }
        }
    }
}
