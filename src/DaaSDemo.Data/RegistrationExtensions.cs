using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Raven.Client;
using Raven.Client.Documents;
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
        public static void AddDaaSDataAccess(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            
            services.AddSingleton<IDocumentStore>(serviceProvider => 
            {
                DatabaseOptions databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
                if (String.IsNullOrWhiteSpace(databaseOptions.ConnectionString))
                    throw new InvalidOperationException("Application configuration is missing database connection string.");

                DocumentStore store = new DocumentStore
                {
                    Urls = new string[]
                    {
                        databaseOptions.ConnectionString
                    },
                    Database = "DaaS",
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

                return store.Initialize();
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
