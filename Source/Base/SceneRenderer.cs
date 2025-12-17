// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Renders the scene and handles shadow mapping for the scene.
/// This process created two RenderTargets to store the shadow maps.
/// One is for the directional or "sun" light, the other is for the player shadow
/// which appears directly below the player. This is done to aide jumping and 
/// is common in platformer games.
/// </summary>
public class SceneRenderer
{
    private GraphicsDevice _graphicsDevice;
    private Effect _shadowEffect;
    private SpriteBatch _spriteBatch;
    private readonly RenderTarget2D[] _shadowMaps = new RenderTarget2D[2];
    private readonly Matrix[] _lightViewMatrix = new Matrix[2];
    private Matrix _lightProjectionMatrix;
    private int _shadowIndex;

    // Shadow map resolution
    private int _shadowMapSize = 2048;

    // Light properties
    public Vector3 LightDirection { get; set; }

    /// <summary>
    /// The sun light position.
    /// </summary>
    public Vector3 LightPosition0
    {
        get
        {
            return TargetPosition + (LightDirection * 1500.0f);
        }
    }

    /// <summary>
    /// The placement light position used for players/enemies/coins.
    /// </summary>
    public Vector3 LightPosition1
    {
        get
        {
            return TargetPosition + (Vector3.Up * 1500.0f);
        }
    }

    /// <summary>
    /// The specular intensity of the light.
    /// </summary>
    public float SpecularIntensity { get; set; }

    /// <summary>
    /// The shininess of the light.
    /// </summary>
    public float Shininess { get; set; }

    /// <summary>
    /// The intensity of the sun light.
    /// </summary>
    public float SunIntensity { get; set; }

    /// <summary>
    /// The color of the sun light.
    /// </summary>
    public Vector3 SunColor { get; set; }

    /// <summary>
    /// The target position of the light.
    /// </summary>
    public Vector3 TargetPosition { get; set; }

    /// <summary>
    /// The up vector of the light.
    /// </summary>
    public Vector3 UpVector { get; set; } = Vector3.Up;

    /// <summary>
    /// Initializes a new instance of the <see cref="SceneRenderer"/> class.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device.</param>
    /// <param name="spriteBatch">The sprite batch.</param>
    public SceneRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        CreateRenderTargets();

        // Set default light properties
        LightDirection = Vector3.Normalize(new Vector3(10, 20, 10));
        SpecularIntensity = 0f;
        Shininess = 0.0f;
        SunColor = new Vector3(1.0f, 0.9f, 0.9f);
        SunIntensity = 1.1f;
    }

    /// <summary>
    /// Creates the render targets for shadow mapping.
    /// </summary>
    private void CreateRenderTargets()
    {
        _shadowMaps[0] = new RenderTarget2D(
            _graphicsDevice,
            _shadowMapSize,
            _shadowMapSize,
            false,
            SurfaceFormat.Single,
            DepthFormat.Depth24);

        _shadowMaps[1] = new RenderTarget2D(
            _graphicsDevice,
            _shadowMapSize,
            _shadowMapSize,
            false,
            SurfaceFormat.Single,
            DepthFormat.Depth24);
    }

    /// <summary>
    /// Loads the content for the shadow processor.
    /// </summary>
    /// <param name="content">The content manager.</param>
    public void LoadContent(ContentManager content)
    {
        _shadowEffect = content.Load<Effect>("Effects/ShadowEffect");
    }

    /// <summary>
    /// Updates the light view and projection matrices.
    /// </summary>
    private void UpdateLightMatrices()
    {
        // Create view matrix from light's perspective
        _lightViewMatrix[0] = Matrix.CreateLookAt(
            LightPosition0,
            TargetPosition, // look at target
            UpVector);

        _lightViewMatrix[1] = Matrix.CreateLookAt(
            LightPosition1,
            TargetPosition,
            Vector3.Forward);

        // Create orthographic projection for directional light
        _lightProjectionMatrix = Matrix.CreateOrthographic(
            2048, 2048, 0.1f, 5000f);
    }

    /// <summary>
    /// Begins the shadow map pass for the specified light index.
    /// By default index 0 is the global "scene" light.
    /// index 1 is the player shadow light.
    /// </summary>
    /// <param name="index">The light index.</param>
    public void BeginShadowMapPass(int index)
    {
        _shadowIndex = index;

        // Update light matrices based on current light position.
        UpdateLightMatrices();

        // Set render target to shadow map.
        _graphicsDevice.SetRenderTargets(_shadowMaps[index]);

        // Clear with white (meaning far depth).
        _graphicsDevice.Clear(Color.White);

        _shadowEffect.CurrentTechnique = _shadowEffect.Techniques["RenderDepth"];
    }

    /// <summary>
    /// Ends the shadow map pass. We do this by resetting the _graphicsDevice RenderTarget.
    /// At this point the ShadowMap is complete and can be used for rendering.
    /// </summary>
    public void EndShadowMapPass()
    {
        _graphicsDevice.SetRenderTarget(null);
    }

    /// <summary>
    /// Draws the entity to the shadow map.
    /// </summary>
    /// <param name="entity">The entity to draw.</param>
    public void DrawEntityToShadowMap(Entity entity)
    {
        var model = entity.Model;
        if (model == null || !entity.Visible)
            return;

        // Set the state needed to draw to the shadow map.
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        _graphicsDevice.BlendState = BlendState.Opaque;

        var world = entity.WorldMatrix;

        var modelToLight = _shadowEffect.Parameters["ModelToLight0"];
        var passes = _shadowEffect.CurrentTechnique.Passes;

        Matrix[] transforms = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(transforms);
        foreach (ModelMesh mesh in model.Meshes)
        {
            var meshWorld = transforms[mesh.ParentBone.Index] * entity.MeshTransforms[mesh.ParentBone.Index] * world;

            modelToLight.SetValue(meshWorld * _lightViewMatrix[_shadowIndex] * _lightProjectionMatrix);

            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                _graphicsDevice.Indices = part.IndexBuffer;

                foreach (EffectPass pass in passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                }
            }
        }
    }

    /// <summary>
    /// Draws the model with shadows.
    /// </summary>
    /// <param name="entity">The entity to draw.</param>
    /// <param name="camera">The camera used for rendering.</param>
    /// <param name="blendPass">Whether this is a blend pass. The Blend pass will make objects
    /// transparent if they are close to the camera.</param>
    public void DrawModelWithShadow(Entity entity, Camera camera, bool blendPass)
    {
        Model model = entity.Model;
        if (model == null || !entity.Visible)
            return;

        var color = Color.White;
        if (entity is not Player)
        {
            var FadeNear = GameConstants.SHADOW_NEAR_PLANE;
            var FadeFar = GameConstants.SHADOWN_FAR_PLANE;
            float d = Vector3.Distance(camera.Position, entity.Position);
            float alpha = 1.0f - MathHelper.Clamp((d - FadeFar) / (FadeNear - FadeFar), 0.0f, 1.0f);
            color.A = (byte)Math.Ceiling(255 * alpha);
        }

        if (color.A == 255)
        {
            if (blendPass)
                return;

            _graphicsDevice.BlendState = BlendState.Opaque;
            _graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
        else
        {
            if (!blendPass)
                return;

            _graphicsDevice.BlendState = BlendState.NonPremultiplied;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        }
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        var view = camera.ViewMatrix;
        var world = entity.WorldMatrix;

        var lp0 = Vector3.Normalize(Vector3.TransformNormal(LightPosition0, view));
        var lp1 = Vector3.Normalize(Vector3.TransformNormal(LightPosition1, view));

        Effect effect = _shadowEffect;
        // TODO: We can cache these parameters when loading the effect for better performance.
        // This will remove a lot of dictionary lookups during rendering.
        effect.CurrentTechnique = effect.Techniques["RenderTextured"];
        effect.Parameters["LightPosition0"]?.SetValue(lp0);
        effect.Parameters["LightPosition1"]?.SetValue(lp1);
        effect.Parameters["LightColor"]?.SetValue(SunColor * SunIntensity);
        effect.Parameters["AmbientIntensity"]?.SetValue(0.8f);
        effect.Parameters["Color"]?.SetValue(color.ToVector4());
        effect.Parameters["SpecularIntensity"]?.SetValue(entity.SpecularIntensity);
        effect.Parameters["Shininess"]?.SetValue(entity.Shininess);
        effect.Parameters["ShadowMap0"]?.SetValue(_shadowMaps[0]);
        effect.Parameters["ShadowMap1"]?.SetValue(_shadowMaps[1]);
        effect.Parameters["EdgeFadeScale"]?.SetValue(10.0f);

        // Don't render placement shadows on to an entity that
        // is casting a placement shadow... it looks odd.
        var shadowMask = Vector2.One;
        if (entity.CastPlacementShadow)
            shadowMask.Y = 0;
        effect.Parameters["ShadowMask"]?.SetValue(shadowMask);

        Matrix[] transforms = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(transforms);
        foreach (ModelMesh mesh in model.Meshes)
        {
            // Calculate the world matrix for the mesh
            Matrix meshWorld = transforms[mesh.ParentBone.Index] * entity.MeshTransforms[mesh.ParentBone.Index] * world;

            // Calculate all the necessary matrices
            Matrix worldViewMatrix = meshWorld * view;
            Matrix worldViewProjMatrix = meshWorld * view * camera.ProjectionMatrix;
            Matrix lightWorldViewProjMatrix0 = meshWorld * _lightViewMatrix[0] * _lightProjectionMatrix;
            Matrix lightWorldViewProjMatrix1 = meshWorld * _lightViewMatrix[1] * _lightProjectionMatrix;

            // Calculate normal matrix (inverse transpose of the world-view matrix)
            Matrix temp = worldViewMatrix;
            temp.Translation = Vector3.Zero;
            Matrix worldViewIT = Matrix.Transpose(Matrix.Invert(temp));

            // TODO: We can cache these parameters when loading the effect for better performance.
            // This will remove a lot of dictionary lookups during rendering.
            effect.Parameters["NormalToView"]?.SetValue(worldViewIT);
            effect.Parameters["ModelToScreen"]?.SetValue(worldViewProjMatrix);
            effect.Parameters["ModelToLight0"]?.SetValue(lightWorldViewProjMatrix0);
            effect.Parameters["ModelToLight1"]?.SetValue(lightWorldViewProjMatrix1);
            effect.Parameters["ModelToView"]?.SetValue(worldViewMatrix);

            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                // Get the texture from the original effect.
                BasicEffect originalEffect = part.Effect as BasicEffect;
                effect.Parameters["Texture"]?.SetValue(originalEffect.Texture);

                _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                _graphicsDevice.Indices = part.IndexBuffer;

                foreach (EffectPass pass in _shadowEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                }
            }
        }
    }

    /// <summary>
    /// Optional: Utility method to visualize the shadow map for debugging
    /// </summary>
    /// <param name="destination">The destination rectangle for the shadow map visualization</param>
    public void DebugDrawShadowMap(Rectangle destination)
    {
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
        _spriteBatch.Draw(_shadowMaps[0], destination, Color.White);
        destination.Offset(destination.Width + 10, 0);
        _spriteBatch.Draw(_shadowMaps[1], destination, Color.White);
        _spriteBatch.End();
    }
}
