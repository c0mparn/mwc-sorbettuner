using System;
using UnityEngine;
using MelonLoader;

namespace SorbetTuner.Tuners
{
    /// <summary>
    /// Handles brake tuning: force multiplier and front/rear bias.
    /// </summary>
    public class BrakeTuner
    {
        private readonly CarFinder _carFinder;
        
        // Backing fields for validated properties
        private float _brakeForce = 1.0f;
        private float _brakeBias = TuningConstants.Stock.BrakeBias;
        
        // Validated properties
        public float BrakeForce 
        { 
            get => _brakeForce;
            set => _brakeForce = Mathf.Clamp(value, 0.5f, TuningConstants.Limits.MaxBrakeForce);
        }
        
        public float BrakeBias 
        { 
            get => _brakeBias;
            set => _brakeBias = Mathf.Clamp(value, 0.3f, 0.8f); // 30-80% front
        }
        
        // Cached original value
        private float _originalBrakeTorque = 2000f;
        private bool _originalCaptured = false;
        
        public BrakeTuner(CarFinder carFinder)
        {
            _carFinder = carFinder;
        }
        
        /// <summary>
        /// Applies brake tuning to all wheels.
        /// </summary>
        public void Apply()
        {
            var axles = _carFinder.Axles;
            if (axles == null) return;
            
            try
            {
                var allWheelsField = ReflectionCache.GetField(axles.GetType(), new[] { "allWheels" });
                if (allWheelsField == null)
                {
                    MelonLogger.Warning("Could not find allWheels field for brake tuning");
                    return;
                }
                
                Array wheels = allWheelsField.GetValue(axles) as Array;
                if (wheels == null || wheels.Length == 0)
                {
                    MelonLogger.Warning("No wheels found for brake tuning");
                    return;
                }
                
                // Capture original brake value if not yet done
                if (!_originalCaptured)
                {
                    CaptureOriginalBrakeTorque(wheels);
                }
                
                // Apply to each wheel
                int wheelsModified = 0;
                for (int i = 0; i < wheels.Length; i++)
                {
                    object wheel = wheels.GetValue(i);
                    if (wheel == null) continue;
                    
                    // Typically: 0,1 = front, 2,3 = rear
                    bool isFront = i < 2;
                    
                    // Calculate brake force with bias
                    // BrakeBias = 0.6 means 60% front, 40% rear
                    float biasMultiplier = isFront ? BrakeBias * 2f : (1f - BrakeBias) * 2f;
                    float newBrakeTorque = _originalBrakeTorque * BrakeForce * biasMultiplier;
                    
                    if (ReflectionCache.SetValue(wheel, TuningConstants.FieldNames.BrakeTorque, newBrakeTorque, false))
                    {
                        wheelsModified++;
                    }
                }
                
                MelonLogger.Msg($"Applied brake tuning to {wheelsModified} wheels (Force: {BrakeForce:F1}x, Bias: {BrakeBias * 100:F0}% front)");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to apply brake tuning: {ex.Message}");
            }
        }
        
        private void CaptureOriginalBrakeTorque(Array wheels)
        {
            if (wheels.Length == 0) return;
            
            object firstWheel = wheels.GetValue(0);
            if (firstWheel == null) return;
            
            _originalBrakeTorque = ReflectionCache.GetValue<float>(firstWheel, TuningConstants.FieldNames.BrakeTorque, 2000f);
            _originalCaptured = true;
            MelonLogger.Msg($"Captured original brake torque: {_originalBrakeTorque}");
        }
        
        /// <summary>
        /// Resets to stock values.
        /// </summary>
        public void ResetToStock()
        {
            _brakeForce = 1.0f;
            _brakeBias = TuningConstants.Stock.BrakeBias;
        }
    }
}
