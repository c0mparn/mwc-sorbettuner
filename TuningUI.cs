using System;
using System.IO;
using UnityEngine;
using MelonLoader;
using Newtonsoft.Json;

namespace SorbetTuner
{
    /// <summary>
    /// In-game UI for the Sorbet Tuner using Unity IMGUI.
    /// </summary>
    public class TuningUI : MonoBehaviour
    {
        private TuningManager _tuningManager;
        private bool _isVisible = false;
        
        // Window settings
        private Rect _windowRect = new Rect(40, 40, 480, 560);
        private Vector2 _scrollPos = Vector2.zero;
        private readonly int _windowId = 84621; // Random unique ID
        
        // Tab system
        private enum Tab { Engine, Transmission, Brakes, Misc }
        private Tab _currentTab = Tab.Engine;
        
        // Preset system
        private string _presetName = "MyPreset";
        private string _statusMessage = "";
        private float _statusTime = 0f;
        
        // UI Styles (initialized once)
        private GUIStyle _windowStyle;
        private GUIStyle _tabStyle;
        private GUIStyle _tabActiveStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _centeredBoldStyle;
        private GUIStyle _italicStyle;
        private GUIStyle _smallLabelStyle;
        private GUIStyle _shortcutHintStyle;
        private bool _stylesInitialized = false;
        
        // Colors (MWC Aesthetic)
        private readonly Color _accentColor = new Color(0.88f, 0.68f, 0f); // Caution Yellow
        private readonly Color _bgColor = new Color(0.25f, 0.25f, 0.25f, 0.98f); // Dirty Gray
        private readonly Color _tabBgColor = new Color(0.15f, 0.15f, 0.15f); // Deep Gray
        private readonly Color _tabActiveColor = new Color(0.88f, 0.68f, 0f); // Caution Yellow
        
        // Textures
        private Texture2D _windowBgTex;
        private Texture2D _tabBgTex;
        private Texture2D _tabActiveTex;
        private Texture2D _buttonTex;
        private Texture2D _buttonHoverTex;
        private Texture2D _sliderTrackTex;
        private Texture2D _sliderThumbTex;

        public void Initialize(TuningManager manager)
        {
            _tuningManager = manager;
            CreateTextures();
        }

        private void CreateTextures()
        {
            // Clean up any existing textures first to prevent leaks on re-initialization
            DestroyTextures();
            
            _windowBgTex = MakeTexture(2, 2, _bgColor);
            _tabBgTex = MakeTexture(2, 2, _tabBgColor);
            _tabActiveTex = MakeTexture(2, 2, _tabActiveColor);
            _buttonTex = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f));
            _buttonHoverTex = MakeTexture(2, 2, new Color(0.35f, 0.35f, 0.35f));
            _sliderTrackTex = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f));
            _sliderThumbTex = MakeTexture(2, 2, _accentColor);
        }
        
        private void DestroyTextures()
        {
            if (_windowBgTex != null) { Destroy(_windowBgTex); _windowBgTex = null; }
            if (_tabBgTex != null) { Destroy(_tabBgTex); _tabBgTex = null; }
            if (_tabActiveTex != null) { Destroy(_tabActiveTex); _tabActiveTex = null; }
            if (_buttonTex != null) { Destroy(_buttonTex); _buttonTex = null; }
            if (_buttonHoverTex != null) { Destroy(_buttonHoverTex); _buttonHoverTex = null; }
            if (_sliderTrackTex != null) { Destroy(_sliderTrackTex); _sliderTrackTex = null; }
            if (_sliderThumbTex != null) { Destroy(_sliderThumbTex); _sliderThumbTex = null; }
        }
        
        private void OnDestroy()
        {
            DestroyTextures();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _windowBgTex, textColor = Color.white },
                onNormal = { background = _windowBgTex, textColor = Color.white },
                padding = new RectOffset(15, 15, 30, 15),
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            _tabStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _tabBgTex, textColor = Color.gray },
                hover = { background = _buttonHoverTex, textColor = Color.white },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 10, 10),
                alignment = TextAnchor.MiddleCenter
            };

            _tabActiveStyle = new GUIStyle(_tabStyle)
            {
                normal = { background = _tabActiveTex, textColor = Color.black },
                hover = { background = _tabActiveTex, textColor = Color.black }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = _accentColor },
                margin = new RectOffset(0, 0, 15, 5)
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.yellow },
                fixedWidth = 85
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = _buttonTex, textColor = Color.white },
                hover = { background = _buttonHoverTex, textColor = Color.white },
                active = { background = _tabActiveTex, textColor = Color.black },
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(20, 20, 10, 10)
            };

            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = Color.green },
                alignment = TextAnchor.MiddleCenter
            };
            
            // Additional cached styles (avoid creating in OnGUI)
            _centeredBoldStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            
            _italicStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic
            };
            
            _smallLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10
            };
            
            _shortcutHintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.MiddleCenter
            };

            // Custom slider styles
            GUI.skin.horizontalSlider.normal.background = _sliderTrackTex;
            GUI.skin.horizontalSliderThumb.normal.background = _sliderThumbTex;
            // Note: Don't set fixedWidth/fixedHeight - causes hitbox misalignment

            _stylesInitialized = true;
        }

        public void ToggleVisibility()
        {
            _isVisible = !_isVisible;
            // Note: Cursor visibility is forced in OnGUI while menu is open
            // When closed, the game will restore its normal cursor state
        }

        private void LateUpdate()
        {
            // Force cursor visible every frame while menu is open
            // Using LateUpdate to override after the game's Update sets it
            if (_isVisible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible || _tuningManager == null) return;
            
            // Force cursor visible in OnGUI - runs after all Update/LateUpdate
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            InitializeStyles();
            
            // Draw window
            _windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, "▓▒ TUNING TERMINAL v1.0 ▒▓", _windowStyle);
            
            // Keep window on screen
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0, Screen.width - _windowRect.width);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0, Screen.height - _windowRect.height);
        }

        private void DrawWindow(int id)
        {
            // Close button
            if (GUI.Button(new Rect(_windowRect.width - 30, 5, 25, 20), "✕"))
            {
                ToggleVisibility(); // Use toggle so cursor is properly restored
            }

            GUILayout.Space(5);
            
            // Car status
            string carStatus = _tuningManager.IsCarFound() 
                ? $"[ LINKED: {_tuningManager.GetCarName()} ]" 
                : "[ SCANNING CONTROL BUS... ]";
            GUI.color = _tuningManager.IsCarFound() ? Color.green : Color.red;
            GUILayout.Label(carStatus, _centeredBoldStyle);
            GUI.color = Color.white;
            
            GUILayout.Space(5);
            
            // Tab buttons
            GUILayout.BeginHorizontal();
            DrawTabButton("Engine", Tab.Engine);
            DrawTabButton("Trans", Tab.Transmission);
            DrawTabButton("Brakes", Tab.Brakes);
            DrawTabButton("Misc", Tab.Misc);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Tab content scrollable
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(350));
            GUILayout.BeginVertical(GUI.skin.box);
            switch (_currentTab)
            {
                case Tab.Engine: DrawEngineTab(); break;
                case Tab.Transmission: DrawTransmissionTab(); break;
                case Tab.Brakes: DrawBrakesTab(); break;
                case Tab.Misc: DrawMiscTab(); break;
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            // Bottom buttons
            DrawBottomButtons();
            
            // Status message
            if (!string.IsNullOrEmpty(_statusMessage) && Time.time < _statusTime)
            {
                GUILayout.Label(_statusMessage, _statusStyle);
            }
            
            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 35, 25));
        }

        private void DrawTabButton(string label, Tab tab)
        {
            if (GUILayout.Button(label, _currentTab == tab ? _tabActiveStyle : _tabStyle))
            {
                _currentTab = tab;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // ENGINE TAB
        // ═══════════════════════════════════════════════════════════════
        
        private void DrawEngineTab()
        {
            GUILayout.Label("🔌 ENGINE PARAMETERS", _headerStyle);
            
            // Get stock values for comparison
            float stockHP = TuningConstants.Stock.Power;
            float stockTorque = TuningConstants.Stock.Torque;
            float stockRPM = TuningConstants.Stock.RevLimit;
            
            // Power multiplier (capped at 2x)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Power Stage", GUILayout.Width(120));
            _tuningManager.PowerMultiplier = GUILayout.HorizontalSlider(
                _tuningManager.PowerMultiplier, 0.5f, 2.0f);
            // Show Stock → Modified format
            float newHP = _tuningManager.GetEffectiveHP();
            GUI.color = newHP > stockHP ? Color.green : (newHP < stockHP ? Color.yellow : Color.white);
            GUILayout.Label($"{stockHP:F0} → {newHP:F0} HP", _valueStyle, GUILayout.Width(100));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            
            // Torque multiplier (capped at 2x)
            GUILayout.BeginHorizontal();
            GUILayout.Label("Torque Index", GUILayout.Width(120));
            _tuningManager.TorqueMultiplier = GUILayout.HorizontalSlider(
                _tuningManager.TorqueMultiplier, 0.5f, 2.0f);
            float newTorque = _tuningManager.GetEffectiveTorque();
            GUI.color = newTorque > stockTorque ? Color.green : (newTorque < stockTorque ? Color.yellow : Color.white);
            GUILayout.Label($"{stockTorque:F0} → {newTorque:F0} Nm", _valueStyle, GUILayout.Width(100));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(8);
            
            // Rev limiter
            GUILayout.BeginHorizontal();
            GUILayout.Label("RPM Cut-off", GUILayout.Width(120));
            _tuningManager.RevLimiter = GUILayout.HorizontalSlider(
                _tuningManager.RevLimiter, 5000f, 8000f);
            GUI.color = _tuningManager.RevLimiter > stockRPM ? Color.green : (Mathf.Abs(_tuningManager.RevLimiter - stockRPM) < 100 ? Color.white : Color.yellow);
            GUILayout.Label($"{stockRPM:F0} → {_tuningManager.RevLimiter:F0}", _valueStyle, GUILayout.Width(100));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            // Info box
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label("💡 Green = increase | Yellow = decrease | Limit: 2x");
            GUI.color = Color.white;

            GUILayout.Space(15);
            GUILayout.Label("🌀 FORCED INDUCTION", _headerStyle);
            
            // Turbo Boost
            GUILayout.BeginHorizontal();
            GUILayout.Label("Boost Pressure", GUILayout.Width(120));
            _tuningManager.BoostPressure = GUILayout.HorizontalSlider(
                _tuningManager.BoostPressure, 0f, TuningConstants.Limits.MaxBoostPSI);
            GUI.color = _tuningManager.BoostPressure > 0 ? Color.cyan : Color.white;
            float bar = _tuningManager.BoostPressure * 0.06895f;
            GUILayout.Label($"{_tuningManager.BoostPressure:F0} PSI / {bar:F1} BAR", _valueStyle, GUILayout.Width(140));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            GUILayout.Label("⚡ NITROUS OXIDE", _headerStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("N2O Charges", GUILayout.Width(120));
            _tuningManager.NitrousCharges = (int)GUILayout.HorizontalSlider(
                _tuningManager.NitrousCharges, 0, 5);
            GUILayout.Label($"{_tuningManager.NitrousCharges}/5", _valueStyle, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Nitrous Activation Button
            GUI.enabled = _tuningManager.CanUseNitrous;
            if (GUILayout.Button(_tuningManager.IsNitrousActive ? "🔥🔥 NITROUS ACTIVE 🔥🔥" : "ACTIVATE NITROUS", GUILayout.Height(30)))
            {
                _tuningManager.ActivateNitrous();
            }
            GUI.enabled = true;
        }

        // ═══════════════════════════════════════════════════════════════
        // TRANSMISSION TAB
        // ═══════════════════════════════════════════════════════════════
        
        private void DrawTransmissionTab()
        {
            GUILayout.Label("⚙️ GEARBOX RATIOS", _headerStyle);
            
            // Final drive
            GUILayout.BeginHorizontal();
            GUILayout.Label("Master Drive Ratio", GUILayout.Width(150));
            _tuningManager.FinalDriveRatio = GUILayout.HorizontalSlider(
                _tuningManager.FinalDriveRatio, 2.5f, 6.0f);
            GUILayout.Label($"{_tuningManager.FinalDriveRatio:F2}", _valueStyle);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.Label("Gear Ratios:", _italicStyle);
            GUILayout.Space(5);
            
            // Individual gear ratios
            string[] gearNames = { "1st", "2nd", "3rd", "4th", "5th" };
            float[] minRatios = { 2.0f, 1.2f, 0.8f, 0.6f, 0.4f };
            float[] maxRatios = { 5.0f, 3.5f, 2.5f, 2.0f, 1.5f };
            
            for (int i = 0; i < 5; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"  {gearNames[i]} Gear", GUILayout.Width(150));
                _tuningManager.GearRatios[i] = GUILayout.HorizontalSlider(
                    _tuningManager.GearRatios[i], minRatios[i], maxRatios[i]);
                GUILayout.Label($"{_tuningManager.GearRatios[i]:F2}", _valueStyle);
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(15);

            GUILayout.Space(10);
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.Label("💡 Lower ratio = taller gear (higher speed)");
            GUILayout.Label("💡 Higher drive = faster pull (lower speed)");
            GUI.color = Color.white;

            GUILayout.Space(20);
            GUILayout.Label("🚀 ASSIST SYSTEMS", _headerStyle);

            // Launch Control
            GUILayout.BeginHorizontal();
            _tuningManager.LaunchControlEnabled = GUILayout.Toggle(_tuningManager.LaunchControlEnabled, " Enable Launch Control");
            GUILayout.EndHorizontal();

            if (_tuningManager.LaunchControlEnabled)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("  Target RPM", GUILayout.Width(150));
                _tuningManager.LaunchControlRPM = GUILayout.HorizontalSlider(_tuningManager.LaunchControlRPM, 3000f, 6000f);
                GUILayout.Label($"{_tuningManager.LaunchControlRPM:F0}", _valueStyle);
                GUILayout.EndHorizontal();
            }
        }

        // ═══════════════════════════════════════════════════════════════


        // ═══════════════════════════════════════════════════════════════
        // BRAKES TAB
        // ═══════════════════════════════════════════════════════════════
        
        private void DrawBrakesTab()
        {
            GUILayout.Label("🛑 BRAKING FORCE", _headerStyle);
            
            // Brake force
            GUILayout.BeginHorizontal();
            GUILayout.Label("Master Pressure", GUILayout.Width(150));
            _tuningManager.BrakeForce = GUILayout.HorizontalSlider(
                _tuningManager.BrakeForce, 0.5f, 2.5f);
            GUILayout.Label($"{_tuningManager.BrakeForce:F1}x", _valueStyle);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Brake bias
            GUILayout.BeginHorizontal();
            GUILayout.Label("Bias Split (F/R)", GUILayout.Width(150));
            _tuningManager.BrakeBias = GUILayout.HorizontalSlider(
                _tuningManager.BrakeBias, 0.3f, 0.8f);
            string biasLabel = _tuningManager.BrakeBias < 0.5f ? "REAR" : 
                               _tuningManager.BrakeBias > 0.6f ? "FRONT" : "BAL";
            GUILayout.Label($"{_tuningManager.BrakeBias * 100:F0}% F ({biasLabel})", _valueStyle);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15);

            // Visual brake bias indicator
            GUILayout.Label("FLUID DISTRIBUTION:", _smallLabelStyle);
            GUILayout.BeginHorizontal();
            GUI.color = Color.red;
            GUILayout.Box("", GUILayout.Width(200 * _tuningManager.BrakeBias), GUILayout.Height(15));
            GUI.color = Color.yellow;
            GUILayout.Box("", GUILayout.Width(200 * (1 - _tuningManager.BrakeBias)), GUILayout.Height(15));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("FRONT", GUILayout.Width(100));
            GUILayout.FlexibleSpace();
            GUILayout.Label("REAR", GUILayout.Width(100));
            GUILayout.EndHorizontal();
        }

        // ═══════════════════════════════════════════════════════════════
        // MISC TAB
        // ═══════════════════════════════════════════════════════════════
        
        private void DrawMiscTab()
        {
            GUILayout.Label("🛠️ MISCELLANEOUS", _headerStyle);
            
            // Weight reduction
            GUILayout.BeginHorizontal();
            GUILayout.Label("Weight Stripping", GUILayout.Width(150));
            _tuningManager.WeightReduction = GUILayout.HorizontalSlider(
                _tuningManager.WeightReduction, 0f, 50f);
            float effectiveWeight = TuningConstants.Stock.Mass * (1 - _tuningManager.WeightReduction / 100f);
            GUILayout.Label($"{_tuningManager.WeightReduction:F0}% ({effectiveWeight:F0}kg)", _valueStyle);
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Grip multiplier
            GUILayout.BeginHorizontal();
            GUILayout.Label("Traction Index", GUILayout.Width(150));
            _tuningManager.GripMultiplier = GUILayout.HorizontalSlider(
                _tuningManager.GripMultiplier, 1.0f, 4.0f);
            GUILayout.Label($"{_tuningManager.GripMultiplier:F1}x", _valueStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(20);
            GUILayout.Label("⚖ HANDLING & BALANCE", _headerStyle);

            // Traction Control
            GUILayout.BeginHorizontal();
            _tuningManager.TractionControlEnabled = GUILayout.Toggle(_tuningManager.TractionControlEnabled, " Traction Control Assist");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Center of Mass Offset (m):");

            // CoM X
            GUILayout.BeginHorizontal();
            GUILayout.Label("  Left/Right (X)", GUILayout.Width(130));
            _tuningManager.CenterOfMassX = GUILayout.HorizontalSlider(_tuningManager.CenterOfMassX, -0.5f, 0.5f);
            GUILayout.Label($"{_tuningManager.CenterOfMassX:F2}", _valueStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            // CoM Y
            GUILayout.BeginHorizontal();
            GUILayout.Label("  Up/Down (Y)", GUILayout.Width(130));
            _tuningManager.CenterOfMassY = GUILayout.HorizontalSlider(_tuningManager.CenterOfMassY, -0.5f, 0.5f);
            GUILayout.Label($"{_tuningManager.CenterOfMassY:F2}", _valueStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            // CoM Z
            GUILayout.BeginHorizontal();
            GUILayout.Label("  Forward/Back (Z)", GUILayout.Width(130));
            _tuningManager.CenterOfMassZ = GUILayout.HorizontalSlider(_tuningManager.CenterOfMassZ, -0.5f, 0.5f);
            GUILayout.Label($"{_tuningManager.CenterOfMassZ:F2}", _valueStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.Label("💡 Moving weight back (Z-) helps traction.");
            GUILayout.Label("💡 Moving weight down (Y-) improves stability.");
            GUI.color = Color.white;

            GUILayout.Space(20);
            
            if (GUILayout.Button("❄ DE-ICE SURFACES", GUILayout.Height(40)))
            {
                _tuningManager.RemoveFrost();
                ShowStatus("DE-ICER APPLIED");
            }
            
            GUILayout.Space(20);
            
            // Presets section
            GUILayout.Label("💾 DATA CASSETTES", _headerStyle);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("LABEL:", GUILayout.Width(100));
            _presetName = GUILayout.TextField(_presetName, GUILayout.Width(150));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("STORE DATA", _buttonStyle))
            {
                SavePreset(_presetName);
            }
            if (GUILayout.Button("LOAD DATA", _buttonStyle))
            {
                LoadPreset(_presetName);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(15);
            
            // Refresh car button
            if (GUILayout.Button("🔄 RE-SCAN FOR Sorbet", _buttonStyle))
            {
                _tuningManager.RefreshCarReference();
                ShowStatus("BUS RESET - SCANNING...");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // BOTTOM BUTTONS
        // ═══════════════════════════════════════════════════════════════
        
        private void DrawBottomButtons()
        {
            GUILayout.BeginHorizontal();
            
            // Apply button
            GUI.backgroundColor = _accentColor;
            if (GUILayout.Button("⚙ WRITE TO ECU", _buttonStyle, GUILayout.Height(35)))
            {
                _tuningManager.ApplyTuning();
                ShowStatus("DATA WRITTEN TO ECU");
            }
            GUI.backgroundColor = Color.white;
            
            // Undo button (disabled if no undo state)
            GUI.enabled = _tuningManager.HasUndoState;
            if (GUILayout.Button("↩ UNDO", _buttonStyle, GUILayout.Height(35), GUILayout.Width(70)))
            {
                _tuningManager.Undo();
                ShowStatus("REVERTED TO PREVIOUS");
            }
            GUI.enabled = true;
            
            // Redo button (disabled if no redo state)
            GUI.enabled = _tuningManager.HasRedoState;
            if (GUILayout.Button("↪ REDO", _buttonStyle, GUILayout.Height(35), GUILayout.Width(70)))
            {
                _tuningManager.Redo();
                ShowStatus("RESTORED CHANGE");
            }
            GUI.enabled = true;
            
            // Reset button
            if (GUILayout.Button("🔄 RESET", _buttonStyle, GUILayout.Height(35), GUILayout.Width(80)))
            {
                _tuningManager.ResetToStock();
                ShowStatus("FACTORY DEFAULTS RESTORED");
            }
            
            GUILayout.EndHorizontal();
            
            // Keyboard shortcut hints
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            GUILayout.Label("F9: Apply | F10: Reset | Ctrl+Z: Undo | Ctrl+Y: Redo", _shortcutHintStyle);
            GUI.color = Color.white;
        }

        private void ShowStatus(string message)
        {
            _statusMessage = message;
            _statusTime = Time.time + 3f;
        }

        // ═══════════════════════════════════════════════════════════════
        // PRESET SAVE/LOAD
        // ═══════════════════════════════════════════════════════════════
        
        private string GetPresetsPath()
        {
            // .NET 3.5 only supports 2-argument Path.Combine
            string path = Path.Combine(Application.dataPath, "..");
            path = Path.Combine(path, "Mods");
            path = Path.Combine(path, "SorbetTuner");
            path = Path.Combine(path, "Presets");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        
        /// <summary>
        /// Sanitizes a preset name to be safe for use as a filename.
        /// </summary>
        private string SanitizePresetName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";
            
            // Remove invalid filename characters
            char[] invalidChars = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
            foreach (char c in invalidChars)
            {
                name = name.Replace(c.ToString(), "");
            }
            
            // Trim whitespace and limit length
            name = name.Trim();
            if (name.Length > 50) name = name.Substring(0, 50);
            if (string.IsNullOrEmpty(name)) return "Unnamed";
            
            return name;
        }

        private void SavePreset(string name)
        {
            // Sanitize the preset name for safe file operations
            name = SanitizePresetName(name);
            
            try
            {
                var preset = new TuningPreset
                {
                    Name = name,
                    PowerMultiplier = _tuningManager.PowerMultiplier,
                    TorqueMultiplier = _tuningManager.TorqueMultiplier,
                    RevLimiter = _tuningManager.RevLimiter,
                    BoostPressure = _tuningManager.BoostPressure,
                    NitrousCharges = _tuningManager.NitrousCharges,
                    GearRatios = _tuningManager.GearRatios,
                    FinalDriveRatio = _tuningManager.FinalDriveRatio,
                    LaunchControlEnabled = _tuningManager.LaunchControlEnabled,
                    LaunchControlRPM = _tuningManager.LaunchControlRPM,
                    BrakeForce = _tuningManager.BrakeForce,
                    BrakeBias = _tuningManager.BrakeBias,
                    WeightReduction = _tuningManager.WeightReduction,
                    TopSpeedLimit = _tuningManager.TopSpeedLimit,
                    GripMultiplier = _tuningManager.GripMultiplier,
                    TractionControlEnabled = _tuningManager.TractionControlEnabled,
                    CenterOfMassX = _tuningManager.CenterOfMassX,
                    CenterOfMassY = _tuningManager.CenterOfMassY,
                    CenterOfMassZ = _tuningManager.CenterOfMassZ
                };

                string json = JsonConvert.SerializeObject(preset, Formatting.Indented);
                string path = Path.Combine(GetPresetsPath(), $"{name}.json");
                File.WriteAllText(path, json);
                
                MelonLogger.Msg($"Saved preset: {path}");
                ShowStatus($"✓ Saved preset: {name}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save preset: {ex.Message}");
                ShowStatus($"✗ Failed to save: {ex.Message}");
            }
        }

        private void LoadPreset(string name)
        {
            // Sanitize the preset name for safe file operations
            name = SanitizePresetName(name);
            
            try
            {
                string path = Path.Combine(GetPresetsPath(), $"{name}.json");
                if (!File.Exists(path))
                {
                    ShowStatus($"✗ Preset not found: {name}");
                    return;
                }

                string json = File.ReadAllText(path);
                var preset = JsonConvert.DeserializeObject<TuningPreset>(json);
                
                _tuningManager.PowerMultiplier = preset.PowerMultiplier;
                _tuningManager.TorqueMultiplier = preset.TorqueMultiplier;
                _tuningManager.RevLimiter = preset.RevLimiter;
                _tuningManager.BoostPressure = preset.BoostPressure;
                _tuningManager.NitrousCharges = preset.NitrousCharges;
                
                // Validate GearRatios before assignment to prevent crash on malformed presets
                if (preset.GearRatios != null && preset.GearRatios.Length == 5)
                {
                    _tuningManager.GearRatios = preset.GearRatios;
                }
                
                _tuningManager.FinalDriveRatio = preset.FinalDriveRatio;
                _tuningManager.LaunchControlEnabled = preset.LaunchControlEnabled;
                _tuningManager.LaunchControlRPM = preset.LaunchControlRPM;
                _tuningManager.BrakeForce = preset.BrakeForce;
                _tuningManager.BrakeBias = preset.BrakeBias;
                _tuningManager.WeightReduction = preset.WeightReduction;
                _tuningManager.TopSpeedLimit = preset.TopSpeedLimit;
                _tuningManager.GripMultiplier = preset.GripMultiplier;
                _tuningManager.TractionControlEnabled = preset.TractionControlEnabled;
                _tuningManager.CenterOfMassX = preset.CenterOfMassX;
                _tuningManager.CenterOfMassY = preset.CenterOfMassY;
                _tuningManager.CenterOfMassZ = preset.CenterOfMassZ;
                
                MelonLogger.Msg($"Loaded preset: {path}");
                ShowStatus($"✓ Loaded preset: {name}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load preset: {ex.Message}");
                ShowStatus($"✗ Failed to load: {ex.Message}");
            }
        }
    }
}
