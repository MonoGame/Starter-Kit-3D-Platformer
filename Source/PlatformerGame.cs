// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class PlatformerGame : Game
{
    private enum GameState
    {
        SplashScreen,
        LoadingScreen,
        MenuScreen,
        MainScene,
        PauseScreen,
    }

#if DEVMODE
    [Flags]
    private enum DebugFlags
    {
        None = 0,
        ShowCollisionMesh = 1 << 0,
        ShowRenderTargets = 1 << 1,
        ShowMetrics = 1 << 2,
        ShowAll = ShowCollisionMesh | ShowRenderTargets | ShowMetrics
    }
#endif

    private GameState _currentState = GameState.SplashScreen;
    private Texture2D _splashTexture;
    private Texture2D _coinTexture;
    private Texture2D _overlayTexture;
    private Texture2D _menuBackgroundTexture;
    private Texture2D _logoTexture;
    private float _splashTimer = 0f;
    private float _loadingTimer = 0f;
    private const float SplashDurationInSeconds = 3f;
    private const float LoadingDurationInSeconds = 2f;
    private readonly Color _skyColor = new Color(0.752941f, 0.776471f, 0.827451f);
    private readonly Color _menuColor = new Color(255,182,0);
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    /// <summary>
    /// A scale to gameplay time for debugging.
    /// </summary>
    public static float TimeScale = 1f;

    private SpriteFont _font;
    private SpriteFont _debugFont;

    private PostProcessor _postProcessor;
    private ShadowProcessor _shadowProcessor;

    private SoundEffectInstance _song;

#if DEVMODE
    private DebugFlags _debugFlags = DebugFlags.None;
#endif

    private Menu<GameState> _mainMenu;
    private Menu<GameState> _pauseMenu;

    private SceneLoader _sceneLoader;

    private Scene _menuScene;
    private Scene _loadingScene;
    private Scene _currentScene;

    public PlatformerGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = (int)GameConstants.BASE_RESOLUTION_WIDTH;
        _graphics.PreferredBackBufferHeight = (int)GameConstants.BASE_RESOLUTION_HEIGHT;
        _graphics.PreferredBackBufferFormat = SurfaceFormat.Color;
        _graphics.PreferredDepthStencilFormat = DepthFormat.Depth24;
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        _graphics.PreferMultiSampling = true;
        _graphics.SynchronizeWithVerticalRetrace = true;
        // work around for MSAA issues in windows
        // https://github.com/MonoGame/MonoGame/issues/7914
        _graphics.PreparingDeviceSettings += (s, e) => e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 4;
        _graphics.ApplyChanges();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "3D Platformer Game";
        Window.AllowUserResizing = true;
        Window.AllowAltF4 = true;
    }

    protected override void Initialize()
    {
        base.Initialize();

    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _postProcessor = new PostProcessor(GraphicsDevice, _spriteBatch);
        _postProcessor.LoadContent(Content);
        _shadowProcessor = new ShadowProcessor(GraphicsDevice, _spriteBatch);
        _shadowProcessor.LoadContent(Content);
        _shadowProcessor.LightDirection = Vector3.Normalize(new Vector3(10, 20, 10));
        _shadowProcessor.SpecularIntensity = 10f;
        _shadowProcessor.Shininess = 160.0f;
        _sceneLoader = new SceneLoader(GraphicsDevice, Content);
        _overlayTexture = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1);
        _overlayTexture.SetData(new[] { Color.Black });
        _splashTexture = Content.Load<Texture2D>("splash-screen");
        _menuBackgroundTexture = Content.Load<Texture2D>("Textures/menu");
        _logoTexture = Content.Load<Texture2D>("Textures/logo");
        _font = Content.Load<SpriteFont>("Font/hud");
        _coinTexture = Content.Load<Texture2D>("Textures/coin");
        _debugFont = Content.Load<SpriteFont>("Font/debug");
        Content.Load<Model>("Models/platform-large");
        Content.Load<Model>("Models/cloud");
        Content.Load<Model>("Models/character");

        _song = Content.Load<SoundEffect>("Sounds/bright").CreateInstance();
        _song.IsLooped = true;
        _song.Volume = 0.0f;
#if !DEVMODE
        _song.Play();
#endif
        _mainMenu = new Menu<GameState>(_font, Content, Exit);
        _mainMenu.AddItem("Start Game", () =>
        {
            LoadLevel("level1");
            _currentState = GameState.MainScene;
        });
        _mainMenu.AddItem("Quit", Exit);
        _pauseMenu = new Menu<GameState>(_font, Content, () => _currentState = GameState.MainScene);
        _pauseMenu.AddItem("Resume", () => _currentState = GameState.MainScene);
        _pauseMenu.AddItem("Main Menu", () =>
        {
            _currentState = GameState.MenuScreen;
            _mainMenu.Activate();
        });

        _menuScene = _sceneLoader.LoadScene("menu");
        _menuScene.AcceptInput = false;

        // create a loading scene and manually add an animated entity to it
        _loadingScene = _sceneLoader.LoadScene("loading");
        _loadingScene.AcceptInput = false;
        _loadingScene.HasPlayer = false;
        var playerLoading = new AnimatedEntity(Content.Load<Model>("Models/character"), Content)
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity
        };
        playerLoading.PlayAnimation("jump");
        _loadingScene.Entities.Add(playerLoading);
    }

    string[] levels = new string[]
    {
        "level1",
        "level2",
        "level3"
    };
    int currentLevel = 0;

    private void LoadNextLevel()
    {
        currentLevel++;
        if (currentLevel < levels.Length)
        {
            LoadLevel(levels[currentLevel]);
        }
        else
        {
            // Reset to the first level or handle end of game logic
            currentLevel = 0;
            LoadLevel(levels[currentLevel]);
        }
    }

    private void LoadLevel(string level)
    {
        Vector3 lightPosition = new Vector3(100, 200, 100);
        _currentScene = _sceneLoader.LoadScene(level);
        _shadowProcessor.LightDirection = Vector3.Normalize(_currentScene.LightPosition);
        _shadowProcessor.SpecularIntensity = 0.1f;
        _shadowProcessor.Shininess = 0.5f;
        _currentScene.ResetTimer = 0;
    }

    protected override void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Fade in the music volume.
        if (_song != null && _song.Volume < 1.0f && _song.State == SoundState.Playing)
            _song.Volume = MathF.Min(1.0f, _song.Volume + (deltaTime * 0.5f));

        // Capture the current input state here once which is
        // then used all through out the game.
        InputState.Update();

        if (InputState.IsButtonPressed(Buttons.Back) || InputState.IsKeyPressed(Keys.Escape))
        {
            switch (_currentState)
            {
                case GameState.MainScene:
                    // Pause the game
                    _currentState = GameState.PauseScreen;
                    _pauseMenu.Activate();
                    break;

                case GameState.PauseScreen:
                    // Resume the game
                    _currentState = GameState.MainScene;
                    break;

                case GameState.MenuScreen:
                case GameState.SplashScreen:
                case GameState.LoadingScreen:
                    // Exit to desktop or main menu
                    Exit();
                    break;
            }
        }

        if (InputState.IsKeyDown(Keys.LeftAlt) && InputState.IsKeyPressed(Keys.Enter))
        {
            // Toggle fullscreen mode
            _graphics.ToggleFullScreen();
        }

        // Handle debug flags toggling
#if DEVMODE
        if (InputState.IsKeyDown(Keys.LeftControl) || InputState.IsKeyDown(Keys.RightControl))
        {
            if (InputState.IsKeyPressed(Keys.F1))
            {
                _debugFlags ^= DebugFlags.ShowCollisionMesh;
            }
            if (InputState.IsKeyPressed(Keys.F2))
            {
                _debugFlags ^= DebugFlags.ShowRenderTargets;
            }
            if (InputState.IsKeyPressed(Keys.F3))
            {
                _debugFlags ^= DebugFlags.ShowMetrics;
            }
            if (InputState.IsKeyPressed(Keys.P))
            {
                // TODO: print screen.
            }
            if (InputState.IsKeyPressed(Keys.OemPlus))
            {
                TimeScale += 0.1f; // Increase time scale by 0.1x
                if (TimeScale > 10f) // Prevent excessive time scale
                {
                    TimeScale = 10f;
                }
            }
            if (InputState.IsKeyPressed(Keys.OemMinus))
            {
                TimeScale -= 0.1f; // Decrease time scale by 0.1x
                if (TimeScale < 0.1f) // Prevent negative or zero time scale
                {
                    TimeScale = 0.1f;
                }
            }
            if (InputState.IsKeyPressed(Keys.M))
            {
                if (_song.State == SoundState.Playing)
                {
                    _song.Pause();
                }
                else
                {
                    _song.Volume = 0.0f; // Reset volume to 0 before resuming
                    _song.Resume();
                }
            }
        }
#endif

        // TODO: Add your update logic here
        switch (_currentState)
        {
            case GameState.SplashScreen:
                // Handle splash screen logic
                _splashTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Transition to main scene after SplashDuration seconds
                if (_splashTimer >= SplashDurationInSeconds)
                {
                    _splashTimer = 0f; // Reset splash timer
                    _currentState = GameState.MenuScreen;
                    _mainMenu.Activate();
                }
                break;

            case GameState.LoadingScreen:
                _loadingScene.Update(gameTime);
                _loadingTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_loadingTimer >= LoadingDurationInSeconds)
                {
                    _loadingTimer = 0f; // Reset the timer
                    _currentState = GameState.MainScene; // Transition to main scene
                }
                break;

            case GameState.MenuScreen:
                // TODO: Handle menu screen logic
                _menuScene.Update(gameTime);
                _mainMenu.Update(gameTime);
                break;

            case GameState.PauseScreen:
                deltaTime = 0f; // Pause the game logic
                _pauseMenu.Update(gameTime);
                goto case GameState.MainScene; // Pause screen logic is handled in the main scene update
            case GameState.MainScene:
                // Handle main scene logic

                // We use a scaled time here mostly for testing/debugging.
                var scaledTime = new GameTime(gameTime.TotalGameTime,
                    TimeSpan.FromSeconds(deltaTime * TimeScale));

                _currentScene.Update(scaledTime);

                _shadowProcessor.TargetPosition = _currentScene.Player.Position;

                if (_currentScene.Goal.Complete)
                {
                    _currentState = GameState.LoadingScreen;
                    LoadNextLevel(); // Reload the level
                }

                if (_currentScene.ResetTimer > 0)
                {
                    _currentScene.ResetTimer -= deltaTime * TimeScale;
                    if (_currentScene.ResetTimer < 0)
                    {
                        _currentState = GameState.LoadingScreen;
                        LoadLevel(levels[currentLevel]); // Reload the level
                    }
                }

                break;
        }

        base.Update(gameTime);
    }

    private void DrawHud(GameTime gameTime, Rectangle rect, Vector2 scale)
    {
        // Apply a globl scale to make sure all the HUD elements are scaled correctly
        // This is useful for different screen resolutions and aspect ratios.
        // The scale is based on the original resolution of 1280x720.
        _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(new Vector3(rect.X, rect.Y, 0)) * Matrix.CreateScale(scale.X, scale.Y, 0f));

        _spriteBatch.Draw(_coinTexture, new Rectangle(10, 10, 100, 100), Color.White);
        _spriteBatch.DrawString(_font, $"{_currentScene.Player.Score}", new Vector2(110, 30), Color.White);
        if (_currentScene.Goal.GoalReached)
        {
            var textSize = _font.MeasureString("Level Complete!");
            _spriteBatch.DrawString(_font, "Level Complete!", new Vector2((GameConstants.BASE_RESOLUTION_WIDTH / 2) - (textSize.X / 2), (GameConstants.BASE_RESOLUTION_HEIGHT / 2) - (textSize.Y / 2)), Color.White);
        }
#if DEVMODE
        if (_debugFlags.HasFlag(DebugFlags.ShowMetrics))
        {
            DrawMetrics(gameTime);
        }
#endif
        _spriteBatch.End();
    }

    private void DrawMetrics(GameTime gameTime)
    {
        if (_debugFlags.HasFlag(DebugFlags.ShowMetrics))
        {
            // Draw any additional metrics here
            _spriteBatch.DrawString(_debugFont, $"FPS: {1f / (float)gameTime.ElapsedGameTime.TotalSeconds:0.00}", new Vector2(10, 110), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Time Scale: {TimeScale:0.00}", new Vector2(10, 130), Color.White);
            //_spriteBatch.DrawString(_debugFont, $"Entities: {_entities.Count}", new Vector2(10, 150), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Clear: {GraphicsDevice.Metrics.ClearCount}", new Vector2(10, 170), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Draw: {GraphicsDevice.Metrics.DrawCount}", new Vector2(10, 190), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Primitives: {GraphicsDevice.Metrics.PrimitiveCount}", new Vector2(10, 210), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Sprites: {GraphicsDevice.Metrics.SpriteCount}", new Vector2(10, 230), Color.White);
            _spriteBatch.DrawString(_debugFont, $"GC Gen 0: {GC.CollectionCount(0)}", new Vector2(10, 250), Color.White);
            _spriteBatch.DrawString(_debugFont, $"GC Gen 1: {GC.CollectionCount(1)}", new Vector2(10, 270), Color.White);
            _spriteBatch.DrawString(_debugFont, $"GC Gen 2: {GC.CollectionCount(2)}", new Vector2(10, 290), Color.White);
            _spriteBatch.DrawString(_debugFont, $"GC Total: {GC.CollectionCount(3)}", new Vector2(10, 310), Color.White);
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            _spriteBatch.DrawString(_debugFont, $"GC Memory: {gcMemoryInfo.TotalAvailableMemoryBytes / (1024 * 1024):0.00} MB", new Vector2(10, 350), Color.White);
            _spriteBatch.DrawString(_debugFont, $"GC Fragmentation: {gcMemoryInfo.FragmentedBytes / (1024 * 1024):0.00} MB", new Vector2(10, 370), Color.White);
            _spriteBatch.DrawString(_debugFont, $"GC Heap Size: {gcMemoryInfo.HeapSizeBytes / (1024 * 1024):0.00} MB", new Vector2(10, 390), Color.White);
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Transparent);
        var screenRect = _graphics.GraphicsDevice.Viewport.Bounds;
        Vector2 uiScale = new Vector2(screenRect.Width / GameConstants.BASE_RESOLUTION_WIDTH, screenRect.Height / GameConstants.BASE_RESOLUTION_HEIGHT); // Scale UI based on screen size

        // TODO: Add your drawing code here
        switch (_currentState)
        {
            case GameState.SplashScreen:
                // Draw splash screen
                GraphicsDevice.Clear(Color.Black);

                _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));

                // Draw splash texture centered on screen
                Rectangle destinationRectangle = new Rectangle(
                    ((int)GameConstants.BASE_RESOLUTION_WIDTH - _splashTexture.Width) / 2,
                    ((int)GameConstants.BASE_RESOLUTION_HEIGHT - _splashTexture.Height) / 2,
                    _splashTexture.Width,
                    _splashTexture.Height);

                _spriteBatch.Draw(_splashTexture, destinationRectangle, Color.White);

                _spriteBatch.End();
                break;

            case GameState.LoadingScreen:
                // Draw splash screen
                _loadingScene.Draw(gameTime, GraphicsDevice, _shadowProcessor, _postProcessor, _spriteBatch);
                // GraphicsDevice.Clear(Color.Black);
                // _postProcessor.BeginScene();
                // _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));

                // // Draw splash texture centered on screen
                // destinationRectangle = new Rectangle(
                //     ((int)GameConstants.BASE_RESOLUTION_WIDTH - _splashTexture.Width) / 2,
                //     ((int)GameConstants.BASE_RESOLUTION_HEIGHT - _splashTexture.Height) / 2,
                //     _splashTexture.Width,
                //     _splashTexture.Height);

                // _spriteBatch.Draw(_splashTexture, destinationRectangle, Color.White);

                // _spriteBatch.End();
                // _postProcessor.EndScene();
                break;

            case GameState.MenuScreen:
                GraphicsDevice.Clear(_skyColor);
                _menuScene.Draw(gameTime, GraphicsDevice, _shadowProcessor, _postProcessor, _spriteBatch);
                _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));
                _spriteBatch.Draw(_logoTexture, new Rectangle((int)GameConstants.BASE_RESOLUTION_WIDTH - (_logoTexture.Width - 100), 50, _logoTexture.Width - 200, _logoTexture.Height - 50), Color.White);
                _spriteBatch.End();
                var offset = new Vector3(GameConstants.BASE_RESOLUTION_WIDTH / 2f - _mainMenu.GetMenuWidth() / 2f + 320, GameConstants.BASE_RESOLUTION_HEIGHT / 2f - _mainMenu.GetMenuHeight() / 2f + 150, 0f);
                _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(offset) * Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));
                _mainMenu.Draw(_spriteBatch);
                _spriteBatch.End();
                break;

            case GameState.PauseScreen:
            case GameState.MainScene:

                // Draw the scene
                _currentScene.Draw(gameTime, GraphicsDevice, _shadowProcessor, _postProcessor, _spriteBatch);
#if DEVMODE
                if (_debugFlags.HasFlag(DebugFlags.ShowCollisionMesh))
                {
                    _currentScene.DrawCollisionMeshs(_spriteBatch);
                }
#endif
#if DEVMODE
                _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwise);
                if (_debugFlags.HasFlag(DebugFlags.ShowCollisionMesh))
                    _currentScene.Player.DrawBillboards(GraphicsDevice, _spriteBatch, _currentScene.Camera);
                _spriteBatch.End();
#endif
                // Draw the score etc.
                DrawHud(gameTime, screenRect, uiScale);
#if DEVMODE
                if (_debugFlags.HasFlag(DebugFlags.ShowRenderTargets))
                {
                    _shadowProcessor.DebugDrawShadowMap(new Rectangle(0, 0, 256, 256));
                    _postProcessor.DebugDrawRenderTargets(new Rectangle(256, 0, 256, 256));
                }
#endif
                if (_currentState == GameState.PauseScreen)
                {
                    // Draw semi-transparent overlay
                    offset = new Vector3(GameConstants.BASE_RESOLUTION_WIDTH / 2f - _pauseMenu.GetMenuWidth() / 2f, GameConstants.BASE_RESOLUTION_HEIGHT / 2f - _pauseMenu.GetMenuHeight() / 2f, 0f);
                    _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(offset) * Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));
                    _spriteBatch.Draw(_overlayTexture,
                        new Rectangle(-(int)offset.X, -(int)offset.Y, (int)GameConstants.BASE_RESOLUTION_WIDTH, (int)GameConstants.BASE_RESOLUTION_HEIGHT),
                        Color.Black * 0.5f);
                    // Draw the pause menu centered horizontally
                    _pauseMenu.Draw(_spriteBatch);
                    _spriteBatch.End();
                }
                break;
        }

        base.Draw(gameTime);
    }
}
