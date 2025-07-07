// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Text.Json;

public class JumpPad : Entity
{
    private float _jumpForce;

    public JumpPad(Model model, ContentManager content) 
        : base(model, content)
    {
        IsBlockingMovement = false;
    }

    public override void SetProperties(JsonElement data)
    {
        base.SetProperties(data);
        _jumpForce = data.GetProperty("jumpforce").GetSingle();
    }

    public override bool CheckCollision(Entity other)
    {
        var collision = base.CheckCollision(other);

        if (collision && other is Player player)
        {            
            var force = new Vector3(0, _jumpForce * GameConstants.PLAYER_JUMP_FORCE, 0);
            var matrix = Matrix.CreateFromQuaternion(Rotation);
            force = Vector3.TransformNormal(force, matrix);
            player.AddForce(force);
        }

        return collision;
    }
}