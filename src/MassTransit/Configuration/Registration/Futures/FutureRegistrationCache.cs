namespace MassTransit.Registration.Futures
{
    using System;
    using Automatonymous;
    using Internals.Extensions;
    using MassTransit.Futures;
    using Metadata;


    public static class FutureRegistrationCache
    {
        public static void Register(Type futureType, IContainerRegistrar registrar)
        {
            Cached.Instance.GetOrAdd(futureType).Register(registrar);
        }

        public static void AddFuture(IRegistrationConfigurator configurator, Type futureType, Type futureDefinitionType)
        {
            Cached.Instance.GetOrAdd(futureType).AddFuture(configurator, futureDefinitionType);
        }

        static CachedRegistration Factory(Type type)
        {
            if (!type.HasInterface(typeof(SagaStateMachine<FutureState>)))
                throw new ArgumentException($"The type is not a future: {TypeMetadataCache.GetShortName(type)}", nameof(type));

            return (CachedRegistration)Activator.CreateInstance(typeof(CachedRegistration<>).MakeGenericType(type));
        }


        static class Cached
        {
            internal static readonly RegistrationCache<CachedRegistration> Instance = new RegistrationCache<CachedRegistration>(Factory);
        }


        interface CachedRegistration
        {
            void Register(IContainerRegistrar registrar);
            void AddFuture(IRegistrationConfigurator registry, Type futureDefinitionType);
        }


        class CachedRegistration<TFuture> :
            CachedRegistration
            where TFuture : MassTransitStateMachine<FutureState>
        {
            public void Register(IContainerRegistrar registrar)
            {
                registrar.RegisterFuture<TFuture>();
            }

            public void AddFuture(IRegistrationConfigurator registry, Type futureDefinitionType)
            {
                var configurator = registry.AddFuture<TFuture>(futureDefinitionType);
            }
        }
    }
}
