using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace TerraNova;

/// <summary>
/// Manages GLSL shader programs (vertex and fragment shaders)
/// </summary>
public class Shader : IDisposable
{
    private readonly int _handle;
    private bool _disposed = false;

    /// <summary>
    /// Creates a shader program from vertex and fragment shader source code
    /// </summary>
    public Shader(string vertexSource, string fragmentSource)
    {
        // Compile vertex shader
        int vertexShader = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, vertexSource);
        GL.CompileShader(vertexShader);
        CheckShaderCompileErrors(vertexShader, "VERTEX");

        // Compile fragment shader
        int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, fragmentSource);
        GL.CompileShader(fragmentShader);
        CheckShaderCompileErrors(fragmentShader, "FRAGMENT");

        // Link shaders into a program
        _handle = GL.CreateProgram();
        GL.AttachShader(_handle, vertexShader);
        GL.AttachShader(_handle, fragmentShader);
        GL.LinkProgram(_handle);
        CheckProgramLinkErrors(_handle);

        // Clean up - shaders are now linked into the program
        GL.DetachShader(_handle, vertexShader);
        GL.DetachShader(_handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    /// <summary>
    /// Activates this shader for rendering
    /// </summary>
    public void Use()
    {
        GL.UseProgram(_handle);
    }

    /// <summary>
    /// Sets a Matrix4 uniform in the shader
    /// </summary>
    public void SetMatrix4(string name, Matrix4 matrix)
    {
        int location = GL.GetUniformLocation(_handle, name);
        GL.UniformMatrix4(location, false, ref matrix);
    }

    /// <summary>
    /// Sets a Vector3 uniform in the shader
    /// </summary>
    public void SetVector3(string name, Vector3 vector)
    {
        int location = GL.GetUniformLocation(_handle, name);
        GL.Uniform3(location, vector);
    }

    /// <summary>
    /// Sets a float uniform in the shader
    /// </summary>
    public void SetFloat(string name, float value)
    {
        int location = GL.GetUniformLocation(_handle, name);
        GL.Uniform1(location, value);
    }

    /// <summary>
    /// Sets an int uniform in the shader
    /// </summary>
    public void SetInt(string name, int value)
    {
        int location = GL.GetUniformLocation(_handle, name);
        GL.Uniform1(location, value);
    }

    private void CheckShaderCompileErrors(int shader, string type)
    {
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Shader compilation error ({type}):\n{infoLog}");
        }
    }

    private void CheckProgramLinkErrors(int program)
    {
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            throw new Exception($"Shader program linking error:\n{infoLog}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            GL.DeleteProgram(_handle);
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
