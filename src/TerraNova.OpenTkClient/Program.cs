using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace TerraNova.OpenTkClient;

public class Program
{
    public static void Main(string[] args)
    {
        var gameWindowSettings = GameWindowSettings.Default;
        gameWindowSettings.UpdateFrequency = 60.0;

        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(800, 600),
            Title = "Terra Nova",
            Flags = ContextFlags.ForwardCompatible,
            APIVersion = new Version(4, 3),
            Profile = ContextProfile.Core,
            StartVisible = true,
            StartFocused = true,
            WindowBorder = WindowBorder.Resizable,
        };

        using (var window = new TerraNovaWindow(gameWindowSettings, nativeWindowSettings))
        {
            window.Run();
        }
    }
}
