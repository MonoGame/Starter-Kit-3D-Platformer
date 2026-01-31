// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

/// <summary>
/// An entity that supports mesh animation.
/// Note this does not support skeletal animation.
/// It simply updates the Model's MeshTransforms based on keyframes defined in an AnimationClip.
/// </summary>
public class AnimatedEntity : Entity
{
    TimeSpan currentTimeValue;
    AnimationClip currentClip;

    bool isLooping = true;

    Pose[] keyFrameTransforms;

    // Animation blending properties
    private TimeSpan animationTransitionDuration = TimeSpan.FromMilliseconds(200);
    private TimeSpan transitionElapsedTime;
    private bool isTransitioning = false;
    private Pose[] previousFrameTransforms;
    private AnimationClip previousClip;
    private TimeSpan previousTimeValue;

    /// <summary>
    /// Gets or sets the duration for transitioning between animations.
    /// </summary>
    public TimeSpan AnimationTransitionDuration
    {
        get => animationTransitionDuration;
        set => animationTransitionDuration = value;
    }

    /// <summary>
    /// Gets whether the entity is currently transitioning between animations.
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedEntity"/> class.
    /// </summary>
    /// <param name="model">The name of the entity.</param>
    /// <param name="contentManager">The content manager managing this content.</param>
    public AnimatedEntity(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        if (model.Tag is ModelData data)
            AnimationData = data.AnimationData;

        keyFrameTransforms = new Pose[model.Bones.Count];
        previousFrameTransforms = new Pose[model.Bones.Count];

        for (int i = 0; i < keyFrameTransforms.Length; i++)
        {
            keyFrameTransforms[i] = Pose.Identity;
            previousFrameTransforms[i] = Pose.Identity;
        }
    }

    /// <summary>
    /// Gets or sets the animation data associated with this entity.
    /// </summary>
    public AnimationData AnimationData { get; set; }

    /// <summary>
    /// Gets or sets the current animation clip being played by this entity.
    /// </summary>
    public AnimationClip CurrentClip
    {
        get => currentClip;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // Store previous animation state for blending
            if (currentClip != null && currentClip != value && animationTransitionDuration > TimeSpan.Zero)
            {
                previousClip = currentClip;
                previousTimeValue = currentTimeValue;
                Array.Copy(keyFrameTransforms, previousFrameTransforms, keyFrameTransforms.Length);

                isTransitioning = true;
                transitionElapsedTime = TimeSpan.Zero;
            }

            currentClip = value;
            currentTimeValue = TimeSpan.Zero;

            // Reset mesh transforms to identity.
            for (int i = 0; i < MeshTransforms.Length; i++)
            {
                MeshTransforms[i] = Matrix.Identity;
            }
        }
    }

    /// <summary>
    /// Plays the specified animation clip.
    /// </summary>
    /// <param name="clipName">The name of the animation clip to play.</param>
    /// <param name="reset">Whether to reset the animation.</param>
    /// <param name="loop">Whether the animation should loop.</param>
    /// <exception cref="ArgumentException">Thrown when the animation clip is not found.</exception>
    public void PlayAnimation(string clipName, bool reset = false, bool loop = true)
    {
        if (AnimationData != null && AnimationData.Animations.TryGetValue(clipName, out var clip))
        {
            if (CurrentClip == clip && !reset)
            {
                // If the same clip is already playing, we might want to reset it or do nothing.
                return;
            }
            CurrentClip = clip;
            isLooping = loop;
        }
        else
        {
            throw new ArgumentException($"Animation clip '{clipName}' not found in AnimationData.");
        }
    }

    /// <summary>
    /// Gets the pose for a bone at a specific time, with interpolation between keyframes.
    /// </summary>
    private Pose GetInterpolatedPose(int boneIndex, TimeSpan time, AnimationClip clip)
    {
        var keyframes = clip.Keyframes;

        // Find the keyframes that bracket the current time
        Keyframe previousKeyframe = null;
        Keyframe nextKeyframe = null;

        for (int i = 0; i < keyframes.Count; i++)
        {
            var keyframe = keyframes[i];
            if (keyframe.Index != boneIndex) continue;

            if (keyframe.Time <= time)
            {
                previousKeyframe = keyframe;
            }
            else if (nextKeyframe == null)
            {
                nextKeyframe = keyframe;
                break;
            }
        }

        // If we only have one keyframe or no keyframes, return identity or the single keyframe
        if (previousKeyframe == null && nextKeyframe == null)
        {
            return Pose.Identity;
        }

        if (previousKeyframe != null && nextKeyframe == null)
        {
            // Only previous keyframe exists
            return new Pose
            {
                Translation = previousKeyframe.Translation,
                Rotation = previousKeyframe.Orientation,
                Scale = previousKeyframe.Scale
            };
        }

        if (previousKeyframe == null && nextKeyframe != null)
        {
            // Only next keyframe exists
            return new Pose
            {
                Translation = nextKeyframe.Translation,
                Rotation = nextKeyframe.Orientation,
                Scale = nextKeyframe.Scale
            };
        }

        // Both keyframes exist - interpolate between them
        var timeDifference = nextKeyframe.Time - previousKeyframe.Time;
        var timeProgress = time - previousKeyframe.Time;

        float blendFactor = timeDifference.TotalMilliseconds > 0
            ? (float)(timeProgress.TotalMilliseconds / timeDifference.TotalMilliseconds)
            : 0f;

        blendFactor = MathHelper.Clamp(blendFactor, 0f, 1f);

        var prevPose = new Pose
        {
            Translation = previousKeyframe.Translation,
            Rotation = previousKeyframe.Orientation,
            Scale = previousKeyframe.Scale
        };

        var nextPose = new Pose
        {
            Translation = nextKeyframe.Translation,
            Rotation = nextKeyframe.Orientation,
            Scale = nextKeyframe.Scale
        };

        return Pose.Slerp(prevPose, nextPose, blendFactor);
    }

    /// <summary>
    /// Helper used by the Update method to refresh the BoneTransforms data.
    /// </summary>
    /// <param name="time">The current time of the animation.</param>
    /// <param name="relativeToCurrentTime">Whether the time is relative to the current time.</param>
    /// <exception cref="InvalidOperationException">Thrown when the animation player is not started.</exception>
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
            {
                if (isLooping)
                {
                    time -= CurrentClip.Duration;
                }
                else
                {
                    currentClip = null;
                    return;
                }
            }
        }

        if ((time < TimeSpan.Zero) || (time >= CurrentClip.Duration))
            throw new ArgumentOutOfRangeException("time");

        currentTimeValue = time;

        // Get interpolated poses for current animation
        for (int i = 0; i < keyFrameTransforms.Length; i++)
        {
            keyFrameTransforms[i] = GetInterpolatedPose(i, currentTimeValue, CurrentClip);
        }

        // Handle inter-clip blending if transitioning
        if (isTransitioning && previousClip != null)
        {
            // Update transition time
            transitionElapsedTime += time - (currentTimeValue - time);

            if (transitionElapsedTime >= animationTransitionDuration)
            {
                // Transition complete
                isTransitioning = false;
                previousClip = null;
            }
            else
            {
                // Calculate blend factor (0 = fully previous, 1 = fully current)
                float blendFactor = (float)(transitionElapsedTime.TotalMilliseconds / animationTransitionDuration.TotalMilliseconds);
                blendFactor = MathHelper.Clamp(blendFactor, 0f, 1f);

                // Get poses from previous animation
                var previousPoses = new Pose[keyFrameTransforms.Length];
                for (int i = 0; i < previousPoses.Length; i++)
                {
                    previousPoses[i] = GetInterpolatedPose(i, previousTimeValue, previousClip);
                }

                // Blend between previous and current poses
                for (int i = 0; i < keyFrameTransforms.Length; i++)
                {
                    keyFrameTransforms[i] = Pose.Slerp(previousPoses[i], keyFrameTransforms[i], blendFactor);
                }
            }
        }

        // Apply bone hierarchy and convert to matrices
        for (int i = 0; i < Model.Bones.Count; i++)
        {
            Matrix transform = Matrix.Identity;
            if (keyFrameTransforms[i] != Pose.Identity)
            {
                var parent = Model.Bones[i].Parent;
                while (parent != null)
                {
                    if (parent.Meshes.Count == 0)
                    {
                        // If the parent has no meshes, we need to apply its
                        // keyframe animation transform otherwise it will be missed
                        // during rendering.
                        keyFrameTransforms[i] *= keyFrameTransforms[parent.Index];
                    }
                    transform *= Model.Bones[parent.Index].Transform;
                    parent = parent.Parent;
                }
                MeshTransforms[i] = keyFrameTransforms[i].ToMatrix() * transform;
            }
        }
    }

    /// <summary>
    /// Updates the animated entity.
    /// </summary>
    /// <param name="gameTime">The game time.</param>
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
