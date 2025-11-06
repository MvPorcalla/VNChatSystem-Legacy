using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridColumns : MonoBehaviour
{
    [Header("Grid Settings")]
    [Range(1, 10)]
    [SerializeField] private int maxColumns = 4;
    [SerializeField] private float padding = 10f;
    [SerializeField] private float spacing = 10f;
    
    [Header("Performance")]
    [Tooltip("Only recalculate when width changes (recommended: true)")]
    [SerializeField] private bool onlyOnResize = true;
    
    private GridLayoutGroup grid;
    private RectTransform rectTransform;
    private float lastWidth = -1f;
    private int lastColumnCount = -1;
    
    // ═══════════════════════════════════════════════════════════
    // ░ INITIALIZATION
    // ═══════════════════════════════════════════════════════════
    
    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();
    }
    
    private void OnEnable()
    {
        // Force recalculation when enabled
        lastWidth = -1f;
        CalculateColumns();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ░ UPDATE LOGIC
    // ═══════════════════════════════════════════════════════════
    
    private void Update()
    {
        if (grid == null || rectTransform == null)
            return;
        
        // ✅ OPTIMIZATION: Only recalculate if width changed
        if (onlyOnResize)
        {
            float currentWidth = rectTransform.rect.width;
            
            // Skip if width hasn't changed (avoids recalculating every frame)
            if (Mathf.Approximately(currentWidth, lastWidth))
                return;
            
            lastWidth = currentWidth;
        }
        
        CalculateColumns();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ░ COLUMN CALCULATION
    // ═══════════════════════════════════════════════════════════
    
    private void CalculateColumns()
    {
        float totalWidth = rectTransform.rect.width - padding;
        float cellWidth = grid.cellSize.x;
        
        // ✅ OPTIMIZATION: Early exit if width is invalid
        if (totalWidth <= 0 || cellWidth <= 0)
            return;
        
        // Calculate how many columns can fit
        int columns = CalculateFittingColumns(totalWidth, cellWidth, spacing, maxColumns);
        
        // ✅ OPTIMIZATION: Only update if column count changed
        if (columns != lastColumnCount)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            lastColumnCount = columns;
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Force layout rebuild in editor
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            }
            #endif
        }
    }
    
    /// <summary>
    /// Calculates how many columns fit within the given width.
    /// </summary>
    private int CalculateFittingColumns(float availableWidth, float cellWidth, float spacing, int maxCols)
    {
        for (int cols = maxCols; cols > 0; cols--)
        {
            float neededWidth = (cellWidth * cols) + (spacing * (cols - 1));
            
            if (neededWidth <= availableWidth)
                return cols;
        }
        
        return 1; // Minimum 1 column
    }
    
    // ═══════════════════════════════════════════════════════════
    // ░ PUBLIC API (for manual recalculation)
    // ═══════════════════════════════════════════════════════════
    
    /// <summary>
    /// Forces a recalculation of columns (useful after adding/removing items).
    /// </summary>
    public void ForceRecalculate()
    {
        lastWidth = -1f;
        lastColumnCount = -1;
        CalculateColumns();
    }
    
    /// <summary>
    /// Updates settings and recalculates.
    /// </summary>
    public void UpdateSettings(int newMaxColumns, float newPadding, float newSpacing)
    {
        maxColumns = Mathf.Clamp(newMaxColumns, 1, 10);
        padding = newPadding;
        spacing = newSpacing;
        ForceRecalculate();
    }
    
    // ═══════════════════════════════════════════════════════════
    // ░ INSPECTOR VALIDATION
    // ═══════════════════════════════════════════════════════════
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Recalculate when inspector values change
        if (grid != null && rectTransform != null)
        {
            lastWidth = -1f;
            CalculateColumns();
        }
    }
    #endif
}