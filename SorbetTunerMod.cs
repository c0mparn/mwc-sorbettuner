using MSCLoader;
using UnityEngine;

namespace SorbetTuner
{
    /// <summary>
    /// Main MSCLoader mod entry point for the Sorbet 1600 LTD Tuner.
    /// </summary>
    public class SorbetTunerMod : Mod
    {
        public override string ID => "SorbetTuner";
        public override string Name => "Sorbet Tuner";
        public override string Author => "FirstMod";
        public override string Version => "1.4.0";
        public override Game SupportedGames => Game.MyWinterCar;

        public static SorbetTunerMod Instance { get; private set; }
        
        private GameObject _modObject;
        private SorbetTunerBehaviour _behaviour;
        
        // Keybinds
        public static SettingsKeybind KeyToggleMenu;
        public static SettingsKeybind KeyApplyTuning;
        public static SettingsKeybind KeyResetStock;
        public static SettingsKeybind KeyUndo;
        public static SettingsKeybind KeyRedo;
        public static SettingsKeybind KeyNitrous;
        
        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, OnLoad);
            SetupFunction(Setup.Update, OnUpdate);
            SetupFunction(Setup.ModSettings, ModSettings);
        }
        
        private void ModSettings()
        {
            Keybind.AddHeader("Tuning Menu");
            KeyToggleMenu = Keybind.Add("ToggleMenu", "Toggle Tuning Menu", KeyCode.F8);
            KeyApplyTuning = Keybind.Add("ApplyTuning", "Apply Tuning", KeyCode.F9);
            KeyResetStock = Keybind.Add("ResetStock", "Reset to Stock", KeyCode.F10);
            
            Keybind.AddHeader("Undo/Redo");
            KeyUndo = Keybind.Add("Undo", "Undo", KeyCode.Z, KeyCode.LeftControl);
            KeyRedo = Keybind.Add("Redo", "Redo", KeyCode.Y, KeyCode.LeftControl);
            
            Keybind.AddHeader("Nitrous");
            KeyNitrous = Keybind.Add("Nitrous", "Activate Nitrous", KeyCode.N);
        }
        
        private void OnLoad()
        {
            Instance = this;
            
            // Create a persistent GameObject to hold our components
            _modObject = new GameObject("SorbetTuner");
            GameObject.DontDestroyOnLoad(_modObject);
            
            // Add the behaviour component
            _behaviour = _modObject.AddComponent<SorbetTunerBehaviour>();
            
            ModConsole.Print("Sorbet Tuner v1.0.0 initialized!");
            ModConsole.Print("Press F8 to open the tuning menu.");
        }
        
        private void OnUpdate()
        {
            if (_behaviour != null)
            {
                _behaviour.DoUpdate();
            }
        }
    }
    
    /// <summary>
    /// MonoBehaviour to handle Update loop and initialization.
    /// </summary>
    public class SorbetTunerBehaviour : MonoBehaviour
    {
        private TuningUI _tuningUI;
        private TuningManager _tuningManager;
        private bool _initialized = false;

        public void DoUpdate()
        {
            // Try to initialize if not done yet
            if (!_initialized && Time.time > 5f && Application.loadedLevelName != "Intro")
            {
                InitializeTuner();
            }
            
            // Toggle UI with keybind
            if (SorbetTunerMod.KeyToggleMenu != null && SorbetTunerMod.KeyToggleMenu.GetKeybindDown())
            {
                if (_tuningUI != null)
                {
                    _tuningUI.ToggleVisibility();
                }
                else
                {
                    InitializeTuner();
                }
            }
            
            // Apply tuning
            if (SorbetTunerMod.KeyApplyTuning != null && SorbetTunerMod.KeyApplyTuning.GetKeybindDown() && _tuningManager != null)
            {
                _tuningManager.ApplyTuning();
            }
            
            // Reset to stock
            if (SorbetTunerMod.KeyResetStock != null && SorbetTunerMod.KeyResetStock.GetKeybindDown() && _tuningManager != null)
            {
                _tuningManager.ResetToStock();
            }
            
            // Undo
            if (SorbetTunerMod.KeyUndo != null && SorbetTunerMod.KeyUndo.GetKeybindDown() && _tuningManager != null)
            {
                if (_tuningManager.HasUndoState)
                {
                    _tuningManager.Undo();
                }
            }
            
            // Redo
            if (SorbetTunerMod.KeyRedo != null && SorbetTunerMod.KeyRedo.GetKeybindDown() && _tuningManager != null)
            {
                if (_tuningManager.HasRedoState)
                {
                    _tuningManager.Redo();
                }
            }
            
            // Nitrous
            if (SorbetTunerMod.KeyNitrous != null && SorbetTunerMod.KeyNitrous.GetKeybindDown() && _tuningManager != null)
            {
                if (_tuningManager.CanUseNitrous)
                {
                    _tuningManager.ActivateNitrous();
                }
            }
        }
        
        private void InitializeTuner()
        {
            if (_initialized) return;
            
            try
            {
                ModConsole.Print("Initializing Sorbet Tuner...");
                
                // Initialize managers on this same GameObject
                _tuningManager = gameObject.AddComponent<TuningManager>();
                _tuningUI = gameObject.AddComponent<TuningUI>();
                _tuningUI.Initialize(_tuningManager);
                
                _initialized = true;
                ModConsole.Print("Sorbet Tuner ready! Press F8 to open menu.");
            }
            catch (System.Exception ex)
            {
                ModConsole.Error("Failed to initialize: " + ex.Message);
                ModConsole.Error(ex.StackTrace);
            }
        }
    }
}
