namespace TerraNova.Shared;

/// <summary>
/// Shared rendering settings and constants for lighting
/// </summary>
public static class RenderSettings
{
    /// <summary>
    /// Lighting configuration for block faces
    /// </summary>
    public static class Lighting
    {
        /// <summary>
        /// Base ambient light level (minimum brightness)
        /// </summary>
        public const float AmbientLight = 0.4f;

        /// <summary>
        /// Brightness multiplier for top faces (receives most sunlight)
        /// </summary>
        public const float TopFaceBrightness = 1.0f;

        /// <summary>
        /// Brightness multiplier for bottom faces (darkest)
        /// </summary>
        public const float BottomFaceBrightness = 0.5f;

        /// <summary>
        /// Brightness multiplier for side faces (moderate)
        /// </summary>
        public const float SideFaceBrightness = 0.75f;

        /// <summary>
        /// Ambient occlusion darkness level when surrounded by blocks
        /// </summary>
        public const float AmbientOcclusionStrength = 0.3f;
    }
}
