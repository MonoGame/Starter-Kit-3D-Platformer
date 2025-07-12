// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;

public static class GameConstants
{
    public const string AssemblyName = "3DPlatformer";
    /// <summary>
    /// Base resolution for the game.
    /// This is used to scale the UI and other elements based on the actual screen resolution.
    /// </summary>
    public const float BASE_RESOLUTION_WIDTH = 1280f;
    public const float BASE_RESOLUTION_HEIGHT = 720f;

    /// <summary>
    /// Default Earth gravity for reference.
    /// </summary>
    public const float EARTH_GRAVITY = -9.81f;

    /// <summary>
    /// The gravity for non-player objects. (like falling platforms).
    /// </summary>
    public const float GRAVITY = EARTH_GRAVITY * 10.0f;

    /// <summary>
    /// The gravity force while the player is jumping (holding the jump button).
    /// </summary>
    public const float PLAYER_JUMP_GRAVITY = EARTH_GRAVITY * 300.0f;

    /// <summary>
    /// The gravity force when then player is not jumping or has a negative velocity.
    /// </summary>
    public const float PLAYER_FALL_GRAVITY = EARTH_GRAVITY * 350.0f;

    /// <summary>
    /// The instant velocity force of the players jump.
    /// </summary>
    public const float PLAYER_JUMP_FORCE = 700.0f;

    public const float PLAYER_MAX_FALL_SPEED = 800.0f;

    public const float PLAYER_MOVE_SPEED = 360;

    // Controls how quickly the player rotates
    public const float PLAYER_ROTATION_SPEED = 6.0f;
}
