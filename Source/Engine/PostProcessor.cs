// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class PostProcessor
{
    private GraphicsDevice _graphicsDevice;
    private RenderTarget2D _mainRenderTarget;
    private RenderTarget2D _bloomExtractTarget;
    private RenderTarget2D _bloomBlurTarget;
    private Effect _bloomEffect;
    private Effect _vignetteEffect;
    private SpriteBatch _spriteBatch;

    public PostProcessor(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        CreateRenderTargets();
    }

    private void CreateRenderTargets()
    {
        _mainRenderTarget = new RenderTarget2D(
            _graphicsDevice,
            _graphicsDevice.PresentationParameters.BackBufferWidth,
            _graphicsDevice.PresentationParameters.BackBufferHeight,
            false,
            _graphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24);

        _bloomExtractTarget = new RenderTarget2D(
            _graphicsDevice,
            _graphicsDevice.PresentationParameters.BackBufferWidth / 2,
            _graphicsDevice.PresentationParameters.BackBufferHeight / 2,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);

        _bloomBlurTarget = new RenderTarget2D(
            _graphicsDevice,
            _graphicsDevice.PresentationParameters.BackBufferWidth / 2,
            _graphicsDevice.PresentationParameters.BackBufferHeight / 2,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
    }

    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        _bloomEffect = content.Load<Effect>("Effects/BloomEffect");
        _vignetteEffect = content.Load<Effect>("Effects/VignetteEffect");
    }

    public void BeginScene()
    {
        _graphicsDevice.SetRenderTarget(_mainRenderTarget);
    }

    public void EndScene()
    {
        _graphicsDevice.SetRenderTarget(null);
        ApplyPostProcessing();
    }

    private void ApplyPostProcessing()
    {
        // Extract bright areas for bloom.
        _graphicsDevice.SetRenderTarget(_bloomExtractTarget);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["BloomExtract"];
        _bloomEffect.Parameters["BloomThreshold"].SetValue(0.9f);
        _bloomEffect.CurrentTechnique.Passes[0].Apply();
        _spriteBatch.Draw(_mainRenderTarget, _bloomExtractTarget.Bounds, Color.White);
        _spriteBatch.End();

        // Blur the bloom.
        _graphicsDevice.SetRenderTarget(_bloomBlurTarget);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["GaussianBlur"];
        _bloomEffect.Parameters["TexelSize"].SetValue(2.0f / _bloomBlurTarget.Width);
        _bloomEffect.CurrentTechnique.Passes[0].Apply();
        _spriteBatch.Draw(_bloomExtractTarget, _bloomBlurTarget.Bounds, Color.White);
        _spriteBatch.End();

        // Draw main scene.
        _graphicsDevice.SetRenderTarget(null);
        var fullscreen = _graphicsDevice.Viewport.Bounds;
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
        _spriteBatch.Draw(_mainRenderTarget, fullscreen, Color.White);
        _spriteBatch.End();

        // Overlay bloom.
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive);
        _spriteBatch.Draw(_bloomBlurTarget, fullscreen, Color.White);
        _spriteBatch.End();

        // Apply vignette.
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        _vignetteEffect.Parameters["Radius"]?.SetValue(new Vector2(1.15f));
        _vignetteEffect.Parameters["Center"]?.SetValue(new Vector2(0.5f));
        _vignetteEffect.Parameters["Smoothness"]?.SetValue(0.96f);
        _vignetteEffect.CurrentTechnique.Passes[0].Apply();
        _spriteBatch.Draw(_mainRenderTarget, fullscreen, Color.Black * 0.8f);
        _spriteBatch.End();
    }

    public void DebugDrawRenderTargets(Rectangle rectangle)
    {
        _spriteBatch.Begin();
        //_spriteBatch.Draw(_mainRenderTarget, rectangle, Color.White);
        //_spriteBatch.Draw(_bloomExtractTarget, new Rectangle(rectangle.X + rectangle.Width, rectangle.Y, rectangle.Width, rectangle.Height), Color.White);
        _spriteBatch.Draw(_bloomBlurTarget, new Rectangle(rectangle.X + (rectangle.Width * 2), rectangle.Y, rectangle.Width, rectangle.Height), Color.White);
        _spriteBatch.End();
    }
}