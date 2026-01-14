using System;
using System.Collections.Generic;
using System.Reflection;
using MSCLoader;

namespace SorbetTuner
{
    /// <summary>
    /// Caches reflection lookups and provides safe field access methods.
    /// Addresses the fragile reflection-heavy approach by caching discovered fields.
    /// </summary>
    public static class ReflectionCache
    {
        private static readonly Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        private static readonly HashSet<string> _missingFields = new HashSet<string>();
        
        private const BindingFlags AllFields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        
        /// <summary>
        /// Gets a cached FieldInfo, trying multiple field names.
        /// Returns null if no matching field is found.
        /// </summary>
        public static FieldInfo GetField(Type type, string[] fieldNames)
        {
            if (type == null || fieldNames == null || fieldNames.Length == 0)
                return null;
            
            // Build cache key
            string cacheKey = type.FullName + ":" + fieldNames[0];
            
            // Check cache first
            if (_fieldCache.TryGetValue(cacheKey, out FieldInfo cached))
                return cached;
            
            // Check if we already know this field doesn't exist
            if (_missingFields.Contains(cacheKey))
                return null;
            
            // Try each field name
            foreach (string fieldName in fieldNames)
            {
                FieldInfo field = type.GetField(fieldName, AllFields);
                if (field != null)
                {
                    _fieldCache[cacheKey] = field;
                    return field;
                }
            }
            
            // Mark as missing so we don't search again
            _missingFields.Add(cacheKey);
            return null;
        }
        
        /// <summary>
        /// Gets a field value safely with null handling.
        /// Returns defaultValue if the field doesn't exist or value is null.
        /// </summary>
        public static T GetValue<T>(object obj, string[] fieldNames, T defaultValue = default)
        {
            if (obj == null) return defaultValue;
            
            try
            {
                FieldInfo field = GetField(obj.GetType(), fieldNames);
                if (field == null) return defaultValue;
                
                object value = field.GetValue(obj);
                if (value == null) return defaultValue;
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Sets a field value safely with validation.
        /// Returns true if the value was set successfully.
        /// </summary>
        public static bool SetValue(object obj, string[] fieldNames, object value, bool logOnFailure = true)
        {
            if (obj == null) return false;
            
            try
            {
                FieldInfo field = GetField(obj.GetType(), fieldNames);
                if (field == null)
                {
                    if (logOnFailure)
                        ModConsole.Print($"Warning: Field not found: {fieldNames[0]} on {obj.GetType().Name}");
                    return false;
                }
                
                // Convert value to correct type if needed
                object convertedValue = Convert.ChangeType(value, field.FieldType);
                field.SetValue(obj, convertedValue);
                return true;
            }
            catch (Exception ex)
            {
                if (logOnFailure)
                    ModConsole.Error($"Failed to set {fieldNames[0]}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Sets a field value and logs the change.
        /// </summary>
        public static bool SetValueWithLog(object obj, string[] fieldNames, object newValue, string displayName = null)
        {
            if (obj == null) return false;
            
            try
            {
                FieldInfo field = GetField(obj.GetType(), fieldNames);
                if (field == null)
                {
                    ModConsole.Print($"Warning: {displayName ?? fieldNames[0]} field not found");
                    return false;
                }
                
                object oldValue = field.GetValue(obj);
                object convertedValue = Convert.ChangeType(newValue, field.FieldType);
                field.SetValue(obj, convertedValue);
                
                ModConsole.Print($"Set {displayName ?? fieldNames[0]}: {oldValue} -> {newValue}");
                return true;
            }
            catch (Exception ex)
            {
                ModConsole.Error($"Failed to set {displayName ?? fieldNames[0]}: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a field exists on the given type.
        /// </summary>
        public static bool HasField(Type type, string[] fieldNames)
        {
            return GetField(type, fieldNames) != null;
        }
        
        /// <summary>
        /// Gets all fields from a type that match a predicate.
        /// </summary>
        public static FieldInfo[] FindFields(Type type, Func<FieldInfo, bool> predicate)
        {
            if (type == null) return new FieldInfo[0];
            
            List<FieldInfo> matches = new List<FieldInfo>();
            foreach (FieldInfo field in type.GetFields(AllFields))
            {
                if (predicate(field))
                    matches.Add(field);
            }
            return matches.ToArray();
        }
        
        /// <summary>
        /// Clears the cache. Useful if game components are reloaded.
        /// </summary>
        public static void ClearCache()
        {
            _fieldCache.Clear();
            _missingFields.Clear();
        }
    }
}
