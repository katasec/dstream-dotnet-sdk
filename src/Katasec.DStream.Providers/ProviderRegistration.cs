using System;
using System.Collections.Generic;
using Katasec.DStream.Plugin.Interfaces;
using Katasec.DStream.Providers.Input;
using Katasec.DStream.Providers.Output;

namespace Katasec.DStream.Providers
{
    /// <summary>
    /// Provides registration and discovery of input and output providers
    /// </summary>
    public static class ProviderRegistry
    {
        private static readonly Dictionary<string, Func<IInput>> _inputProviders = new();
        private static readonly Dictionary<string, Func<IOutput>> _outputProviders = new();

        /// <summary>
        /// Static constructor to register standard providers
        /// </summary>
        static ProviderRegistry()
        {
            // Register standard input providers
            RegisterInputProvider<NullInputProvider>("null");
            
            // Register standard output providers
            RegisterOutputProvider<ConsoleOutputProvider>("console");
        }

        /// <summary>
        /// Registers an input provider with the specified name
        /// </summary>
        /// <typeparam name="T">Type of input provider</typeparam>
        /// <param name="name">Name to register the provider under</param>
        public static void RegisterInputProvider<T>(string name) where T : IInput, new()
        {
            _inputProviders[name] = () => new T();
        }

        /// <summary>
        /// Registers an output provider with the specified name
        /// </summary>
        /// <typeparam name="T">Type of output provider</typeparam>
        /// <param name="name">Name to register the provider under</param>
        public static void RegisterOutputProvider<T>(string name) where T : IOutput, new()
        {
            _outputProviders[name] = () => new T();
        }

        /// <summary>
        /// Gets an input provider by name
        /// </summary>
        /// <param name="name">Name of the provider</param>
        /// <returns>The input provider instance</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the provider is not found</exception>
        public static IInput GetInputProvider(string name)
        {
            if (_inputProviders.TryGetValue(name, out var factory))
            {
                return factory();
            }
            
            throw new KeyNotFoundException($"Input provider '{name}' not found");
        }

        /// <summary>
        /// Gets an output provider by name
        /// </summary>
        /// <param name="name">Name of the provider</param>
        /// <returns>The output provider instance</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the provider is not found</exception>
        public static IOutput GetOutputProvider(string name)
        {
            if (_outputProviders.TryGetValue(name, out var factory))
            {
                return factory();
            }
            
            throw new KeyNotFoundException($"Output provider '{name}' not found");
        }

        /// <summary>
        /// Checks if an input provider with the specified name exists
        /// </summary>
        /// <param name="name">Name of the provider</param>
        /// <returns>True if the provider exists, false otherwise</returns>
        public static bool HasInputProvider(string name)
        {
            return _inputProviders.ContainsKey(name);
        }

        /// <summary>
        /// Checks if an output provider with the specified name exists
        /// </summary>
        /// <param name="name">Name of the provider</param>
        /// <returns>True if the provider exists, false otherwise</returns>
        public static bool HasOutputProvider(string name)
        {
            return _outputProviders.ContainsKey(name);
        }

        /// <summary>
        /// Gets all registered input provider names
        /// </summary>
        /// <returns>Collection of provider names</returns>
        public static IEnumerable<string> GetInputProviderNames()
        {
            return _inputProviders.Keys;
        }

        /// <summary>
        /// Gets all registered output provider names
        /// </summary>
        /// <returns>Collection of provider names</returns>
        public static IEnumerable<string> GetOutputProviderNames()
        {
            return _outputProviders.Keys;
        }
    }
}
