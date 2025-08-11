// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
/// <summary>
/// Represents the current state of the menu transition.
/// </summary>
public enum MenuState
{
    Hidden,        // Menu is not visible
    TransitionIn,  // Menu is transitioning in
    Active,        // Menu is fully visible and interactive
    TransitionOut  // Menu is transitioning out
}