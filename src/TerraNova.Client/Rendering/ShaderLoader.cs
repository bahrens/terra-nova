using System.Reflection;

namespace TerraNova.Client.Rendering;

public enum ShaderPlatform
{
    OpenGL,
    WebGL,
}

public static class ShaderLoader
{
    public static string LoadShaderSource(string shaderName, ShaderPlatform platform)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourcePath = platform switch
        {
            ShaderPlatform.OpenGL => $"TerraNova.Client.Shaders.OpenGL.{shaderName}",
            ShaderPlatform.WebGL => $"TerraNova.Client.Shaders.WebGL.{shaderName}",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null)
        {
            throw new InvalidOperationException($"Shader resource not found: {resourcePath}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
