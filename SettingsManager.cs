using System;
using System.IO;
using System.Collections.Generic;
using MSCLoader;

namespace SorbetTuner
{
    /// <summary>
    /// Handles saving/loading settings and managing undo/redo state.
    /// </summary>
    public class SettingsManager
    {
        private readonly TuningManager _manager;
        
        // Stack-based undo/redo system
        private readonly Stack<TuningState> _undoStack = new Stack<TuningState>();
        private readonly Stack<TuningState> _redoStack = new Stack<TuningState>();
        private const int MaxUndoLevels = 10;
        
        public bool HasUndoState => _undoStack.Count > 0;
        public bool HasRedoState => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;
        
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
            // Use Mods/SorbetTuner folder for consistency with preset storage
            // .NET 3.5 only supports 2-argument Path.Combine
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
            path = Path.Combine(path, "SorbetTuner");
            return path;
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
            var state = CaptureCurrentState();
            
            // Limit undo stack size
            if (_undoStack.Count >= MaxUndoLevels)
            {
                // Remove oldest state (convert to array, skip first, rebuild stack)
                var states = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = states.Length - 2; i >= 0; i--)
                {
                    _undoStack.Push(states[i]);
                }
            }
            
            _undoStack.Push(state);
            
            // Clear redo stack when new action is taken
            _redoStack.Clear();
        }
        
        private TuningState CaptureCurrentState()
        {
            return new TuningState
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
                DrivetrainMode = _manager.Transmission.DrivetrainMode,
                BrakeForce = _manager.Brakes.BrakeForce,
                BrakeBias = _manager.Brakes.BrakeBias,
                WeightReduction = _manager.Handling.WeightReduction,
                GripMultiplier = _manager.Handling.GripMultiplier,
                CenterOfMassX = _manager.Handling.CenterOfMassX,
                CenterOfMassY = _manager.Handling.CenterOfMassY,
                CenterOfMassZ = _manager.Handling.CenterOfMassZ
            };
        }
        
        private void ApplyState(TuningState state)
        {
            _manager.Engine.PowerMultiplier = state.PowerMultiplier;
            _manager.Engine.TorqueMultiplier = state.TorqueMultiplier;
            _manager.Engine.RevLimiter = state.RevLimiter;
            _manager.Engine.BoostPressure = state.BoostPressure;
            _manager.Engine.NitrousCharges = state.NitrousCharges;
            _manager.Transmission.GearRatios = (float[])state.GearRatios.Clone();
            _manager.Transmission.FinalDriveRatio = state.FinalDriveRatio;
            _manager.Transmission.LaunchControlEnabled = state.LaunchControlEnabled;
            _manager.Transmission.LaunchControlRPM = state.LaunchControlRPM;
            _manager.Transmission.TractionControlEnabled = state.TractionControlEnabled;
            _manager.Transmission.DrivetrainMode = state.DrivetrainMode;
            _manager.Brakes.BrakeForce = state.BrakeForce;
            _manager.Brakes.BrakeBias = state.BrakeBias;
            _manager.Handling.WeightReduction = state.WeightReduction;
            _manager.Handling.GripMultiplier = state.GripMultiplier;
            _manager.Handling.CenterOfMassX = state.CenterOfMassX;
            _manager.Handling.CenterOfMassY = state.CenterOfMassY;
            _manager.Handling.CenterOfMassZ = state.CenterOfMassZ;
        }
        
        /// <summary>
        /// Restores the previous state (undo).
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count == 0)
            {
                ModConsole.Print("Warning: No undo state available!");
                return;
            }
            
            // Save current state to redo stack before undoing
            _redoStack.Push(CaptureCurrentState());
            
            // Pop and apply the previous state
            var previousState = _undoStack.Pop();
            ApplyState(previousState);
            
            _manager.ApplyTuning();
            ModConsole.Print($"Undid tuning change. ({_undoStack.Count} undo states remaining)");
        }
        
        /// <summary>
        /// Restores the next state (redo).
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count == 0)
            {
                ModConsole.Print("Warning: No redo state available!");
                return;
            }
            
            // Save current state to undo stack before redoing
            _undoStack.Push(CaptureCurrentState());
            
            // Pop and apply the redo state
            var redoState = _redoStack.Pop();
            ApplyState(redoState);
            
            _manager.ApplyTuning();
            ModConsole.Print($"Redid tuning change. ({_redoStack.Count} redo states remaining)");
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
                    $"DrivetrainMode={(int)_manager.Transmission.DrivetrainMode}",
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
                ModConsole.Print($"Auto-saved settings to: {GetSettingsPath()}");
            }
            catch (Exception ex)
            {
                ModConsole.Error($"Failed to save settings: {ex.Message}");
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
                
                ModConsole.Print($"Loaded last settings from: {path}");
                return true;
            }
            catch (Exception ex)
            {
                ModConsole.Error($"Failed to load settings: {ex.Message}");
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
                case "DrivetrainMode": _manager.Transmission.DrivetrainMode = (DrivetrainMode)int.Parse(value); break;
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
            public DrivetrainMode DrivetrainMode;
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
