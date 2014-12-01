using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace JetEntityFrameworkProvider.Test.DbFirst
{
    [SetUpFixture]
    public class SetUpDbFirst
    {

        public static EntityConnection EntityConnection;

        [SetUp]
        public void Init()
        {

            JetEntityFrameworkProvider.JetCommand.ShowSqlStatements = true;

            //EntityConnection ec = GetSqlServerEntityConnection();
            EntityConnection = GetJetEntityConnection();
            //EntityConnection ec = GetOleDbEntityConnection();

        }


        [TearDown]
        public void Dispose()
        {
            EntityConnection.Dispose();
        }



        internal static EntityConnection GetOleDbEntityConnection()
        {
            // This provider raises an exception during ec.Open()
            /*
            System.Data.ProviderIncompatibleException was unhandled
  HResult=-2146232032
  Message=Il tipo di factory 'System.Data.OleDb.OleDbFactory' del provider dell'archivio non implementa l'interfaccia IServiceProvider. Utilizzare un provider dell'archivio che implementi tale interfaccia.
  Source=System.Data.Entity
  StackTrace:
       in System.Data.Common.DbProviderServices.GetProviderServices(DbProviderFactory factory)
       in System.Data.Metadata.Edm.StoreItemCollection.Loader.InitializeProviderManifest(Action`3 addError)
       in System.Data.Metadata.Edm.StoreItemCollection.Loader.OnProviderManifestTokenNotification(String token, Action`3 addError)
       in System.Data.EntityModel.SchemaObjectModel.Schema.HandleProviderManifestTokenAttribute(XmlReader reader)
       in System.Data.EntityModel.SchemaObjectModel.Schema.HandleAttribute(XmlReader reader)
       in System.Data.EntityModel.SchemaObjectModel.SchemaElement.ParseAttribute(XmlReader reader)
       in System.Data.EntityModel.SchemaObjectModel.SchemaElement.Parse(XmlReader reader)
       in System.Data.EntityModel.SchemaObjectModel.Schema.HandleTopLevelSchemaElement(XmlReader reader)
       in System.Data.EntityModel.SchemaObjectModel.Schema.InternalParse(XmlReader sourceReader, String sourceLocation)
       in System.Data.EntityModel.SchemaObjectModel.Schema.Parse(XmlReader sourceReader, String sourceLocation)
       in System.Data.EntityModel.SchemaObjectModel.SchemaManager.ParseAndValidate(IEnumerable`1 xmlReaders, IEnumerable`1 sourceFilePaths, SchemaDataModelOption dataModel, AttributeValueNotification providerNotification, AttributeValueNotification providerManifestTokenNotification, ProviderManifestNeeded providerManifestNeeded, IList`1& schemaCollection)
       in System.Data.Metadata.Edm.StoreItemCollection.Loader.LoadItems(IEnumerable`1 xmlReaders, IEnumerable`1 sourceFilePaths)
       in System.Data.Metadata.Edm.StoreItemCollection.Init(IEnumerable`1 xmlReaders, IEnumerable`1 filePaths, Boolean throwOnError, DbProviderManifest& providerManifest, DbProviderFactory& providerFactory, String& providerManifestToken, Memoizer`2& cachedCTypeFunction)
       in System.Data.Metadata.Edm.StoreItemCollection..ctor(IEnumerable`1 xmlReaders, IEnumerable`1 filePaths)
       in System.Data.Metadata.Edm.MetadataCache.StoreMetadataEntry.LoadStoreCollection(EdmItemCollection edmItemCollection, MetadataArtifactLoader loader)
       in System.Data.Metadata.Edm.MetadataCache.StoreItemCollectionLoader.LoadItemCollection(StoreMetadataEntry entry)
       in System.Data.Metadata.Edm.MetadataCache.LoadItemCollection[T](IItemCollectionLoader`1 itemCollectionLoader, T entry)
       in System.Data.Metadata.Edm.MetadataCache.GetOrCreateStoreAndMappingItemCollections(String cacheKey, MetadataArtifactLoader loader, EdmItemCollection edmItemCollection, Object& entryToken)
       in System.Data.EntityClient.EntityConnection.LoadStoreItemCollections(MetadataWorkspace workspace, DbConnection storeConnection, DbProviderFactory factory, DbConnectionOptions connectionOptions, EdmItemCollection edmItemCollection, MetadataArtifactLoader artifactLoader)
       in System.Data.EntityClient.EntityConnection.GetMetadataWorkspace(Boolean initializeAllCollections)
       in System.Data.EntityClient.EntityConnection.InitializeMetadata(DbConnection newConnection, DbConnection originalConnection, Boolean closeOriginalConnectionOnFailure)
       in System.Data.EntityClient.EntityConnection.Open()
       in JetEntityFrameworkProvider.Test.Program.GetOleDbEntityConnection() in c:\Users\Utente\Documents\Visual Studio 2013\Projects\How to implement the1\JetEntityFrameworkProvider.Test\Program.cs:riga 68
       in JetEntityFrameworkProvider.Test.Program.Main(String[] args) in c:\Users\Utente\Documents\Visual Studio 2013\Projects\How to implement the1\JetEntityFrameworkProvider.Test\Program.cs:riga 39
       in System.AppDomain._nExecuteAssembly(RuntimeAssembly assembly, String[] args)
       in System.AppDomain.ExecuteAssembly(String assemblyFile, Evidence assemblySecurity, String[] args)
       in Microsoft.VisualStudio.HostingProcess.HostProc.RunUsersAssembly()
       in System.Threading.ThreadHelper.ThreadStart_Context(Object state)
       in System.Threading.ExecutionContext.RunInternal(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
       in System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state, Boolean preserveSyncCtx)
       in System.Threading.ExecutionContext.Run(ExecutionContext executionContext, ContextCallback callback, Object state)
       in System.Threading.ThreadHelper.ThreadStart()
  InnerException: 
*/

            OleDbConnectionStringBuilder oleDbConnectionStringBuilder = new OleDbConnectionStringBuilder();
            oleDbConnectionStringBuilder.Provider = "Microsoft.Jet.OLEDB.4.0";
            oleDbConnectionStringBuilder.DataSource = @".\Test.mdb";

            // See also School.edmx

            EntityConnectionStringBuilder entityBuilder = new EntityConnectionStringBuilder();
            string connectionString = string.Format("metadata=res://*/School.csdl|res://*/School.ssdl|res://*/School.msl;provider=System.Data.OleDb;provider connection string='{0}'", oleDbConnectionStringBuilder);
            entityBuilder.ConnectionString = connectionString;
            EntityConnection ec = new EntityConnection(entityBuilder.ToString());
            ec.Open();
            ec.Close();
            return ec;
        }

        internal static EntityConnection GetJetEntityConnection()
        {
            OleDbConnectionStringBuilder oleDbConnectionStringBuilder = new OleDbConnectionStringBuilder();
            oleDbConnectionStringBuilder.Provider = "Microsoft.Jet.OLEDB.4.0";
            oleDbConnectionStringBuilder.DataSource = @".\Test.mdb";

            // See also School.edmx

            EntityConnectionStringBuilder entityBuilder = new EntityConnectionStringBuilder();
            string connectionString = string.Format("metadata=res://*/School.csdl|res://*/School.ssdl|res://*/School.msl;provider=JetEntityFrameworkProvider;provider connection string='{0}'", oleDbConnectionStringBuilder);
            entityBuilder.ConnectionString = connectionString;
            EntityConnection ec = new EntityConnection(entityBuilder.ToString());
            ec.Open();
            ec.Close();
            return ec;
        }

        internal static EntityConnection GetSqlServerEntityConnection()
        {
            SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder();
            sqlConnectionStringBuilder.DataSource = ".";
            sqlConnectionStringBuilder.InitialCatalog = "MySchool";
            sqlConnectionStringBuilder.UserID = "sa";
            sqlConnectionStringBuilder.Password = "dacambiare";

            // See also School.edmx

            EntityConnectionStringBuilder entityBuilder = new EntityConnectionStringBuilder();
            entityBuilder.Provider = "System.Data.EntityClient";
            string connectionString = string.Format("metadata=res://*/School.csdl|res://*/School.ssdl|res://*/School.msl;provider=System.Data.SqlClient;provider connection string='{0}'", sqlConnectionStringBuilder);
            entityBuilder.ConnectionString = connectionString;
            EntityConnection ec = new EntityConnection(entityBuilder.ToString());
            ec.Open();
            ec.Close();
            return ec;
        }


    }
}
