using Microsoft.JSInterop;
using TerraNova.GameLogic;
using TerraNova.Shared;

namespace TerraNova.Web;

/// <summary>
/// Three.js renderer implementation using JS Interop.
/// This class bridges C# game logic with the Three.js JavaScript renderer.
/// </summary>
public class ThreeJsRenderer : IRenderer
{
    private readonly IJSRuntime _jsRuntime;

    public ThreeJsRenderer(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Update or create a chunk mesh in the Three.js scene
    /// </summary>
    public async Task UpdateChunk(Vector3i chunkPos, ChunkMeshData meshData)
    {
        try
        {
            // Call JavaScript function to update the chunk
            await _jsRuntime.InvokeVoidAsync(
                "terraNovaRenderer.updateChunk",
                chunkPos.X, chunkPos.Y, chunkPos.Z,
                meshData.Vertices,
                meshData.Colors,
                meshData.TexCoords,
                meshData.Indices
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating chunk: {ex.Message}");
        }
    }

    /// <summary>
    /// Remove a chunk mesh from the Three.js scene
    /// </summary>
    public async void RemoveChunk(Vector3i chunkPos)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "terraNovaRenderer.removeChunk",
                chunkPos.X, chunkPos.Y, chunkPos.Z
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing chunk: {ex.Message}");
        }
    }

    /// <summary>
    /// Update camera position and rotation in the Three.js scene
    /// </summary>
    public async void SetCamera(Vector3 position, Vector3 rotation)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "terraNovaRenderer.setCamera",
                position.X, position.Y, position.Z,
                rotation.X, rotation.Y, rotation.Z
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting camera: {ex.Message}");
        }
    }

    /// <summary>
    /// Highlight a block for selection in the Three.js scene
    /// </summary>
    public async void HighlightBlock(Vector3i blockPos, bool highlight)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "terraNovaRenderer.highlightBlock",
                highlight,
                blockPos.X, blockPos.Y, blockPos.Z
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error highlighting block: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize the noise texture using shared C# generation code
    /// </summary>
    public async Task InitializeNoiseTextureAsync()
    {
        try
        {
            // Generate the noise texture using shared code
            byte[] textureData = TextureGenerator.GenerateGrayscaleNoiseTexture(16);

            // Pass to JavaScript
            await _jsRuntime.InvokeVoidAsync(
                "terraNovaRenderer.setNoiseTexture",
                textureData,
                16 // size
            );

            Console.WriteLine("Noise texture initialized from C# shared code");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing noise texture: {ex.Message}");
        }
    }

    /// <summary>
    /// Initialize block colors from C# BlockHelper (single source of truth)
    /// </summary>
    public async Task InitializeBlockColorsAsync()
    {
        try
        {
            Console.WriteLine("InitializeBlockColorsAsync starting...");

            // Get all block types and their colors from shared C# code
            var blockColors = new Dictionary<int, object>();

            foreach (BlockType blockType in Enum.GetValues(typeof(BlockType)))
            {
                var color = BlockHelper.GetBlockColor(blockType);
                blockColors[(int)blockType] = new { r = color.r, g = color.g, b = color.b };
            }

            Console.WriteLine($"Prepared {blockColors.Count} block colors");

            // Serialize to JSON string
            string blockColorsJson = System.Text.Json.JsonSerializer.Serialize(blockColors);
            Console.WriteLine($"Serialized block colors JSON (length: {blockColorsJson.Length})");

            // Pass to JavaScript using proper JSInterop instead of eval
            var success = await _jsRuntime.InvokeAsync<bool>("terraNova.initializeBlockColors", blockColorsJson);

            if (success)
            {
                Console.WriteLine("Block colors initialized from C# BlockHelper (single source of truth) - SUCCESSFULLY APPLIED");
            }
            else
            {
                Console.WriteLine("Block colors initialized from C# BlockHelper (single source of truth) - STORED AS PENDING (will be applied when blockInteraction module loads)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing block colors: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
