using System;
using UnityEngine;
using MelonLoader;

namespace SorbetTuner.Tuners
{
    /// <summary>
    /// Handles handling tuning: weight, grip, center of mass, and misc features.
    /// </summary>
    public class HandlingTuner
    {
        private readonly CarFinder _carFinder;
        
        // Backing fields for validated properties
        private float _weightReduction = 0f;
        private float _gripMultiplier = 1.0f;
        private float _centerOfMassX = 0f;
        private float _centerOfMassY = 0f;
        private float _centerOfMassZ = 0f;
        private float _topSpeedLimit = 0f;
        
        // Validated properties
        public float WeightReduction 
        { 
            get => _weightReduction;
            set => _weightReduction = Mathf.Clamp(value, 0f, TuningConstants.Limits.MaxWeightReduction);
        }
        
        public float GripMultiplier 
        { 
            get => _gripMultiplier;
            set => _gripMultiplier = Mathf.Clamp(value, 1.0f, TuningConstants.Limits.MaxGripMultiplier);
        }
        
        public float CenterOfMassX 
        { 
            get => _centerOfMassX;
            set => _centerOfMassX = Mathf.Clamp(value, -0.5f, 0.5f);
        }
        
        public float CenterOfMassY 
        { 
            get => _centerOfMassY;
            set => _centerOfMassY = Mathf.Clamp(value, -0.5f, 0.5f);
        }
        
        public float CenterOfMassZ 
        { 
            get => _centerOfMassZ;
            set => _centerOfMassZ = Mathf.Clamp(value, -0.5f, 0.5f);
        }
        
        public float TopSpeedLimit 
        { 
            get => _topSpeedLimit;
            set => _topSpeedLimit = Mathf.Clamp(value, 0f, 250f);
        }
        
        public HandlingTuner(CarFinder carFinder)
        {
            _carFinder = carFinder;
        }
        
        /// <summary>
        /// Applies weight reduction.
        /// </summary>
        public void ApplyWeight()
        {
            var carBody = _carFinder.CarBody;
            if (carBody == null) return;
            
            float reductionMultiplier = 1f - (WeightReduction / 100f);
            float newMass = _carFinder.OriginalMass * reductionMultiplier;
            newMass = Mathf.Max(newMass, TuningConstants.Limits.MinMass);
            
            MelonLogger.Msg($"Adjusting mass: {_carFinder.OriginalMass:F0} kg -> {newMass:F0} kg");
            carBody.mass = newMass;
        }
        
        /// <summary>
        /// Applies center of mass offset.
        /// </summary>
        public void ApplyCenterOfMass()
        {
            var carBody = _carFinder.CarBody;
            if (carBody == null) return;
            
            _carFinder.StoreCenterOfMass();
            
            Vector3 offset = new Vector3(CenterOfMassX, CenterOfMassY, CenterOfMassZ);
            carBody.centerOfMass = _carFinder.OriginalCenterOfMass + offset;
            
            if (offset != Vector3.zero)
            {
                MelonLogger.Msg($"Applied Center of Mass offset: {offset}");
            }
        }
        
        /// <summary>
        /// Applies grip multiplier to all wheels.
        /// </summary>
        public void ApplyGrip()
        {
            var axles = _carFinder.Axles;
            if (axles == null) return;
            
            try
            {
                var allWheelsField = ReflectionCache.GetField(axles.GetType(), new[] { "allWheels" });
                if (allWheelsField == null) return;
                
                Array wheels = allWheelsField.GetValue(axles) as Array;
                if (wheels == null) return;
                
                int modified = 0;
                foreach (object wheel in wheels)
                {
                    if (wheel == null) continue;
                    
                    // Apply to both forward and sideways grip
                    if (ReflectionCache.SetValue(wheel, new[] { "forwardGripFactor" }, GripMultiplier, false))
                        modified++;
                    if (ReflectionCache.SetValue(wheel, new[] { "sidewaysGripFactor" }, GripMultiplier, false))
                        modified++;
                }
                
                MelonLogger.Msg($"Applied grip multiplier {GripMultiplier:F1}x to {wheels.Length} wheels.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to apply grip tuning: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Applies all handling tuning.
        /// </summary>
        public void Apply()
        {
            ApplyWeight();
            ApplyCenterOfMass();
            ApplyGrip();
        }
        
        /// <summary>
        /// Removes frost/ice from car windows.
        /// </summary>
        public void RemoveFrost()
        {
            var car = _carFinder.Car;
            if (car == null)
            {
                MelonLogger.Warning("Cannot remove frost - car not found!");
                return;
            }
            
            int objectCount = 0;
            int matCount = 0;
            
            // Disable frost objects
            Transform[] allTransforms = car.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                string name = t.name.ToLower();
                if (name.Contains("frost") || name.Contains("frozen"))
                {
                    if (t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        objectCount++;
                    }
                }
            }
            
            // Clear frost material properties
            Renderer[] renderers = car.GetComponentsInChildren<Renderer>(true);
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
            
            MelonLogger.Msg($"Defrost complete! Disabled {objectCount} objects and modified {matCount} materials.");
        }
        
        /// <summary>
        /// Resets to stock values.
        /// </summary>
        public void ResetToStock()
        {
            _weightReduction = 0f;
            _gripMultiplier = 1.0f;
            _centerOfMassX = 0f;
            _centerOfMassY = 0f;
            _centerOfMassZ = 0f;
            _topSpeedLimit = 0f;
            
            // Reset car body
            var carBody = _carFinder.CarBody;
            if (carBody != null)
            {
                carBody.mass = _carFinder.OriginalMass;
                if (_carFinder.CenterOfMassStored)
                {
                    carBody.centerOfMass = _carFinder.OriginalCenterOfMass;
                }
            }
        }
    }
}
