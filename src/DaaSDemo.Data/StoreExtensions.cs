using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;

namespace DaaSDemo.Data
{
    using Models.Data;

    /// <summary>
    ///     Extension methods for the RavenDB document store.
    /// </summary>
    public static class StoreExtensions
    {
        /// <summary>
        ///     Create / update initial data.
        /// </summary>
        /// <param name="store">
        ///     The RavenDB document store.
        /// </param>
        public static void CreateInitialData(this IDocumentStore store)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));
            
            using (IDocumentSession session = store.OpenSession())
            {
                if (session.Load<AppRole>(AppRole.MakeId("admin")) == null)
                {
                    session.Store(new AppRole
                    {
                        Name = "Administrator",
                        NormalizedName = "ADMIN"
                    });
                }
                if (session.Load<AppRole>(AppRole.MakeId("user")) == null)
                {
                    session.Store(new AppRole
                    {
                        Name = "User",
                        NormalizedName = "USER"
                    });
                }

                session.SaveChanges();
            }
        }
    }
}
