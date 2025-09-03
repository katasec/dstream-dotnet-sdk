using System;
using System.Collections.Generic;
using System.Linq;
using Katasec.DStream.Plugin.Interfaces;

namespace Katasec.DStream.Plugin
{
    /// <summary>
    /// Registry for input and output providers.
    /// Allows plugins to dynamically register and resolve providers by name.
    /// </summary>
    public class ProviderRegistry
    {
        private static readonly Dictionary<string, Type> _inputProviders = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Type> _outputProviders = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Registers an input provider implementation
        /// </summary>
        /// <typeparam name="T">The type implementing IInput</typeparam>
        /// <param name="name">The name to register the provider under</param>
        /// <exception cref="ArgumentException">Thrown if a provider with the same name is already registered</exception>
        public static void RegisterInputProvider<T>(string name) where T : IInput
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Provider name cannot be null or empty", nameof(name));

            if (_inputProviders.ContainsKey(name))
                throw new ArgumentException($"An input provider with name '{name}' is already registered", nameof(name));

            _inputProviders[name] = typeof(T);
        }

        /// <summary>
        /// Registers an output provider implementation
        /// </summary>
        /// <typeparam name="T">The type implementing IOutput</typeparam>
        /// <param name="name">The name to register the provider under</param>
        /// <exception cref="ArgumentException">Thrown if a provider with the same name is already registered</exception>
        public static void RegisterOutputProvider<T>(string name) where T : IOutput
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Provider name cannot be null or empty", nameof(name));

            if (_outputProviders.ContainsKey(name))
                throw new ArgumentException($"An output provider with name '{name}' is already registered", nameof(name));

            _outputProviders[name] = typeof(T);
        }

        /// <summary>
        /// Creates an instance of an input provider by name
        /// </summary>
        /// <param name="name">The name of the registered provider</param>
        /// <returns>A new instance of the input provider</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no provider is registered with the given name</exception>
        /// <exception cref="InvalidOperationException">Thrown if the provider cannot be instantiated</exception>
        public static IInput CreateInputProvider(string name)
        {
            if (!_inputProviders.TryGetValue(name, out var type))
                throw new KeyNotFoundException($"No input provider registered with name '{name}'");

            try
            {
                return (IInput)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create input provider '{name}'", ex);
            }
        }

        /// <summary>
        /// Creates an instance of an output provider by name
        /// </summary>
        /// <param name="name">The name of the registered provider</param>
        /// <returns>A new instance of the output provider</returns>
        /// <exception cref="KeyNotFoundException">Thrown if no provider is registered with the given name</exception>
        /// <exception cref="InvalidOperationException">Thrown if the provider cannot be instantiated</exception>
        public static IOutput CreateOutputProvider(string name)
        {
            if (!_outputProviders.TryGetValue(name, out var type))
                throw new KeyNotFoundException($"No output provider registered with name '{name}'");

            try
            {
                return (IOutput)Activator.CreateInstance(type)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create output provider '{name}'", ex);
            }
        }

        /// <summary>
        /// Gets all registered input provider names
        /// </summary>
        /// <returns>A collection of registered input provider names</returns>
        public static IEnumerable<string> GetRegisteredInputProviders()
        {
            return _inputProviders.Keys.ToList();
        }

        /// <summary>
        /// Gets all registered output provider names
        /// </summary>
        /// <returns>A collection of registered output provider names</returns>
        public static IEnumerable<string> GetRegisteredOutputProviders()
        {
            return _outputProviders.Keys.ToList();
        }

        /// <summary>
        /// Clears all registered providers
        /// </summary>
        public static void Clear()
        {
            _inputProviders.Clear();
            _outputProviders.Clear();
        }
    }
}
