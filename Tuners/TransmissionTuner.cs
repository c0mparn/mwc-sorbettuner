using System;
using System.Reflection;
using UnityEngine;
using MelonLoader;

namespace SorbetTuner.Tuners
{
    /// <summary>
    /// Handles transmission tuning: gear ratios, final drive, launch control, traction control.
    /// </summary>
    public class TransmissionTuner
    {
        private readonly CarFinder _carFinder;
        
        // Backing fields for validated properties
        private float[] _gearRatios = (float[])TuningConstants.Stock.GearRatios.Clone();
        private float _finalDriveRatio = TuningConstants.Stock.FinalDrive;
        private float _launchControlRPM = TuningConstants.Stock.LaunchControlRPM;
        
        // Gear ratio limits per gear
        private static readonly float[] MinGearRatios = { 2.0f, 1.2f, 0.8f, 0.6f, 0.4f };
        private static readonly float[] MaxGearRatios = { 5.0f, 3.5f, 2.5f, 2.0f, 1.5f };
        
        // Validated properties
        public float[] GearRatios 
        { 
            get => _gearRatios;
            set 
            {
                if (value == null || value.Length != 5) return;
                for (int i = 0; i < 5; i++)
                {
                    _gearRatios[i] = Mathf.Clamp(value[i], MinGearRatios[i], MaxGearRatios[i]);
                }
            }
        }
        
        public float FinalDriveRatio 
        { 
            get => _finalDriveRatio;
            set => _finalDriveRatio = Mathf.Clamp(value, 2.5f, 6.0f);
        }
        
        public bool LaunchControlEnabled { get; set; } = false;
        
        public float LaunchControlRPM 
        { 
            get => _launchControlRPM;
            set => _launchControlRPM = Mathf.Clamp(value, 3000f, 6000f);
        }
        
        public bool TractionControlEnabled { get; set; } = false;
        
        // Internal state
        private float _lastRPMSet = -1f;
        private bool _tcActive = false;
        
        public TransmissionTuner(CarFinder carFinder)
        {
            _carFinder = carFinder;
        }
        
        /// <summary>
        /// Sets a single gear ratio with validation.
        /// </summary>
        public void SetGearRatio(int gearIndex, float ratio)
        {
            if (gearIndex < 0 || gearIndex >= 5) return;
            _gearRatios[gearIndex] = Mathf.Clamp(ratio, MinGearRatios[gearIndex], MaxGearRatios[gearIndex]);
        }
        
        /// <summary>
        /// Applies transmission tuning.
        /// </summary>
        public void Apply()
        {
            var drivetrain = _carFinder.Drivetrain;
            if (drivetrain == null)
            {
                MelonLogger.Warning("Drivetrain not found - cannot apply transmission tuning");
                return;
            }
            
            // Final drive
            ReflectionCache.SetValueWithLog(drivetrain, TuningConstants.FieldNames.FinalDrive, FinalDriveRatio, "finalDriveRatio");
            
            // Gear ratios
            ApplyGearRatios(drivetrain);
        }
        
        private void ApplyGearRatios(Component drivetrain)
        {
            var gearField = ReflectionCache.GetField(drivetrain.GetType(), TuningConstants.FieldNames.GearRatios);
            if (gearField == null)
            {
                MelonLogger.Warning("gearRatios field not found");
                return;
            }
            
            float[] currentRatios = gearField.GetValue(drivetrain) as float[];
            if (currentRatios == null || currentRatios.Length < 7)
            {
                MelonLogger.Warning("gearRatios array not found or too short.");
                return;
            }
            
            // Game array structure: [0]=Reverse, [1]=Neutral, [2-6]=Gears 1-5
            float[] newRatios = (float[])currentRatios.Clone();
            for (int i = 0; i < 5; i++)
            {
                newRatios[i + 2] = _gearRatios[i];
            }
            gearField.SetValue(drivetrain, newRatios);
            MelonLogger.Msg("Updated gear ratios 1-5.");
        }
        
        /// <summary>
        /// Updates launch control and traction control. Call from Update().
        /// </summary>
        public void Update(float revLimiter, float originalTorque, float torqueMultiplier, bool nitrousActive)
        {
            var drivetrain = _carFinder.Drivetrain;
            var carBody = _carFinder.CarBody;
            
            if (drivetrain == null || carBody == null) return;
            
            float speed = carBody.velocity.magnitude * 3.6f; // km/h
            
            // Launch Control
            if (LaunchControlEnabled && !nitrousActive)
            {
                UpdateLaunchControl(drivetrain, speed, revLimiter);
            }
            else if (_lastRPMSet != -1f)
            {
                // Reset to normal if LC was disabled or Nitrous became active
                ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.RPM, revLimiter, false);
                _lastRPMSet = -1f;
            }
            
            // Traction Control
            if (TractionControlEnabled)
            {
                UpdateTractionControl(drivetrain, speed, originalTorque, torqueMultiplier);
            }
        }
        
        private void UpdateLaunchControl(Component drivetrain, float speed, float revLimiter)
        {
            try
            {
                float targetRPM = (speed < TuningConstants.LaunchControl.SpeedThreshold) ? LaunchControlRPM : revLimiter;
                
                if (Mathf.Abs(_lastRPMSet - targetRPM) > 1f)
                {
                    ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.RPM, targetRPM, false);
                    _lastRPMSet = targetRPM;
                }
            }
            catch { }
        }
        
        private void UpdateTractionControl(Component drivetrain, float speed, float originalTorque, float torqueMultiplier)
        {
            try
            {
                float currentRPM = ReflectionCache.GetValue<float>(drivetrain, TuningConstants.FieldNames.CurrentRPM, 0f);
                
                if (currentRPM > TuningConstants.TractionControl.RPMThreshold && 
                    speed < TuningConstants.TractionControl.SpeedThreshold)
                {
                    // Reduce torque
                    ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.Torque, 
                        originalTorque * TuningConstants.TractionControl.TorqueReduction, false);
                    _tcActive = true;
                }
                else if (_tcActive)
                {
                    // Restore torque
                    ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.Torque, 
                        originalTorque * torqueMultiplier, false);
                    _tcActive = false;
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Resets to stock values.
        /// </summary>
        public void ResetToStock()
        {
            _gearRatios = (float[])TuningConstants.Stock.GearRatios.Clone();
            _finalDriveRatio = TuningConstants.Stock.FinalDrive;
            LaunchControlEnabled = false;
            _launchControlRPM = TuningConstants.Stock.LaunchControlRPM;
            TractionControlEnabled = false;
        }
    }
}
