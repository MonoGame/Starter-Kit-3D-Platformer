// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
    private float _splashTimer = 0f;
    private float _loadingTimer = 0f;
    private const float SplashDurationInSeconds = 3f;
    private const float LoadingDurationInSeconds = 5f;
    private readonly Color _skyColor = new Color(0.752941f, 0.776471f, 0.827451f);
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    /// <summary>
    /// A scale to gameplay time for debugging.
    /// </summary>
    public static float TimeScale = 1f;

    private SpriteFont _font;
    private SpriteFont _debugFont;

    private List<Entity> _entities = new List<Entity>();
    private Queue<Entity> _entitiesToRemove = new Queue<Entity>();

    private List<Entity> _drawList = new List<Entity>();

    private Camera _camera;
    private Player _player;

    private Goal _goal;
    private Dust _dust;
    private PostProcessor _postProcessor;
    private ShadowProcessor _shadowProcessor;

    private SoundEffectInstance _song;

#if DEVMODE
    private DebugFlags _debugFlags = DebugFlags.None;
#endif

    private KeyboardState _previousKeyboardState = new KeyboardState();

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
        _splashTexture = Content.Load<Texture2D>("splash-screen");
        _font = Content.Load<SpriteFont>("Font/hud");
        _coinTexture = Content.Load<Texture2D>("Textures/coin");
        _debugFont = Content.Load<SpriteFont>("Font/debug");
        Content.Load<Model>("Models/platform-large");
        Content.Load<Model>("Models/cloud");
        Content.Load<Model>("Models/character");
         _dust = new Dust(Content.Load<Model>("Models/dust"), Content);

        _song = Content.Load<SoundEffect>("Sounds/bright").CreateInstance();
        _song.IsLooped = true;
        _song.Volume = 0.0f;
        _song.Play();

        LoadLevel();
    }

    string[] loadingText = 
    {
        "Loading",
        "Loading.",
        "Loading..",
        "Loading...",
    };

    string[] levels = new string[]
    {
        "level3",
        "level2",
        "level3"
    };
    int currentLevel = 0;

    private void LoadNextLevel()
    {
        currentLevel++;
        if (currentLevel < levels.Length)
        {
            LoadLevel();
        }
        else
        {
            // Reset to the first level or handle end of game logic
            currentLevel = 0;
            LoadLevel();
        }
    }
    
    private void LoadLevel()
    {
        _entities.Clear();
        _entitiesToRemove.Clear();
        var loader = new LevelLoader(Content);
        Vector3 lightPosition = new Vector3(100, 200, 100);
        loader.LoadLevel(levels[currentLevel], _entities, ref lightPosition);
        _shadowProcessor.LightDirection = Vector3.Normalize(lightPosition);
        _shadowProcessor.SpecularIntensity = 0.1f;
        _shadowProcessor.Shininess = 0.5f;

        _camera = new Camera(GraphicsDevice)
        {
            Position = new Vector3(0, 0, 0),
            Target = Vector3.Zero,
            UpDirection = Vector3.Up
        };

        var spawnPoint = _entities.Find(e => e is SpawnPoint);
        _goal = _entities.Find(e => e is Goal) as Goal;

        _player = new Player(GraphicsDevice, Content.Load<Model>("Models/character"), Content)
        {
            Position = spawnPoint.Position,
            Rotation = spawnPoint.Rotation,
        };
    }


    protected override void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Fade in the music volume.
            if (_song != null && _song.Volume < 1.0f && _song.State == SoundState.Playing)
                _song.Volume = MathF.Min(1.0f, _song.Volume + (deltaTime * 0.5f));

        var currentKeyboardState = Keyboard.GetState();
        var gamePadState = GamePad.GetState(PlayerIndex.One);
        if (gamePadState.Buttons.Back == ButtonState.Pressed || currentKeyboardState.IsKeyDown(Keys.Escape))
            Exit();

        if (currentKeyboardState.IsKeyDown(Keys.LeftAlt))
        {
            if (currentKeyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter))
            {
                // Toggle fullscreen mode
                _graphics.ToggleFullScreen();
            }
        }

        // Handle debug flags toggling
#if DEVMODE
        if (currentKeyboardState.IsKeyDown(Keys.LeftControl) || currentKeyboardState.IsKeyDown(Keys.RightControl))
        {
            if (currentKeyboardState.IsKeyDown(Keys.F1) && _previousKeyboardState.IsKeyUp(Keys.F1))
            {
                _debugFlags ^= DebugFlags.ShowCollisionMesh;
            }
            if (currentKeyboardState.IsKeyDown(Keys.F2) && _previousKeyboardState.IsKeyUp(Keys.F2))
            {
                _debugFlags ^= DebugFlags.ShowRenderTargets;
            }
            if (currentKeyboardState.IsKeyDown(Keys.F3) && _previousKeyboardState.IsKeyUp(Keys.F3))
            {
                _debugFlags ^= DebugFlags.ShowMetrics;
            }
            if (currentKeyboardState.IsKeyDown(Keys.P) && _previousKeyboardState.IsKeyUp(Keys.P))
            {
                _currentState = _currentState == GameState.PauseScreen ? GameState.MainScene : GameState.PauseScreen;
            }
            if (currentKeyboardState.IsKeyDown(Keys.OemPlus) && _previousKeyboardState.IsKeyUp(Keys.OemPlus))
            {
                TimeScale += 0.1f; // Increase time scale by 0.1x
                if (TimeScale > 10f) // Prevent excessive time scale
                {
                    TimeScale = 10f;
                }
            }
            if (currentKeyboardState.IsKeyDown(Keys.OemMinus) && _previousKeyboardState.IsKeyUp(Keys.OemMinus))
            {
                TimeScale -= 0.1f; // Decrease time scale by 0.1x
                if (TimeScale < 0.1f) // Prevent negative or zero time scale
                {
                    TimeScale = 0.1f;
                }
            }
            if (currentKeyboardState.IsKeyDown(Keys.M) && _previousKeyboardState.IsKeyUp(Keys.M))
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
                    _currentState = GameState.MainScene;
                }
                break;

            case GameState.LoadingScreen:
                _loadingTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_loadingTimer >= LoadingDurationInSeconds)
                {
                    _loadingTimer = 0f; // Reset the timer
                    _currentState = GameState.MainScene; // Transition to main scene
                }
                break;

            case GameState.MenuScreen:
                // TODO: Handle menu screen logic
                break;

            case GameState.PauseScreen:
                deltaTime = 0f; // Pause the game logic
                goto case GameState.MainScene; // Pause screen logic is handled in the main scene update
            case GameState.MainScene:
                // Handle main scene logic
                
                // We use a scaled time here mostly for testing/debugging.
                var scaledTime = new GameTime(gameTime.TotalGameTime,
                    TimeSpan.FromSeconds(deltaTime * TimeScale));

                // TODO: Shoukd the player really update before the world?
                _player.Forward = _camera.ForwardDirection;
                _player.Update(scaledTime);
                _dust.Update(scaledTime);

                // TODO: Maybe all entities should have this callback?
                _player.PreCollision();

                foreach (var entity in _entities)
                {
                    entity.Update(scaledTime);
                    entity.CheckCollision(_player);
                    _player.CheckCollision(entity);
                    if (entity.Dead())
                    {
                        _entitiesToRemove.Enqueue(entity);
                    }
                }
                while (_entitiesToRemove.Count > 0)
                {
                    var entity = _entitiesToRemove.Dequeue();
                    _entities.Remove(entity);
                }

                if (_player.Dead())
                {
                    _currentState = GameState.LoadingScreen;
                    LoadLevel(); // Reload the level
                }
                else if (_player.IsMoving && _player.IsGrounded)
                {
                    _dust.AddDust(scaledTime, _player.Position);
                }

                _camera.Target = _player.Position;
                _camera.Update(scaledTime);
                _shadowProcessor.TargetPosition = _player.Position;

                if (_goal.Complete)
                {
                    _currentState = GameState.LoadingScreen;
                    LoadNextLevel(); // Reload the level
                }

                break;
        }

        base.Update(gameTime);

        _previousKeyboardState = currentKeyboardState;
    }

    void DrawHud(GameTime gameTime, Rectangle rect, Vector2 scale)
    {
        // Apply a globl scale to make sure all the HUD elements are scaled correctly
        // This is useful for different screen resolutions and aspect ratios.
        // The scale is based on the original resolution of 1280x720.
        _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(scale.X, scale.Y, 0f) * Matrix.CreateTranslation(new Vector3(rect.X, rect.Y, 0)));

        _spriteBatch.Draw(_coinTexture, new Rectangle(10, 10, 100, 100), Color.White);
        _spriteBatch.DrawString(_font, $"{_player.Score}", new Vector2(110, 30), Color.White);
        if (_goal.GoalReached)
        {
            var textSize = _font.MeasureString("Level Complete!");
            _spriteBatch.DrawString(_font, "Level Complete!", new Vector2((GameConstants.BASE_RESOLUTION_WIDTH / 2) - (textSize.X / 2), (GameConstants.BASE_RESOLUTION_HEIGHT / 2) - (textSize.Y / 2)), Color.White);
        }
#if DEVMODE
        if (_debugFlags.HasFlag(DebugFlags.ShowMetrics))
        {
            // Draw any additional metrics here
            _spriteBatch.DrawString(_debugFont, $"FPS: {1f / (float)gameTime.ElapsedGameTime.TotalSeconds:0.00}", new Vector2(10, 110), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Time Scale: {TimeScale:0.00}", new Vector2(10, 130), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Entities: {_entities.Count}", new Vector2(10, 150), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Clear: {GraphicsDevice.Metrics.ClearCount}", new Vector2(10, 170), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Draw: {GraphicsDevice.Metrics.DrawCount}", new Vector2(10, 190), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Primitives: {GraphicsDevice.Metrics.PrimitiveCount}", new Vector2(10, 210), Color.White);
            _spriteBatch.DrawString(_debugFont, $"Sprites: {GraphicsDevice.Metrics.SpriteCount}", new Vector2(10, 230), Color.White);
        }
#endif
        _spriteBatch.End();
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
                GraphicsDevice.Clear(Color.Black);

                _spriteBatch.Begin(transformMatrix: Matrix.CreateScale(uiScale.X, uiScale.Y, 0f));

                // Draw splash texture centered on screen
                destinationRectangle = new Rectangle(
                    ((int)GameConstants.BASE_RESOLUTION_WIDTH - _splashTexture.Width) / 2,
                    ((int)GameConstants.BASE_RESOLUTION_HEIGHT - _splashTexture.Height) / 2,
                    _splashTexture.Width,
                    _splashTexture.Height);

                _spriteBatch.Draw(_splashTexture, destinationRectangle, Color.White);
                
                _spriteBatch.DrawString(_font, loadingText[(int)(_loadingTimer / 0.5f) % loadingText.Length], new Vector2((GameConstants.BASE_RESOLUTION_WIDTH / 2) - 50, GameConstants.BASE_RESOLUTION_HEIGHT - 100), Color.Purple);

                _spriteBatch.End();
                break;

            case GameState.MenuScreen:
                break;

            case GameState.PauseScreen:
            case GameState.MainScene:

                // Draw the shadow map.
                {
                    _drawList.Clear();
                    _drawList.AddRange(_entities);
                    _drawList.Add(_player);
                    _drawList.Add(_dust);

                    // Draw closest to the camera first.
                    var cameraPos = _shadowProcessor.LightPosition0;
                    _drawList.Sort((a, b) =>
                    {
                        var dista = Vector3.DistanceSquared(a.Position, cameraPos);
                        var distb = Vector3.DistanceSquared(b.Position, cameraPos);
                        return dista.CompareTo(distb);
                    });

                    _shadowProcessor.BeginShadowMapPass(0);
                    foreach (var entity in _drawList)
                        if (!entity.CastPlacementShadow)
                            _shadowProcessor.DrawEntityToShadowMap(entity);
                    _shadowProcessor.BeginShadowMapPass(1);
                    foreach (var entity in _drawList)
                        if (entity.CastPlacementShadow)
                            _shadowProcessor.DrawEntityToShadowMap(entity);

                    _shadowProcessor.EndShadowMapPass();
                }

                _postProcessor.BeginScene();

                // Draw main scene
                {
                    GraphicsDevice.Clear(_skyColor);

                    _drawList.Clear();
                    _drawList.AddRange(_entities);
                    _drawList.Add(_player);

                    // Draw closest to the camera first.
                    var cameraPos = _camera.Position;
                    _drawList.Sort((a, b) =>
                    {
                        var dista = Vector3.DistanceSquared(a.Position, cameraPos);
                        var distb = Vector3.DistanceSquared(b.Position, cameraPos);
                        return dista.CompareTo(distb);
                    });

                    // First draw the opaque pass.
                    foreach (var entity in _drawList)
                    {
                        if (entity.Model is null)
                            continue;

                        _shadowProcessor.DrawModelWithShadow(entity, _camera, false);
                    }

                    // Now draw the transparent objects reversing the list furthest to closest.
                    _drawList.Reverse();
                    foreach (var entity in _drawList)
                    {
                        if (entity.Model is null)
                            continue;

                        _shadowProcessor.DrawModelWithShadow(entity, _camera, true);
                    }

                    _dust.Draw(GraphicsDevice, _spriteBatch, _camera);
                }
#if DEVMODE
                if (_debugFlags.HasFlag(DebugFlags.ShowCollisionMesh))
                {
                    foreach (var entity in _entities)
                    {
                        entity.Draw(GraphicsDevice, _spriteBatch, _camera);
                    }
                    _player.Draw(GraphicsDevice, _spriteBatch, _camera);
                }
#endif
                // Draw all 2D particle effects.
                {
                    // Enable alpha blending and disable depth writing (but keep depth testing)
                    _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.DepthRead, RasterizerState.CullCounterClockwise);
                    foreach (var entity in _entities)
                    {
                        entity.DrawBillboards(GraphicsDevice, _spriteBatch, _camera);
                    }

#if DEVMODE
                    if (_debugFlags.HasFlag(DebugFlags.ShowCollisionMesh))
                        _player.DrawBillboards(GraphicsDevice, _spriteBatch, _camera);

#endif
                    _spriteBatch.End();
                }

                _postProcessor.EndScene();

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
                    // TODO: Draw a pause overlay
                }
                break;
        }

        base.Draw(gameTime);
    }
}
