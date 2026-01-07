using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(SorbetTuner.SorbetTunerMod), "Sorbet Tuner", "1.0.0", "FirstMod")]
[assembly: MelonGame("TopDownTown", "My Winter Car")]

namespace SorbetTuner
{
    /// <summary>
    /// Main MelonLoader mod entry point for the Sorbet 1600 LTD Tuner.
    /// </summary>
    public class SorbetTunerMod : MelonMod
    {
        public static SorbetTunerMod Instance { get; private set; }
        
        private TuningUI _tuningUI;
        private TuningManager _tuningManager;
        private GameObject _modObject;
        private bool _initialized = false;
        
        // Keybind to toggle UI
        public KeyCode ToggleKey { get; set; } = KeyCode.F8;
        
        public override void OnInitializeMelon()
        {
            Instance = this;
            LoggerInstance.Msg("Sorbet Tuner v1.0.0 initialized!");
            LoggerInstance.Msg("Press F8 to open the tuning menu.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            LoggerInstance.Msg("Scene loaded: " + sceneName + " (index: " + buildIndex + ")");
            
            // Try to initialize on any scene that's not the intro/splash
            if (!_initialized && buildIndex > 0)
            {
                InitializeTuner();
            }
        }

        private void InitializeTuner()
        {
            if (_initialized) return;
            
            try
            {
                LoggerInstance.Msg("Initializing Sorbet Tuner...");
                
                // Create a persistent GameObject to hold our components
                _modObject = new GameObject("SorbetTuner");
                GameObject.DontDestroyOnLoad(_modObject);
                
                // Initialize managers
                _tuningManager = _modObject.AddComponent<TuningManager>();
                _tuningUI = _modObject.AddComponent<TuningUI>();
                _tuningUI.Initialize(_tuningManager);
                
                _initialized = true;
                LoggerInstance.Msg("Sorbet Tuner ready! Press F8 to open menu.");
            }
            catch (System.Exception ex)
            {
                LoggerInstance.Error("Failed to initialize: " + ex.Message);
                LoggerInstance.Error(ex.StackTrace);
            }
        }

        public override void OnUpdate()
        {
            // Try to initialize if not done yet
            if (!_initialized && Time.time > 5f)
            {
                InitializeTuner();
            }
            
            // Toggle UI with F8
            if (Input.GetKeyDown(ToggleKey))
            {
                LoggerInstance.Msg("F8 pressed!");
                
                if (_tuningUI != null)
                {
                    _tuningUI.ToggleVisibility();
                    LoggerInstance.Msg("Toggled UI visibility");
                }
                else
                {
                    LoggerInstance.Warning("TuningUI is null - trying to initialize...");
                    InitializeTuner();
                }
            }
            
            // F9: Apply tuning
            if (Input.GetKeyDown(KeyCode.F9) && _tuningManager != null)
            {
                _tuningManager.ApplyTuning();
                LoggerInstance.Msg("F9: Applied tuning");
            }
            
            // F10: Reset to stock
            if (Input.GetKeyDown(KeyCode.F10) && _tuningManager != null)
            {
                _tuningManager.ResetToStock();
                LoggerInstance.Msg("F10: Reset to stock");
            }
            
            // Ctrl+Z: Undo
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                && Input.GetKeyDown(KeyCode.Z) && _tuningManager != null)
            {
                if (_tuningManager.HasUndoState)
                {
                    _tuningManager.Undo();
                    LoggerInstance.Msg("Ctrl+Z: Undid last change");
                }
            }
            
            // Ctrl+Y: Redo
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) 
                && Input.GetKeyDown(KeyCode.Y) && _tuningManager != null)
            {
                if (_tuningManager.HasRedoState)
                {
                    _tuningManager.Redo();
                    LoggerInstance.Msg("Ctrl+Y: Redid last change");
                }
            }
            
            // N: Activate Nitrous
            if (Input.GetKeyDown(KeyCode.N) && _tuningManager != null)
            {
                if (_tuningManager.CanUseNitrous)
                {
                    _tuningManager.ActivateNitrous();
                    LoggerInstance.Msg("N: Nitrous Activated!");
                }
            }
        }

        public override void OnDeinitializeMelon()
        {
            if (_modObject != null)
            {
                GameObject.Destroy(_modObject);
            }
        }
    }
}
