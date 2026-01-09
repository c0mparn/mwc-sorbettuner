using System;

namespace SorbetTuner
{
    /// <summary>
    /// Serializable preset class for saving/loading tuning configurations.
    /// </summary>
    [Serializable]
    public class TuningPreset
    {
        public string Name = "Default";
        
        // Engine
        public float PowerMultiplier = 1.0f;
        public float TorqueMultiplier = 1.0f;
        public float RevLimiter = 6500f;
        public float BoostPressure = 0f;
        public int NitrousCharges = 0;
        
        // Transmission / Assists
        public float[] GearRatios = { 3.45f, 1.85f, 1.36f, 1.07f, 0.80f };
        public float FinalDriveRatio = 3.94f;
        public bool LaunchControlEnabled = false;
        public float LaunchControlRPM = 4500f;
        public int DrivetrainMode = 2; // 0=RWD, 1=AWD, 2=FWD (stock)
        
        // Brakes
        public float BrakeForce = 1.0f;
        public float BrakeBias = 0.6f;
        
        // Misc / Handling
        public float WeightReduction = 0f;
        public float TopSpeedLimit = 0f;
        public float GripMultiplier = 1.0f;
        public bool TractionControlEnabled = false;
        public float CenterOfMassX = 0f;
        public float CenterOfMassY = 0f;
        public float CenterOfMassZ = 0f;
    }
}
