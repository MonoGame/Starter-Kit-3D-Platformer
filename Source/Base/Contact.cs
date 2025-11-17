// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;

/// <summary>
/// Contact information for collision detection.
/// </summary>
public struct Contact
{
    public Vector3 point;
    public Vector3 normal;
    public float depth;
}
