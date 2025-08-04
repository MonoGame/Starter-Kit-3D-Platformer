// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

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
        using var stream = TitleContainer.OpenStream("Content/" + sceneName + ".json");

        // Load the level file and create entities based on the data
        // This is a placeholder for actual level loading logic
        // You would typically read the file, parse it, and create entities accordingly
        var levelData = JsonDocument.Parse(stream);
        foreach (var entityData in levelData.RootElement.EnumerateArray())
        {
            entityData.TryGetProperty("type", out var type);
            if (type.ValueKind != JsonValueKind.String)
                continue;

            var typeName = type.GetString();

            if (typeName.Equals("CAMERA"))
            {
                var cameraPosition = entityData.GetProperty("position").ReadVector3FromJson();
                var cameraTarget = entityData.GetProperty("target").ReadVector3FromJson();
                var cameraUp = entityData.GetProperty("up").ReadVector3FromJson(Vector3.Up);
                scene.Camera.Position = cameraPosition;
                scene.Camera.Target = cameraTarget;
                scene.Camera.UpDirection = cameraUp;
                continue;
            }
            else if (typeName.Equals("SPAWNPOINT"))
            {
                var sp = new SpawnPoint(null, _content);
                sp.SetProperties(entityData);
                scene.Entities.Add(sp);
                continue;
            }
            else if (typeName.Equals("LIGHT"))
            {
                scene.LightPosition = entityData.GetProperty("position").ReadVector3FromJson();
                continue;
            }
            else if (typeName.Equals("GOAL"))
            {
                var goal = new Goal(null, _content);
                goal.SetProperties(entityData);
                scene.Entities.Add(goal);
                continue;
            }
            else if (typeName.Equals("MESH"))
            {
                var instanceOf = entityData.GetProperty("instanceof").GetString();
                if (!_assetMap.TryGetValue(instanceOf, out var entityFactory))
                {
                    if (!_assetMap.TryGetValue("default", out entityFactory))
                        throw new Exception($"No entity factory found for {instanceOf}");
                }

                var entity = entityFactory(_content.Load<Model>($"Models/{instanceOf}"), _content);
                entity.SetProperties(entityData);
                scene.Entities.Add(entity);
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
}
