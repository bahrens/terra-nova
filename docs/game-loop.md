# Game Loop Flow

## OpenTK Game Loop Structure

```
┌─────────────────────────────────────────┐
│         ONE FRAME CYCLE                 │
├─────────────────────────────────────────┤
│                                         │
│  1. OnUpdateFrame (Game Logic)          │
│     - Process input                     │
│     - Update positions                  │
│     - Update physics                    │
│     - Update game state                 │
│     - Uses deltaTime                    │
│                                         │
│  2. OnRenderFrame (Drawing)             │
│     - Clear screen                      │
│     - Draw using CURRENT game state     │
│     - Swap buffers                      │
│                                         │
└─────────────────────────────────────────┘
         ↓ (repeat forever)
```

## Implementation in OpenTK

```csharp
protected override void OnUpdateFrame(FrameEventArgs args)
{
    base.OnUpdateFrame(args);

    float deltaTime = (float)args.Time;

    // THIS RUNS FIRST - Update game state
    // - Handle input
    // - Update object positions
    // - Run physics
    // - Update animations

    if (KeyboardState.IsKeyDown(Keys.Escape))
        Close();

    // Example: move an object
    objectPosition.X += velocity * deltaTime;
}

protected override void OnRenderFrame(FrameEventArgs args)
{
    base.OnRenderFrame(args);

    // THIS RUNS SECOND - Draw using updated state
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    // Use the updated objectPosition from OnUpdateFrame
    DrawObject(objectPosition);

    SwapBuffers();
}
```

## Key Differences

| **OnUpdateFrame** | **OnRenderFrame** |
|-------------------|-------------------|
| Updates game logic | Draws to screen |
| Modifies state | Reads state |
| Can run at fixed rate (60 FPS) | Can run at variable rate |
| Uses deltaTime for consistency | Uses current state |
| No OpenGL drawing calls | All OpenGL drawing here |

## Update Frequency Configuration

In `Program.cs`:

```csharp
var gameWindowSettings = GameWindowSettings.Default;
gameWindowSettings.UpdateFrequency = 60.0; // Update 60 times per second

// UpdateFrequency = 60 → OnUpdateFrame called 60 times/sec
// RenderFrequency = 0 (default) → OnRenderFrame runs as fast as possible (or limited by VSync)
```

## Why Separate Update and Render?

1. **Game logic** (update) should run at consistent rate
2. **Rendering** (render) can run faster/slower based on GPU
3. **Decoupling** allows smooth gameplay even if rendering slows down

## Complete Example

```csharp
// Game state
private float rotation = 0f;
private Vector3 position = Vector3.Zero;

protected override void OnUpdateFrame(FrameEventArgs args)
{
    base.OnUpdateFrame(args);

    // Update game state (happens first)
    rotation += 90f * (float)args.Time; // Rotate 90°/second

    if (KeyboardState.IsKeyDown(Keys.W))
        position.Z -= 5f * (float)args.Time;
}

protected override void OnRenderFrame(FrameEventArgs args)
{
    base.OnRenderFrame(args);

    // Draw using the updated rotation and position
    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    // Create transform matrix from updated state
    var transform = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotation))
                  * Matrix4.CreateTranslation(position);

    // Render with this transform
    DrawTriangle(transform);

    SwapBuffers();
}
```

## Important

**The update happens BEFORE the render, so rendering always uses the latest game state!**
