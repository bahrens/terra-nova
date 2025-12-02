using OpenTK.Graphics.OpenGL4;
using TerraNova.Client.Rendering;

namespace TerraNova.OpenTkClient.Rendering;

public class OpenTkShaderProgram : IDisposable
{
    private readonly int _handle;
    private readonly Dictionary<string, int> _uniformLocations = new();
    private bool _disposed;

    public OpenTkShaderProgram(string shaderName)
    {
        var vertexSource = ShaderLoader.LoadShaderSource($"{shaderName}.vert", ShaderPlatform.OpenGL);
        var fragmentSource = ShaderLoader.LoadShaderSource($"{shaderName}.frag", ShaderPlatform.OpenGL);

        var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        _handle = GL.CreateProgram();
        GL.AttachShader(_handle, vertexShader);
        GL.AttachShader(_handle, fragmentShader);
        GL.LinkProgram(_handle);

        GL.GetProgram(_handle, GetProgramParameterName.LinkStatus, out var status);
        if (status == 0)
        {
            var infoLog = GL.GetProgramInfoLog(_handle);
            GL.DeleteProgram(_handle);
            throw new InvalidOperationException($"Error linking shader program: {infoLog}");
        }

        GL.DetachShader(_handle, vertexShader);
        GL.DetachShader(_handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        CacheUniformLocations();
    }

    public void Use()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        GL.UseProgram(_handle);
    }

    public void SetMatrix4(string name, System.Numerics.Matrix4x4 matrix)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_uniformLocations.TryGetValue(name, out var location))
        {
            float[] values =
            [
              matrix.M11, matrix.M12, matrix.M13, matrix.M14,
              matrix.M21, matrix.M22, matrix.M23, matrix.M24,
              matrix.M31, matrix.M32, matrix.M33, matrix.M34,
              matrix.M41, matrix.M42, matrix.M43, matrix.M44
            ];
            GL.UniformMatrix4(location, 1, true, values);
        }
        else
        {
            throw new ArgumentException($"Uniform '{name}' not found in shader.");
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (!_disposed)
        {
            GL.DeleteProgram(_handle);
            _disposed = true;
        }
    }

    private static int CompileShader(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var status);
        if (status == 0)
        {
            var infoLog = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"Error compiling {type} shader: {infoLog}");
        }
        return shader;
    }

    private void CacheUniformLocations()
    {
        GL.GetProgram(_handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

        for (var i = 0; i < numberOfUniforms; i++)
        {
            var key = GL.GetActiveUniform(_handle, i, out _, out _);
            var location = GL.GetUniformLocation(_handle, key);
            _uniformLocations[key] = location;
        }
    }
}
