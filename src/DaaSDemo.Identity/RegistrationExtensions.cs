using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DaaSDemo.Identity
{
    using Models.Data;
    using Stores;

    /// <summary>
    ///     Extension methods for registering and configuring DaaS Identity components and services.
    /// </summary>
    public static class RegistrationExtensions
    {
        /// <summary>
        ///     Add the DaaS Identity data-stores to ASP.NET Core Identity.
        /// </summary>
        /// <param name="identityBuilder">
        ///     The ASP.NET Core Identity builder.
        /// </param>
        /// <returns>
        ///     The ASP.NET Core Identity builder (enables inline use).
        /// </returns>
        public static IdentityBuilder AddDaaSIdentityStores(this IdentityBuilder identityBuilder)
        {
            if (identityBuilder == null)
                throw new ArgumentNullException(nameof(identityBuilder));
            
            identityBuilder.Services.AddScoped<IUserStore<AppUser>, RavenUserStore>();
            identityBuilder.Services.AddScoped<IRoleStore<AppRole>, RavenRoleStore>();

            return identityBuilder;
        }
    }
}
