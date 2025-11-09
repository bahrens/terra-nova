using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TerraNova.Shared;

namespace TerraNova.GameLogic;

/// <summary>
/// Manages chunk mesh generation with async processing using background worker threads.
/// Builds chunk meshes on background threads and queues them for GPU upload on the main thread.
/// </summary>
public class AsyncChunkMeshBuilder : IDisposable
{
    private readonly World _world;
    private readonly ConcurrentQueue<Vector2i> _dirtyChunks = new();
    private readonly ConcurrentQueue<(Vector2i chunkPos, ChunkMeshData meshData)> _completedMeshes = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task[] _workerTasks;
    private const int MaxMeshUploadsPerFrame = 3; // Frame budget to prevent stuttering

    public AsyncChunkMeshBuilder(World world)
    {
        _world = world;

        // Start background worker threads (4 workers for parallel mesh building)
        int workerCount = Math.Max(2, Environment.ProcessorCount / 2);
        _workerTasks = new Task[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            _workerTasks[i] = Task.Run(() => WorkerLoop(_cancellationTokenSource.Token));
        }
    }

    /// <summary>
    /// Background worker that continuously processes dirty chunks
    /// </summary>
    private void WorkerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_dirtyChunks.TryDequeue(out var chunkPos))
            {
                var chunk = _world.GetChunk(chunkPos);
                if (chunk != null)
                {
                    // Build mesh on background thread (CPU-intensive work)
                    var meshData = ChunkMeshBuilder.BuildChunkMesh(chunk, _world);
                    _completedMeshes.Enqueue((chunkPos, meshData));
                }
            }
            else
            {
                // No work available, sleep briefly to avoid busy-waiting
                Thread.Sleep(1);
            }
        }
    }

    /// <summary>
    /// Queue a chunk for mesh generation on background thread.
    /// </summary>
    public void EnqueueChunk(Vector2i chunkPos)
    {
        _dirtyChunks.Enqueue(chunkPos);
    }

    /// <summary>
    /// Process completed meshes and upload to renderer (called on main thread).
    /// Implements frame budget to prevent uploading too many meshes in one frame.
    /// Returns number of meshes processed.
    /// </summary>
    public int ProcessCompletedMeshes(IRenderer renderer)
    {
        int processedCount = 0;

        // Process up to MaxMeshUploadsPerFrame meshes per frame to avoid stuttering
        while (processedCount < MaxMeshUploadsPerFrame && _completedMeshes.TryDequeue(out var item))
        {
            renderer.UpdateChunk(item.chunkPos, item.meshData);
            processedCount++;
        }

        return processedCount;
    }

    /// <summary>
    /// Get number of chunks waiting to be uploaded to renderer
    /// </summary>
    public int PendingMeshCount => _completedMeshes.Count;

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        Task.WaitAll(_workerTasks);
        _cancellationTokenSource.Dispose();
    }
}
