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
        Viewport viewport = _graphicsDevice.Viewport;
        Rectangle fullscreen = new Rectangle(0, 0, viewport.Width, viewport.Height);
        // Extract bright areas for bloom
        _graphicsDevice.SetRenderTarget(_bloomExtractTarget);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["BloomExtract"];
        _bloomEffect.Parameters["BloomThreshold"].SetValue(0.8f);
        _bloomEffect.CurrentTechnique.Passes[0].Apply();
        _spriteBatch.Draw(_mainRenderTarget, fullscreen, Color.White);
        _spriteBatch.End();

        // Blur the bloom
        _graphicsDevice.SetRenderTarget(_bloomBlurTarget);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["GaussianBlur"];
        _bloomEffect.Parameters["BlurAmount"].SetValue(new Vector2(1.0f, 0.0f));
        _bloomEffect.CurrentTechnique.Passes[0].Apply();
        _spriteBatch.Draw(_bloomExtractTarget, fullscreen, Color.White);
        _spriteBatch.End();

        // Final composite with vignette
        _graphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        
        // Draw main scene
        _spriteBatch.Draw(_mainRenderTarget, fullscreen, Color.White);
        
        // Overlay bloom
        _spriteBatch.Draw(_bloomBlurTarget, fullscreen , Color.White * 0.8f);
        
        // Apply vignette
        _vignetteEffect.Parameters["Radius"].SetValue(new Vector2(0.8f, 0.8f));
        _vignetteEffect.Parameters["Center"].SetValue(new Vector2(0.5f, 0.5f));
        _vignetteEffect.Parameters["Smoothness"].SetValue(0.5f);
        _vignetteEffect.CurrentTechnique.Passes[0].Apply();
        _spriteBatch.Draw(_mainRenderTarget, fullscreen, Color.White * 0.3f);

        // _spriteBatch.Draw(_bloomBlurTarget, new Rectangle (0,0,200,200), Color.White);
        // _spriteBatch.Draw(_mainRenderTarget, new Rectangle (200,0,200,200), Color.White);
        // _spriteBatch.Draw(_bloomBlurTarget, new Rectangle (400,0,200,200), Color.White);
        
        _spriteBatch.End();
    }
}