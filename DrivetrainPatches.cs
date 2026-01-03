using System;
using System.Reflection;
using UnityEngine;
using MelonLoader;
using HarmonyLib;

namespace SorbetTuner
{
    /// <summary>
    /// Harmony patches for modifying the Drivetrain component.
    /// </summary>
    public static class DrivetrainPatches
    {
        private static bool _initialized = false;
        private static Type _drivetrainType = null;
        private static FieldInfo _maxTorqueField = null;
        private static FieldInfo _maxPowerField = null;
        private static FieldInfo _engineTorqueField = null;
        private static FieldInfo _enginePowerField = null;
        
        /// <summary>
        /// Initialize by finding the Drivetrain type and its fields.
        /// </summary>
        public static void Initialize(Component drivetrainComponent)
        {
            if (_initialized) return;
            
            try
            {
                _drivetrainType = drivetrainComponent.GetType();
                MelonLogger.Msg("=== Drivetrain Analysis ===");
                MelonLogger.Msg("Type: " + _drivetrainType.FullName);
                
                // Get all fields
                FieldInfo[] fields = _drivetrainType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MelonLogger.Msg("Fields found: " + fields.Length);
                
                foreach (FieldInfo field in fields)
                {
                    string fieldName = field.Name.ToLower();
                    object value = null;
                    
                    try
                    {
                        value = field.GetValue(drivetrainComponent);
                    }
                    catch { }
                    
                    // Log fields related to power/torque/engine
                    if (fieldName.Contains("torque") || fieldName.Contains("power") || 
                        fieldName.Contains("engine") || fieldName.Contains("max") ||
                        fieldName.Contains("rpm") || fieldName.Contains("force"))
                    {
                        string valueStr = value != null ? value.ToString() : "null";
                        MelonLogger.Msg("  " + field.Name + " (" + field.FieldType.Name + ") = " + valueStr);
                        
                        // Cache important fields
                        if (fieldName.Contains("maxtorque") || fieldName == "torque")
                        {
                            _maxTorqueField = field;
                        }
                        else if (fieldName.Contains("maxpower") || fieldName == "power")
                        {
                            _maxPowerField = field;
                        }
                        else if (fieldName.Contains("enginetorque"))
                        {
                            _engineTorqueField = field;
                        }
                        else if (fieldName.Contains("enginepower"))
                        {
                            _enginePowerField = field;
                        }
                    }
                }
                
                // Also check properties
                PropertyInfo[] properties = _drivetrainType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MelonLogger.Msg("Properties found: " + properties.Length);
                
                foreach (PropertyInfo prop in properties)
                {
                    string propName = prop.Name.ToLower();
                    
                    if (propName.Contains("torque") || propName.Contains("power") || 
                        propName.Contains("engine") || propName.Contains("max") ||
                        propName.Contains("rpm"))
                    {
                        object value = null;
                        try { value = prop.GetValue(drivetrainComponent, null); } catch { }
                        string valueStr = value != null ? value.ToString() : "null";
                        MelonLogger.Msg("  [Prop] " + prop.Name + " (" + prop.PropertyType.Name + ") = " + valueStr);
                    }
                }
                
                MelonLogger.Msg("===========================");
                _initialized = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to analyze Drivetrain: " + ex.Message);
            }
        }
        
        /// <summary>
        /// Apply power/torque multipliers to the drivetrain.
        /// </summary>
        public static void ApplyTuning(Component drivetrainComponent, float powerMultiplier, float torqueMultiplier)
        {
            if (drivetrainComponent == null)
            {
                MelonLogger.Warning("Drivetrain component is null!");
                return;
            }
            
            if (!_initialized)
            {
                Initialize(drivetrainComponent);
            }
            
            try
            {
                Type driveType = drivetrainComponent.GetType();
                
                // Try common field names for power/torque
                string[] torqueFieldNames = new string[] { "maxTorque", "MaxTorque", "engineMaxTorque", "torque", "Torque", "maxEngineTorque" };
                string[] powerFieldNames = new string[] { "maxPower", "MaxPower", "engineMaxPower", "power", "Power", "maxEnginePower", "maxHP", "horsePower" };
                
                bool appliedTorque = false;
                bool appliedPower = false;
                
                // Try to find and modify torque
                foreach (string fieldName in torqueFieldNames)
                {
                    FieldInfo field = driveType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && (field.FieldType == typeof(float) || field.FieldType == typeof(double)))
                    {
                        float currentValue = Convert.ToSingle(field.GetValue(drivetrainComponent));
                        float newValue = currentValue * torqueMultiplier;
                        field.SetValue(drivetrainComponent, Convert.ChangeType(newValue, field.FieldType));
                        MelonLogger.Msg("Set " + fieldName + ": " + currentValue + " -> " + newValue);
                        appliedTorque = true;
                        break;
                    }
                }
                
                // Try to find and modify power
                foreach (string fieldName in powerFieldNames)
                {
                    FieldInfo field = driveType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field != null && (field.FieldType == typeof(float) || field.FieldType == typeof(double)))
                    {
                        float currentValue = Convert.ToSingle(field.GetValue(drivetrainComponent));
                        float newValue = currentValue * powerMultiplier;
                        field.SetValue(drivetrainComponent, Convert.ChangeType(newValue, field.FieldType));
                        MelonLogger.Msg("Set " + fieldName + ": " + currentValue + " -> " + newValue);
                        appliedPower = true;
                        break;
                    }
                }
                
                if (!appliedTorque)
                {
                    MelonLogger.Warning("Could not find torque field to modify");
                }
                if (!appliedPower)
                {
                    MelonLogger.Warning("Could not find power field to modify");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to apply drivetrain tuning: " + ex.Message);
            }
        }
    }
}
