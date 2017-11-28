using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Raven.Client;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DaaSDemo.Data
{
    using Common.Options;
    using Models.Data;

    public static class RegistrationExtensions
    {
        /// <summary>
        ///     The assembly containing indexes for the DaaS management database.
        /// </summary>
        public static readonly Assembly IndexesAssembly = typeof(RegistrationExtensions).Assembly;

        /// <summary>
        ///     Add components for access to the DaaS management database.
        /// </summary>
        /// <param name="services">
        ///     The service collection to configure.
        /// </param>
        /// <param name="databaseName">
        ///     The name of the DaaS database.
        /// </param>
        /// <param name="createIndexes">
        ///     Create / update indexes when first connecting to the database?
        /// </param>
        public static void AddDaaSDataAccess(this IServiceCollection services, string databaseName = "DaaS", bool createIndexes = false)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (String.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'databaseName'.", nameof(databaseName));
            
            services.AddSingleton<IDocumentStore>(serviceProvider => 
            {
                ILogger<IDocumentStore> logger = serviceProvider.GetRequiredService<ILogger<IDocumentStore>>();

                DatabaseOptions databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
                if (String.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
                    throw new InvalidOperationException("Application configuration is missing database connection string.");

                logger.LogInformation("Will use RavenDB server at {RavenServerUrl} for the management database.", databaseOptions.ConnectionString);

                DocumentStore store = new DocumentStore
                {
                    Urls = new string[]
                    {
                        databaseOptions.ConnectionString
                    },
                    Database = databaseName,
                    Conventions =
                    {
                        IdentityPartsSeparator = "-",
                        CustomizeJsonSerializer = serializer =>
                        {
                            serializer.TypeNameHandling = TypeNameHandling.Auto;
                            serializer.Converters.Add(
                                new StringEnumConverter()
                            );
                        },
                        FindCollectionName = type =>
                        {
                            EntitySet attribute = type.GetCustomAttribute<EntitySet>();
                            if (attribute != null)
                                return attribute.Name;

                            return type.Name;
                        }
                    }
                };

                logger.LogDebug("Initialising the RavenDB document store...");

                store.Initialize();

                logger.LogDebug("RavenDB document store initialised.");

                if (createIndexes)
                {
                    logger.LogInformation("Configuring RavenDB indexes...");

                    IndexCreation.CreateIndexes(IndexesAssembly, store);

                    logger.LogInformation("RavenDB indexes configured.");
                }

                return store;
            });

            services.AddTransient<IDocumentSession>(
                serviceProvider => serviceProvider.GetRequiredService<IDocumentStore>().OpenSession()
            );

            services.AddTransient<IAsyncDocumentSession>(
                serviceProvider => serviceProvider.GetRequiredService<IDocumentStore>().OpenAsyncSession()
            );
        }
    }
}
