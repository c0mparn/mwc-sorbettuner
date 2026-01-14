using System;
using UnityEngine;
using MSCLoader;

namespace SorbetTuner.Tuners
{
    /// <summary>
    /// Handles engine tuning: power, torque, rev limit, turbo, and nitrous.
    /// </summary>
    public class EngineTuner
    {
        private readonly CarFinder _carFinder;
        
        // Backing fields for validated properties
        private float _powerMultiplier = 1.0f;
        private float _torqueMultiplier = 1.0f;
        private float _revLimiter = TuningConstants.Stock.RevLimit;
        private float _boostPressure = 0f;
        private int _nitrousCharges = 0;
        private float _fuelConsumption = 1.0f;
        private float _speedoCorrection = 1.0f;
        
        // Validated properties with bounds checking
        public float PowerMultiplier 
        { 
            get => _powerMultiplier;
            set => _powerMultiplier = Mathf.Clamp(value, 0.5f, TuningConstants.Limits.MaxPowerMultiplier);
        }
        
        public float TorqueMultiplier 
        { 
            get => _torqueMultiplier;
            set => _torqueMultiplier = Mathf.Clamp(value, 0.5f, TuningConstants.Limits.MaxTorqueMultiplier);
        }
        
        public float RevLimiter 
        { 
            get => _revLimiter;
            set => _revLimiter = Mathf.Clamp(value, TuningConstants.Limits.MinRPM, TuningConstants.Limits.MaxRPM);
        }
        
        public float BoostPressure 
        { 
            get => _boostPressure;
            set => _boostPressure = Mathf.Clamp(value, 0f, TuningConstants.Limits.MaxBoostPSI);
        }
        
        public int NitrousCharges 
        { 
            get => _nitrousCharges;
            set => _nitrousCharges = Mathf.Clamp(value, 0, TuningConstants.Limits.MaxNitrousCharges);
        }
        
        public float FuelConsumption 
        { 
            get => _fuelConsumption;
            set => _fuelConsumption = Mathf.Clamp(value, 0.1f, 3.0f);
        }
        
        public float SpeedoCorrection 
        { 
            get => _speedoCorrection;
            set => _speedoCorrection = Mathf.Clamp(value, 0.5f, 1.5f);
        }
        
        // Nitrous state
        private bool _nitrousActive;
        private float _nitrousEndTime;
        private float _nitrousCooldownEnd;
        
        public bool IsNitrousActive => _nitrousActive && Time.time < _nitrousEndTime;
        public bool CanUseNitrous => NitrousCharges > 0 && !_nitrousActive && Time.time > _nitrousCooldownEnd;
        
        public EngineTuner(CarFinder carFinder)
        {
            _carFinder = carFinder;
        }
        
        // Track last applied values to reduce log spam
        private float _lastAppliedTorque = -1f;
        private float _lastAppliedPower = -1f;
        private float _lastAppliedRPM = -1f;
        
        /// <summary>
        /// Applies engine tuning to the drivetrain.
        /// </summary>
        public void Apply()
        {
            var drivetrain = _carFinder.Drivetrain;
            if (drivetrain == null)
            {
                ModConsole.Print("Warning: Drivetrain not found - cannot apply power/torque tuning");
                return;
            }
            
            // Apply torque multiplier
            float newTorque = _carFinder.OriginalMaxTorque * TorqueMultiplier;
            if (Mathf.Abs(newTorque - _lastAppliedTorque) > 0.1f)
            {
                if (ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.Torque, newTorque))
                {
                    ModConsole.Print($"Set maxTorque: {_carFinder.OriginalMaxTorque:F0} Nm -> {newTorque:F0} Nm");
                    _lastAppliedTorque = newTorque;
                }
            }
            else
            {
                ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.Torque, newTorque, false);
            }
            
            // Apply power multiplier with turbo boost
            float turboMultiplier = 1f + (BoostPressure * TuningConstants.Timing.TurboBoostPerPSI);
            float nitrousMultiplier = IsNitrousActive ? TuningConstants.Timing.NitrousBoostMultiplier : 1f;
            float newPower = _carFinder.OriginalMaxPower * PowerMultiplier * turboMultiplier * nitrousMultiplier;
            
            if (Mathf.Abs(newPower - _lastAppliedPower) > 0.1f)
            {
                if (ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.Power, newPower))
                {
                    string turboInfo = BoostPressure > 0 ? $" [+{BoostPressure:F0} PSI]" : "";
                    string nitrousInfo = IsNitrousActive ? " [N2O]" : "";
                    ModConsole.Print($"Set maxPower: {_carFinder.OriginalMaxPower:F0} HP -> {newPower:F0} HP{turboInfo}{nitrousInfo}");
                    _lastAppliedPower = newPower;
                }
            }
            else
            {
                ReflectionCache.SetValue(drivetrain, TuningConstants.FieldNames.Power, newPower, false);
            }
            
            // Rev limiter disabled - modifying maxRPM causes physics instability
        }
        
        /// <summary>
        /// Activates nitrous if available.
        /// </summary>
        public bool ActivateNitrous()
        {
            if (!CanUseNitrous) return false;
            
            _nitrousCharges--;
            _nitrousActive = true;
            _nitrousEndTime = Time.time + TuningConstants.Timing.NitrousDuration;
            
            ModConsole.Print($"NITROUS ACTIVATED! Charges left: {NitrousCharges}");
            return true;
        }
        
        /// <summary>
        /// Updates nitrous state. Call from Update().
        /// </summary>
        public bool UpdateNitrous()
        {
            if (_nitrousActive && Time.time >= _nitrousEndTime)
            {
                _nitrousActive = false;
                _nitrousCooldownEnd = Time.time + TuningConstants.Timing.NitrousCooldown;
                ModConsole.Print("Nitrous depleted. Entering cooldown.");
                return true; // Signal to re-apply tuning
            }
            return false;
        }
        
        /// <summary>
        /// Resets to stock values.
        /// </summary>
        public void ResetToStock()
        {
            _powerMultiplier = 1.0f;
            _torqueMultiplier = 1.0f;
            _revLimiter = TuningConstants.Stock.RevLimit;
            _boostPressure = 0f;
            _nitrousCharges = 0;
            _fuelConsumption = 1.0f;
            _speedoCorrection = 1.0f;
        }
        
        /// <summary>
        /// Gets the effective HP with all modifiers.
        /// </summary>
        public float GetEffectiveHP()
        {
            float baseHP = TuningConstants.Stock.Power * PowerMultiplier;
            float turboHP = baseHP * (BoostPressure * TuningConstants.Timing.TurboBoostPerPSI);
            float finalHP = baseHP + turboHP;
            if (IsNitrousActive) finalHP *= TuningConstants.Timing.NitrousBoostMultiplier;
            return finalHP;
        }
        
        /// <summary>
        /// Gets the effective torque.
        /// </summary>
        public float GetEffectiveTorque()
        {
            return TuningConstants.Stock.Torque * TorqueMultiplier;
        }
    }
}
