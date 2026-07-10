// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Represents the game over screen.
/// </summary>
public class GameOver
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly SpriteFont _font;
    private readonly Texture2D _logo;
    private readonly Menu _menu;
    private Color textColor = new Color(255, 182, 0);

    /// <summary>
    /// An Action which is fired when the player chooses to return to the main menu.
    /// </summary>
    public Action OnReturnToMainMenu;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameOver"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device used for rendering.</param>
    /// <param name="content">The content manager for loading assets.</param>
    /// <param name="font">The font used for rendering text.</param>
    public GameOver(GraphicsDevice graphicsDevice, ContentManager content, SpriteFont font)
    {
        _graphicsDevice = graphicsDevice;
        _content = content;
        _font = font;
        _logo = content.Load<Texture2D>("Textures/foundation");

        _menu = new Menu(font, content);

        _menu.AddItem("Return to Main Menu", () =>
        {
            OnReturnToMainMenu?.Invoke();
        });
        _menu.AddItem("MonoGame Website", () =>
        {
            OpenUrl(GameConstants.WEBSITE_URL);
        });
        _menu.AddItem("Support Us", () =>
        {
            OpenUrl(GameConstants.SUPPORT_URL);
        });
        _menu.AddItem("View Source Code", () =>
        {
            OpenUrl(GameConstants.CODE_URL);
        });

        _menu.TextColor = textColor;

        _menu.BasePosition = new Vector2(GameConstants.BASE_RESOLUTION_WIDTH / 2 - (_menu.GetMenuWidth() / 2) - 100, GameConstants.BASE_RESOLUTION_HEIGHT - _menu.GetMenuHeight() - 50);
    }

    /// <summary>
    /// Opens the specified URL in the default web browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    private void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            System.Diagnostics.Process.Start(url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            System.Diagnostics.Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            System.Diagnostics.Process.Start("open", url);
    }

    /// <summary>
    /// Activates the game over menu.
    /// </summary>
    public void Activate()
    {
        _menu.Activate();
    }

    /// <summary>
    /// Updates the game over screen.
    /// </summary>
    /// <param name="gameTime">The current frame game time.</param>
    public void Update(GameTime gameTime)
    {
        // Code to update the game over screen
        _menu.Update(gameTime);
    }

    /// <summary>
    /// Draws the game over screen.
    /// </summary>
    /// <param name="gameTime">The current frame game time.</param>
    /// <param name="spriteBatch">The sprite batch used for drawing.
    /// There is no need to call Begin/End as it will be called by the calling Draw method.</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Code to draw the game over screen
        var sb = new StringBuilder();
        var textSize = _font.MeasureString("You have reached the end of the demo!");
        sb.AppendLine("You have reached the end of the demo!");
        sb.AppendLine("Thank you for playing.");
        spriteBatch.DrawString(_font, sb.ToString(), new Vector2((GameConstants.BASE_RESOLUTION_WIDTH / 2) - (textSize.X / 1.5f), 200), textColor);
        spriteBatch.Draw(_logo, new Rectangle((int)(GameConstants.BASE_RESOLUTION_WIDTH / 2) - ((_logo.Width / 4) / 2), 10, _logo.Width / 4, _logo.Height / 4), Color.White);
        _menu.Draw(spriteBatch);
    }
}
