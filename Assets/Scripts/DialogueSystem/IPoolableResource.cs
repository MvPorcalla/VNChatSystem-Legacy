//=====================================
// IPoolableResource.cs
//=====================================

namespace ChatDialogueSystem
{
    /// <summary>
    /// Interface for objects managed by PoolingManager that require cleanup and reset logic.
    /// Implement this on components that need custom pooling behavior.
    /// </summary>
    public interface IPoolableResource
    {
        /// <summary>
        /// Called when the object is returned to the pool.
        /// Clear dynamic content here (e.g., dynamically loaded sprites, text).
        /// DO NOT clear static prefab references (backgrounds, UI elements).
        /// </summary>
        void OnRecycle();

        /// <summary>
        /// Called when the object is taken from the pool for reuse.
        /// Reset state here (e.g., re-enable components, reset transforms).
        /// </summary>
        void OnReuse();
    }
}