using Akka.Actor;
using Akka.Actor.Dsl;
using Akka.Configuration;
using HTTPlease;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace DaaSDemo.TestHarness
{
    using Common.Options;
    using Data;
    using Identity.Stores;
    using KubeClient;
    using KubeClient.Models;
    using Models.Data;
    using Provisioning.Actors;
    using Provisioning.Filters;
    using Provisioning.Messages;
    using Raven.Client.Documents;

    /// <summary>
    ///     A general-purpose test harness.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     The asynchronous program entry-point.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing program execution.
        /// </returns>
        static async Task AsyncMain()
        {
            const string emailAddress = "tintoy@tintoy.io";
            const string password = "password";

            var user = new AppUser
            {
                UserName = emailAddress,
                Email = emailAddress,
                EmailConfirmed = true
            };

            Log.Information("Building...");
            IServiceProvider serviceProvider = BuildServiceProvider();

            IDocumentStore documentStore = serviceProvider.GetRequiredService<IDocumentStore>();
            documentStore.CreateInitialData();

            using (var scope = serviceProvider.CreateScope())
            {
                Log.Information("Resolving...");
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

                Log.Information("Creating...");
                IdentityResult result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    Log.Information("CreateResult: {@Result}", result);
                    
                    return;
                }

                Log.Information("Adding password...");
                result = await userManager.AddPasswordAsync(user, password);
                if (!result.Succeeded)
                {
                    Log.Information("AddPasswordResult: {@Result}", result);
                    
                    return;
                }

                result = await userManager.AddToRolesAsync(user, new string[]
                {
                    "user",
                    "admin"
                });
                if (!result.Succeeded)
                {
                    Log.Information("AddToRolesResult: {@Result}", result);
                    
                    return;
                }
            }

            using (var scope = serviceProvider.CreateScope())
            {
                Log.Information("Resolving...");
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                IPasswordValidator<AppUser> passwordValidator = scope.ServiceProvider.GetRequiredService<IPasswordValidator<AppUser>>();

                Log.Information("Validating password...");
                IdentityResult result = await passwordValidator.ValidateAsync(userManager, user, password);
                if (!result.Succeeded)
                {
                    Log.Information("ValidateResult: {@Result}", result);
                    
                    return;
                }
            }

            Log.Information("Done...");
        }

        /// <summary>
        ///     Build a service provider for use in the test harness.
        /// </summary>
        /// <returns>
        ///     The service provider.
        /// </returns>
        static IServiceProvider BuildServiceProvider()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(
                    Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json")
                )
                .Build();

            IServiceCollection services = new ServiceCollection();

            services.AddDaaSOptions(configuration);
            services.AddDaaSDataAccess();

            services.AddLogging(logging =>
            {
                logging.AddSerilog(Log.Logger);
            });

            services.AddDataProtection(dataProtection =>
            {
                dataProtection.ApplicationDiscriminator = "DaaS.Demo";
            });

            services
                .AddIdentity<AppUser, AppRole>(identity =>
                {
                    identity.Password.RequireUppercase = false;
                    identity.Password.RequireLowercase = false;
                    identity.Password.RequireDigit = false;
                    identity.Password.RequireNonAlphanumeric = false;
                })
                .AddUserStore<RavenUserStore>()
                .AddRoleStore<RavenRoleStore>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        static void Main()
        {
            SynchronizationContext.SetSynchronizationContext(
                new SynchronizationContext()
            );
            ConfigureLogging();

            try
            {
                AsyncMain().GetAwaiter().GetResult();
            }
            catch (AggregateException unexpectedErrorFromTask)
            {
                foreach (Exception unexpectedError in unexpectedErrorFromTask.InnerExceptions)
                    Log.Error(unexpectedError, "Unexpected error: {ErrorMessage}", unexpectedError.Message);
            }
            catch (Exception unexpectedError)
            {
                Log.Error(unexpectedError, "Unexpected error: {ErrorMessage}", unexpectedError.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        ///     Configure the global logger.
        /// </summary>
        static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.LiterateConsole()
                .CreateLogger();
        }
    }
}
