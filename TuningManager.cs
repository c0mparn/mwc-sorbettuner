using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace SorbetTuner
{
    /// <summary>
    /// Manages all tuning parameters and applies them to the Sorbet 1600 LTD.
    /// </summary>
    public class TuningManager : MonoBehaviour
    {
        public static TuningManager Instance { get; private set; }
        
        private void Update()
        {
            // ALWAYS try to find car if we don't have all components
            if (_sorbetCar == null)
            {
                GameObject root = GameObject.Find("SORBET(190-200psi)");
                if (root != null)
                {
                    FindSorbetCar(root);
                }
            }

            if (!_carFound || _mainCarBody == null || _drivetrain == null) return;

            // Periodic analysis if not finished
            if (!_axlesAnalyzed && _axles != null) AnalyzeAxles();
            if (!_drivetrainAnalyzed && _drivetrain != null) AnalyzeDrivetrain();

            // Handle Nitrous Timing
            if (_nitrousActive && Time.time >= _nitrousEndTime)
            {
                _nitrousActive = false;
                _nitrousCooldownEnd = Time.time + 10f; // 10s cooldown
                ApplyTuning(); // Re-apply normal tuning
                MelonLogger.Msg("Nitrous depleted. Entering cooldown.");
            }

            // Handle Launch Control
            if (LaunchControlEnabled && !IsNitrousActive)
            {
                try
                {
                    float speed = _mainCarBody.velocity.magnitude * 3.6f; // km/h
                    float targetRPM = (speed < 5f) ? LaunchControlRPM : RevLimiter;

                    // Only apply if the target RPM has changed from what we last set
                    if (Mathf.Abs(_lastRPMSet - targetRPM) > 1f)
                    {
                        Type driveType = _drivetrain.GetType();
                        FieldInfo rpmField = driveType.GetField("maxRPM", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (rpmField != null)
                        {
                            rpmField.SetValue(_drivetrain, targetRPM);
                            _lastRPMSet = targetRPM;
                        }
                    }
                }
                catch { }
            }
            else if (_lastRPMSet != -1f)
            {
                // Reset to normal if LC was disabled or Nitrous became active
                try
                {
                    Type driveType = _drivetrain.GetType();
                    FieldInfo rpmField = driveType.GetField("maxRPM", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (rpmField != null)
                    {
                        rpmField.SetValue(_drivetrain, RevLimiter);
                    }
                }
                catch { }
                _lastRPMSet = -1f;
            }

            // Handle Traction Control (Simplified)
            if (TractionControlEnabled && _drivetrain != null)
            {
                try
                {
                    float speed = _mainCarBody.velocity.magnitude * 3.6f;
                    Type driveType = _drivetrain.GetType();
                    FieldInfo rpmField = driveType.GetField("rpm", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (rpmField != null)
                    {
                        float currentRPM = (float)rpmField.GetValue(_drivetrain);
                        FieldInfo torqueField = driveType.GetField("maxTorque", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        
                        if (torqueField != null)
                        {
                            if (currentRPM > 4000f && speed < 30f)
                            {
                                // Reduce torque temporarily
                                torqueField.SetValue(_drivetrain, _originalMaxTorque * 0.5f); // More aggressive reduction
                                _tcActive = true;
                            }
                            else if (_tcActive)
                            {
                                // Restore torque
                                torqueField.SetValue(_drivetrain, _originalMaxTorque * TorqueMultiplier);
                                _tcActive = false;
                            }
                        }
                    }
                }
                catch { }
            }
        }
        
        // Reference to the car
        private GameObject _sorbetCar;
        private Rigidbody _mainCarBody;
        private Component _drivetrain;
        private Component _axles;
        private bool _carFound = false;
        private bool _drivetrainAnalyzed = false;
        private bool _axlesAnalyzed = false;
        
        // Original drivetrain values for reset
        private float _originalMaxTorque = 130f;
        private float _originalMaxPower = 72f; // HP (direct value from game)
        private float _originalMaxRPM = 9000f;
        private float _lastRPMSet = -1f;
        private bool _tcActive = false;
        private float[] _originalGearRatios;
        private float _originalFinalDrive = 3.94f;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // ENGINE TUNING
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Power multiplier (1.0 = stock 72 HP)</summary>
        public float PowerMultiplier { get; set; } = 1.0f;
        
        /// <summary>Torque multiplier (1.0 = stock 130 Nm)</summary>
        public float TorqueMultiplier { get; set; } = 1.0f;
        
        /// <summary>Rev limiter in RPM (stock ~6500)</summary>
        public float RevLimiter { get; set; } = 6500f;
        
        /// <summary>Turbo boost pressure in PSI (0 = no turbo)</summary>
        public float BoostPressure { get; set; } = 0f;
        
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // NITROUS SYSTEM
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Number of nitrous charges (0-5)</summary>
        public int NitrousCharges { get; set; } = 0;
        private bool _nitrousActive = false;
        private float _nitrousEndTime = 0f;
        private float _nitrousCooldownEnd = 0f;
        public bool IsNitrousActive => _nitrousActive && Time.time < _nitrousEndTime;
        public bool CanUseNitrous => NitrousCharges > 0 && !_nitrousActive && Time.time > _nitrousCooldownEnd;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // LAUNCH CONTROL
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Launch control enabled</summary>
        public bool LaunchControlEnabled { get; set; } = false;
        
        /// <summary>Launch control RPM limit</summary>
        public float LaunchControlRPM { get; set; } = 4500f;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // TRACTION CONTROL
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Traction control enabled</summary>
        public bool TractionControlEnabled { get; set; } = false;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // CENTER OF MASS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Center of mass offset X (-0.5 to 0.5)</summary>
        public float CenterOfMassX { get; set; } = 0f;
        
        /// <summary>Center of mass offset Y (-0.5 to 0.5)</summary>
        public float CenterOfMassY { get; set; } = 0f;
        
        /// <summary>Center of mass offset Z (-0.5 to 0.5)</summary>
        public float CenterOfMassZ { get; set; } = 0f;
        
        private Vector3 _originalCenterOfMass = Vector3.zero;
        private bool _comStored = false;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // TRANSMISSION TUNING
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Gear ratios for gears 1-5 (lower = taller gear)</summary>
        public float[] GearRatios { get; set; } = new float[] { 3.45f, 1.85f, 1.36f, 1.07f, 0.80f };
        
        /// <summary>Final drive ratio (affects overall acceleration vs top speed)</summary>
        public float FinalDriveRatio { get; set; } = 3.94f;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // BRAKE TUNING
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Brake force multiplier (higher = more stopping power)</summary>
        public float BrakeForce { get; set; } = 1.0f;
        
        /// <summary>Brake bias (0 = rear, 0.5 = balanced, 1 = front)</summary>
        public float BrakeBias { get; set; } = 0.6f;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // MISC TUNING
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        /// <summary>Weight reduction percentage (0-50%)</summary>
        public float WeightReduction { get; set; } = 0f;
        
        /// <summary>Top speed limiter in km/h (0 = no limit)</summary>
        public float TopSpeedLimit { get; set; } = 0f;
        
        /// <summary>Grip multiplier (1.0 = stock, higher = more grip)</summary>
        public float GripMultiplier { get; set; } = 1.0f;

        /// <summary>Fuel consumption multiplier (1.0 = stock)</summary>
        public float FuelConsumption { get; set; } = 1.0f;

        /// <summary>Speedometer correction multiplier (1.0 = stock)</summary>
        public float SpeedoCorrection { get; set; } = 1.0f;
        
        // Original values for reset
        private float _originalMass = 955f;
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // UNDO STATE
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        private TuningState _previousState = null;
        public bool HasUndoState => _previousState != null;
        
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
            public float BrakeForce;
            public float BrakeBias;
            public float WeightReduction;
            public float GripMultiplier;
            public bool TractionControlEnabled;
            public float CenterOfMassX;
            public float CenterOfMassY;
            public float CenterOfMassZ;
            public float FuelConsumption;
            public float SpeedoCorrection;
        }
        
        private void SaveCurrentStateForUndo()
        {
            _previousState = new TuningState
            {
                PowerMultiplier = this.PowerMultiplier,
                TorqueMultiplier = this.TorqueMultiplier,
                RevLimiter = this.RevLimiter,
                BoostPressure = this.BoostPressure,
                NitrousCharges = this.NitrousCharges,
                GearRatios = (float[])this.GearRatios.Clone(),
                FinalDriveRatio = this.FinalDriveRatio,
                LaunchControlEnabled = this.LaunchControlEnabled,
                LaunchControlRPM = this.LaunchControlRPM,
                BrakeForce = this.BrakeForce,
                BrakeBias = this.BrakeBias,
                WeightReduction = this.WeightReduction,
                GripMultiplier = this.GripMultiplier,
                TractionControlEnabled = this.TractionControlEnabled,
                CenterOfMassX = this.CenterOfMassX,
                CenterOfMassY = this.CenterOfMassY,
                CenterOfMassZ = this.CenterOfMassZ,
                FuelConsumption = this.FuelConsumption,
                SpeedoCorrection = this.SpeedoCorrection
            };
        }
        
        public void Undo()
        {
            if (_previousState == null)
            {
                MelonLogger.Warning("No undo state available!");
                return;
            }
            
            PowerMultiplier = _previousState.PowerMultiplier;
            TorqueMultiplier = _previousState.TorqueMultiplier;
            RevLimiter = _previousState.RevLimiter;
            BoostPressure = _previousState.BoostPressure;
            NitrousCharges = _previousState.NitrousCharges;
            GearRatios = (float[])_previousState.GearRatios.Clone();
            FinalDriveRatio = _previousState.FinalDriveRatio;
            LaunchControlEnabled = _previousState.LaunchControlEnabled;
            LaunchControlRPM = _previousState.LaunchControlRPM;
            BrakeForce = _previousState.BrakeForce;
            BrakeBias = _previousState.BrakeBias;
            WeightReduction = _previousState.WeightReduction;
            GripMultiplier = _previousState.GripMultiplier;
            TractionControlEnabled = _previousState.TractionControlEnabled;
            CenterOfMassX = _previousState.CenterOfMassX;
            CenterOfMassY = _previousState.CenterOfMassY;
            CenterOfMassZ = _previousState.CenterOfMassZ;
            FuelConsumption = _previousState.FuelConsumption;
            SpeedoCorrection = _previousState.SpeedoCorrection;
            
            _previousState = null; // Clear undo state after using it
            
            ApplyTuning();
            MelonLogger.Msg("Undid last tuning change.");
        }
        
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // STOCK VALUES (for reference and reset)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        
        public static class StockValues
        {
            public const float Power = 72f; // HP
            public const float Torque = 130f; // Nm
            public const float RevLimit = 6500f;
            public static readonly float[] GearRatios = new float[] { 3.45f, 1.85f, 1.36f, 1.07f, 0.80f };
            public const float FinalDrive = 3.94f;
            public const float Mass = 955f; // kg (from game)
        }
        
        private void Awake()
        {
            MelonLogger.Msg("TuningManager Awake() called!");
            Instance = this;
            
            // Ensure Mods/SorbetTuner folder exists
            string modsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods/SorbetTuner");
            if (!Directory.Exists(modsDir)) Directory.CreateDirectory(modsDir);
        }
        /// <summary>
        /// Attempts to find the Sorbet car in the scene.
        /// </summary>
        public void FindSorbetCar(GameObject root = null)
        {
            if (root == null)
            {
                if (_carFound && _sorbetCar != null && _mainCarBody != null) return;
                
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains("SORBET") && obj.name.Contains("psi"))
                    {
                        Rigidbody rb = obj.GetComponent<Rigidbody>();
                        if (rb != null && rb.mass > 500f)
                        {
                            root = obj;
                            break;
                        }
                    }
                }
            }

            if (root != null)
            {
                _sorbetCar = root;
                _mainCarBody = root.GetComponent<Rigidbody>();
                if (_mainCarBody != null)
                {
                    _originalMass = _mainCarBody.mass;
                    _carFound = true;
                    
                    MelonLogger.Msg("Found Sorbet main body: " + root.name);
                    // DEBUG: MelonLogger.Msg("   " + _mainCarBody.mass + " kg");
                    
                    // Find components on this object and children
                    FindDrivetrain();
                    FindAxisController();
                    FindCarDynamics();
                    AnalyzeAxles();
                    
                    // Auto-load and apply last settings
                    if (LoadLastSettings())
                    {
                        MelonLogger.Msg("Auto-applying last tuning settings...");
                        ApplyTuning();
                    }
                }
            }
        }
        
        private string GetSettingsPath()
        {
            string dir = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "SorbetTuner");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "last_settings.json");
        }
        
        private void SaveLastSettings()
        {
            try
            {
                var lines = new System.Collections.Generic.List<string>
                {
                    "PowerMultiplier=" + PowerMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "TorqueMultiplier=" + TorqueMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "RevLimiter=" + RevLimiter.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "BoostPressure=" + BoostPressure.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "NitrousCharges=" + NitrousCharges.ToString(),
                    "FinalDriveRatio=" + FinalDriveRatio.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "LaunchControlEnabled=" + LaunchControlEnabled.ToString(),
                    "LaunchControlRPM=" + LaunchControlRPM.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "BrakeForce=" + BrakeForce.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "BrakeBias=" + BrakeBias.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "WeightReduction=" + WeightReduction.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "GripMultiplier=" + GripMultiplier.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "TractionControlEnabled=" + TractionControlEnabled.ToString(),
                    "CenterOfMassX=" + CenterOfMassX.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "CenterOfMassY=" + CenterOfMassY.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "CenterOfMassZ=" + CenterOfMassZ.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "FuelConsumption=" + FuelConsumption.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "SpeedoCorrection=" + SpeedoCorrection.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "GearRatios=" + string.Join(",", System.Array.ConvertAll(GearRatios, g => g.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                };
                
                File.WriteAllLines(GetSettingsPath(), lines.ToArray());
                MelonLogger.Msg("Auto-saved settings to: " + GetSettingsPath());
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to save settings: " + ex.Message);
            }
        }
        
        private bool LoadLastSettings()
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
                    
                    switch (key)
                    {
                        case "PowerMultiplier": PowerMultiplier = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "TorqueMultiplier": TorqueMultiplier = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "RevLimiter": RevLimiter = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "FinalDriveRatio": FinalDriveRatio = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "BrakeForce": BrakeForce = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "BrakeBias": BrakeBias = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "WeightReduction": WeightReduction = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "GripMultiplier": GripMultiplier = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "BoostPressure": BoostPressure = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "NitrousCharges": NitrousCharges = int.Parse(value); break;
                        case "LaunchControlEnabled": LaunchControlEnabled = bool.Parse(value); break;
                        case "LaunchControlRPM": LaunchControlRPM = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "TractionControlEnabled": TractionControlEnabled = bool.Parse(value); break;
                        case "CenterOfMassX": CenterOfMassX = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "CenterOfMassY": CenterOfMassY = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "CenterOfMassZ": CenterOfMassZ = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "FuelConsumption": FuelConsumption = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "SpeedoCorrection": SpeedoCorrection = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                        case "GearRatios":
                            string[] gears = value.Split(',');
                            if (gears.Length == 5)
                            {
                                for (int i = 0; i < 5; i++)
                                    GearRatios[i] = float.Parse(gears[i], System.Globalization.CultureInfo.InvariantCulture);
                            }
                            break;
                    }
                }
                
                MelonLogger.Msg("Loaded last settings from: " + path);
                return true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to load settings: " + ex.Message);
            }
            return false;
        }

        private void FindCarDynamics()
        {
            foreach (Component c in _sorbetCar.GetComponents<Component>())
            {
                if (c.GetType().FullName == "CarDynamics")
                {
                    _carDynamics = c;
                    AnalyzeCarDynamics();
                    break;
                }
            }
        }
        private void AnalyzeCarDynamics()
        {
            if (_carDynamics == null) return;
            
            try
            {
                Type type = _carDynamics.GetType();
                // DEBUG: MelonLogger.Msg("=== CarDynamics Fields ===");
                // Log ALL fields for deep research
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (FieldInfo field in fields)
                {
                    try {
                        object val = field.GetValue(_carDynamics);
                        // DEBUG: MelonLogger.Msg("   " + field.Name + " (" + field.FieldType.Name + ") = " + (val ?? "null"));
                    } catch { }
                }

                // Log all methods with their parameter types
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (MethodInfo m in methods)
                {
                    if (m.DeclaringType == type)
                    {
                        string pars = "";
                        foreach (var p in m.GetParameters()) pars += p.ParameterType.Name + " " + p.Name + ", ";
                        // DEBUG: MelonLogger.Msg("   " + m.Name + "(" + pars.TrimEnd(',', ' ') + ")");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error analyzing CarDynamics: " + ex.Message);
            }
        }

        private Component _carDynamics;

        private void FindAxisController()
        {
            _axisController = _sorbetCar.GetComponent<Component>(); // We'll get it by type name
            foreach (Component c in _sorbetCar.GetComponents<Component>())
            {
                if (c.GetType().FullName == "AxisCarController")
                {
                    _axisController = c;
                    AnalyzeAxisController();
                    break;
                }
            }
        }

        private void AnalyzeAxisController()
        {
            if (_axisController == null) return;
            
            try
            {
                Type type = _axisController.GetType();
                // DEBUG: MelonLogger.Msg("=== AxisCarController Fields ===");
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object val = field.GetValue(_axisController);
                        string name = field.Name.ToLower();
                        if (name.Contains("gear") || name.Contains("ratio") || name.Contains("differential") || 
                            name.Contains("drive") || name.Contains("shift"))
                        {
                            // DEBUG: MelonLogger.Msg("   " + field.Name + " (" + field.FieldType.Name + ") = " + (val != null ? val.ToString() : "null"));
                            
                            // Log array contents
                            if (val is System.Array arr)
                            {
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    MelonLogger.Msg("    [" + i + "] = " + arr.GetValue(i));
                                }
                            }
                        }
                    }
                    catch { }
                }
                MelonLogger.Msg("===============================");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to analyze AxisCarController: " + ex.Message);
            }
        }

        private Component _axisController;

        private void LogAllComponents(GameObject obj)
        {
            // DEBUG: MelonLogger.Msg("=== Deep Search: " + obj.name + " ===");
            SearchHierarchy(obj.transform, 0);
            MelonLogger.Msg("======================================");
        }

        private void SearchHierarchy(Transform t, int depth)
        {
            string indent = new string(' ', depth * 2);
            string name = t.name.ToLower();
            
            // Log everything that looks like a window or frost
            if (name.Contains("window") || name.Contains("glass") || name.Contains("screen") || 
                name.Contains("frost") || name.Contains("ice") || name.Contains("windshield"))
            {
                MelonLogger.Msg(indent + "Found Match: " + t.name);
                foreach (Component c in t.GetComponents<Component>())
                {
                    MelonLogger.Msg(indent + "  - " + c.GetType().FullName);
                    
                    // If it's a Renderer, log material properties
                    if (c is Renderer r)
                    {
                        LogMaterialProperties(r, indent + "    ");
                    }
                }
            }

            for (int i = 0; i < t.childCount; i++)
            {
                SearchHierarchy(t.GetChild(i), depth + 1);
            }
        }

        private void LogMaterialProperties(Renderer r, string indent)
        {
            foreach (Material m in r.materials)
            {
                if (m == null) continue;
                MelonLogger.Msg(indent + "Material: " + m.name + " (Shader: " + m.shader.name + ")");
                
                // We can't easily list all properties without more reflection or specific names
                // but we can try common ones if we find a window shader
                if (m.shader.name.ToLower().Contains("window") || m.shader.name.ToLower().Contains("car"))
                {
                    string[] commonProps = { "_Frost", "_FrostAmount", "_FrostAlpha", "_Snow", "_SnowAmount" };
                    foreach (string prop in commonProps)
                    {
                        if (m.HasProperty(prop))
                        {
                            MelonLogger.Msg(indent + "  Property: " + prop + " = " + m.GetFloat(prop));
                        }
                    }
                }
            }
        }
        
        private void FindDrivetrain()
        {
            if (_sorbetCar == null) return;
            
            Component[] components = _sorbetCar.GetComponents<Component>();
            foreach (Component c in components)
            {
                string typeName = c.GetType().Name;
                
                if (typeName == "Drivetrain")
                {
                    _drivetrain = c;
                    MelonLogger.Msg("Found Drivetrain component!");
                }
                else if (typeName == "Axles")
                {
                    _axles = c;
                    MelonLogger.Msg("Found Axles component!");
                }
            }
            
            if (_drivetrain != null) AnalyzeDrivetrain();
            if (_axles != null) AnalyzeAxles();
            
            if (_drivetrain == null)
            {
                MelonLogger.Warning("Drivetrain component not found!");
            }
        }
        
        /// <summary>
        /// Attempts to remove all frost and snow from car windows and body.
        /// </summary>
        public void RemoveFrost()
        {
            if (!_carFound || _sorbetCar == null)
            {
                MelonLogger.Warning("Cannot remove frost - car not found!");
                return;
            }

            int objectCount = 0;
            int matCount = 0;
            
            // Get all transforms and look for frost objects to disable
            Transform[] allTransforms = _sorbetCar.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                string name = t.name.ToLower();
                if (name.Contains("frost") || name.Contains("frozen"))
                {
                    // Disable the object or its renderer
                    if (t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        objectCount++;
                    }
                }
            }

            // Also check all materials just in case
            Renderer[] renderers = _sorbetCar.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer r in renderers)
            {
                foreach (Material m in r.materials)
                {
                    if (m == null) continue;
                    
                    bool modified = false;
                    string[] frostProps = { "_Frost", "_FrostAmount", "_FrostAlpha", "_Snow", "_SnowAmount" };
                    
                    foreach (string prop in frostProps)
                    {
                        if (m.HasProperty(prop))
                        {
                            m.SetFloat(prop, 0f);
                            modified = true;
                        }
                    }
                    
                    if (modified) matCount++;
                }
            }
            
            MelonLogger.Msg("Defrost complete! Disabled " + objectCount + " objects and modified " + matCount + " materials.");
        }

        private void AnalyzeAxles()
        {
            if (_axles == null) return;
            
            try
            {
                Type axlesType = _axles.GetType();
                // DEBUG: MelonLogger.Msg("=== Axles Fields ===");
                
                FieldInfo[] fields = axlesType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object val = field.GetValue(_axles);
                        // DEBUG: MelonLogger.Msg("   " + field.Name + " (" + field.FieldType.Name + ") = " + (val ?? "null"));
                    }
                    catch { }
                }

                // Scan Axle objects (frontAxle, rearAxle)
                string[] axleFieldNames = { "frontAxle", "rearAxle" };
                foreach (string axName in axleFieldNames)
                {
                    FieldInfo axField = axlesType.GetField(axName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (axField != null)
                    {
                        object axleObj = axField.GetValue(_axles);
                        if (axleObj != null)
                        {
                            Type axType = axleObj.GetType();
                            // DEBUG: MelonLogger.Msg("=== Axle Fields (" + axName + ") ===");
                            foreach (FieldInfo f in axType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                try {
                                    object val = f.GetValue(axleObj);
                                    // DEBUG: MelonLogger.Msg("   " + f.Name + " (" + f.FieldType.Name + ") = " + (val ?? "null"));
                                } catch { }
                            }
                        }
                    }
                }

                // Find allWheels field to analyze individual wheels
                FieldInfo allWheelsField = axlesType.GetField("allWheels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (allWheelsField != null)
                {
                    System.Array wheelsArray = allWheelsField.GetValue(_axles) as System.Array;
                    if (wheelsArray != null && wheelsArray.Length > 0)
                    {
                        MelonLogger.Msg("Found " + wheelsArray.Length + " wheels! Analyzing Wheel type...");
                        object firstWheel = wheelsArray.GetValue(0);
                        if (firstWheel != null)
                        {
                            Type wheelType = firstWheel.GetType();
                            // DEBUG: MelonLogger.Msg("=== Wheel Fields (Sample) ===");
                            
                            foreach (FieldInfo wf in wheelType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                try
                                {
                                    object wval = wf.GetValue(firstWheel);
                                    // DEBUG: MelonLogger.Msg("   " + wf.Name + " (" + wf.FieldType.Name + ") = " + (wval ?? "null"));
                                }
                                catch { }
                            }
                            
                            foreach (MethodInfo m in wheelType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                // Method analysis disabled for release
                            }
                        }
                    }
                }
                
                MelonLogger.Msg("====================");
                _axlesAnalyzed = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to analyze Axles: " + ex.Message);
            }
        }
        
        private void AnalyzeDrivetrain()
        {
            if (_drivetrain == null || _drivetrainAnalyzed) return;
            
            try
            {
                Type driveType = _drivetrain.GetType();
                // DEBUG: MelonLogger.Msg("=== Drivetrain Fields ===");
                
                FieldInfo[] fields = driveType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object val = field.GetValue(_drivetrain);
                        string name = field.Name.ToLower();
                        
                        // Log ALL fields that might be relevant
                        if (name.Contains("rpm") || name.Contains("limit") || name.Contains("cut") || 
                            name.Contains("max") || name.Contains("torque") || name.Contains("power") ||
                            name.Contains("engine") || name.Contains("fuel") || name.Contains("ignit") ||
                            name.Contains("gear") || name.Contains("ratio") || name.Contains("trans") ||
                            name.Contains("final") || name.Contains("drive"))
                        {
                            // DEBUG: MelonLogger.Msg("   " + field.Name + " (" + field.FieldType.Name + ") = " + (val != null ? val.ToString() : "null"));
                            
                            // Log array contents (added for gearRatios research)
                            if (val is System.Array arr)
                            {
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    MelonLogger.Msg("    [" + i + "] = " + arr.GetValue(i));
                                }
                            }
                        }

                        // Store original values if not already stored
                        if (name == "maxtorque" && val != null) _originalMaxTorque = Convert.ToSingle(val);
                        if (name == "maxpower" && val != null) _originalMaxPower = Convert.ToSingle(val);
                        if (name == "maxrpm" && val != null) _originalMaxRPM = Convert.ToSingle(val);
                        if (name == "gearratios" && val is float[] ratios) _originalGearRatios = (float[])ratios.Clone();
                        if (name == "finaldriveratio" && val != null) _originalFinalDrive = Convert.ToSingle(val);
                    }
                    catch { }
                }
                
                MelonLogger.Msg("=========================");
                _drivetrainAnalyzed = true;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to analyze drivetrain: " + ex.Message);
            }
        }

        /// <summary>
        /// Applies all current tuning settings to the car.
        /// </summary>
        public void ApplyTuning()
        {
            if (!_carFound || _mainCarBody == null)
            {
                MelonLogger.Warning("Cannot apply tuning - car not found! Searching again...");
                _carFound = false;
                FindSorbetCar();
                
                if (!_carFound)
                {
                    return;
                }
            }

            // Save current state for undo
            SaveCurrentStateForUndo();
            
            MelonLogger.Msg("Applying tuning settings...");
            
            // Apply weight reduction
            ApplyWeightReduction();
            
            // Apply drivetrain tuning (power/torque/rev limit + turbo boost)
            ApplyDrivetrainTuning();

            // Apply transmission tuning (gear ratios/final drive)
            ApplyTransmissionTuning();

            // Apply center of mass
            ApplyCenterOfMass();

            // Apply grip tuning
            ApplyGripTuning();
            
            // Apply brake tuning
            ApplyBrakeTuning();
            
            MelonLogger.Msg("Tuning applied!");
            MelonLogger.Msg("  Rev Limit: " + RevLimiter.ToString("F0") + " RPM");
            MelonLogger.Msg("  Final Drive: " + FinalDriveRatio.ToString("F2"));
            if (CenterOfMassX != 0 || CenterOfMassY != 0 || CenterOfMassZ != 0)
                MelonLogger.Msg("  CoM Offset: X=" + CenterOfMassX.ToString("F2") + " Y=" + CenterOfMassY.ToString("F2") + " Z=" + CenterOfMassZ.ToString("F2"));
            
            // Auto-save settings
            SaveLastSettings();
        }

        private void ApplyBrakeTuning()
        {
            if (_axles == null) return;

            try
            {
                Type axlesType = _axles.GetType();
                FieldInfo allWheelsField = axlesType.GetField("allWheels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (allWheelsField == null)
                {
                    MelonLogger.Warning("Could not find allWheels field for brake tuning");
                    return;
                }

                System.Array wheels = allWheelsField.GetValue(_axles) as System.Array;
                if (wheels == null || wheels.Length == 0)
                {
                    MelonLogger.Warning("No wheels found for brake tuning");
                    return;
                }

                // Get original brake values from first wheel if not captured yet
                float originalBrakeTorque = 2000f; // Default estimate
                object firstWheel = wheels.GetValue(0);
                if (firstWheel != null)
                {
                    Type wheelType = firstWheel.GetType();
                    
                    // Try to find brake-related fields
                    string[] brakeFieldNames = { "brakeFrictionTorque", "maxBrakeTorque", "brakeTorque", "brakeForce", "braking" };
                    foreach (string fieldName in brakeFieldNames)
                    {
                        FieldInfo field = wheelType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            object val = field.GetValue(firstWheel);
                            if (val != null)
                            {
                                originalBrakeTorque = Convert.ToSingle(val);
                                MelonLogger.Msg("Found brake field: " + fieldName + " = " + originalBrakeTorque);
                                break;
                            }
                        }
                    }
                }

                // Apply brake tuning to each wheel
                int wheelsModified = 0;
                for (int i = 0; i < wheels.Length; i++)
                {
                    object wheel = wheels.GetValue(i);
                    if (wheel == null) continue;
                    
                    Type wheelType = wheel.GetType();
                    
                    // Determine if front or rear wheel based on index or name
                    // Typically: 0,1 = front, 2,3 = rear
                    bool isFront = i < 2;
                    
                    // Calculate brake force with bias
                    // BrakeBias = 0.6 means 60% front, 40% rear
                    float biasMultiplier = isFront ? BrakeBias * 2f : (1f - BrakeBias) * 2f;
                    float newBrakeTorque = originalBrakeTorque * BrakeForce * biasMultiplier;
                    
                    // Try different field names
                    string[] brakeFieldNames = { "brakeFrictionTorque", "maxBrakeTorque", "brakeTorque", "brakeForce" };
                    foreach (string fieldName in brakeFieldNames)
                    {
                        FieldInfo field = wheelType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (field != null)
                        {
                            field.SetValue(wheel, newBrakeTorque);
                            wheelsModified++;
                            break;
                        }
                    }
                }
                
                MelonLogger.Msg("Applied brake tuning to " + wheelsModified + " wheels (Force: " + BrakeForce.ToString("F1") + "x, Bias: " + (BrakeBias * 100).ToString("F0") + "% front)");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to apply brake tuning: " + ex.Message);
            }
        }

        private void ApplyTransmissionTuning()
        {
            if (_drivetrain == null)
            {
                MelonLogger.Warning("Drivetrain not found - cannot apply transmission tuning");
                return;
            }

            try
            {
                Type driveType = _drivetrain.GetType();
                
                // Final Drive
                FieldInfo finalDriveField = driveType.GetField("finalDriveRatio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (finalDriveField != null)
                {
                    finalDriveField.SetValue(_drivetrain, FinalDriveRatio);
                    MelonLogger.Msg("Set finalDriveRatio: " + FinalDriveRatio.ToString("F2"));
                }
                else
                {
                    MelonLogger.Warning("finalDriveRatio field not found");
                }

                // Gear Ratios
                FieldInfo gearRatiosField = driveType.GetField("gearRatios", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (gearRatiosField != null)
                {
                    float[] currentRatios = (float[])gearRatiosField.GetValue(_drivetrain);
                    if (currentRatios != null && currentRatios.Length >= 7)
                    {
                        // Game array structure: [0]=Reverse, [1]=Neutral, [2-6]=Gears 1-5
                        // Our GearRatios property is [0-4] = Gears 1-5
                        float[] newRatios = (float[])currentRatios.Clone();
                        for (int i = 0; i < 5; i++)
                        {
                            newRatios[i + 2] = GearRatios[i];
                        }
                        gearRatiosField.SetValue(_drivetrain, newRatios);
                        MelonLogger.Msg("Updated gear ratios 1-5.");
                    }
                    else
                    {
                        MelonLogger.Warning("gearRatios array not found or too short.");
                    }
                }
                else
                {
                    MelonLogger.Warning("gearRatios field not found");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to apply transmission tuning: " + ex.Message);
            }
        }

        private void ApplyGripTuning()
        {
            if (_axles == null) return;

            try
            {
                Type axlesType = _axles.GetType();
                FieldInfo allWheelsField = axlesType.GetField("allWheels", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
                if (allWheelsField != null)
                {
                    System.Array wheels = allWheelsField.GetValue(_axles) as System.Array;
                    if (wheels != null)
                    {
                        foreach (object wheel in wheels)
                        {
                            if (wheel == null) continue;
                            Type wheelType = wheel.GetType();

                            // Modify grip factors
                            string[] gripFields = { "forwardGripFactor", "sidewaysGripFactor" };
                            foreach (string fieldName in gripFields)
                            {
                                FieldInfo field = wheelType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (field != null)
                                {
                                    field.SetValue(wheel, GripMultiplier);
                                }
                            }
                        }
                        MelonLogger.Msg("Applied grip multiplier " + GripMultiplier.ToString("F1") + "x to " + wheels.Length + " wheels.");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to apply grip tuning: " + ex.Message);
            }
        }

        private void ApplyWeightReduction()
        {
            if (_mainCarBody == null) return;
            
            float reductionMultiplier = 1f - (WeightReduction / 100f);
            float newMass = _originalMass * reductionMultiplier;
            newMass = Mathf.Max(newMass, 500f);
            
            MelonLogger.Msg("Adjusting mass: " + _originalMass.ToString("F0") + " kg -> " + newMass.ToString("F0") + " kg");
            _mainCarBody.mass = newMass;
        }
        
        private void ApplyDrivetrainTuning()
        {
            if (_drivetrain == null)
            {
                MelonLogger.Warning("Drivetrain not found - cannot apply power/torque tuning");
                return;
            }
            
            try
            {
                Type driveType = _drivetrain.GetType();
                
                // Apply torque multiplier
                FieldInfo torqueField = driveType.GetField("maxTorque", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (torqueField != null)
                {
                    float newTorque = _originalMaxTorque * TorqueMultiplier;
                    torqueField.SetValue(_drivetrain, newTorque);
                    MelonLogger.Msg("Set maxTorque: " + _originalMaxTorque.ToString("F0") + " -> " + newTorque.ToString("F0") + " Nm");
                }
                else
                {
                    MelonLogger.Warning("maxTorque field not found");
                }
                
                // Apply power multiplier with turbo boost (maxPower is in HP directly)
                FieldInfo powerField = driveType.GetField("maxPower", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (powerField != null)
                {
                    // Include turbo boost: 5% power increase per PSI
                    float turboMultiplier = 1f + (BoostPressure * 0.05f);
                    float newPower = _originalMaxPower * PowerMultiplier * turboMultiplier;
                    powerField.SetValue(_drivetrain, newPower);
                    string turboInfo = BoostPressure > 0 ? " [+" + BoostPressure.ToString("F0") + " PSI]" : "";
                    MelonLogger.Msg("Set maxPower: " + _originalMaxPower.ToString("F0") + " HP -> " + newPower.ToString("F0") + " HP" + turboInfo);
                }
                else
                {
                    MelonLogger.Warning("maxPower field not found");
                }

                // Apply rev limiter (maxRPM)
                FieldInfo rpmField = driveType.GetField("maxRPM", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (rpmField != null)
                {
                    rpmField.SetValue(_drivetrain, RevLimiter);
                    MelonLogger.Msg("Set maxRPM: " + _originalMaxRPM.ToString("F0") + " -> " + RevLimiter.ToString("F0") + " RPM");
                }
                else
                {
                    MelonLogger.Warning("maxRPM field not found");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Failed to apply drivetrain tuning: " + ex.Message);
            }
        }

        /// <summary>
        /// Resets all tuning to stock values.
        /// </summary>
        public void ResetToStock()
        {
            PowerMultiplier = 1.0f;
            TorqueMultiplier = 1.0f;
            RevLimiter = StockValues.RevLimit;
            BoostPressure = 0f;
            NitrousCharges = 0;
            GearRatios = (float[])StockValues.GearRatios.Clone();
            FinalDriveRatio = StockValues.FinalDrive;
            LaunchControlEnabled = false;
            LaunchControlRPM = 4500f;
            BrakeForce = 1.0f;
            BrakeBias = 0.6f;
            WeightReduction = 0f;
            TopSpeedLimit = 0f;
            GripMultiplier = 1.0f;
            TractionControlEnabled = false;
            CenterOfMassX = 0f;
            CenterOfMassY = 0f;
            CenterOfMassZ = 0f;
            
            // Reset mass
            if (_mainCarBody != null)
            {
                _mainCarBody.mass = _originalMass;
                _mainCarBody.centerOfMass = _originalCenterOfMass;
            }
            
            // Reset drivetrain
            if (_drivetrain != null)
            {
                try
                {
                    Type driveType = _drivetrain.GetType();
                    
                    FieldInfo torqueField = driveType.GetField("maxTorque", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (torqueField != null) torqueField.SetValue(_drivetrain, _originalMaxTorque);
                    
                    FieldInfo powerField = driveType.GetField("maxPower", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (powerField != null) powerField.SetValue(_drivetrain, _originalMaxPower);

                    FieldInfo rpmField = driveType.GetField("maxRPM", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (rpmField != null) rpmField.SetValue(_drivetrain, _originalMaxRPM);
                    
                    FieldInfo finalDriveField = driveType.GetField("finalDriveRatio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (finalDriveField != null) finalDriveField.SetValue(_drivetrain, _originalFinalDrive);
                    
                    FieldInfo gearRatiosField = driveType.GetField("gearRatios", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (gearRatiosField != null && _originalGearRatios != null)
                    {
                        gearRatiosField.SetValue(_drivetrain, (float[])_originalGearRatios.Clone());
                    }
                }
                catch { }
            }
            
            MelonLogger.Msg("Reset all tuning to stock values.");
        }

        public float GetEffectiveHP() 
        { 
            float baseHP = StockValues.Power * PowerMultiplier;
            float turboHP = baseHP * (BoostPressure * 0.05f);
            float finalHP = baseHP + turboHP;
            if (IsNitrousActive) finalHP *= 1.5f; // 50% boost from N2O
            return finalHP;
        }
        public float GetEffectiveTorque() { return StockValues.Torque * TorqueMultiplier; }
        public bool IsCarFound() { return _carFound && _mainCarBody != null; }
        
        public string GetCarName()
        {
            if (_mainCarBody == null) return "Not Found";
            string driveStatus = _drivetrain != null ? " âœ“" : " âš ";
            return "SORBET (" + _mainCarBody.mass.ToString("F0") + " kg)" + driveStatus;
        }
        
        public void RefreshCarReference()
        {
            _carFound = false;
            _sorbetCar = null;
            _mainCarBody = null;
            _drivetrain = null;
            _drivetrainAnalyzed = false;
            _comStored = false;
            FindSorbetCar();
        }

        private void ApplyCenterOfMass()
        {
            if (_mainCarBody == null) return;

            if (!_comStored)
            {
                _originalCenterOfMass = _mainCarBody.centerOfMass;
                _comStored = true;
            }

            _mainCarBody.centerOfMass = _originalCenterOfMass + new Vector3(CenterOfMassX, CenterOfMassY, CenterOfMassZ);
            MelonLogger.Msg("Applied Center of Mass offset: " + new Vector3(CenterOfMassX, CenterOfMassY, CenterOfMassZ).ToString());
        }

        public void ActivateNitrous()
        {
            if (!CanUseNitrous) return;

            NitrousCharges--;
            _nitrousActive = true;
            _nitrousEndTime = Time.time + 3f; // 3 seconds of boost
            ApplyTuning(); // Re-apply with nitrous boost
            MelonLogger.Msg("NITROUS ACTIVATED! Charges left: " + NitrousCharges);
        }
    }
}

