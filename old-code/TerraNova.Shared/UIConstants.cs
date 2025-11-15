namespace TerraNova.Shared;

/// <summary>
/// Shared UI constants used across all clients (desktop and web)
/// </summary>
public static class UIConstants
{
    /// <summary>
    /// Crosshair dimensions
    /// </summary>
    public static class Crosshair
    {
        /// <summary>
        /// Length of the crosshair lines in pixels (odd number for perfect centering)
        /// </summary>
        public const float Length = 19.0f;

        /// <summary>
        /// Thickness of the crosshair lines in pixels
        /// </summary>
        public const float Thickness = 2.0f;
    }

    /// <summary>
    /// Hotbar dimensions and layout
    /// </summary>
    public static class Hotbar
    {
        /// <summary>
        /// Number of slots in the hotbar
        /// </summary>
        public const int SlotCount = 9;

        /// <summary>
        /// Size of each hotbar slot in pixels
        /// </summary>
        public const float SlotSize = 50.0f;

        /// <summary>
        /// Spacing between slots in pixels
        /// </summary>
        public const float SlotSpacing = 4.0f;

        /// <summary>
        /// Distance from the bottom of the screen in pixels
        /// </summary>
        public const float BottomMargin = 20.0f;

        /// <summary>
        /// Border thickness for slots in pixels
        /// </summary>
        public const float BorderThickness = 2.0f;

        /// <summary>
        /// Border thickness for selected slot in pixels
        /// </summary>
        public const float SelectedBorderThickness = 3.0f;
    }
}
