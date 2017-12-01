using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DaaSDemo.STS
{
    using System.Security.Claims;
    using Common.Options;
    using Data;

    /// <summary>
    ///     Startup logic for the Database-as-a-Service demo UI.
    /// </summary>
    public class Startup
    {
        /// <summary>
        ///     Create a new <see cref="Startup"/>.
        /// </summary>
        /// <param name="configuration">
        ///     The application configuration.
        /// </param>
        public Startup(IConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            Configuration = configuration;
        }

        /// <summary>
        ///     The application configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     Configure application services.
        /// </summary>
        /// <param name="services">
        ///     The application service collection.
        /// </param>
        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.AddOptions();
            services.AddDaaSOptions(Configuration);
            services.AddDaaSDataAccess();

            services.AddDataProtection(dataProtection =>
            {
                dataProtection.ApplicationDiscriminator = "DaaS.Demo";
            });

            services.AddCors();

            services.AddMvc()
                .AddJsonOptions(json =>
                {
                    json.SerializerSettings.Converters.Add(
                        new StringEnumConverter()
                    );
                });

            // TODO: Create or reuse RavenDB data stores for this information (consider using ASP.NET Core Identity if we can find a workable RavenDB backing store for it).

            SecurityOptions securityOptions = SecurityOptions.From(Configuration);

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryApiResources(new []
                {
                    new ApiResource("daas_api_v1", "DaaS API v1")
                })
                .AddInMemoryIdentityResources(new IdentityResource[]
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResource("roles", new string[] { "roles" })
                })
                .AddInMemoryClients(new Client[]
                {
                    new Client
                    {
                        ClientId = "daas-ui-dev",
                        ClientName = "DaaS Portal (development)",

                        AllowedGrantTypes = GrantTypes.Implicit,
                        RequireConsent = false,
                        AllowOfflineAccess = true,
                        AllowAccessTokensViaBrowser = true,

                        RedirectUris = securityOptions.PortalBaseAddresses
                            .SelectMany(
                                baseAddress => new string[]
                                {
                                    $"{baseAddress}/oidc/signin/popup",
                                    $"{baseAddress}/oidc/signin/silent",
                                    $"{baseAddress}/signin-oidc"
                                }
                            )
                            .ToArray(),
                        PostLogoutRedirectUris = 
                            securityOptions.PortalBaseAddresses
                                .SelectMany(
                                    baseAddress => new string[]
                                    {
                                        $"{baseAddress}/oidc/signout",
                                        $"{baseAddress}/signout-callback-oidc"
                                    }
                                )
                                .ToArray(),
                        AllowedScopes = new List<string>
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            "roles",
                            "daas_api_v1"
                        }
                    }
                })
                .AddTestUsers(new List<TestUser>
                {
                    new TestUser
                    {
                        SubjectId = "User/1",
                        Username = "tintoy",
                        Password = "woozle",
                        Claims =
                        {
                            new Claim("name", "tintoy"),
                            new Claim("email", "tintoy@tintoy.io"),
                            new Claim("roles", "admin"),
                            new Claim("roles", "user")
                        }
                    }
                });
        }

        /// <summary>
        ///     Configure the application pipeline.
        /// </summary>
        /// <param name="app">
        ///     The application pipeline builder.
        /// </param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnvironment, IApplicationLifetime appLifetime)
        {
            if (hostingEnvironment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            app.UseCors(cors =>
            {
                cors.AllowAnyHeader();
                cors.AllowAnyMethod();
                cors.AllowAnyOrigin();
            });

            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}
