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

        JetConnection _jetConnection;

        [SetUp]
        public void Init()
        {
            _jetConnection = Helpers.GetJetConnection();
        }

        [TearDown]
        public void TearDown()
        {
            _jetConnection.Dispose();
        }

        [Test]
        public void Show()
        {
            Helpers.ShowDataReaderContent(_jetConnection, "show tables");
            Helpers.ShowDataReaderContent(_jetConnection, "show tablecolumns");
            Helpers.ShowDataReaderContent(_jetConnection, "show indexes");
            Helpers.ShowDataReaderContent(_jetConnection, "show indexcolumns");
            Helpers.ShowDataReaderContent(_jetConnection, "show views");
            Helpers.ShowDataReaderContent(_jetConnection, "show viewcolumns");
            Helpers.ShowDataReaderContent(_jetConnection, "show constraints");
            Helpers.ShowDataReaderContent(_jetConnection, "show checkconstraints");
            Helpers.ShowDataReaderContent(_jetConnection, "show constraintcolumns");
            Helpers.ShowDataReaderContent(_jetConnection, "show foreignKeyconstraints");
            Helpers.ShowDataReaderContent(_jetConnection, "show foreignKeys");
            Helpers.ShowDataReaderContent(_jetConnection, "show viewconstraints");
            Helpers.ShowDataReaderContent(_jetConnection, "show viewconstraintcolumns");
            Helpers.ShowDataReaderContent(_jetConnection, "show viewforeignkeys");
        }

        [Test]
        public void ShowWithWhere()
        {
            Helpers.ShowDataReaderContent(_jetConnection, "show indexes where Name like 'PK*'");
            Helpers.ShowDataReaderContent(_jetConnection, "show indexcolumns where index like 'PK*'");
        }

        [Test]
        public void ShowWithWhereOrder()
        {
            Helpers.ShowDataReaderContent(_jetConnection, "show indexes where Name like 'PK*' order by Name");
            Helpers.ShowDataReaderContent(_jetConnection, "show indexcolumns where index like 'PK*' order by Index, Ordinal");
        }

        [Test]
        public void ShowWithOrder()
        {
            Helpers.ShowDataReaderContent(_jetConnection, "show indexes order by Name");
            Helpers.ShowDataReaderContent(_jetConnection, "show indexcolumns order by Index, Ordinal");
        }

    }
}
