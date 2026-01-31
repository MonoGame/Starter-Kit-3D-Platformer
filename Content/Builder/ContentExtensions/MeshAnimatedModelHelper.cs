// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

internal static class MeshAnimatedModelHelper
{
    public static Dictionary<string, AnimationClip> ProcessNodeAnimations(NodeContent node, ModelBoneContentCollection bones)
    {
        Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        foreach (var child in node.Children)
        {
            foreach (var c in ProcessNodeAnimations(child, bones))
            {
                if (!clips.ContainsKey(c.Key))
                {
                    clips.Add(c.Key, c.Value);
                    continue;
                }
                else
                {
                    AnimationClip clip = clips[c.Key];
                    foreach (var kf in c.Value.Keyframes)
                    {
                        // check if the keyframe already exists
                        if (clip.Keyframes.Any(k => k.Index == kf.Index && kf.Scale == k.Scale && kf.Orientation == kf.Orientation && kf.Translation == kf.Translation && k.Time == kf.Time))
                            continue;
                        clip.Keyframes.Add(kf);
                    }
                    clip.Keyframes.Sort(CompareKeyframeTimes);
                    clips[c.Key] = clip;
                }
            }
        }
        foreach (var c in ProcessAnimations(node, node.Animations, bones))
        {
            if (!clips.ContainsKey(c.Key))
            {
                clips.Add(c.Key, c.Value);
                continue;
            }
            else
            {
                AnimationClip clip = clips[c.Key];
                foreach (var kf in c.Value.Keyframes)
                {
                    // check if the keyframe already exists
                    if (clip.Keyframes.Any(k => k.Index == kf.Index && kf.Scale == k.Scale && kf.Orientation == kf.Orientation && kf.Translation == kf.Translation && k.Time == kf.Time))
                        continue;
                    clip.Keyframes.Add(kf);
                }
                clip.Keyframes.Sort(CompareKeyframeTimes);
                clips[c.Key] = clip;
            }
        }
        return clips;
    }

    // <summary>
    /// Converts an intermediate format content pipeline AnimationContentDictionary
    /// object to our runtime AnimationClip format.
    /// </summary>
    static Dictionary<string, AnimationClip> ProcessAnimations(NodeContent node,
        AnimationContentDictionary animations, ModelBoneContentCollection bones)
    {
        // Build up a table mapping bone names to indices.
        Dictionary<string, int> boneMap = new Dictionary<string, int>();

        for (int i = 0; i < bones.Count; i++)
        {
            string boneName = bones[i].Name;

            if (!string.IsNullOrEmpty(boneName))
                boneMap.Add(boneName, i);
        }

        // Convert each animation in turn.
        Dictionary<string, AnimationClip> animationClips;
        animationClips = new Dictionary<string, AnimationClip>();

        foreach (KeyValuePair<string, AnimationContent> animation in animations)
        {
            AnimationClip processed = ProcessAnimation(node, animation.Value, boneMap);

            if (animationClips.ContainsKey(animation.Key))
            {
                throw new InvalidContentException(string.Format(
                    "Found multiple animations with the same name '{0}'.",
                    animation.Key));
            }
            animationClips.Add(animation.Key, processed);
        }

        return animationClips;
    }

    /// <summary>
    /// Converts an intermediate format content pipeline AnimationContent
    /// object to our runtime AnimationClip format.
    /// </summary>
    static AnimationClip ProcessAnimation(NodeContent node, AnimationContent animation,
                                            Dictionary<string, int> boneMap)
    {
        List<Keyframe> keyframes = new List<Keyframe>();

        // For each input animation channel.
        foreach (KeyValuePair<string, AnimationChannel> channel in
            animation.Channels)
        {
            // Look up what bone this channel is controlling.
            if (!boneMap.TryGetValue(channel.Key, out int index))
            {
                continue;
            }
            var transform = FindAbsoluteTransform(node, channel.Key);

            // Convert the keyframe data.
            foreach (AnimationKeyframe keyframe in channel.Value)
            {
                keyframes.Add(new Keyframe(index, keyframe.Time, keyframe.Transform)
                {
                    ChannelName = channel.Key
                });
            }
        }

        // Sort the merged keyframes by time.
        keyframes.Sort(CompareKeyframeTimes);

        if (animation.Duration <= TimeSpan.Zero)
            throw new InvalidContentException("Animation has a zero duration.");

        return new AnimationClip(animation.Duration, keyframes);
    }

    /// <summary>
    /// Comparison function for sorting keyframes into ascending time order.
    /// </summary>
    static int CompareKeyframeTimes(Keyframe a, Keyframe b)
    {
        return a.Time.CompareTo(b.Time);
    }

    /// <summary>
    /// </summary>
    /// <param name="node"></param>
    public static void FlattenAnimationKeyframes(NodeContent node)
    {
        foreach (var child in node.Children)
        {
            FlattenAnimationKeyframes(child);
        }
        if (node.Parent == null)
            return;
        foreach (var animation in node.Animations)
        {
            foreach (var channel in animation.Value.Channels)
            {
                foreach (var keyframe in channel.Value)
                {
                    if (keyframe.Transform == Matrix.Identity)
                    {
                        // leave it alone
                        continue;
                    }
                    var transform = FindAbsoluteTransform(node, channel.Key);
                    keyframe.Transform = Matrix.Invert(transform) * keyframe.Transform;
                }
            }
        }
    }

    static Matrix FindAbsoluteTransform(NodeContent node, string name)
    {
        if (node.Name == name)
            return node.AbsoluteTransform;
        foreach (NodeContent child in node.Children)
        {
            Matrix transform = FindAbsoluteTransform(child, name);
            if (transform != Matrix.Identity)
                return transform;
        }
        return Matrix.Identity;
    }
}
