using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using HCLog.Net;

namespace Katasec.DStream.Plugin;

/// <summary>
/// Utility methods for converting between protobuf structures and strongly-typed configuration objects
/// </summary>
public static class ConfigurationUtils
{
    /// <summary>
    /// Converts a protobuf Struct to a strongly-typed configuration object
    /// </summary>
    /// <typeparam name="TConfig">The type of configuration object to create</typeparam>
    /// <param name="protoStruct">The protobuf Struct to convert</param>
    /// <param name="logger">Optional logger for debug information</param>
    /// <returns>A new instance of TConfig populated with values from the protobuf Struct</returns>
    public static TConfig ConvertToTypedConfig<TConfig>(Struct protoStruct, HCLogger? logger = null) where TConfig : class, new()
    {
        logger?.Debug("Converting protobuf Struct to typed configuration of type {0}", typeof(TConfig).Name);
        
        if (protoStruct == null)
        {
            logger?.Debug("Protobuf Struct is null, returning default configuration");
            return new TConfig();
        }
        
        try
        {
            // Convert the protobuf struct to a dictionary
            var dict = protoStruct.Fields.ToDictionary(
                kvp => kvp.Key,
                kvp => ConvertValue(kvp.Value, logger));
            
            // Log the dictionary contents for debugging
            if (logger != null)
            {
                logger.Debug("Converted dictionary contents:");
                foreach (var kvp in dict)
                {
                    logger.Debug("  {0}: {1}", kvp.Key, kvp.Value?.ToString() ?? "null");
                }
            }
            
            // Use System.Text.Json to convert the dictionary to the typed config
            var jsonString = JsonSerializer.Serialize(dict);
            logger?.Debug("Serialized JSON: {0}", jsonString);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<TConfig>(jsonString, options) ?? new TConfig();
            logger?.Debug("Successfully converted to typed configuration");
            
            return result;
        }
        catch (Exception ex)
        {
            logger?.Error("Error converting protobuf Struct to typed configuration: {0}", ex.Message);
            return new TConfig();
        }
    }
    
    /// <summary>
    /// Converts a protobuf Value to a .NET object
    /// </summary>
    /// <param name="value">The protobuf Value to convert</param>
    /// <param name="logger">Optional logger for debug information</param>
    /// <returns>The equivalent .NET object</returns>
    private static object? ConvertValue(Value value, HCLogger? logger = null)
    {
        if (logger == null)
        {
            throw new ApplicationException("No logger configured");
        }
        
        if (value == null)
        {
            return null;
        }
        
        switch (value.KindCase)
        {
            case Value.KindOneofCase.NullValue:
                logger?.Debug("Converting NullValue");
                return null;
                
            case Value.KindOneofCase.NumberValue:
                logger?.Debug("Converting NumberValue: {0}", value.NumberValue);
                return value.NumberValue;
                
            case Value.KindOneofCase.StringValue:
                logger?.Debug("Converting StringValue: {0}", value.StringValue);
                return value.StringValue;
                
            case Value.KindOneofCase.BoolValue:
                logger?.Debug("Converting BoolValue: {0}", value.BoolValue);
                return value.BoolValue;
                
            case Value.KindOneofCase.StructValue:
                logger?.Debug("Converting StructValue");
                return value.StructValue.Fields.ToDictionary(
                    kvp => kvp.Key,
                    kvp => ConvertValue(kvp.Value, logger));
                
            case Value.KindOneofCase.ListValue:
                logger?.Debug("Converting ListValue with {0} items", value.ListValue.Values.Count);
                return value.ListValue.Values.Select(v => ConvertValue(v, logger)).ToList();
                
            default:
                logger?.Debug("Unknown Value kind: {0}", value.KindCase);
                return null;
        }
    }
    
    /// <summary>
    /// Converts a strongly-typed configuration object to a protobuf Struct
    /// </summary>
    /// <typeparam name="TConfig">The type of configuration object</typeparam>
    /// <param name="config">The configuration object to convert</param>
    /// <param name="logger">Optional logger for debug information</param>
    /// <returns>A protobuf Struct representing the configuration</returns>
    public static Struct ConvertFromTypedConfig<TConfig>(TConfig config, HCLogger? logger = null) where TConfig : class
    {
        if (logger == null)
        {
            throw new ApplicationException("No logger configured");
        }
        logger?.Debug("Converting typed configuration to protobuf Struct");
        
        if (config == null)
        {
            logger?.Debug("Configuration object is null, returning empty Struct");
            return new Struct();
        }
        
        try
        {
            // Serialize the config object to JSON
            var jsonString = JsonSerializer.Serialize(config);
            logger?.Debug("Serialized JSON: {0}", jsonString);
            
            // Deserialize to a dictionary
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            
            // Create a new Struct
            var result = new Struct();
            
            // Convert each dictionary entry to a protobuf Value
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    result.Fields[kvp.Key] = CreateValue(kvp.Value, logger);
                }
            }
            
            logger?.Debug("Successfully converted to protobuf Struct");
            return result;
        }
        catch (Exception ex)
        {
            logger?.Error("Error converting typed configuration to protobuf Struct: {0}", ex.Message);
            return new Struct();
        }
    }
    
    /// <summary>
    /// Creates a protobuf Value from a .NET object
    /// </summary>
    /// <param name="obj">The .NET object to convert</param>
    /// <param name="logger">Optional logger for debug information</param>
    /// <returns>A protobuf Value representing the object</returns>
    private static Value CreateValue(object obj, HCLogger? logger = null)
    {
        if (logger == null)
        {
            throw new ApplicationException("No logger configured");
        }
        if (obj == null)
        {
            return Value.ForNull();
        }
        
        // Handle different types
        switch (obj)
        {
            case string s:
                logger?.Debug("Creating StringValue: {0}", s);
                return Value.ForString(s);
                
            case bool b:
                logger?.Debug("Creating BoolValue: {0}", b);
                return Value.ForBool(b);
                
            case int i:
                logger?.Debug("Creating NumberValue from int: {0}", i);
                return Value.ForNumber(i);
                
            case long l:
                logger?.Debug("Creating NumberValue from long: {0}", l);
                return Value.ForNumber(l);
                
            case double d:
                logger?.Debug("Creating NumberValue from double: {0}", d);
                return Value.ForNumber(d);
                
            case float f:
                logger?.Debug("Creating NumberValue from float: {0}", f);
                return Value.ForNumber(f);
                
            case decimal dec:
                logger?.Debug("Creating NumberValue from decimal: {0}", dec);
                return Value.ForNumber((double)dec);
                
            case Dictionary<string, object> dict:
                logger?.Debug("Creating StructValue from dictionary");
                var structValue = new Struct();
                foreach (var kvp in dict)
                {
                    structValue.Fields[kvp.Key] = CreateValue(kvp.Value, logger);
                }
                return Value.ForStruct(structValue);
                
            case JsonElement element:
                logger?.Debug("Creating Value from JsonElement of kind: {0}", element.ValueKind);
                return CreateValueFromJsonElement(element, logger);
                
            case IEnumerable<object> list:
                logger?.Debug("Creating ListValue");
                var values = new List<Value>();
                foreach (var item in list)
                {
                    values.Add(CreateValue(item, logger));
                }
                
                return Value.ForList(values.ToArray());
                
            default:
                // For complex objects, serialize to JSON and then parse
                logger?.Debug("Creating Value from complex object of type: {0}", obj.GetType().Name);
                var json = JsonSerializer.Serialize(obj);
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                return CreateValueFromJsonElement(jsonElement, logger);
        }
    }
    
    /// <summary>
    /// Creates a protobuf Value from a JsonElement
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <param name="logger">Optional logger for debug information</param>
    /// <returns>A protobuf Value representing the JsonElement</returns>
    private static Value CreateValueFromJsonElement(JsonElement element, HCLogger? logger = null)
    {
        if (logger == null)
        {
            throw new ApplicationException("No logger configured");
        }
        
        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
                return Value.ForNull();
                
            case JsonValueKind.String:
                return Value.ForString(element.GetString() ?? string.Empty);
                
            case JsonValueKind.Number:
                if (element.TryGetDouble(out double number))
                {
                    return Value.ForNumber(number);
                }
                return Value.ForString(element.GetRawText());
                
            case JsonValueKind.True:
                return Value.ForBool(true);
                
            case JsonValueKind.False:
                return Value.ForBool(false);
                
            case JsonValueKind.Object:
                var structValue = new Struct();
                foreach (var property in element.EnumerateObject())
                {
                    structValue.Fields[property.Name] = CreateValueFromJsonElement(property.Value, logger);
                }
                return Value.ForStruct(structValue);
                
            case JsonValueKind.Array:
                var valuesList = new List<Value>();
                foreach (var item in element.EnumerateArray())
                {
                    valuesList.Add(CreateValueFromJsonElement(item, logger));
                }
                return Value.ForList(valuesList.ToArray());
                
            default:
                logger?.Debug("Unhandled JsonValueKind: {0}", element.ValueKind);
                return Value.ForNull();
        }
    }
}
