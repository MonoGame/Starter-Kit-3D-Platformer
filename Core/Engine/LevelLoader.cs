using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class LevelLoader
{
    Dictionary<string, Func<Model, ContentManager, Entity>> _assetMap = new Dictionary<string, Func<Model, ContentManager, Entity>>();
    private readonly ContentManager _content;

    public LevelLoader(ContentManager content)
    {
        _content = content;
        _assetMap = new Dictionary<string, Func<Model, ContentManager, Entity>>
        {
            ["platform-falling"] = (model, content) => new FallingPlatform(model, content),
            ["coin"] = (model, content) => new Coin(model, content),
            ["platform"] = (model, content) => new Platform(model, content),
            ["platform-medium"] = (model, content) => new Platform(model, content),
            ["platform-large"] = (model, content) => new Platform(model, content),
            ["platform-grass-large-round"] = (model, content) => new Platform(model, content),
            ["cloud"] = (model, content) => new Cloud(model, content),
            ["default"] = (model, content) => new Entity(model, content)
        };
    }

    Vector3 ReadVector3FromJson(JsonElement positionElement)
    {
        if (positionElement.ValueKind != JsonValueKind.Array)
            return Vector3.Zero;

        var array = positionElement.EnumerateArray();
        float x = 0, y = 0, z = 0;
        int index = 0;
        
        foreach (var element in array)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                switch (index)
                {
                    case 0: x = element.GetSingle(); break;
                    case 1: y = element.GetSingle(); break;
                    case 2: z = element.GetSingle(); break;
                }
            }
            index++;
        }
        
        return new Vector3(x, y, z);
    }

    Quaternion ReadRotationFromJson(JsonElement rotationElement)
    {
        if (rotationElement.ValueKind != JsonValueKind.Array)
            return Quaternion.Identity;

        var array = rotationElement.EnumerateArray();
        float x = 0, y = 0, z = 0;
        int index = 0;
        
        foreach (var element in array)
        {
            if (element.ValueKind == JsonValueKind.Number)
            {
                switch (index)
                {
                    case 0: x = element.GetSingle(); break;
                    case 1: y = element.GetSingle(); break;
                    case 2: z = element.GetSingle(); break;
                }
            }
            index++;
        }
        return Quaternion.CreateFromYawPitchRoll(z, x, y);
    }

    public void LoadLevel(string levelName, List<Entity> entities, ref Vector3 lightPosition)
    {
        using var stream = TitleContainer.OpenStream("Content/" + levelName + ".json");
        // Load the level file and create entities based on the data
        // This is a placeholder for actual level loading logic
        // You would typically read the file, parse it, and create entities accordingly
        JsonDocument levelData = JsonDocument.Parse(stream);
        foreach (var entityData in levelData.RootElement.EnumerateArray())
        {
            entityData.TryGetProperty("type", out var type);
            if (type.ValueKind != JsonValueKind.String)
            {
                continue;
            }
            if (type.GetString().Equals("SPAWNPOINT"))
            {
                var spawnPoint = new SpawnPoint(null, _content);
                spawnPoint.Position = ReadVector3FromJson (entityData.GetProperty("position"));
                spawnPoint.Rotation = ReadRotationFromJson (entityData.GetProperty("rotation"));
                entities.Add(spawnPoint);
                continue;
            }
            if (type.GetString().Equals("LIGHT"))
            {
                lightPosition = ReadVector3FromJson (entityData.GetProperty("position"));
                continue;
            }
            if (type.GetString().Equals("GOAL"))
            {
                var goal = new Goal(null, _content);
                goal.Position = ReadVector3FromJson (entityData.GetProperty("position"));
                goal.Radius = entityData.GetProperty("radius").GetSingle();
                entities.Add(goal);
                continue;
            }
            if (type.GetString().Equals("MESH"))
            {
                string instanceof = entityData.GetProperty("instanceof").GetString();
                if (!_assetMap.TryGetValue(instanceof, out var entityFactory))
                {
                    if (!_assetMap.TryGetValue("default", out entityFactory))
                    {
                        throw new Exception($"No entity factory found for {instanceof}");
                    }
                }
                Entity entity = entityFactory(_content.Load<Model>($"Models/{instanceof}"), _content);
                Vector3 position = ReadVector3FromJson (entityData.GetProperty("position"));
                Quaternion rotation = ReadRotationFromJson (entityData.GetProperty("rotation"));
                Vector3 scale = ReadVector3FromJson (entityData.GetProperty("scale"));
                bool collidable = entityData.GetProperty("collidable").GetBoolean();
                
                // Set the position, rotation, and scale
                entity.Position = position;
                entity.Rotation = rotation;
                entity.Scale = scale;
                entity.IsBlockingMovement = collidable;

                // Add the entity to the game world
                entities.Add(entity);
            }
        }
    }
}