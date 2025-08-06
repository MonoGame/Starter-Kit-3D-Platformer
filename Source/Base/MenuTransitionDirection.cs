// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
/// <summary>
/// Specifies the direction from which menu items will transition in.
/// </summary>
public enum MenuTransitionDirection
{
    Top,
    Bottom,
    Left,
    Right,
    None // No transition, items appear immediately
}