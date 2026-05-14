using System.Collections.Generic;

/// <summary>
/// Root content type for a compiled scene asset.
/// </summary>
public class SceneAssetContent
{
    public List<SceneNodeContent> Nodes { get; set; } = new();
}

/// <summary>
/// Root content type for the compiled level index asset.
/// </summary>
public class SceneListContent
{
    public List<string> SceneNames { get; set; } = new();
}

/// <summary>
/// Shared scene node types produced by the content pipeline.
/// </summary>
public enum SceneNodeType
{
    Unknown = 0,
    Scene,
    Camera,
    Light,
    SpawnPoint,
    Goal,
    Mesh
}

/// <summary>
/// Shared scene node data produced by the content pipeline.
/// </summary>
public class SceneNodeContent
{
}