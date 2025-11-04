using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TerraNova.GameLogic;
using TerraNova.Shared;
using OpenTKVector3 = OpenTK.Mathematics.Vector3;
using SharedVector3 = TerraNova.Shared.Vector3;
using SharedVector3i = TerraNova.Shared.Vector3i;

namespace TerraNova;

/// <summary>
/// OpenTK-based renderer implementation for desktop client.
/// Manages OpenGL resources and chunk mesh rendering.
/// </summary>
public class OpenTKRenderer : IRenderer, IDisposable
{
    private readonly Dictionary<SharedVector3i, ChunkMeshData> _chunkMeshDataCache = new();
    private readonly Dictionary<SharedVector3i, ChunkMesh> _chunkMeshes = new();
    private readonly World _world;
    private Camera? _camera;
    private OpenTKVector3 _cameraRotation;
    private SharedVector3i? _highlightedBlock;

    private Shader _shader = null!;
    private Shader _borderedShader = null!;
    private Texture _grassTexture = null!;

    public OpenTKRenderer(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Initialize OpenGL resources (must be called after OpenGL context is created)
    /// </summary>
    public void Initialize()
    {
        // Load shaders
        string vertexShaderSource = File.ReadAllText("Shaders/basic.vert");
        string fragmentShaderSource = File.ReadAllText("Shaders/basic.frag");
        _shader = new Shader(vertexShaderSource, fragmentShaderSource);

        // Load bordered shader for selected blocks
        string borderedFragmentShaderSource = File.ReadAllText("Shaders/bordered.frag");
        _borderedShader = new Shader(vertexShaderSource, borderedFragmentShaderSource);

        // Generate grayscale noisy texture (multiplied with vertex colors for variation)
        byte[] noisePixels = TextureGenerator.GenerateGrayscaleNoiseTexture(16);
        _grassTexture = new Texture(16, 16, noisePixels);
    }

    /// <summary>
    /// Update or create a chunk mesh at the specified position
    /// </summary>
    public Task UpdateChunk(SharedVector3i chunkPos, ChunkMeshData meshData)
    {
        // Cache the mesh data
        _chunkMeshDataCache[chunkPos] = meshData;

        // Remove existing mesh if present
        if (_chunkMeshes.TryGetValue(chunkPos, out var existingMesh))
        {
            existingMesh.Dispose();
            _chunkMeshes.Remove(chunkPos);
        }

        // Create chunk object
        var chunk = new Chunk(chunkPos);

        // Fill chunk with blocks from mesh data (we need this for ChunkMesh construction)
        // Note: This is a simplification - in a real implementation we'd pass mesh data directly
        // For now, we'll reconstruct the chunk from the world
        var chunkMesh = new ChunkMesh(chunk, _world);
        _chunkMeshes[chunkPos] = chunkMesh;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove a chunk mesh at the specified position
    /// </summary>
    public void RemoveChunk(SharedVector3i chunkPos)
    {
        if (_chunkMeshes.TryGetValue(chunkPos, out var mesh))
        {
            mesh.Dispose();
            _chunkMeshes.Remove(chunkPos);
        }

        _chunkMeshDataCache.Remove(chunkPos);
    }

    /// <summary>
    /// Update camera position and rotation for rendering
    /// </summary>
    public void SetCamera(SharedVector3 position, SharedVector3 rotation)
    {
        // Note: We'll need to update this to use the camera properly
        // For now, we store the rotation for later use
        _cameraRotation = new OpenTKVector3(rotation.X, rotation.Y, rotation.Z);
    }

    /// <summary>
    /// Set the camera reference (called by Game.cs)
    /// </summary>
    public void SetCameraReference(Camera camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Highlight a block at the specified position
    /// </summary>
    public void HighlightBlock(SharedVector3i blockPos, bool highlight)
    {
        _highlightedBlock = highlight ? blockPos : null;
    }

    /// <summary>
    /// Render all chunks and the highlighted block
    /// </summary>
    public void Render(int viewportWidth, int viewportHeight)
    {
        if (_camera == null)
            return;

        // Set view and projection matrices (same for all blocks)
        Matrix4 view = _camera.GetViewMatrix();
        float aspectRatio = (float)viewportWidth / viewportHeight;
        Matrix4 projection = _camera.GetProjectionMatrix(aspectRatio);
        Matrix4 model = Matrix4.Identity; // Blocks are already positioned, so model is identity

        // Draw all chunks with normal shader
        _shader.Use();
        _grassTexture.Bind(0);
        _shader.SetInt("blockTexture", 0);
        _shader.SetMatrix4("view", view);
        _shader.SetMatrix4("projection", projection);
        _shader.SetMatrix4("model", model);

        foreach (var chunkMesh in _chunkMeshes.Values)
        {
            chunkMesh.Draw();
        }

        // If a block is highlighted, draw it again with the bordered shader
        if (_highlightedBlock.HasValue)
        {
            BlockType selectedBlockType = _world.GetBlock(
                _highlightedBlock.Value.X,
                _highlightedBlock.Value.Y,
                _highlightedBlock.Value.Z);

            if (selectedBlockType != BlockType.Air)
            {
                BlockFaces visibleFaces = _world.GetVisibleFaces(
                    _highlightedBlock.Value.X,
                    _highlightedBlock.Value.Y,
                    _highlightedBlock.Value.Z);

                // Create a temporary mesh for the selected block with bordered shader
                using var selectedBlockMesh = new CubeMesh(
                    new OpenTKVector3(_highlightedBlock.Value.X, _highlightedBlock.Value.Y, _highlightedBlock.Value.Z),
                    selectedBlockType,
                    visibleFaces);

                // Enable polygon offset to prevent z-fighting with the chunk mesh
                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(-1.0f, -1.0f); // Negative values pull towards camera

                _borderedShader.Use();
                _grassTexture.Bind(0);
                _borderedShader.SetInt("blockTexture", 0);
                _borderedShader.SetMatrix4("view", view);
                _borderedShader.SetMatrix4("projection", projection);
                _borderedShader.SetMatrix4("model", model);

                selectedBlockMesh.Draw();

                // Disable polygon offset after drawing
                GL.Disable(EnableCap.PolygonOffsetFill);
            }
        }
    }

    public void Dispose()
    {
        _shader?.Dispose();
        _borderedShader?.Dispose();
        _grassTexture?.Dispose();

        foreach (var mesh in _chunkMeshes.Values)
        {
            mesh.Dispose();
        }
        _chunkMeshes.Clear();
    }
}
