using Microsoft.Extensions.Logging;
using TerraNova.Core;
using TerraNova.Shared;

namespace TerraNova.Physics;

/// <summary>
/// Voxel physics implementation of IPhysicsBody.
/// Stores physics state for a body and integrates with VoxelCollisionSystem.
/// </summary>
public class VoxelPhysicsBody : IPhysicsBody
{
    private Vector3 _position;
    private Vector3 _velocity;
    private bool _affectedByGravity;
    private bool _isStatic;
    private VoxelPhysicsShape? _shape;
    private bool _isGrounded;
    private ILogger? _logger;
    private bool _autoJumpEnabled;

    // Smooth jump state (using ease-in cubic for natural upward acceleration)
    private bool _isJumping;
    private float _jumpStartVelocity;
    private float _jumpDuration;
    private float _jumpElapsedTime;

    // Auto-jump cooldown (prevents spam when stuck against walls)
    private float _lastAutoJumpTime;
    private const float AutoJumpCooldown = 0.5f; // 500ms between auto-jumps

    public VoxelPhysicsBody(ILogger? logger = null)
    {
        _position = Vector3.Zero;
        _velocity = Vector3.Zero;
        _affectedByGravity = false;
        _isStatic = false;
        _isGrounded = false;
        _isJumping = false;
        _jumpStartVelocity = 0f;
        _jumpDuration = 0f;
        _jumpElapsedTime = 0f;
        _lastAutoJumpTime = -AutoJumpCooldown; // Allow immediate first auto-jump
        _autoJumpEnabled = true; // Default to enabled
        _logger = logger;
    }

    public Vector3 Position
    {
        get => _position;
        set => _position = value;
    }

    public Vector3 Velocity
    {
        get => _velocity;
        set => _velocity = value;
    }

    public bool AffectedByGravity
    {
        get => _affectedByGravity;
        set => _affectedByGravity = value;
    }

    public bool IsStatic
    {
        get => _isStatic;
        set => _isStatic = value;
    }

    public bool IsGrounded
    {
        get => _isGrounded;
        internal set => _isGrounded = value; // Set by VoxelCollisionSystem during collision detection
    }

    /// <summary>
    /// Gets whether the body is currently executing a smooth jump
    /// </summary>
    public bool IsJumping => _isJumping;

    /// <summary>
    /// Gets or sets whether auto-jump is enabled for this body.
    /// When enabled, the body will automatically jump when walking into climbable ledges.
    /// Default: true
    /// </summary>
    public bool AutoJumpEnabled
    {
        get => _autoJumpEnabled;
        set => _autoJumpEnabled = value;
    }

    public void ApplyForce(Vector3 force)
    {
        // For voxel physics, we apply impulse directly to velocity
        // Force is treated as instantaneous impulse (force * deltaTime with deltaTime = 1 frame)
        // This works for jump impulses and similar instantaneous forces
        if (!_isStatic)
        {
            _velocity += force;
        }
    }

    public void SetShape(IPhysicsShape shape)
    {
        if (shape is not VoxelPhysicsShape voxelShape)
        {
            throw new ArgumentException("Shape must be VoxelPhysicsShape for voxel physics body", nameof(shape));
        }

        _shape = voxelShape;
    }

    /// <summary>
    /// Get the collision shape for this body.
    /// </summary>
    public VoxelPhysicsShape? Shape => _shape;

    /// <summary>
    /// Start a smooth jump with ease-in cubic acceleration curve.
    /// Used for both manual jumps (spacebar) and auto-jumps (ledge climbing).
    /// </summary>
    /// <param name="startVelocity">Initial upward velocity in m/s (5.0 reaches ~1.25m height)</param>
    /// <param name="duration">Duration of acceleration phase in seconds (0.3s feels natural)</param>
    public void StartJump(float startVelocity, float duration)
    {
        if (_isJumping)
            return; // Already jumping, ignore

        _isJumping = true;
        _jumpStartVelocity = startVelocity;
        _jumpDuration = duration;
        _jumpElapsedTime = 0f;

        // Set initial upward velocity
        _velocity = new Vector3(_velocity.X, startVelocity, _velocity.Z);
    }

    /// <summary>
    /// Start an auto-jump with cooldown protection.
    /// Prevents spam-jumping when stuck against walls.
    /// </summary>
    /// <param name="startVelocity">Initial upward velocity in m/s</param>
    /// <param name="duration">Duration of acceleration phase in seconds</param>
    /// <param name="currentTime">Current game time for cooldown tracking</param>
    /// <returns>True if auto-jump started, false if on cooldown</returns>
    public bool TryStartAutoJump(float startVelocity, float duration, float currentTime)
    {
        _logger?.LogInformation("[AUTO-JUMP BODY] TryStartAutoJump called: currentTime={CurrentTime:F2}, lastAutoJumpTime={LastTime:F2}, cooldown={Cooldown:F2}",
            currentTime, _lastAutoJumpTime, AutoJumpCooldown);
        _logger?.LogInformation("[AUTO-JUMP BODY] Time since last auto-jump: {TimeSince:F2}s (cooldown: {Cooldown:F2}s)",
            currentTime - _lastAutoJumpTime, AutoJumpCooldown);

        // Check cooldown
        if ((currentTime - _lastAutoJumpTime) < AutoJumpCooldown)
        {
            _logger?.LogInformation("[AUTO-JUMP BODY] Auto-jump BLOCKED by cooldown");
            return false;
        }

        _logger?.LogInformation("[AUTO-JUMP BODY] Auto-jump ALLOWED - starting jump!");
        _lastAutoJumpTime = currentTime;
        StartJump(startVelocity, duration);
        return true;
    }

    /// <summary>
    /// Update smooth jump physics (call during physics step, before gravity).
    /// Uses ease-in cubic curve for natural upward acceleration.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds</param>
    internal void UpdateJumpPhysics(float deltaTime)
    {
        if (!_isJumping)
            return;

        _jumpElapsedTime += deltaTime;

        if (_jumpElapsedTime < _jumpDuration)
        {
            // Still in acceleration phase - apply ease-in cubic
            float t = _jumpElapsedTime / _jumpDuration;
            float easeInCubic = t * t * t; // Smooth acceleration curve

            // Interpolate velocity from jumpStartVelocity down to 0
            // This gives a smooth upward acceleration with gradual deceleration
            float targetVelocity = _jumpStartVelocity * (1.0f - easeInCubic);

            // Preserve horizontal velocity, only modify Y
            _velocity = new Vector3(_velocity.X, targetVelocity, _velocity.Z);
        }
        else
        {
            // Jump acceleration complete - gravity takes over
            _isJumping = false;
            _jumpElapsedTime = 0f;
        }
    }
}
