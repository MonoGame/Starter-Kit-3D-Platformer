// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class AnimatedEntity : Entity
{
    TimeSpan currentTimeValue;
    AnimationClip currentClip;
    int currentKeyframe = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedEntity"/> class.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    public AnimatedEntity(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        if (model.Tag is AnimationData)
            AnimationData = model.Tag as AnimationData;
    }

    /// <summary>
    /// Gets or sets the animation data associated with this entity.
    /// </summary>
    public AnimationData AnimationData { get; set; }

    /// <summary>
    /// Gets or sets the current animation clip being played by this entity.
    /// This property is used to determine which animation clip is currently active.
    /// It is typically set when an animation starts and can be used to check the state of
    /// the entity's animation system.
    /// </summary>
    public AnimationClip CurrentClip {
        get => currentClip;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            currentClip = value;
            currentTimeValue = TimeSpan.Zero;
            currentKeyframe = 0;

            // Reset mesh transforms to identity.
            for (int i = 0; i < MeshTransforms.Length; i++)
            {
                MeshTransforms[i] = Matrix.Identity;
            }
        }
    }

    public void PlayAnimation(string clipName, bool reset = false)
    {
        if (AnimationData != null && AnimationData.Animations.TryGetValue(clipName, out var clip))
        {
            if (CurrentClip == clip && !reset)
            {
                // If the same clip is already playing, we might want to reset it or do nothing.
                return;
            }
            CurrentClip = clip;
        }
        else
        {
            throw new ArgumentException($"Animation clip '{clipName}' not found in AnimationData.");
        }
    }

    /// <summary>
    /// Helper used by the Update method to refresh the BoneTransforms data.
    /// </summary>
    public void UpdateMeshTransforms(TimeSpan time, bool relativeToCurrentTime)
    {
        if (CurrentClip == null)
            throw new InvalidOperationException(
                        "AnimationPlayer.Update was called before StartClip");

        // Update the animation position.
        if (relativeToCurrentTime)
        {
            time += currentTimeValue;

            // If we reached the end, loop back to the start.
            while (time >= CurrentClip.Duration)
                time -= CurrentClip.Duration;
        }

        if ((time < TimeSpan.Zero) || (time >= CurrentClip.Duration))
            throw new ArgumentOutOfRangeException("time");

        // If the position moved backwards, reset the keyframe index.
        if (time < currentTimeValue)
        {
            currentKeyframe = 0;
        }

        currentTimeValue = time;

        // Read keyframe matrices.
        var keyframes = CurrentClip.Keyframes;

        while (currentKeyframe < keyframes.Count)
        {
            Keyframe keyframe = keyframes[currentKeyframe];

            // Stop when we've read up to the current time position.
            if (keyframe.Time > currentTimeValue)
                break;

            var parent = Model.Bones[keyframe.Index].Parent;
            Matrix transform = Matrix.Identity;
            if (parent != null)
            {
                // If the parent has no transform, we need to go up the hierarchy.
                transform *= parent != null ? Model.Bones[parent.Index].Transform : Matrix.Identity;
            }
            MeshTransforms[keyframe.Index] = keyframe.Transform * transform;
            
            currentKeyframe++;
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (CurrentClip != null)
        {
            // Update the animation state based on the current clip and game time.
            // This is where you would typically update the entity's bone transforms
            // based on the keyframes in the CurrentClip.
            UpdateMeshTransforms(gameTime.ElapsedGameTime, true);
        }
        base.Update(gameTime);
    }
}