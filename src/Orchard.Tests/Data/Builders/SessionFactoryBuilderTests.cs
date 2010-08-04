﻿using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using Autofac.Features.Metadata;
using NHibernate.Tool.hbm2ddl;
using NUnit.Framework;
using Orchard.Data.Providers;
using Orchard.Environment.Configuration;
using Orchard.Environment.Descriptor;
using Orchard.Environment.Descriptor.Models;
using Orchard.Environment.ShellBuilders.Models;
using Orchard.Tests.Records;

namespace Orchard.Tests.Data.Builders {
    [TestFixture]
    public class SessionFactoryBuilderTests {
        private string _tempDataFolder;

        [SetUp]
        public void Init() {
            var tempFilePath = Path.GetTempFileName();
            File.Delete(tempFilePath);
            Directory.CreateDirectory(tempFilePath);
            _tempDataFolder = tempFilePath;
        }

        [TearDown]
        public void Term() {
            try { Directory.Delete(_tempDataFolder, true); }
            catch (IOException) { }
        }

        private static void CreateSqlServerDatabase(string databasePath) {
            var databaseName = Path.GetFileNameWithoutExtension(databasePath);
            using (var connection = new SqlConnection(
                "Data Source=.\\SQLEXPRESS;Initial Catalog=tempdb;Integrated Security=true;User Instance=True;")) {
                connection.Open();
                using (var command = connection.CreateCommand()) {
                    command.CommandText =
                        "CREATE DATABASE " + databaseName +
                        " ON PRIMARY (NAME=" + databaseName +
                        ", FILENAME='" + databasePath.Replace("'", "''") + "')";
                    command.ExecuteNonQuery();

                    command.CommandText =
                        "EXEC sp_detach_db '" + databaseName + "', 'true'";
                    command.ExecuteNonQuery();
                }
            }
        }



        [Test]
        public void SqlCeSchemaShouldBeGeneratedAndUsable() {

            var recordDescriptors = new[] {
                new RecordBlueprint {TableName = "Hello", Type = typeof (FooRecord)}
            };

            var parameters = new SessionFactoryParameters {
                Provider = "SqlCe",
                DataFolder = _tempDataFolder,
                RecordDescriptors = recordDescriptors
            };

            var manager = (IDataServicesProviderFactory) new DataServicesProviderFactory(new[] {
                new Meta<CreateDataServicesProvider>(
                    (dataFolder, connectionString) => new SqlCeDataServicesProvider(dataFolder, connectionString),
                    new Dictionary<string, object> {{"ProviderName", "SqlCe"}})
            });

            var configuration = manager
                .CreateProvider(parameters)
                .BuildConfiguration(parameters);
                
            configuration.SetProperty("connection.release_mode", "on_close");

            new SchemaExport(configuration).Execute(false, true, false);

            var sessionFactory = configuration.BuildSessionFactory();

            var session = sessionFactory.OpenSession();
            var foo = new FooRecord {Name = "hi there", Id = 1};
            session.Save(foo);
            session.Flush();
            session.Close();

            Assert.That(foo, Is.Not.EqualTo(0));

            sessionFactory.Close();

        }

        [Test]
        public void SqlServerSchemaShouldBeGeneratedAndUsable() {
            var databasePath = Path.Combine(_tempDataFolder, "Orchard.mdf");
            CreateSqlServerDatabase(databasePath);

            var recordDescriptors = new[] {
                                              new RecordBlueprint {TableName = "Hello", Type = typeof (FooRecord)}
                                          };

            var manager = (IDataServicesProviderFactory)new DataServicesProviderFactory(new[] {
                new Meta<CreateDataServicesProvider>(
                    (dataFolder, connectionString) => new SqlServerDataServicesProvider(dataFolder, connectionString),
                    new Dictionary<string, object> {{"ProviderName", "SqlServer"}})
            });
            var parameters = new SessionFactoryParameters {
                Provider = "SqlServer",
                DataFolder = _tempDataFolder,
                ConnectionString = "Data Source=.\\SQLEXPRESS;AttachDbFileName=" + databasePath + ";Integrated Security=True;User Instance=True;",
                RecordDescriptors = recordDescriptors,
            };

            var configuration = manager
                .CreateProvider(parameters)
                .BuildConfiguration(parameters);

            new SchemaExport(configuration).Execute(false, true, false);

            var sessionFactory = configuration.BuildSessionFactory();

            var session = sessionFactory.OpenSession();
            var foo = new FooRecord { Name = "hi there" };
            session.Save(foo);
            session.Flush();
            session.Close();

            Assert.That(foo, Is.Not.EqualTo(0));

            sessionFactory.Close();
        }
    }
}