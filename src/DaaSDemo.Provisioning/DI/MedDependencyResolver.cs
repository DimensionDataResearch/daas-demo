using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;

namespace DaaSDemo.Provisioning.DI
{
    /// <summary>
    ///     An Akka.NET dependency resolver using Microsoft.Extensions.DependencyInjection.
    /// </summary>
    public class MedDependencyResolver
        : IDependencyResolver, INoSerializationVerificationNeeded
    {
        /// <summary>
        ///     Cached actor types by name.
        /// </summary>
        readonly ConcurrentDictionary<string, Type> _typeCache = new ConcurrentDictionary<string, Type>();

        /// <summary>
        ///     Dependency-injection scopes, keyed by actor.
        /// </summary>
        readonly ConcurrentDictionary<ActorBase, IServiceScope> _scopes = new ConcurrentDictionary<ActorBase, IServiceScope>();

        /// <summary>
        ///     The <see cref="ActorSystem"/> for which dependency resolution is being provided.
        /// </summary>
        readonly ActorSystem _system;

        /// <summary>
        ///     A factory for actor-level dependency injection scopes.
        /// </summary>
        readonly IServiceScopeFactory _scopeFactory;

        /// <summary>
        ///     Create a new <see cref="MedDependencyResolver"/>.
        /// </summary>
        /// <param name="system">
        ///     The <see cref="ActorSystem"/> for which dependency resolution is being provided.
        /// </param>
        /// <param name="scopeFactory">
        ///     A factory for actor-level dependency injection scopes.
        /// </param>
        public MedDependencyResolver(ActorSystem system, IServiceScopeFactory scopeFactory)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (scopeFactory == null)
                throw new ArgumentNullException(nameof(scopeFactory));

            _system = system;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        ///     Generate <see cref="Props"/> to create an actor of the specified type.
        /// </summary>
        /// <typeparam name="TActor">
        ///     The type of actor to create.
        /// </typeparam>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public Props Create<TActor>()
            where TActor : ActorBase
        {
            return Create(typeof(TActor));
        }

        /// <summary>
        ///     Generate <see cref="Props"/> to create an actor of the specified type.
        /// </summary>
        /// <param name="actorType">
        ///     The type of actor to create.
        /// </param>
        /// <returns>
        ///     The configured <see cref="Props"/>.
        /// </returns>
        public Props Create(Type actorType) => _system.GetExtension<DIExt>().Props(actorType);
        
        /// <summary>
        ///     Get the CLR type with the specified name.
        /// </summary>
        /// <param name="typeName">
        ///     The CLR type name.
        /// </param>
        /// <returns>
        ///     The CLR <see cref="Type"/>.
        /// </returns>
        public Type GetType(string typeName)
        {
            if (String.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("Argument cannot be null, empty, or entirely composed of whitespace: 'typeName'.", nameof(typeName));
            
            return _typeCache.GetOrAdd(typeName,
                name => Type.GetType(name)
            );
        }

        /// <summary>
        ///     Create a factory delegate for creating instances of the specified actor type.
        /// </summary>
        /// <param name="actorType">
        ///     The type of actor to create.
        /// </param>
        /// <returns>
        ///     A delegate that returns new instances of the actor.
        /// </returns>
        public Func<ActorBase> CreateActorFactory(Type actorType)
        {
            if (actorType == null)
                throw new ArgumentNullException(nameof(actorType));

            return () =>
            {
                IServiceScope scope = _scopeFactory.CreateScope();
                ActorBase actor = (ActorBase)scope.ServiceProvider.GetRequiredService(actorType);

                _scopes[actor] = scope;

                return actor;
            };
        }

        /// <summary>
        ///     Release (dispose) the dependency-injection scope associated with the specified actor.
        /// </summary>
        /// <param name="actor">
        ///     The target actor.
        /// </param>
        public void Release(ActorBase actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            
            IServiceScope scope;
            if (_scopes.TryRemove(actor, out scope))
                scope.Dispose();
        }
    }
}
