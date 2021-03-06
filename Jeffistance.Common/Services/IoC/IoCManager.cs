using System;
using Microsoft.Extensions.Logging;

namespace Jeffistance.Common.Services.IoC
{
    /// <summary>
    /// Our epic dependency manager.
    /// </summary>
    public static class IoCManager
    {
        private static readonly DependencyCollection _dependencyCollection = new DependencyCollection();

        /// <summary>
        /// Register a type to its implementation.
        /// </summary>
        /// <typeparam name="TInterface">The type that will be resolvable.</typeparam>
        /// <typeparam name="TImplementation">
        /// The type that will be constructed as implementation.
        /// </typeparam>
        public static void Register<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            _dependencyCollection.Register<TInterface, TImplementation>();
        }

        /// <summary>
        /// Resolve given dependency.
        /// </summary>
        /// <typeparam name="T">The interface that will be resolved.</typeparam>
        public static T Resolve<T>()
        {
            return _dependencyCollection.Resolve<T>();
        }

        public static void AddClientLogging(Action<ILoggingBuilder> configure)
        {
            _dependencyCollection.AddClientLogging(configure);
        }

        public static void AddServerLogging(Action<ILoggingBuilder> configure)
        {
            _dependencyCollection.AddServerLogging(configure);
        }

        public static ILogger GetClientLogger()
        {
            return _dependencyCollection.GetClientLogger();
        }

        public static ILogger GetServerLogger()
        {
            return _dependencyCollection.GetServerLogger();
        }

        /// <summary>
        /// Build all the necessary dependencies.
        /// </summary>
        public static void BuildGraph()
        {
            _dependencyCollection.BuildGraph();
        }

        public static void Clear()
        {
            _dependencyCollection.Clear();
        }
    }
}
