// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;


public class ModelData
{
    [ContentSerializer]
    public AnimationData AnimationData;

    [ContentSerializer]
    public List<ConvexHull> CollisionData;
}

