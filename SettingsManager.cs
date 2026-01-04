using System;
using System.IO;
using System.Collections.Generic;
using MelonLoader;

namespace SorbetTuner
{
    /// <summary>
    /// Handles saving/loading settings and managing undo state.
    /// </summary>
    public class SettingsManager
    {
        private readonly TuningManager _manager;
        private TuningState _previousState;
        
        public bool HasUndoState => _previousState != null;
        
        public SettingsManager(TuningManager manager)
        {
            _manager = manager;
            EnsureDirectoryExists();
        }
        
        private void EnsureDirectoryExists()
        {
            string dir = GetSettingsDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        
        private string GetSettingsDirectory()
        {
            return Path.Combine(
                Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                "SorbetTuner"
            );
        }
        
        private string GetSettingsPath()
        {
            return Path.Combine(GetSettingsDirectory(), "last_settings.json");
        }
        
        /// <summary>
        /// Saves current state for undo functionality.
        /// </summary>
        public void SaveUndoState()
        {
            _previousState = new TuningState
            {
                PowerMultiplier = _manager.Engine.PowerMultiplier,
                TorqueMultiplier = _manager.Engine.TorqueMultiplier,
                RevLimiter = _manager.Engine.RevLimiter,
                BoostPressure = _manager.Engine.BoostPressure,
                NitrousCharges = _manager.Engine.NitrousCharges,
                GearRatios = (float[])_manager.Transmission.GearRatios.Clone(),
                FinalDriveRatio = _manager.Transmission.FinalDriveRatio,
                LaunchControlEnabled = _manager.Transmission.LaunchControlEnabled,
                LaunchControlRPM = _manager.Transmission.LaunchControlRPM,
                TractionControlEnabled = _manager.Transmission.TractionControlEnabled,
                BrakeForce = _manager.Brakes.BrakeForce,
                BrakeBias = _manager.Brakes.BrakeBias,
                WeightReduction = _manager.Handling.WeightReduction,
                GripMultiplier = _manager.Handling.GripMultiplier,
                CenterOfMassX = _manager.Handling.CenterOfMassX,
                CenterOfMassY = _manager.Handling.CenterOfMassY,
                CenterOfMassZ = _manager.Handling.CenterOfMassZ
            };
        }
        
        /// <summary>
        /// Restores the previous state (undo).
        /// </summary>
        public void Undo()
        {
            if (_previousState == null)
            {
                MelonLogger.Warning("No undo state available!");
                return;
            }
            
            _manager.Engine.PowerMultiplier = _previousState.PowerMultiplier;
            _manager.Engine.TorqueMultiplier = _previousState.TorqueMultiplier;
            _manager.Engine.RevLimiter = _previousState.RevLimiter;
            _manager.Engine.BoostPressure = _previousState.BoostPressure;
            _manager.Engine.NitrousCharges = _previousState.NitrousCharges;
            _manager.Transmission.GearRatios = (float[])_previousState.GearRatios.Clone();
            _manager.Transmission.FinalDriveRatio = _previousState.FinalDriveRatio;
            _manager.Transmission.LaunchControlEnabled = _previousState.LaunchControlEnabled;
            _manager.Transmission.LaunchControlRPM = _previousState.LaunchControlRPM;
            _manager.Transmission.TractionControlEnabled = _previousState.TractionControlEnabled;
            _manager.Brakes.BrakeForce = _previousState.BrakeForce;
            _manager.Brakes.BrakeBias = _previousState.BrakeBias;
            _manager.Handling.WeightReduction = _previousState.WeightReduction;
            _manager.Handling.GripMultiplier = _previousState.GripMultiplier;
            _manager.Handling.CenterOfMassX = _previousState.CenterOfMassX;
            _manager.Handling.CenterOfMassY = _previousState.CenterOfMassY;
            _manager.Handling.CenterOfMassZ = _previousState.CenterOfMassZ;
            
            _previousState = null;
            
            _manager.ApplyTuning();
            MelonLogger.Msg("Undid last tuning change.");
        }
        
        /// <summary>
        /// Saves current settings to disk.
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                var lines = new List<string>
                {
                    FormatSetting("PowerMultiplier", _manager.Engine.PowerMultiplier),
                    FormatSetting("TorqueMultiplier", _manager.Engine.TorqueMultiplier),
                    FormatSetting("RevLimiter", _manager.Engine.RevLimiter),
                    FormatSetting("BoostPressure", _manager.Engine.BoostPressure),
                    $"NitrousCharges={_manager.Engine.NitrousCharges}",
                    FormatSetting("FinalDriveRatio", _manager.Transmission.FinalDriveRatio),
                    $"LaunchControlEnabled={_manager.Transmission.LaunchControlEnabled}",
                    FormatSetting("LaunchControlRPM", _manager.Transmission.LaunchControlRPM),
                    $"TractionControlEnabled={_manager.Transmission.TractionControlEnabled}",
                    FormatSetting("BrakeForce", _manager.Brakes.BrakeForce),
                    FormatSetting("BrakeBias", _manager.Brakes.BrakeBias),
                    FormatSetting("WeightReduction", _manager.Handling.WeightReduction),
                    FormatSetting("GripMultiplier", _manager.Handling.GripMultiplier),
                    FormatSetting("CenterOfMassX", _manager.Handling.CenterOfMassX),
                    FormatSetting("CenterOfMassY", _manager.Handling.CenterOfMassY),
                    FormatSetting("CenterOfMassZ", _manager.Handling.CenterOfMassZ),
                    $"GearRatios={string.Join(",", Array.ConvertAll(_manager.Transmission.GearRatios, g => g.ToString(System.Globalization.CultureInfo.InvariantCulture)))}"
                };
                
                File.WriteAllLines(GetSettingsPath(), lines.ToArray());
                MelonLogger.Msg($"Auto-saved settings to: {GetSettingsPath()}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads settings from disk.
        /// </summary>
        public bool LoadSettings()
        {
            try
            {
                string path = GetSettingsPath();
                if (!File.Exists(path)) return false;
                
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length != 2) continue;
                    
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    
                    ApplySetting(key, value);
                }
                
                MelonLogger.Msg($"Loaded last settings from: {path}");
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load settings: {ex.Message}");
            }
            return false;
        }
        
        private string FormatSetting(string name, float value)
        {
            return $"{name}={value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }
        
        private void ApplySetting(string key, string value)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            
            switch (key)
            {
                case "PowerMultiplier": _manager.Engine.PowerMultiplier = float.Parse(value, culture); break;
                case "TorqueMultiplier": _manager.Engine.TorqueMultiplier = float.Parse(value, culture); break;
                case "RevLimiter": _manager.Engine.RevLimiter = float.Parse(value, culture); break;
                case "BoostPressure": _manager.Engine.BoostPressure = float.Parse(value, culture); break;
                case "NitrousCharges": _manager.Engine.NitrousCharges = int.Parse(value); break;
                case "FinalDriveRatio": _manager.Transmission.FinalDriveRatio = float.Parse(value, culture); break;
                case "LaunchControlEnabled": _manager.Transmission.LaunchControlEnabled = bool.Parse(value); break;
                case "LaunchControlRPM": _manager.Transmission.LaunchControlRPM = float.Parse(value, culture); break;
                case "TractionControlEnabled": _manager.Transmission.TractionControlEnabled = bool.Parse(value); break;
                case "BrakeForce": _manager.Brakes.BrakeForce = float.Parse(value, culture); break;
                case "BrakeBias": _manager.Brakes.BrakeBias = float.Parse(value, culture); break;
                case "WeightReduction": _manager.Handling.WeightReduction = float.Parse(value, culture); break;
                case "GripMultiplier": _manager.Handling.GripMultiplier = float.Parse(value, culture); break;
                case "CenterOfMassX": _manager.Handling.CenterOfMassX = float.Parse(value, culture); break;
                case "CenterOfMassY": _manager.Handling.CenterOfMassY = float.Parse(value, culture); break;
                case "CenterOfMassZ": _manager.Handling.CenterOfMassZ = float.Parse(value, culture); break;
                case "GearRatios":
                    string[] gears = value.Split(',');
                    if (gears.Length == 5)
                    {
                        for (int i = 0; i < 5; i++)
                            _manager.Transmission.GearRatios[i] = float.Parse(gears[i], culture);
                    }
                    break;
            }
        }
        
        private class TuningState
        {
            public float PowerMultiplier;
            public float TorqueMultiplier;
            public float RevLimiter;
            public float BoostPressure;
            public int NitrousCharges;
            public float[] GearRatios;
            public float FinalDriveRatio;
            public bool LaunchControlEnabled;
            public float LaunchControlRPM;
            public bool TractionControlEnabled;
            public float BrakeForce;
            public float BrakeBias;
            public float WeightReduction;
            public float GripMultiplier;
            public float CenterOfMassX;
            public float CenterOfMassY;
            public float CenterOfMassZ;
        }
    }
}
