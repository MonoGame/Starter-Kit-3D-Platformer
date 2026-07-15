// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// A jump pad entity that launches the player into the air when they collide with it.
/// The jump pad does not block movement so that the player can pass through it.
/// The jump force can be adjusted via the "jumpforce" property in the JSON data.
/// This force can be modified in the level in Blender via a custom property.
/// </summary>
public class JumpPad : AnimatedEntity
{
    private float _jumpForce;

    private SoundEffect _sound;

    public JumpPad(Model model, ContentManager content)
        : base(model, content)
    {
        IsBlockingMovement = false;

        _sound = content.Load<SoundEffect>("Sounds/pad");
    }

    public override void SetProperties(SceneNodeContent data)
    {
        base.SetProperties(data);

        if (!data.HasJumpForce)
            throw new InvalidOperationException($"Scene node '{data.Name}' is missing 'jumpforce'.");

        _jumpForce = data.JumpForce;
    }

    public override bool CheckCollision(Entity other)
    {
        var collision = base.CheckCollision(other);

        // If we were hit by a player and the player is
        // grounded we can apply the "jump" force to them.
        if (collision && other is Player player && player.IsGrounded)
        {
            var force = new Vector3(0, _jumpForce * GameConstants.PLAYER_JUMP_FORCE, 0);
            var matrix = Matrix.CreateFromQuaternion(Rotation);
            force = Vector3.TransformNormal(force, matrix);
            player.AddForce(force);
            PlayAnimation("Jump", loop: false);

            // A little randomization of the pitch makes it feel more fun.
            var pitch = (float)Random.Shared.NextDouble();
            pitch = ((pitch * 2) - 1.0f) * 0.1f;
            _sound.Play(0.5f, pitch, 0);
        }

        return collision;
    }
}
