using System;

namespace SorbetTuner
{
    /// <summary>
    /// Centralized constants for the Sorbet Tuner mod.
    /// Replaces magic numbers scattered throughout the codebase.
    /// </summary>
    public static class TuningConstants
    {
        /// <summary>Stock values for the Sorbet 1600 LTD.</summary>
        public static class Stock
        {
            public const float Power = 72f;           // HP
            public const float Torque = 130f;         // Nm
            public const float RevLimit = 6500f;      // RPM
            public const float MaxRPM = 9000f;        // Engine max
            public const float Mass = 955f;           // kg
            public const float FinalDrive = 3.94f;
            public const float BrakeBias = 0.6f;      // 60% front
            public const float LaunchControlRPM = 4500f;
            public const float BrakeTorque = 2000f;   // Default brake torque
            
            public static readonly float[] GearRatios = new float[] 
            { 
                3.45f,  // 1st
                1.85f,  // 2nd
                1.36f,  // 3rd
                1.07f,  // 4th
                0.80f   // 5th
            };
        }
        
        /// <summary>Tuning limits to prevent extreme/broken values.</summary>
        public static class Limits
        {
            public const float MinRPM = 5000f;
            public const float MaxRPM = 8000f;
            public const float MaxPowerMultiplier = 2.0f;
            public const float MaxTorqueMultiplier = 2.0f;
            public const float MaxBoostPSI = 15f;
            public const float MaxWeightReduction = 50f;     // %
            public const float MinMass = 500f;               // kg
            public const float MaxGripMultiplier = 4.0f;
            public const float MaxBrakeForce = 2.5f;
            public const int MaxNitrousCharges = 5;
        }
        
        /// <summary>Timing constants for systems.</summary>
        public static class Timing
        {
            public const float NitrousDuration = 3f;         // seconds
            public const float NitrousCooldown = 10f;        // seconds
            public const float NitrousBoostMultiplier = 1.5f;
            public const float TurboBoostPerPSI = 0.05f;     // 5% per PSI
        }
        
        /// <summary>Traction control thresholds.</summary>
        public static class TractionControl
        {
            public const float RPMThreshold = 4000f;
            public const float SpeedThreshold = 30f;         // km/h
            public const float TorqueReduction = 0.5f;       // 50% reduction
        }
        
        /// <summary>Launch control thresholds.</summary>
        public static class LaunchControl
        {
            public const float SpeedThreshold = 5f;          // km/h
        }
        
        /// <summary>Known field names for reflection lookups.</summary>
        public static class FieldNames
        {
            public static readonly string[] Torque = { "maxTorque", "MaxTorque", "engineMaxTorque", "torque", "Torque", "maxEngineTorque" };
            public static readonly string[] Power = { "maxPower", "MaxPower", "engineMaxPower", "power", "Power", "maxEnginePower", "maxHP", "horsePower" };
            public static readonly string[] RPM = { "maxRPM", "MaxRPM", "rpmLimit", "revLimit" };
            public static readonly string[] CurrentRPM = { "rpm", "RPM", "currentRPM", "engineRPM" };
            public static readonly string[] GearRatios = { "gearRatios", "GearRatios", "gears" };
            public static readonly string[] FinalDrive = { "finalDriveRatio", "FinalDriveRatio", "finalDrive", "diffRatio" };
            public static readonly string[] BrakeTorque = { "brakeFrictionTorque", "maxBrakeTorque", "brakeTorque", "brakeForce", "braking" };
            public static readonly string[] Grip = { "forwardGripFactor", "sidewaysGripFactor", "gripFactor", "grip" };
        }
    }
}
