using System;
using System.IO;
using UnityEngine;
using MelonLoader;
using SorbetTuner.Tuners;

namespace SorbetTuner
{
    /// <summary>
    /// Coordinates all tuning systems for the Sorbet 1600 LTD.
    /// Delegates actual work to specialized tuner classes.
    /// </summary>
    public class TuningManager : MonoBehaviour
    {
        public static TuningManager Instance { get; private set; }
        
        // Sub-managers
        public CarFinder CarFinder { get; private set; }
        public EngineTuner Engine { get; private set; }
        public TransmissionTuner Transmission { get; private set; }
        public BrakeTuner Brakes { get; private set; }
        public HandlingTuner Handling { get; private set; }
        public SettingsManager Settings { get; private set; }
        
        // Quick access properties
        public bool HasUndoState => Settings?.HasUndoState ?? false;
        public bool HasRedoState => Settings?.HasRedoState ?? false;
        public bool CanUseNitrous => Engine?.CanUseNitrous ?? false;
        public bool IsNitrousActive => Engine?.IsNitrousActive ?? false;
        
        private void Awake()
        {
            MelonLogger.Msg("TuningManager Awake() - Initializing sub-managers...");
            Instance = this;
            
            // Initialize sub-managers
            CarFinder = new CarFinder();
            Engine = new EngineTuner(CarFinder);
            Transmission = new TransmissionTuner(CarFinder);
            Brakes = new BrakeTuner(CarFinder);
            Handling = new HandlingTuner(CarFinder);
            Settings = new SettingsManager(this);
            
            // Ensure mods directory exists
            string modsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods/SorbetTuner");
            if (!Directory.Exists(modsDir)) Directory.CreateDirectory(modsDir);
        }
        
        private void Update()
        {
            // Try to find car if not found
            if (!CarFinder.IsCarFound)
            {
                GameObject root = GameObject.Find("SORBET(190-200psi)");
                if (root != null)
                {
                    CarFinder.FindSorbetCar(root);
                    
                    // Auto-load and apply settings on first find
                    if (CarFinder.IsCarFound && Settings.LoadSettings())
                    {
                        MelonLogger.Msg("Auto-applying last tuning settings...");
                        ApplyTuning();
                    }
                }
                return;
            }
            
            if (CarFinder.CarBody == null || CarFinder.Drivetrain == null) return;
            
            // Periodic analysis
            CarFinder.PeriodicAnalysis();
            
            // Nitrous timing
            if (Engine.UpdateNitrous())
            {
                ApplyTuning(); // Re-apply when nitrous depletes
            }
            
            // Launch control and traction control
            Transmission.Update(
                Engine.RevLimiter, 
                CarFinder.OriginalMaxTorque, 
                Engine.TorqueMultiplier, 
                Engine.IsNitrousActive
            );
        }
        
        /// <summary>
        /// Applies all current tuning settings to the car.
        /// </summary>
        public void ApplyTuning()
        {
            if (!CarFinder.IsCarFound)
            {
                MelonLogger.Warning("Cannot apply tuning - car not found! Searching again...");
                CarFinder.FindSorbetCar();
                
                if (!CarFinder.IsCarFound) return;
            }
            
            Settings.SaveUndoState();
            
            MelonLogger.Msg("Applying tuning settings...");
            
            Handling.Apply();
            Engine.Apply();
            Transmission.Apply();
            Brakes.Apply();
            
            MelonLogger.Msg("Tuning applied!");
            MelonLogger.Msg($"  Rev Limit: {Engine.RevLimiter:F0} RPM");
            MelonLogger.Msg($"  Final Drive: {Transmission.FinalDriveRatio:F2}");
            
            if (Handling.CenterOfMassX != 0 || Handling.CenterOfMassY != 0 || Handling.CenterOfMassZ != 0)
            {
                MelonLogger.Msg($"  CoM Offset: X={Handling.CenterOfMassX:F2} Y={Handling.CenterOfMassY:F2} Z={Handling.CenterOfMassZ:F2}");
            }
            
            Settings.SaveSettings();
        }
        
        /// <summary>
        /// Resets all tuning to stock values.
        /// </summary>
        public void ResetToStock()
        {
            Engine.ResetToStock();
            Transmission.ResetToStock();
            Brakes.ResetToStock();
            Handling.ResetToStock();
            
            // Re-apply stock values to car
            if (CarFinder.IsCarFound && CarFinder.Drivetrain != null)
            {
                ReflectionCache.SetValue(CarFinder.Drivetrain, TuningConstants.FieldNames.Torque, CarFinder.OriginalMaxTorque, false);
                ReflectionCache.SetValue(CarFinder.Drivetrain, TuningConstants.FieldNames.Power, CarFinder.OriginalMaxPower, false);
                ReflectionCache.SetValue(CarFinder.Drivetrain, TuningConstants.FieldNames.RPM, CarFinder.OriginalMaxRPM, false);
                ReflectionCache.SetValue(CarFinder.Drivetrain, TuningConstants.FieldNames.FinalDrive, CarFinder.OriginalFinalDrive, false);
                
                if (CarFinder.OriginalGearRatios != null)
                {
                    var gearField = ReflectionCache.GetField(CarFinder.Drivetrain.GetType(), TuningConstants.FieldNames.GearRatios);
                    if (gearField != null)
                    {
                        gearField.SetValue(CarFinder.Drivetrain, (float[])CarFinder.OriginalGearRatios.Clone());
                    }
                }
            }
            
            MelonLogger.Msg("Reset all tuning to stock values.");
        }
        
        /// <summary>
        /// Undoes the last tuning change.
        /// </summary>
        public void Undo()
        {
            Settings.Undo();
        }
        
        /// <summary>
        /// Redoes the last undone change.
        /// </summary>
        public void Redo()
        {
            Settings.Redo();
        }
        
        /// <summary>
        /// Activates nitrous if available.
        /// </summary>
        public void ActivateNitrous()
        {
            if (Engine.ActivateNitrous())
            {
                ApplyTuning(); // Re-apply with nitrous boost
            }
        }
        
        /// <summary>
        /// Removes frost from car windows.
        /// </summary>
        public void RemoveFrost()
        {
            Handling.RemoveFrost();
        }
        
        /// <summary>
        /// Refreshes car reference.
        /// </summary>
        public void RefreshCarReference()
        {
            CarFinder.Refresh();
        }
        
        /// <summary>
        /// Gets effective HP (with all modifiers).
        /// </summary>
        public float GetEffectiveHP() => Engine.GetEffectiveHP();
        
        /// <summary>
        /// Gets effective torque.
        /// </summary>
        public float GetEffectiveTorque() => Engine.GetEffectiveTorque();
        
        /// <summary>
        /// Checks if car is found.
        /// </summary>
        public bool IsCarFound() => CarFinder.IsCarFound;
        
        /// <summary>
        /// Gets car display name.
        /// </summary>
        public string GetCarName() => CarFinder.GetCarName();
        
        // ═══════════════════════════════════════════════════════════════════
        // LEGACY PROPERTY ACCESSORS (for backwards compatibility with UI)
        // ═══════════════════════════════════════════════════════════════════
        
        // Engine
        public float PowerMultiplier { get => Engine.PowerMultiplier; set => Engine.PowerMultiplier = value; }
        public float TorqueMultiplier { get => Engine.TorqueMultiplier; set => Engine.TorqueMultiplier = value; }
        public float RevLimiter { get => Engine.RevLimiter; set => Engine.RevLimiter = value; }
        public float BoostPressure { get => Engine.BoostPressure; set => Engine.BoostPressure = value; }
        public int NitrousCharges { get => Engine.NitrousCharges; set => Engine.NitrousCharges = value; }
        public float FuelConsumption { get => Engine.FuelConsumption; set => Engine.FuelConsumption = value; }
        public float SpeedoCorrection { get => Engine.SpeedoCorrection; set => Engine.SpeedoCorrection = value; }
        
        // Transmission
        public float[] GearRatios { get => Transmission.GearRatios; set => Transmission.GearRatios = value; }
        public float FinalDriveRatio { get => Transmission.FinalDriveRatio; set => Transmission.FinalDriveRatio = value; }
        public bool LaunchControlEnabled { get => Transmission.LaunchControlEnabled; set => Transmission.LaunchControlEnabled = value; }
        public float LaunchControlRPM { get => Transmission.LaunchControlRPM; set => Transmission.LaunchControlRPM = value; }
        public bool TractionControlEnabled { get => Transmission.TractionControlEnabled; set => Transmission.TractionControlEnabled = value; }
        
        // Brakes
        public float BrakeForce { get => Brakes.BrakeForce; set => Brakes.BrakeForce = value; }
        public float BrakeBias { get => Brakes.BrakeBias; set => Brakes.BrakeBias = value; }
        
        // Handling
        public float WeightReduction { get => Handling.WeightReduction; set => Handling.WeightReduction = value; }
        public float TopSpeedLimit { get => Handling.TopSpeedLimit; set => Handling.TopSpeedLimit = value; }
        public float GripMultiplier { get => Handling.GripMultiplier; set => Handling.GripMultiplier = value; }
        public float CenterOfMassX { get => Handling.CenterOfMassX; set => Handling.CenterOfMassX = value; }
        public float CenterOfMassY { get => Handling.CenterOfMassY; set => Handling.CenterOfMassY = value; }
        public float CenterOfMassZ { get => Handling.CenterOfMassZ; set => Handling.CenterOfMassZ = value; }
    }
}
