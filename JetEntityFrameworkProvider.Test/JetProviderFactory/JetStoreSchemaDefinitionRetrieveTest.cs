using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace JetEntityFrameworkProvider.Test.JetProviderFactory
{
    [TestFixture]
    public class JetStoreSchemaDefinitionRetrieveTest
    {

        DbConnection _connection;

        [SetUp]
        public void Init()
        {
            _connection = Helpers.GetConnection();
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
        }

        [Test]
        public void Show()
        {
            Helpers.ShowDataReaderContent(_connection, "show tables");
            Helpers.ShowDataReaderContent(_connection, "show tablecolumns");
            Helpers.ShowDataReaderContent(_connection, "show indexes");
            Helpers.ShowDataReaderContent(_connection, "show indexcolumns");
            Helpers.ShowDataReaderContent(_connection, "show views");
            Helpers.ShowDataReaderContent(_connection, "show viewcolumns");
            Helpers.ShowDataReaderContent(_connection, "show constraints");
            Helpers.ShowDataReaderContent(_connection, "show checkconstraints");
            Helpers.ShowDataReaderContent(_connection, "show constraintcolumns");
            Helpers.ShowDataReaderContent(_connection, "show foreignKeyconstraints");
            Helpers.ShowDataReaderContent(_connection, "show foreignKeys");
            Helpers.ShowDataReaderContent(_connection, "show viewconstraints");
            Helpers.ShowDataReaderContent(_connection, "show viewconstraintcolumns");
            Helpers.ShowDataReaderContent(_connection, "show viewforeignkeys");
        }

        [Test]
        public void ShowWithWhere()
        {
            Helpers.ShowDataReaderContent(_connection, "show indexes where Name like 'PK*'");
            Helpers.ShowDataReaderContent(_connection, "show indexcolumns where index like 'PK*'");
        }

        [Test]
        public void ShowWithWhereOrder()
        {
            Helpers.ShowDataReaderContent(_connection, "show indexes where Name like 'PK*' order by Name");
            Helpers.ShowDataReaderContent(_connection, "show indexcolumns where index like 'PK*' order by Index, Ordinal");
        }

        [Test]
        public void ShowWithOrder()
        {
            Helpers.ShowDataReaderContent(_connection, "show indexes order by Name");
            Helpers.ShowDataReaderContent(_connection, "show indexcolumns order by Index, Ordinal");
        }

    }
}
