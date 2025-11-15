using Microsoft.Extensions.Logging;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using TerraNova.Core;
using TerraNova.Shared;
using OpenTKVector3 = OpenTK.Mathematics.Vector3;
using SharedVector3 = TerraNova.Shared.Vector3;
using SharedVector3i = TerraNova.Shared.Vector3i;
using SharedVector2i = TerraNova.Shared.Vector2i;

namespace TerraNova;

/// <summary>
/// OpenTK-based renderer implementation for desktop client.
/// Manages OpenGL resources and chunk mesh rendering (2D column chunks).
/// </summary>
public class OpenTKRenderer : IRenderer, IDisposable
{
    private readonly Dictionary<SharedVector2i, ChunkMesh> _chunkMeshes = new();
    private readonly World _world;
    private readonly ILogger<OpenTKRenderer> _logger;
    private Camera? _camera;
    private SharedVector3i? _highlightedBlock;

    // Chunk cleanup constants
    private const int ChunkUnloadDistance = 12; // Chunks beyond this distance (in chunks) from camera will be unloaded
    private double _cleanupTimer = 0.0;
    private const double ChunkCleanupInterval = 2.0; // Run cleanup every N seconds

    private Shader _shader = null!;
    private Shader _borderedShader = null!;
    private Texture _grassTexture = null!;

    public OpenTKRenderer(World world, ILogger<OpenTKRenderer> logger)
    {
        _world = world;
        _logger = logger;
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
    /// Update or create a chunk mesh at the specified 2D position (X, Z only)
    /// </summary>
    public Task UpdateChunk(SharedVector2i chunkPos, ChunkMeshData meshData)
    {
        // Remove existing mesh if present
        if (_chunkMeshes.TryGetValue(chunkPos, out var existingMesh))
        {
            existingMesh.Dispose();
            _chunkMeshes.Remove(chunkPos);
        }

        // Create OpenGL mesh from the pre-generated ChunkMeshData
        var chunkMesh = new ChunkMesh(meshData);
        _chunkMeshes[chunkPos] = chunkMesh;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Remove a chunk mesh at the specified 2D position (X, Z only)
    /// </summary>
    public void RemoveChunk(SharedVector2i chunkPos)
    {
        if (_chunkMeshes.TryGetValue(chunkPos, out var mesh))
        {
            mesh.Dispose();
            _chunkMeshes.Remove(chunkPos);
        }
    }

    /// <summary>
    /// Update renderer state and periodically clean up distant chunks
    /// </summary>
    public void Update(double deltaTime)
    {
        _cleanupTimer += deltaTime;
        if (_cleanupTimer >= ChunkCleanupInterval)
        {
            _cleanupTimer = 0.0;
            CleanupDistantChunks();
        }
    }

    /// <summary>
    /// Removes chunk meshes that are far from the camera
    /// </summary>
    private void CleanupDistantChunks()
    {
        if (_camera == null)
            return;

        // Calculate camera's chunk position
        var cameraPos = _camera.Position.ToShared();
        int cameraChunkX = (int)Math.Floor(cameraPos.X / Chunk.ChunkSize);
        int cameraChunkZ = (int)Math.Floor(cameraPos.Z / Chunk.ChunkSize);

        // Find chunks to unload
        var chunksToUnload = new List<SharedVector2i>();
        foreach (var chunkPos in _chunkMeshes.Keys)
        {
            // Calculate distance in chunks (Chebyshev distance)
            int distanceX = Math.Abs(chunkPos.X - cameraChunkX);
            int distanceZ = Math.Abs(chunkPos.Z - cameraChunkZ);
            int chunkDistance = Math.Max(distanceX, distanceZ);

            if (chunkDistance > ChunkUnloadDistance)
            {
                chunksToUnload.Add(chunkPos);
            }
        }

        // Unload distant chunk meshes (ChunkLoader handles World data cleanup)
        foreach (var chunkPos in chunksToUnload)
        {
            // Remove mesh from GPU
            RemoveChunk(chunkPos);
        }

        if (chunksToUnload.Count > 0)
        {
            _logger.LogInformation("Unloaded {ChunkCount} distant chunk meshes. Total meshes: {TotalMeshCount}",
                chunksToUnload.Count, _chunkMeshes.Count);
        }
    }

    /// <summary>
    /// Set the camera reference for view matrix calculations
    /// </summary>
    /// <param name="cameraView">Camera view interface (must be Camera implementation for OpenTK)</param>
    public void SetCamera(ICameraView cameraView)
    {
        if (cameraView is not Camera cam)
            throw new ArgumentException("Camera must be of type Camera for OpenTKRenderer", nameof(cameraView));

        _camera = cam;
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

                // Generate mesh data using ChunkMeshBuilder
                var meshData = ChunkMeshBuilder.BuildSingleBlockMesh(
                    _highlightedBlock.Value,
                    selectedBlockType,
                    visibleFaces);

                // Create a temporary mesh for the selected block with bordered shader
                using var selectedBlockMesh = new CubeMesh(meshData);

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
