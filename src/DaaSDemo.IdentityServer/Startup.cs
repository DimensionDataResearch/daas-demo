using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DaaSDemo.IdentityServer
{
    using Common.Options;
    using Data;
    using Identity;
    using Models.Data;
    using Options;
    using Services;

    using IdentityConstants = Common.IdentityConstants;

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
            CorsOptions = CorsOptions.From(Configuration);
            SecurityOptions = SecurityOptions.From(Configuration);
        }

        /// <summary>
        ///     The application configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     CORS-related options.
        /// </summary>
        public CorsOptions CorsOptions { get; }

        /// <summary>
        ///     Security-related options.
        /// </summary>
        public SecurityOptions SecurityOptions { get; }

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

            services.AddMvc()
                .AddJsonOptions(json =>
                {
                    json.SerializerSettings.Converters.Add(
                        new StringEnumConverter()
                    );
                });

            services
                .AddIdentity<AppUser, AppRole>(identity =>
                {
                    identity.ClaimsIdentity.UserIdClaimType = JwtClaimTypes.Subject;
                    identity.ClaimsIdentity.UserNameClaimType = JwtClaimTypes.Name;
                    identity.ClaimsIdentity.RoleClaimType = JwtClaimTypes.Role;
                })
                .AddDaaSIdentityStores()
                .AddDefaultTokenProviders();

            AccountOptions.AutomaticRedirectAfterSignOut = true;

            services.AddScoped<AccountService>();
            services.AddScoped<IEmailSender, EmailSender>();

            string[] portalBaseAddresses = (CorsOptions.UI ?? String.Empty).Split(';');
            
            // TODO: Create or reuse RavenDB data stores for some or all of this information (consider using ASP.NET Core Identity if we can find a workable RavenDB backing store for it).

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryApiResources(new []
                {
                    new ApiResource
                    {
                        Name = "daas-api-v1",
                        Description = "DaaS API (v1)",
                        ApiSecrets = new Secret[]
                        {
                            new Secret(
                                "snaus4g3$!".ToSha256() // TODO: Get this from configuration.
                            )
                        },

                        Scopes = new Scope[]
                        {
                            new Scope
                            {
                                Name = "daas-api-v1",
                                Description = "DaaS API (v1)",
                                UserClaims = new string[]
                                {
                                    JwtClaimTypes.Role,
                                    IdentityConstants.JwtClaimTypes.SuperUser
                                }
                            }
                        }
                    }
                })
                .AddInMemoryIdentityResources(new IdentityResource[]
                {
                    new IdentityResources.OpenId(),
                    new IdentityResources.Profile(),
                    new IdentityResource("roles", claimTypes: new string[]
                    {
                        JwtClaimTypes.Role,
                        IdentityConstants.JwtClaimTypes.SuperUser
                    })
                })
                .AddInMemoryClients(new Client[]
                {
                    new Client
                    {
                        ClientId = "daas-ui",
                        ClientName = "DaaS Portal",

                        AllowedGrantTypes = GrantTypes.Implicit,
                        RequireConsent = false,
                        AllowAccessTokensViaBrowser = true,
                        AllowedCorsOrigins = portalBaseAddresses,

                        RedirectUris = portalBaseAddresses.SelectMany(baseAddress => new string[]
                        {
                            $"{baseAddress}/oidc/signin/popup",
                            $"{baseAddress}/oidc/signin/silent",
                            $"{baseAddress}/signin-oidc"
                        }).ToArray(),
                        PostLogoutRedirectUris = portalBaseAddresses.SelectMany(baseAddress => new string[]
                        {
                            $"{baseAddress}/oidc/signout/popup",
                            $"{baseAddress}/signout-callback-oidc"
                        }).ToArray(),
                        AllowedScopes = new List<string>
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            "roles",
                            "daas-api-v1"
                        }
                    },
                    new Client
                    {
                        ClientId = "daas-ui-dev",
                        ClientName = "DaaS Portal (development)",

                        AllowedGrantTypes = GrantTypes.Implicit,
                        RequireConsent = false,
                        AllowAccessTokensViaBrowser = true,
                        AllowedCorsOrigins = new string[]
                        {
                            "http://localhost:5000"
                        },

                        RedirectUris = new string[]
                        {
                            "http://localhost:5000/oidc/signin/popup",
                            "http://localhost:5000/oidc/signin/silent",
                            "http://localhost:5000/signin-oidc"
                        },
                        PostLogoutRedirectUris = new string[]
                        {
                            "http://localhost:5000/oidc/signout/popup",
                            "http://localhost:5000/signout-callback-oidc"
                        },
                        AllowedScopes = new List<string>
                        {
                            IdentityServerConstants.StandardScopes.OpenId,
                            IdentityServerConstants.StandardScopes.Profile,
                            "roles",
                            "daas-api-v1"
                        }
                    }
                })
                .AddAspNetIdentity<AppUser>()
                .AddProfileService<AppUserProfileService>();
        }

        /// <summary>
        ///     Configure the application pipeline.
        /// </summary>
        /// <param name="app">
        ///     The application pipeline builder.
        /// </param>
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHostingEnvironment hostingEnvironment, IApplicationLifetime appLifetime)
        {
            if (hostingEnvironment.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Home/Error");

            ILogger logger = loggerFactory.CreateLogger<Startup>();
            logger.LogInformation("Will allow CORS for portal URLs: {PortalURLs}",
                (CorsOptions.UI ?? String.Empty).Split(';')
            );

            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}
