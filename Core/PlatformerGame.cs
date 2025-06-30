// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class PlatformerGame : Game
{
    private enum GameState
    {
        SplashScreen,
        MainScene
    }

#if DEBUG
    [Flags]
    private enum DebugFlags
    {
        None = 0,
        ShowCollisionMesh = 1 << 0,
        ShowRenderTargets = 1 << 1,
    }
    #endif
    
    private GameState _currentState = GameState.SplashScreen;
    private Texture2D _splashTexture;
    private Texture2D _coinTexture;
    private float _splashTimer = 0f;
    private const float SplashDurationInSeconds = 3f; // 3 seconds
    private readonly Color _skyColor = new Color(0.752941f, 0.776471f, 0.827451f);
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    /// <summary>
    /// A scale to gameplay time for debugging.
    /// </summary>
    public static float TimeScale = 1f;

    private SpriteFont _font;

    private List<Entity> _entities = new List<Entity>();
    private Queue<Entity> _entitiesToRemove = new Queue<Entity>();

    private Camera _camera;
    private Player _player;

    private Goal _goal;
    private Dust _dust; 
    private PostProcessor _postProcessor;
    private ShadowProcessor _shadowProcessor;

    CollisionMesh _collisionMesh;

    #if DEBUG
    private DebugFlags _debugFlags = DebugFlags.None;
    #endif

    private KeyboardState _previousKeyboardState = new KeyboardState();
    

    public PlatformerGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
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
        _shadowProcessor.LightPosition = new Vector3(10, 20, 10);
        _shadowProcessor.SpecularIntensity = 10f;
        _shadowProcessor.Shininess = 160.0f;
        _splashTexture = Content.Load<Texture2D>("splash-screen");
        _coinTexture = Content.Load<Texture2D>("Textures/coin");
        _font = Content.Load<SpriteFont>("Font/hud");
        _dust = new Dust(Content.Load<Model>("Models/dust"), Content);

        LoadLevel ();
    }

    private void LoadLevel()
    { 
        _entities.Clear();
        _entitiesToRemove.Clear();
        var loader = new LevelLoader(Content);
        Vector3 lightPosition = new Vector3(100, 200, 100);
        loader.LoadLevel("level", _entities, ref lightPosition);
        _shadowProcessor.LightPosition = lightPosition;
        _shadowProcessor.SpecularIntensity = 0.1f;
        _shadowProcessor.Shininess = 0.5f;

        _camera = new Camera(GraphicsDevice)
        {
            Position = new Vector3(0, 0, 0),
            Target = Vector3.Zero,
            UpDirection = Vector3.Up
        };

        var spawnPoint = _entities.Find (e => e is SpawnPoint);
        _goal = _entities.Find (e => e is Goal) as Goal;

        _player = new Player(GraphicsDevice, Content.Load<Model>("Models/character"),Content)
        {
            Position = spawnPoint.Position,
            Rotation = spawnPoint.Rotation,
        };

        var platform = _entities.Find(e => e is Platform p && p.Rotation != Quaternion.Identity) as Platform;
        _collisionMesh = new CollisionMesh(platform);
        _collisionMesh.GenerateFromModel(platform.Model);
        _collisionMesh.UpdateWorldCollisionMesh();
        _collisionMesh.ShowCollisionMesh = true;
    }


    protected override void Update(GameTime gameTime)
    {
        var currentKeyboardState = Keyboard.GetState();
        var gamePadState = GamePad.GetState(PlayerIndex.One);
        if (gamePadState.Buttons.Back == ButtonState.Pressed || currentKeyboardState.IsKeyDown(Keys.Escape))
            Exit();

        // Handle debug flags toggling
        #if DEBUG 
        if (currentKeyboardState.IsKeyDown(Keys.F1) && _previousKeyboardState.IsKeyUp(Keys.F1))
        {
            _debugFlags ^= DebugFlags.ShowCollisionMesh;
        }
        if (currentKeyboardState.IsKeyDown(Keys.F2) && _previousKeyboardState.IsKeyUp(Keys.F2))
        {
            _debugFlags ^= DebugFlags.ShowRenderTargets;
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
                    _currentState = GameState.MainScene;
                }
                break;

            case GameState.MainScene:
                // Handle main scene logic
                // 

                // We use a scaled time here mostly for testing/debugging.
                var scaledTime = new GameTime(gameTime.TotalGameTime,
                    TimeSpan.FromSeconds(gameTime.ElapsedGameTime.TotalSeconds * TimeScale));

                _player.Forward = _camera.ForwardDirection;
                _player.Update(scaledTime);
                _dust.Update(scaledTime);
                if (_player.IsMoving && !_player.IsJumping)
                {
                    _dust.AddDust(scaledTime, _player.Position);
                }
                if (_player.Dead())
                {
                    _currentState = GameState.SplashScreen;
                    _splashTimer = 0f; // Reset splash timer
                    LoadLevel(); // Reload the level
                }
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
                _camera.Target = _player.Position;
                _camera.Update(scaledTime);
                _collisionMesh.UpdateWorldCollisionMesh();
                _shadowProcessor.TargetPosition = _player.Position;

                if (_goal.Complete)
                {
                    _currentState = GameState.SplashScreen;
                    _splashTimer = 0f; // Reset splash timer
                    LoadLevel(); // Reload the level
                }
                break;
        }

        base.Update(gameTime);

        _previousKeyboardState = currentKeyboardState;
    }

    void DrawHud()
    {
        _spriteBatch.Begin();
        // Draw HUD elements here
        _spriteBatch.Draw (_coinTexture, new Rectangle (10, 10, 100, 100), Color.White);
        _spriteBatch.DrawString(_font, $"{_player.Score}", new Vector2(110, 30), Color.White);
        if (_goal.GoalReached)
        {
            _spriteBatch.DrawString(_font, "Level Complete!", new Vector2(500, 300), Color.White);
        }
        _spriteBatch.End();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // TODO: Add your drawing code here
        switch (_currentState)
        {
            case GameState.SplashScreen:
                // Draw splash screen
                GraphicsDevice.Clear(Color.Black);
                
                _spriteBatch.Begin();
                
                // Draw splash texture centered on screen
                Rectangle destinationRectangle = new Rectangle(
                    (GraphicsDevice.Viewport.Width - _splashTexture.Width) / 2,
                    (GraphicsDevice.Viewport.Height - _splashTexture.Height) / 2,
                    _splashTexture.Width,
                    _splashTexture.Height);
                    
                _spriteBatch.Draw(_splashTexture, destinationRectangle, Color.White);
                
                _spriteBatch.End();
                break;

            case GameState.MainScene:
                _shadowProcessor.BeginShadowMapPass();
                foreach (var entity in _entities)
                {
                    if (entity.Model is null)
                    {
                        continue;
                    }
                   _shadowProcessor.DrawEntityToShadowMap(entity, entity.WorldMatrix);
                }
                _shadowProcessor.DrawEntityToShadowMap(_player, _player.WorldMatrix);
                _shadowProcessor.DrawEntityToShadowMap(_dust, _dust.WorldMatrix);
                // Set render states
                _shadowProcessor.EndShadowMapPass();
                // Draw main scene
                _postProcessor.BeginScene();
                GraphicsDevice.Clear(_skyColor);
                
                // Set render states
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                GraphicsDevice.BlendState = BlendState.Opaque;
                GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

                _shadowProcessor.BeginShadowRender();
                foreach (var entity in _entities)
                {
                    if (entity.Model is null)
                    {
                        continue;
                    }
                    _shadowProcessor.DrawModelWithShadow(entity, entity.WorldMatrix, _camera.ViewMatrix, _camera.ProjectionMatrix, Color.White);
                }
                _player.DrawShadow(GraphicsDevice, _camera);
                _shadowProcessor.DrawModelWithShadow(_player, _player.WorldMatrix, _camera.ViewMatrix, _camera.ProjectionMatrix, Color.White);
                _dust.Draw(GraphicsDevice, _spriteBatch, _camera);

#if DEBUG
                if (_debugFlags.HasFlag(DebugFlags.ShowCollisionMesh))
                {
                    foreach (var entity in _entities)
                    {
                        entity.Draw(GraphicsDevice, _spriteBatch, _camera);
                    }
                    _player.Draw(GraphicsDevice, _spriteBatch, _camera);
                }
#endif

                // Enable alpha blending and disable depth writing (but keep depth testing)
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

                _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
                foreach (var entity in _entities)
                {
                    entity.DrawBillboards(GraphicsDevice, _spriteBatch, _camera);
                }
                _spriteBatch.End();
                _postProcessor.EndScene();
                // Draw the score etc.
                DrawHud ();
#if DEBUG
                if (_debugFlags.HasFlag(DebugFlags.ShowRenderTargets))
                {
                    _shadowProcessor.DebugDrawShadowMap(new Rectangle(0, 0, 256, 256));
                    _postProcessor.DebugDrawRenderTargets(new Rectangle(256, 0, 256, 256));
                }
#endif
                break;
        }

        base.Draw(gameTime);
    }
}
