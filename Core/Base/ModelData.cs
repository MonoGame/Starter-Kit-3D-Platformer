// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

/// <summary>
/// Holds additional data for a 3D model, including animation data and collision data.
/// ContentSerializerRuntimeType attribute is needed to ensure the
/// XNB deserializer can properly instantiate this type at runtime. 
/// Because we use Shared code, we need to know the final assembly name at built time.
/// This is done using the GameConstants.AssemblyName constant.
/// </summary>
[ContentSerializerRuntimeType($"{nameof(ModelData)}, {GameConstants.AssemblyName}")]
public class ModelData
{
    [ContentSerializer]
    public AnimationData AnimationData;

    [ContentSerializer]
    public List<ConvexHull> CollisionData;

    [ContentSerializer]
    public BoundingBox BoundingBox;
}

