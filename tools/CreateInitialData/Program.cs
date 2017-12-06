using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Serilog;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DaaSDemo.Tools.CreateInitialData
{
    using Common.Options;
    using Data;
    using Identity.Stores;
    using Models.Data;

    /// <summary>
    ///     Tool to create initial data in the DaaS management database.
    /// </summary>
    static class Program
    {
        /// <summary>
        ///     The main program entry-point.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task"/> representing program execution.
        /// </returns>
        static async Task Main(string[] args)
        {
            ConfigureLogging();
            
            try
            {
                if (args.Length != 2)
                {
                    ShowUsage();

                    return;
                }

                string emailAddress = args[0];
                string password = args[1];

                var user = new AppUser
                {
                    UserName = emailAddress,
                    Email = emailAddress,
                    EmailConfirmed = true,
                    IsSuperUser = true
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
            catch (Exception createInitialDataFailed)
            {
                Log.Error(createInitialDataFailed, "Failed to create initial data. {ErrorMessage}", createInitialDataFailed.Message);
            }
        }

        /// <summary>
        ///     Show program usage information.
        /// </summary>
        static void ShowUsage()
        {
            Console.WriteLine("Usage:\n\tdotnet {0}.dll <super-user-email> <super-user-passsword>",
                typeof(Program).Assembly.GetName().Name
            );
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
