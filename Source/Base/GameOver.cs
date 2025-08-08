// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class GameOver
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ContentManager _content;
    private readonly SpriteFont _font;
    private readonly Texture2D _logo;
    private readonly Menu _menu;
    private Color textColor = new Color(255, 182, 0);

    public Action OnReturnToMainMenu;

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
        _menu.AddItem("Visit MonoGame Website", () =>
        {
            OpenUrl(GameConstants.WEBSITEURL);
        });
        _menu.AddItem("Visit Patreon", () =>
        {
            OpenUrl(GameConstants.PATREONURL);
        });
        _menu.AddItem("View Source Code on GitHub", () =>
        {
            OpenUrl(GameConstants.GITHUBURL);
        });

        _menu.BasePosition = new Vector2(GameConstants.BASE_RESOLUTION_WIDTH / 2 - (_menu.GetMenuWidth() / 2), GameConstants.BASE_RESOLUTION_HEIGHT - _menu.GetMenuHeight() - 50);
    }

    private void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            System.Diagnostics.Process.Start(url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            System.Diagnostics.Process.Start("xdg-open", url);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            System.Diagnostics.Process.Start("open", url);
    }

    public void Activate()
    {
        _menu.Activate();
    }

    public void Update(GameTime gameTime)
    {
        // Code to update the game over screen
        _menu.Update(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        // Code to draw the game over screen
        var sb = new StringBuilder();
        var textSize = _font.MeasureString("You have reached the end of the sample!");
        sb.AppendLine("You have reached the end of the sample!");
        sb.AppendLine("Thank you for playing.");
        spriteBatch.DrawString(_font, sb.ToString(), new Vector2((GameConstants.BASE_RESOLUTION_WIDTH / 2) - (textSize.X / 2), 200), textColor);
        spriteBatch.Draw(_logo, new Rectangle((int)(GameConstants.BASE_RESOLUTION_WIDTH / 2) - ((_logo.Width / 4) / 2), 10, _logo.Width / 4, _logo.Height / 4), Color.White);
        _menu.Draw(spriteBatch);
    }
}