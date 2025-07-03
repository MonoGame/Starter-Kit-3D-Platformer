// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class AnimatedEntity : Entity
{
    TimeSpan currentTimeValue;
    AnimationClip currentClip;

    // Animation transition fields
    AnimationClip previousClip;
    TimeSpan previousTimeValue;
    TimeSpan transitionDuration = TimeSpan.FromMilliseconds(200); // Default 200ms transition
    TimeSpan transitionTimeRemaining = TimeSpan.Zero;
    Matrix[] previousTransforms; // Store transforms from the previous animation

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

            // Store previous animation state for blending if we have a current clip
            if (currentClip != null && transitionDuration > TimeSpan.Zero)
            {
                previousClip = currentClip;
                previousTimeValue = currentTimeValue;
                transitionTimeRemaining = transitionDuration;
                
                // Copy current transforms as the starting point for blending
                Array.Copy(MeshTransforms, previousTransforms, MeshTransforms.Length);
            }
            else
            {
                // No transition - direct switch
                previousClip = null;
                transitionTimeRemaining = TimeSpan.Zero;
            }

            currentClip = value;
            currentTimeValue = TimeSpan.Zero;

            // Don't reset transforms here if we're transitioning - let the blend handle it
            if (!IsTransitioning)
            {
                // Reset mesh transforms to identity.
                for (int i = 0; i < MeshTransforms.Length; i++)
                {
                    MeshTransforms[i] = Matrix.Identity;
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the duration for smooth transitions between animation clips.
    /// Default is 200 milliseconds. Set to TimeSpan.Zero to disable transitions.
    /// </summary>
    public TimeSpan AnimationTransitionDuration 
    { 
        get => transitionDuration; 
        set => transitionDuration = value; 
    }

    /// <summary>
    /// Gets whether the entity is currently transitioning between animations.
    /// </summary>
    public bool IsTransitioning => transitionTimeRemaining > TimeSpan.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimatedEntity"/> class.
    /// </summary>
    /// <param name="name">The name of the entity.</param>
    public AnimatedEntity(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        if (model.Tag is ModelData data)
            AnimationData = data.AnimationData;

        previousTransforms = new Matrix[model.Bones.Count];
        for (int i = 0; i < model.Bones.Count; i++)
        {
            previousTransforms[i] = Matrix.Identity; // Initialize with identity
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
    /// Helper used by the Update method to refresh the BoneTransforms data with optional smooth interpolation.
    /// </summary>
    public void UpdateMeshTransforms(TimeSpan time, bool relativeToCurrentTime)
    {
        UpdateMeshTransformsInterpolated(time, relativeToCurrentTime);
    }

    /// <summary>
    /// Helper used by the Update method to refresh the BoneTransforms data with smooth interpolation.
    /// </summary>
    private void UpdateMeshTransformsInterpolated(TimeSpan time, bool relativeToCurrentTime)
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

        currentTimeValue = time;

        // Initialize transforms with identity matrices
        for (int i = 0; i < MeshTransforms.Length; i++)
        {
            MeshTransforms[i] = Matrix.Identity;
        }

        var keyframes = CurrentClip.Keyframes;
        
        // Process each bone that has keyframes
        // We'll use a HashSet to track which bones we've processed to avoid duplicates
        var processedBones = new HashSet<int>();
        
        for (int i = 0; i < keyframes.Count; i++)
        {
            int boneIndex = keyframes[i].Index;
            
            // Skip if we've already processed this bone
            if (processedBones.Contains(boneIndex))
                continue;
                
            processedBones.Add(boneIndex);

            // Find the interpolated transform for this bone
            Matrix interpolatedTransform = FindInterpolatedBoneTransform(keyframes, boneIndex, currentTimeValue);
            
            var parent = Model.Bones[boneIndex].Parent;
            Matrix parentTransform = Matrix.Identity;
            if (parent != null)
            {
                parentTransform = Model.Bones[parent.Index].Transform;
            }
            
            MeshTransforms[boneIndex] = interpolatedTransform * parentTransform;
        }

        // Blend with previous transforms if transitioning
        if (transitionTimeRemaining > TimeSpan.Zero && previousTransforms != null)
        {
            float blendFactor = (float)(transitionDuration.TotalMilliseconds - transitionTimeRemaining.TotalMilliseconds) / (float)transitionDuration.TotalMilliseconds;
            blendFactor = MathHelper.Clamp(blendFactor, 0f, 1f);

            for (int i = 0; i < MeshTransforms.Length; i++)
            {
                // Proper component-wise blending instead of Matrix.Lerp
                Vector3 currentTranslation, currentScale, prevTranslation, prevScale;
                Quaternion currentRotation, prevRotation;

                MeshTransforms[i].Decompose(out currentScale, out currentRotation, out currentTranslation);
                previousTransforms[i].Decompose(out prevScale, out prevRotation, out prevTranslation);

                // Interpolate components separately for better results
                Vector3 blendedTranslation = Vector3.Lerp(prevTranslation, currentTranslation, blendFactor);
                Vector3 blendedScale = Vector3.Lerp(prevScale, currentScale, blendFactor);
                Quaternion blendedRotation = Quaternion.Slerp(prevRotation, currentRotation, blendFactor);

                // Recompose the matrix
                MeshTransforms[i] = Matrix.CreateScale(blendedScale) * 
                    Matrix.CreateFromQuaternion(blendedRotation) * 
                    Matrix.CreateTranslation(blendedTranslation);
            }

            transitionTimeRemaining -= time;
        }
    }

    /// <summary>
    /// Finds and interpolates between keyframes for a single bone efficiently without creating temporary collections.
    /// </summary>
    private Matrix FindInterpolatedBoneTransform(List<Keyframe> allKeyframes, int boneIndex, TimeSpan currentTime)
    {
        Keyframe previousKeyframe = null;
        Keyframe nextKeyframe = null;
        
        // Optimized search assuming keyframes are sorted by time (which they typically are)
        // We can break early once we find both bracketing keyframes for this bone
        bool foundPrevious = false;
        bool foundNext = false;
        
        for (int i = 0; i < allKeyframes.Count && (!foundPrevious || !foundNext); i++)
        {
            var keyframe = allKeyframes[i];
            
            // Skip keyframes that don't belong to this bone
            if (keyframe.Index != boneIndex)
                continue;

            if (keyframe.Time <= currentTime)
            {
                previousKeyframe = keyframe; // Keep updating as we find later ones
                foundPrevious = true;
            }
            else if (!foundNext)
            {
                nextKeyframe = keyframe; // Take the first one after current time
                foundNext = true;
            }
        }

        // Handle edge cases
        if (!foundPrevious && !foundNext)
            return Matrix.Identity;
        
        if (!foundPrevious)
            return Matrix.CreateScale(nextKeyframe.Scale) * 
               Matrix.CreateFromQuaternion(nextKeyframe.Orientation) * 
               Matrix.CreateTranslation(nextKeyframe.Translation);

        if (!foundNext)
            return Matrix.CreateScale(previousKeyframe.Scale) * 
               Matrix.CreateFromQuaternion(previousKeyframe.Orientation) * 
               Matrix.CreateTranslation(previousKeyframe.Translation);

        if (previousKeyframe.Time == nextKeyframe.Time)
            return Matrix.CreateScale(previousKeyframe.Scale) * 
               Matrix.CreateFromQuaternion(previousKeyframe.Orientation) * 
               Matrix.CreateTranslation(previousKeyframe.Translation);

        // Calculate interpolation factor
        double totalTime = (nextKeyframe.Time - previousKeyframe.Time).TotalMilliseconds;
        double elapsedTime = (currentTime - previousKeyframe.Time).TotalMilliseconds;
        float t = totalTime > 0 ? (float)(elapsedTime / totalTime) : 0f;
        t = MathHelper.Clamp(t, 0f, 1f);

        // Interpolate components
        Vector3 interpolatedTranslation = Vector3.Lerp(previousKeyframe.Translation, nextKeyframe.Translation, t);
        Vector3 interpolatedScale = Vector3.Lerp(previousKeyframe.Scale, nextKeyframe.Scale, t);
        Quaternion interpolatedRotation = Quaternion.Slerp(previousKeyframe.Orientation, nextKeyframe.Orientation, t);

        // Recompose the matrix
        return Matrix.CreateScale(interpolatedScale) * 
               Matrix.CreateFromQuaternion(interpolatedRotation) * 
               Matrix.CreateTranslation(interpolatedTranslation);
    }

    public override void Update(GameTime gameTime)
    {
        if (CurrentClip != null)
        {
            // Update the animation state based on the current clip and game time.
            UpdateMeshTransforms(gameTime.ElapsedGameTime, true);
        }
        base.Update(gameTime);
    }
}