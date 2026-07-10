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

//-:cnd:noEmit

/// <summary>
/// This is the main type for your game.
/// </summary>
public class PlatformerGame : Game
{
    private enum GameState
    {
        SplashScreen,
        LoadingScreen,
        MenuScreen,
        MainScene,
        PauseScreen,
        GameOverScreen
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
    private Texture2D _logoTexture;
    private Texture2D _foundationTexture;
    private float _splashTimer = 0f;
    private float _loadingTimer = 0f;
    private const float SplashDurationInSeconds = 3f;
    private const float LoadingDurationInSeconds = 2f;
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    /// <summary>
    /// A scale to gameplay time for debugging.
    /// </summary>
    public static float TimeScale = 1f;

    private SpriteFont _font;
    private SpriteFont _debugFont;

    private PostProcessor _postProcessor;
    private SceneRenderer _sceneRenderer;
    private TransitionProcessor _transitionProcessor;

    private SoundEffectInstance _song;

#if DEVMODE
    private DebugFlags _debugFlags = DebugFlags.None;
#endif

    private Menu _mainMenu;
    private Menu _pauseMenu;

    private SceneLoader _sceneLoader;

    private Scene _menuScene;
    private Scene _loadingScene;
    private Scene _gameOverScene;
    private Scene _currentScene;
    private GameOver _gameOverScreen;

    private string[] levels;
    private int currentLevel = 0;

    public PlatformerGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        // We use a fixed resolution of 1280x720 for the game.
        // we then use RenderTargets to scale to the actual window size.
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
        Window.Title = "3D Platformer";
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
        _sceneRenderer = new SceneRenderer(GraphicsDevice, _spriteBatch);
        _sceneRenderer.LoadContent(Content);
        _sceneRenderer.LightDirection = Vector3.Normalize(new Vector3(10, 20, 10));
        _sceneRenderer.SpecularIntensity = 10f;
        _sceneRenderer.Shininess = 160.0f;
        _transitionProcessor = new TransitionProcessor(GraphicsDevice, _spriteBatch);
        _sceneLoader = new SceneLoader(GraphicsDevice, Content);
        _overlayTexture = new Texture2D(_spriteBatch.GraphicsDevice, 1, 1);
        _overlayTexture.SetData(new[] { Color.Black });
        _splashTexture = Content.Load<Texture2D>("splash-screen");
        _logoTexture = Content.Load<Texture2D>("Textures/logo");
        _coinTexture = Content.Load<Texture2D>("Textures/coin");
        _foundationTexture = Content.Load<Texture2D>("Textures/foundation");
        _font = Content.Load<SpriteFont>("Font/hud");
        _debugFont = Content.Load<SpriteFont>("Font/debug");
        Content.Load<Model>("Models/platform-large");
        Content.Load<Model>("Models/cloud");
        Content.Load<Model>("Models/character");

        _gameOverScreen = new GameOver(GraphicsDevice, Content, _font);
        _gameOverScreen.OnReturnToMainMenu = () =>
        {
            _transitionProcessor.StartTransition(() =>
            {
                _currentState = GameState.MenuScreen;
                _mainMenu.Activate();
            });
        };

        _song = Content.Load<SoundEffect>("Sounds/bright").CreateInstance();
        _song.IsLooped = true;
        _song.Volume = 0.0f;
#if !DEVMODE
        _song.Play();
#endif
        _mainMenu = new Menu(_font, Content, QuitGame, MenuTransitionDirection.Right);
        _mainMenu.AddItem("Start Game", () =>
        {
            currentLevel = 0;            
            _transitionProcessor.StartTransition(() =>
            {
                _currentState = GameState.MainScene;
                LoadLevel(levels[currentLevel]);
            });
        });
        _mainMenu.AddItem("Quit", QuitGame);
        _mainMenu.BasePosition = new Vector2(GameConstants.BASE_RESOLUTION_WIDTH / 2f - _mainMenu.GetMenuWidth() / 2f + 320, GameConstants.BASE_RESOLUTION_HEIGHT / 2f - _mainMenu.GetMenuHeight() / 2f + 150);
        _pauseMenu = new Menu(_font, Content, () => _currentState = GameState.MainScene, MenuTransitionDirection.Top);
        _pauseMenu.AddItem("Resume", () => _currentState = GameState.MainScene);
        _pauseMenu.AddItem("Main Menu", () =>
        {
            _transitionProcessor.StartTransition(() =>
            {
                _currentState = GameState.MenuScreen;
                _mainMenu.Activate();
            });
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

        _gameOverScene = _sceneLoader.LoadScene("gameover");
        _gameOverScene.AcceptInput = false;

        // Get the list of levels from the levels.json file.
        levels = _sceneLoader.GetSceneList();
    }

    private void QuitGame()
    {
    #if IOS
        // iOS does not allow apps to quit programmatically.
        return;
    #else
        Exit();
    #endif
    }

    private void LoadNextLevel()
    {
        currentLevel++;
        if (currentLevel < levels.Length)
        {
            // Load the next level and go to loading screen
            _currentState = GameState.LoadingScreen;
            LoadLevel(levels[currentLevel]);
        }
        else
        {
            // Reset to the first level and go to game over screen
            currentLevel = -1;
            _currentState = GameState.GameOverScreen;
            _gameOverScreen.Activate();
        }
    }

    private void LoadLevel(string level)
    {
        _currentScene = _sceneLoader.LoadScene(level);
        _sceneRenderer.LightDirection = Vector3.Normalize(_currentScene.LightPosition);
        _sceneRenderer.SpecularIntensity = 0.1f;
        _sceneRenderer.Shininess = 0.5f;
        _currentScene.ResetTimer = 0;
    }

    protected override void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update transitions first
        _transitionProcessor.Update(deltaTime);

        // Fade in the music volume.
        if (_song != null && _song.Volume < 1.0f && _song.State == SoundState.Playing)
            _song.Volume = MathF.Min(1.0f, _song.Volume + (deltaTime * 0.5f));

        // Capture the current input state here once which is
        // then used all through out the game.
        InputState.Update();

        // Skip input handling during transitions
        if (!_transitionProcessor.IsTransitioning && (InputState.IsButtonPressed(Buttons.Back) || InputState.IsKeyPressed(Keys.Escape)))
        {
            switch (_currentState)
            {
                case GameState.MainScene:
                    // Pause the game - NO transition, instant
                    _currentState = GameState.PauseScreen;
                    _pauseMenu.Activate();
                    break;

                case GameState.PauseScreen:
                    // Resume the game - NO transition, instant
                    _currentState = GameState.MainScene;
                    break;
                case GameState.GameOverScreen:
                    // Go back to the main menu
                    _transitionProcessor.StartTransition(() =>
                    {
                        _currentState = GameState.MenuScreen;
                        _mainMenu.Activate();
                    });
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
            if (InputState.IsKeyPressed(Keys.B))
            {
                _postProcessor.BloomEnabled = !_postProcessor.BloomEnabled;
            }
            if (InputState.IsKeyPressed(Keys.V))
            {
                _postProcessor.VignetteEnabled = !_postProcessor.VignetteEnabled;
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
                    _transitionProcessor.StartTransition(() =>
                    {
                        _currentState = GameState.MenuScreen;
                        _mainMenu.Activate();
                    });
                }
                break;

            case GameState.LoadingScreen:
                _loadingScene.Update(gameTime);
                _loadingTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_loadingTimer >= LoadingDurationInSeconds)
                {
                    _loadingTimer = 0f; // Reset the timer
                    _transitionProcessor.StartTransition(() =>
                    {
                        _currentState = GameState.MainScene; // Transition to main scene
                    });
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

                _sceneRenderer.TargetPosition = _currentScene.Player.Position;

                if (_currentScene.Goal.Complete && !_transitionProcessor.IsTransitioning)
                {
                    _transitionProcessor.StartTransition(() =>
                    {
                        LoadNextLevel(); // Reload the level
                    });
                }

                if (_currentScene.ResetTimer > 0 && !_transitionProcessor.IsTransitioning)
                {
                    _currentScene.ResetTimer -= deltaTime * TimeScale;
                    if (_currentScene.ResetTimer < 0)
                    {
                        _transitionProcessor.StartTransition(() =>
                        {
                            _currentState = GameState.LoadingScreen;

                            // Have the load screen match the lighting and sky of the current level.
                            _loadingScene.SkyColor = _currentScene.SkyColor;
                            _loadingScene.LightColor = _currentScene.LightColor;

                            LoadLevel(levels[currentLevel]); // Reload the level
                        });
                    }
                }

                break;
            case GameState.GameOverScreen:
                _gameOverScene.Update(gameTime);
                _gameOverScreen.Update(gameTime);
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
            _spriteBatch.DrawString(_font, "Level Complete!", new Vector2((GameConstants.BASE_RESOLUTION_WIDTH / 2) - (textSize.X / 2), GameConstants.BASE_RESOLUTION_HEIGHT - (textSize.Y / 2) - 100), Color.White);
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
                GraphicsDevice.Clear(GameConstants.DEFAULT_BACKGROUND_COLOR);

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
                _loadingScene.Draw(gameTime, GraphicsDevice, _sceneRenderer, _postProcessor, _spriteBatch);
                break;

            case GameState.GameOverScreen:
                // Draw MonoGame logo and url, Patreon logo and url and "Game Over" text.
                // Add Source code GitHub url.
                // Thank Patrons for their support.
                _gameOverScene.Draw(gameTime, GraphicsDevice, _sceneRenderer, _postProcessor, _spriteBatch);
                _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));
                _gameOverScreen.Draw(gameTime, _spriteBatch);
                _spriteBatch.End();
                break;

            case GameState.MenuScreen:
                _menuScene.Draw(gameTime, GraphicsDevice, _sceneRenderer, _postProcessor, _spriteBatch);
                _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));
                _spriteBatch.Draw(_logoTexture,
                    new Rectangle(
                        (int)GameConstants.BASE_RESOLUTION_WIDTH - (_logoTexture.Width - 100),
                        50,
                        (int)(_logoTexture.Width * 0.75f),
                        (int)(_logoTexture.Height * 0.75f)), Color.White);
                _spriteBatch.End();
                _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));
                _mainMenu.Draw(_spriteBatch);
                _spriteBatch.End();
                break;

            case GameState.PauseScreen:
            case GameState.MainScene:

                // Draw the scene
                _currentScene.Draw(gameTime, GraphicsDevice, _sceneRenderer, _postProcessor, _spriteBatch);
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
                    _sceneRenderer.DebugDrawShadowMap(new Rectangle(0, 0, 256, 256));
                    _postProcessor.DebugDrawRenderTargets(new Rectangle(256, 0, 256, 256));
                }
#endif
                if (_currentState == GameState.PauseScreen)
                {
                    var pauseMenuPosition = new Vector2(
                        GameConstants.BASE_RESOLUTION_WIDTH / 2f - _pauseMenu.GetMenuWidth() / 2f,
                        GameConstants.BASE_RESOLUTION_HEIGHT / 2f - _pauseMenu.GetMenuHeight() / 2f
                    );
                    _pauseMenu.BasePosition = pauseMenuPosition;
                    _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));
                    // Draw semi-transparent overlay
                    _spriteBatch.Draw(_overlayTexture,
                        new Rectangle(0, 0, (int)GameConstants.BASE_RESOLUTION_WIDTH, (int)GameConstants.BASE_RESOLUTION_HEIGHT),
                        Color.Black * 0.5f);
                    // Draw the pause menu
                    _pauseMenu.Draw(_spriteBatch);
                    _spriteBatch.End();
                }
                break;
        }

        // Draw transition overlay at the end (over everything including post-processing effects)
        _transitionProcessor.DrawTransition(uiScale, GameConstants.BASE_RESOLUTION_WIDTH, GameConstants.BASE_RESOLUTION_HEIGHT);

        base.Draw(gameTime);
    }
}
