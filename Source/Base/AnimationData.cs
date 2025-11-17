// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

/// <summary>
/// Describes the position of a single bone at a single point in time.
/// ContentSerializerRuntimeType attribute is needed to ensure the
/// XNB deserializer can properly instantiate this type at runtime. 
/// Because we use Shared code, we need to know the final assembly name at built time.
/// This is done using the GameConstants.AssemblyName constant.
/// </summary>
[ContentSerializerRuntimeType($"{nameof(Keyframe)}, {GameConstants.AssemblyName}")]
public class Keyframe
{
    /// <summary>
    /// Constructs a new keyframe object.
    /// </summary>
    public Keyframe(int boneIndex, TimeSpan time, Matrix transform)
    {
        Index = boneIndex;
        Time = time;
        transform.Decompose(out Vector3 scale, out Quaternion orientation, out Vector3 translation);
        Scale = scale;
        Orientation = orientation;
        Translation = translation;
    }


    /// <summary>
    /// Private constructor for use by the XNB deserializer.
    /// </summary>
    private Keyframe()
    {
    }


    /// <summary>
    /// Gets the index of the target Bone that is animated by this keyframe.
    /// </summary>
    [ContentSerializer]
    public int Index { get; set; }

    /// <summary>
    /// Gets the time offset from the start of the animation to this keyframe.
    /// </summary>
    [ContentSerializer]
    public TimeSpan Time { get; private set; }

    /// <summary>
    /// Gets the bone scale for this keyframe.
    /// </summary>
    [ContentSerializer]
    public Vector3 Scale { get; set; }

    /// <summary>
    /// Gets the bone orientation/rotation for this keyframe.
    /// </summary>
    [ContentSerializer]
    public Quaternion Orientation { get; set; }

    /// <summary>
    /// Gets the bone transform for this keyframe.
    /// </summary>
    [ContentSerializer]
    public Vector3 Translation { get; set; }

    public string ChannelName { get; set; }

}

/// <summary>
/// An animation clip is the runtime equivalent of the
/// Microsoft.Xna.Framework.Content.Pipeline.Graphics.AnimationContent type.
/// It holds all the keyframes needed to describe a single animation.
/// ContentSerializerRuntimeType attribute is needed to ensure the
/// XNB deserializer can properly instantiate this type at runtime. 
/// Because we use Shared code, we need to know the final assembly name at built time.
/// This is done using the GameConstants.AssemblyName constant.
/// </summary>
[ContentSerializerRuntimeType($"{nameof(AnimationClip)}, {GameConstants.AssemblyName}")]
public class AnimationClip
{
    /// <summary>
    /// Constructs a new animation clip object.
    /// </summary>
    public AnimationClip(TimeSpan duration, List<Keyframe> keyframes)
    {
        Duration = duration;
        Keyframes = keyframes;
    }


    /// <summary>
    /// Private constructor for use by the XNB deserializer.
    /// </summary>
    private AnimationClip()
    {
    }


    /// <summary>
    /// Gets the total length of the animation.
    /// </summary>
    [ContentSerializer]
    public TimeSpan Duration { get; private set; }


    /// <summary>
    /// Gets a combined list containing all the keyframes for all bones,
    /// sorted by time.
    /// </summary>
    [ContentSerializer]
    public List<Keyframe> Keyframes { get; private set; }
}

/// <summary>
/// An animation data contains a set of named animation clips.
/// ContentSerializerRuntimeType attribute is needed to ensure the
/// XNB deserializer can properly instantiate this type at runtime. 
/// Because we use Shared code, we need to know the final assembly name at built time.
/// This is done using the GameConstants.AssemblyName constant.
/// </summary>
[ContentSerializerRuntimeType($"{nameof(AnimationData)}, {GameConstants.AssemblyName}")]
public class AnimationData
{

    [ContentSerializer]
    public Dictionary<string, AnimationClip> Animations { get; set; }

    public AnimationData(Dictionary<string, AnimationClip> animations)
    {
        Animations = animations ?? throw new ArgumentNullException(nameof(animations), "Animations dictionary cannot be null.");
    }

    private AnimationData()
    {
        Animations = new Dictionary<string, AnimationClip>();
    }

    public void AddAnimation(string name, AnimationClip animationClip)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Animation name cannot be null or empty.", nameof(name));
        if (animationClip == null)
            throw new ArgumentNullException(nameof(animationClip), "Animation clip cannot be null.");

        Animations[name] = animationClip;
    }
    public AnimationClip GetAnimation(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Animation name cannot be null or empty.", nameof(name));
        if (!Animations.TryGetValue(name, out var animationClip))
            throw new KeyNotFoundException($"Animation '{name}' not found.");

        return animationClip;
    }
}
