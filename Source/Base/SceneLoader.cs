// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Loads and manages scenes in the game.
/// Scenes are defined in JSON files and can be loaded at runtime.
/// They can be created by hand , but we use Blender to create them
/// and a export screen is provided to facilitate this process.
/// </summary>
public class SceneLoader
{
    Dictionary<string, Func<Model, ContentManager, Entity>> _assetMap = new();
    private readonly ContentManager _content;
    private readonly GraphicsDevice _graphicsDevice;

    public SceneLoader(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _content = content;
        _graphicsDevice = graphicsDevice;
        _assetMap = new()
        {
            ["platform-falling"] = (model, content) => new FallingPlatform(model, content),
            ["platform-moving"] = (model, content) => new MovingPlatform(model, content),
            ["coin"] = (model, content) => new Coin(model, content),
            ["platform"] = (model, content) => new Platform(model, content),
            ["platform-medium"] = (model, content) => new Platform(model, content),
            ["platform-large"] = (model, content) => new Platform(model, content),
            ["platform-grass-large-round"] = (model, content) => new Platform(model, content),
            ["cloud"] = (model, content) => new Cloud(model, content),
            ["default"] = (model, content) => new Entity(model, content),
            ["jumppad"] = (model, content) => new JumpPad(model, content)
        };
    }


    public Scene LoadScene(string sceneName)
    {
        var scene = new Scene(_graphicsDevice, _content);
        var sceneContent = _content.Load<SceneAssetContent>($"Levels/{sceneName}");

        // Load the level file and create entities based on the data
        // This is a placeholder for actual level loading logic
        // You would typically read the file, parse it, and create entities accordingly

        foreach (var entityData in sceneContent.Nodes)
        {
            switch (entityData.Type)
            {
                case SceneNodeType.Unknown:
                    continue;

                case SceneNodeType.Camera:
                    if (!entityData.HasPosition)
                        throw new InvalidOperationException($"Scene '{sceneName}' camera is missing a position.");

                    var cameraTarget = Vector3.Zero;
                    var cameraUp = Vector3.Up;

                    if (entityData.HasUp)
                        cameraUp = entityData.Up;

                    if (entityData.HasDirection)
                        cameraTarget = Vector3.Normalize(entityData.Direction) * 100f;

                    if (entityData.HasFieldOfView)
                        scene.Camera.FieldOfView = entityData.FieldOfView;

                    scene.Camera.Position = entityData.Position;
                    scene.Camera.SetTarget(cameraTarget);
                    scene.Camera.UpDirection = cameraUp;
                    continue;

                case SceneNodeType.SpawnPoint:
                    var spawnPointEntity = new SpawnPoint(null, _content);
                    spawnPointEntity.SetProperties(entityData);
                    scene.Entities.Add(spawnPointEntity);
                    continue;


                case SceneNodeType.Light:
                    if (!entityData.HasPosition)
                        throw new InvalidOperationException($"Scene '{sceneName}' light is missing a position.");

                    if (!entityData.HasIntensity)
                        throw new InvalidOperationException($"Scene '{sceneName}' light is missing an intensity.");

                    scene.LightPosition = entityData.Position;
                    scene.LightColor = SceneContentValueConverters.ToColor(entityData.ColorHex, Color.Purple);
                    scene.LightIntensity = entityData.Intensity;
                    continue;

                case SceneNodeType.Goal:
                    var goal = new Goal(null, _content);
                    goal.SetProperties(entityData);
                    scene.Entities.Add(goal);
                    continue;

                case SceneNodeType.Scene:
                    scene.SkyColor = SceneContentValueConverters.ToColor(entityData.BackgroundHex, GameConstants.DEFAULT_BACKGROUND_COLOR);
                    continue;

                case SceneNodeType.Mesh:
                    var instanceOf = entityData.InstanceOf;
                    if (string.IsNullOrWhiteSpace(instanceOf))
                        throw new InvalidOperationException($"Scene '{sceneName}' mesh '{entityData.Name}' is missing an 'instanceof' value.");

                    if (!_assetMap.TryGetValue(instanceOf, out var entityFactory))
                    {
                        if (!_assetMap.TryGetValue("default", out entityFactory))
                            throw new Exception($"No entity factory found for {instanceOf}");
                    }

                    var model = _content.Load<Model>($"Models/{instanceOf}");
                    var entity = entityFactory(model, _content);
                    entity.SetProperties(entityData);
                    scene.Entities.Add(entity);
                    continue;

                default:
                    continue;
            }
        }

        var spawnPoint = scene.Entities.Find(e => e is SpawnPoint);
        var _goal = scene.Entities.Find(e => e is Goal) as Goal;
        if (_goal != null)
            _goal.Complete = false;

        scene.Player.Position = spawnPoint?.Position ?? Vector3.Zero;
        scene.Player.Rotation = spawnPoint?.Rotation ?? Quaternion.Identity;
        scene.Player.PlayAnimation("idle");
        return scene;
    }

    /// <summary>
    /// Gets a list of all available scene names.
    /// </summary>
    /// <returns> A list of scene names.</returns>
    public string[] GetSceneList()
    {
        return _content.Load<SceneListContent>("Levels/levels").SceneNames.ToArray();
    }
}
