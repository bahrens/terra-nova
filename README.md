# Terra Nova

A voxel-based game built with C# and .NET 9.

## Projects

- **TerraNova.Core** - Shared game logic
- **TerraNova.Client** - Client rendering abstractions
- **TerraNova.OpenTkClient** - Native desktop client (OpenTK)
- **TerraNova.WebClient** - Browser client (Blazor WebAssembly + WebGL)
- **TerraNova.Server** - Game server

## Build

```bash
cd src
dotnet build
```

## Run

Desktop client:
```bash
cd src/TerraNova.OpenTkClient
dotnet run
```

Web client:
```bash
cd src/TerraNova.WebClient
dotnet run
```
