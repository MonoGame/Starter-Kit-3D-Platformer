// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;

//-:cnd:noEmit

/// <summary>
/// Holds various game constants used throughout the game.
/// </summary>
public static class GameConstants
{
    /// <summary>
    /// The assembly name for the game. This is used in ContentSerializerRuntimeType attributes
    /// to ensure the XNB deserializer can properly instantiate types at runtime.
    /// IMPORTANT: This must match the actual assembly name of the final built game!
    /// </summary>
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

    public const float PLAYER_MAX_FALL_SPEED = 1000.0f;

    public const float PLAYER_MOVE_SPEED = 360;

    // Controls how quickly the player rotates
    public const float PLAYER_ROTATION_SPEED = 6.0f;

    // Shadow mapping constants
    public const float SHADOWN_FAR_PLANE = 300f;
    public const float SHADOW_NEAR_PLANE = 200f;

    // Menu based constants.
    public const float INPUT_COOLDOWN_TIME = 0.15f; // Prevents rapid input
    public const float SCALE_SPEED = 5.0f; // Speed of oscillation
    public const float MIN_SCALE = 1.0f;   // Minimum scale
    public const float MAX_SCALE = 1.05f;   // Maximum scale

    // Menu transition constants
    public const float MENU_TRANSITION_DURATION = 0.5f; // Duration of menu transitions in seconds
    public const float MENU_TRANSITION_OFFSET = 200f;   // How far off-screen menu items start
    public const string SUPPORT_URL = "https://www.monogame.net/donate";
    public const string WEBSITE_URL = "https://www.monogame.net";
    public const string CODE_URL = "https://github.com/MonoGame/Starter-Kit-3D-Platformer";
    public static readonly Color DEFAULT_BACKGROUND_COLOR = Color.SkyBlue; // new Color(0.752941f, 0.776471f, 0.827451f);
    public const float DEFAULT_CELEBRATION_TIMER = 5.0f; // Duration of celebration effect after completing a level
}
