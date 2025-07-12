// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Text.Json;

public class JumpPad : AnimatedEntity
{
    private float _jumpForce;
    private float _ignoreTimer;

    private SoundEffect _sound;

    public JumpPad(Model model, ContentManager content) 
        : base(model, content)
    {
        IsBlockingMovement = false;

        _sound = content.Load<SoundEffect>("Sounds/pad");
    }

    public override void SetProperties(JsonElement data)
    {
        base.SetProperties(data);
        _jumpForce = data.GetProperty("jumpforce").GetSingle();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _ignoreTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_ignoreTimer < 0)
            _ignoreTimer = 0.0f;
    }

    public override bool CheckCollision(Entity other)
    {
        var collision = base.CheckCollision(other);

        if (_ignoreTimer == 0.0f && collision && other is Player player)
        {
            var force = new Vector3(0, _jumpForce * GameConstants.PLAYER_JUMP_FORCE, 0);
            var matrix = Matrix.CreateFromQuaternion(Rotation);
            force = Vector3.TransformNormal(force, matrix);
            player.AddForce(force);
            PlayAnimation("Jump", loop: false);
            player.PlayAnimation("jump");
            player.IsJumping = true;

            // A little randomization of the pitch makes it feel more fun.
            var pitch = (float)Random.Shared.NextDouble();
            pitch = ((pitch * 2) - 1.0f) * 0.1f;
            _sound.Play(0.5f, pitch, 0);

            _ignoreTimer = 0.5f;
        }

        return collision;
    }
}