//=====================================
// DebugHelper.cs
//=====================================

using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics; // For [Conditional] attribute
using Debug = UnityEngine.Debug; // Resolve ambiguity

namespace ChatDialogueSystem
{
    /// <summary>
    /// High-performance debug logging system with:
    /// - Zero runtime cost in production builds (via [Conditional])
    /// - Category-based filtering
    /// - Runtime toggles in Editor/Development builds
    /// - Rich console formatting
    /// </summary>
    public static class DebugHelper
    {
        // Debug categories - add more as needed
        public enum Category
        {
            ChatManager,
            PoolingManager,
            TimedMessages,
            SaveManager,
            MugiParser,
            AutoScroll,
            UI,
            Addressables,
            Performance,
            All
        }

        // ═══════════════════════════════════════════════════════════
        // 🎛️ CONFIGURATION (Editor/Dev builds only)
        // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static bool globalDebugEnabled = true;

        private static Dictionary<Category, bool> categoryEnabled = new Dictionary<Category, bool>()
        {
            { Category.ChatManager, false },
            { Category.PoolingManager, false },
            { Category.TimedMessages, false },
            { Category.SaveManager, true },
            { Category.MugiParser, false },
            { Category.AutoScroll, false },
            { Category.UI, false },
            { Category.Addressables, false },
            { Category.Performance, false },
            { Category.All, true }
        };

        private static Dictionary<Category, string> categoryColors = new Dictionary<Category, string>()
        {
            { Category.ChatManager, "#4CAF50" },      // Green
            { Category.PoolingManager, "#2196F3" },   // Blue
            { Category.TimedMessages, "#FF9800" },    // Orange
            { Category.SaveManager, "#9C27B0" },     // Purple
            { Category.MugiParser, "#F44336" },       // Red
            { Category.AutoScroll, "#00BCD4" },       // Cyan
            { Category.UI, "#FFEB3B" },               // Yellow
            { Category.Addressables, "#795548" },     // Brown
            { Category.Performance, "#E91E63" },      // Pink
            { Category.All, "#FFFFFF" }               // White
        };
#endif

        // ═══════════════════════════════════════════════════════════
        // ⚡ OPTIMIZED LOGGING API (Zero cost in production)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Standard log - Completely removed in production builds
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(Category category, string message, Object context = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!ShouldLog(category)) return;
            Debug.Log(FormatMessage(category, message), context);
#endif
        }

        /// <summary>
        /// Warning log - Removed in production builds
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(Category category, string message, Object context = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!ShouldLog(category)) return;
            Debug.LogWarning(FormatMessage(category, message), context);
#endif
        }

        /// <summary>
        /// Error log - ALWAYS kept (even in production) for crash analytics
        /// </summary>
        public static void LogError(Category category, string message, Object context = null)
        {
            Debug.LogError(FormatMessage(category, message), context);
        }

        /// <summary>
        /// Log with custom color - Removed in production
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogColored(Category category, string message, Color color, Object context = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!ShouldLog(category)) return;
            string hexColor = ColorUtility.ToHtmlStringRGB(color);
            Debug.Log($"<color=#{hexColor}>[{category}]</color> {message}", context);
#endif
        }

        // ═══════════════════════════════════════════════════════════
        // 📊 SPECIALIZED LOGGING (Removed in production)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Log pooling statistics - Removed in production
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogPoolStats(string poolName, int active, int pooled, int total)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!ShouldLog(Category.PoolingManager)) return;
            Log(Category.PoolingManager, 
                $"Pool '{poolName}' - Active: {active}, Pooled: {pooled}, Total: {total}");
#endif
        }

        /// <summary>
        /// Log performance timing - Removed in production
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogPerformance(string operation, float milliseconds)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!ShouldLog(Category.Performance)) return;
            Log(Category.Performance, 
                $"{operation} took {milliseconds:F2}ms");
#endif
        }

        /// <summary>
        /// Hot path optimization - Use in Update/tight loops
        /// Avoids string interpolation when logging is disabled
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogFormat(Category category, string format, params object[] args)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!ShouldLog(category)) return;
            Debug.LogFormat(FormatMessage(category, string.Format(format, args)));
#endif
        }

        // ═══════════════════════════════════════════════════════════
        // 🔬 PROFILING HELPERS (Removed in production)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Begin profiler sample - Automatically removed in production
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void BeginSample(string sampleName)
        {
            UnityEngine.Profiling.Profiler.BeginSample(sampleName);
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void EndSample()
        {
            UnityEngine.Profiling.Profiler.EndSample();
        }

        // ═══════════════════════════════════════════════════════════
        // ✅ ASSERTIONS (Removed in production)
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Development-time assertion - Removed in production
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!condition)
            {
                Debug.LogError($"❌ ASSERTION FAILED: {message}");
#if UNITY_EDITOR
                Debug.Break(); // Pause editor
#endif
            }
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void AssertNotNull(object obj, string objectName)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (obj == null)
            {
                Debug.LogError($"❌ ASSERTION FAILED: {objectName} is NULL");
#if UNITY_EDITOR
                Debug.Break();
#endif
            }
#endif
        }

        // ═══════════════════════════════════════════════════════════
        // 🎛️ RUNTIME CONFIGURATION (Editor/Dev builds only)
        // ═══════════════════════════════════════════════════════════

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Enable or disable a specific debug category
        /// </summary>
        public static void SetCategoryEnabled(Category category, bool enabled)
        {
            if (categoryEnabled.ContainsKey(category))
            {
                categoryEnabled[category] = enabled;
            }
        }

        /// <summary>
        /// Enable or disable all debug output
        /// </summary>
        public static void SetGlobalDebugEnabled(bool enabled)
        {
            globalDebugEnabled = enabled;
        }

        /// <summary>
        /// Check if a category is enabled
        /// </summary>
        public static bool IsCategoryEnabled(Category category)
        {
            return globalDebugEnabled && 
                   categoryEnabled.ContainsKey(category) && 
                   categoryEnabled[category];
        }

        /// <summary>
        /// Enable all categories
        /// </summary>
        public static void EnableAll()
        {
            foreach (var key in new List<Category>(categoryEnabled.Keys))
            {
                categoryEnabled[key] = true;
            }
        }

        /// <summary>
        /// Disable all categories
        /// </summary>
        public static void DisableAll()
        {
            foreach (var key in new List<Category>(categoryEnabled.Keys))
            {
                categoryEnabled[key] = false;
            }
        }

        /// <summary>
        /// Load configuration from ScriptableObject
        /// </summary>
        public static void LoadConfig(DebugConfig config)
        {
            if (config == null) return;

            globalDebugEnabled = config.globalDebugEnabled;
            
            foreach (var setting in config.categorySettings)
            {
                if (categoryEnabled.ContainsKey(setting.category))
                {
                    categoryEnabled[setting.category] = setting.enabled;
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // 🔧 HELPER METHODS
        // ═══════════════════════════════════════════════════════════

        private static bool ShouldLog(Category category)
        {
            if (!globalDebugEnabled) return false;
            if (category == Category.All) return true;
            return categoryEnabled.ContainsKey(category) && categoryEnabled[category];
        }

        private static string FormatMessage(Category category, string message)
        {
            string color = categoryColors.ContainsKey(category) 
                ? categoryColors[category] 
                : "#FFFFFF";
            
            return $"<color={color}>[{category}]</color> {message}";
        }
#else
        // ⚡ Production builds: Minimal footprint
        private static string FormatMessage(Category category, string message)
        {
            return $"[{category}] {message}"; // No color in production
        }
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // 📋 SCRIPTABLEOBJECT CONFIGURATION
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// ScriptableObject for persistent debug configuration
    /// Create via: Assets > Create > Chat System > Debug Config
    /// </summary>
    [CreateAssetMenu(fileName = "DebugConfig", menuName = "Chat System/Debug Config", order = 100)]
    public class DebugConfig : ScriptableObject
    {
        [Header("Global Settings")]
        [Tooltip("Master switch for all debug logging")]
        public bool globalDebugEnabled = true;

        [Header("Category Settings")]
        [Tooltip("Toggle individual logging categories")]
        public List<CategorySetting> categorySettings = new List<CategorySetting>()
        {
            new CategorySetting { category = DebugHelper.Category.ChatManager, enabled = true },
            new CategorySetting { category = DebugHelper.Category.PoolingManager, enabled = false },
            new CategorySetting { category = DebugHelper.Category.TimedMessages, enabled = true },
            new CategorySetting { category = DebugHelper.Category.SaveManager, enabled = false },
            new CategorySetting { category = DebugHelper.Category.MugiParser, enabled = false },
            new CategorySetting { category = DebugHelper.Category.AutoScroll, enabled = false },
            new CategorySetting { category = DebugHelper.Category.UI, enabled = false },
            new CategorySetting { category = DebugHelper.Category.Addressables, enabled = false },
            new CategorySetting { category = DebugHelper.Category.Performance, enabled = false }
        };

        [System.Serializable]
        public class CategorySetting
        {
            public DebugHelper.Category category;
            public bool enabled;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnEnable()
        {
            // Auto-load when config is loaded
            DebugHelper.LoadConfig(this);
        }
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // 🎮 RUNTIME DEBUG MENU (Optional - Editor/Dev only)
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Optional: Runtime debug menu component
    /// Attach to any GameObject to get a debug toggle UI
    /// Only works in Editor/Development builds
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Configuration")]
        public DebugConfig debugConfig;
        public KeyCode toggleKey = KeyCode.F12;

        [Header("UI Settings")]
        public bool showMenu = false;
        public Vector2 menuPosition = new Vector2(10, 10);
        public Vector2 menuSize = new Vector2(300, 400);

        private Vector2 scrollPosition;

        private void Start()
        {
            if (debugConfig != null)
            {
                DebugHelper.LoadConfig(debugConfig);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                showMenu = !showMenu;
            }
        }

        private void OnGUI()
        {
            if (!showMenu) return;

            GUILayout.BeginArea(new Rect(menuPosition.x, menuPosition.y, menuSize.x, menuSize.y), GUI.skin.box);
            
            GUILayout.Label("Debug Helper Menu", GUI.skin.box);
            
            bool globalEnabled = DebugHelper.IsCategoryEnabled(DebugHelper.Category.All);
            bool newGlobalEnabled = GUILayout.Toggle(globalEnabled, "Global Debug Enabled");
            if (newGlobalEnabled != globalEnabled)
            {
                DebugHelper.SetGlobalDebugEnabled(newGlobalEnabled);
            }

            GUILayout.Space(10);
            GUILayout.Label("Categories:", GUI.skin.box);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            foreach (DebugHelper.Category category in System.Enum.GetValues(typeof(DebugHelper.Category)))
            {
                if (category == DebugHelper.Category.All) continue;

                bool isEnabled = DebugHelper.IsCategoryEnabled(category);
                bool newEnabled = GUILayout.Toggle(isEnabled, category.ToString());
                
                if (newEnabled != isEnabled)
                {
                    DebugHelper.SetCategoryEnabled(category, newEnabled);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            if (GUILayout.Button("Enable All"))
            {
                DebugHelper.EnableAll();
            }

            if (GUILayout.Button("Disable All"))
            {
                DebugHelper.DisableAll();
            }

            GUILayout.EndArea();
        }
#endif
    }
}