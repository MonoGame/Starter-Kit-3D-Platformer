using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ShadowProcessor
{
    private GraphicsDevice _graphicsDevice;
    private RenderTarget2D _shadowMap;
    private Effect _shadowEffect;
    private SpriteBatch _spriteBatch;
    
    // Light properties
    public Vector3 LightPosition { get; set; }
    public float SpecularIntensity { get; set; }
    public float Shininess { get; set; }

    public Vector3 TargetPosition { get; set; }
    public Vector3 UpVector { get; set; } = Vector3.Up;
    
    // Matrices
    private Matrix _lightViewMatrix;
    private Matrix _lightProjectionMatrix;
    
    // Shadow map resolution
    private int _shadowMapSize = 2048;

    public ShadowProcessor(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        CreateRenderTargets();
        
        // Set default light properties
        LightPosition = new Vector3(10, 20, 10);
        SpecularIntensity = 0f;
        Shininess = 0.0f;
        
        // Set up default light matrices
        UpdateLightMatrices();
    }

    private void CreateRenderTargets()
    {
        _shadowMap = new RenderTarget2D(
            _graphicsDevice,
            _shadowMapSize,
            _shadowMapSize,
            false,
            SurfaceFormat.Single, // Use Single for higher precision depth values
            DepthFormat.Depth24);
    }
    
    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        _shadowEffect = content.Load<Effect>("Effects/ShadowEffect");
    }
    
    private void UpdateLightMatrices()
    {
        // Create view matrix from light's perspective
        _lightViewMatrix = Matrix.CreateLookAt(
            LightPosition,
            TargetPosition, // look at target
            UpVector);
        
        // Create orthographic projection for directional light
        _lightProjectionMatrix = Matrix.CreateOrthographic(
            1024, 1024, 0.1f, 5000f);
    }
    
    public void BeginShadowMapPass()
    {
        // Update light matrices based on current light position
        UpdateLightMatrices();

        _graphicsDevice.DepthStencilState = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferFunction = CompareFunction.LessEqual
        };
        _graphicsDevice.RasterizerState = RasterizerState.CullClockwise;
        
        // Set render target to shadow map
        _graphicsDevice.SetRenderTarget(_shadowMap);
        _graphicsDevice.Clear(Color.White); // Clear with white (meaning far depth)

        _shadowEffect.Parameters["ShadowMapSampler+ShadowMap"].SetValue(_shadowMap);
        _shadowEffect.CurrentTechnique = _shadowEffect.Techniques["RenderDepth"];
    }
    
    public void EndShadowMapPass()
    {
        _graphicsDevice.SetRenderTarget(null);
    }

    public void BeginShadowRender ()
    {
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.DepthStencilState = new DepthStencilState
        {
            DepthBufferEnable = true,
            DepthBufferFunction = CompareFunction.LessEqual
        };
    }
    
    public void DrawEntityToShadowMap(Entity entity, Matrix world)
    {
        var model = entity.Model;
        if (model == null)
            return;
        Matrix[] transforms = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(transforms);
        foreach (ModelMesh mesh in model.Meshes)
        {
            Matrix meshWorld =  transforms[mesh.ParentBone.Index] * entity.MeshTransforms[mesh.ParentBone.Index] * world;
            Matrix meshLightWorldViewProj = meshWorld * _lightViewMatrix * _lightProjectionMatrix;
            
            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                _graphicsDevice.SetVertexBuffer(part.VertexBuffer);
                _graphicsDevice.Indices = part.IndexBuffer;

                _shadowEffect.Parameters["ModelToLight"].SetValue(meshWorld * _lightViewMatrix * _lightProjectionMatrix);

                foreach (EffectPass pass in _shadowEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                }
            }
        }
    }
    
    public void DrawModelWithShadow(Entity entity, Matrix world, Matrix view, Matrix projection, Color color)
    {
        Model model = entity.Model;
        if (model == null)
            return;
        Matrix[] transforms = new Matrix[model.Bones.Count];
        model.CopyAbsoluteBoneTransformsTo(transforms);
        foreach (ModelMesh mesh in model.Meshes)
        {
            Matrix meshWorld = transforms[mesh.ParentBone.Index] * entity.MeshTransforms[mesh.ParentBone.Index] * world;
            
            // Calculate all the necessary matrices
            Matrix worldViewMatrix = meshWorld * view;
            Matrix worldViewProjMatrix = meshWorld * view * projection;
            Matrix lightWorldViewProjMatrix = meshWorld * _lightViewMatrix * _lightProjectionMatrix;
            
            // Calculate normal matrix (inverse transpose of the world-view matrix)
            Matrix temp = worldViewMatrix;
            temp.Translation = Vector3.Zero;
            Matrix worldViewIT = Matrix.Transpose(Matrix.Invert(temp));
            
            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                // Save original effect
                BasicEffect originalEffect = part.Effect as BasicEffect;
                
                // Apply shadow rendering effect
                Effect effect = _shadowEffect;
                effect.CurrentTechnique = effect.Techniques["RenderTextured"];
                
                // Set shader parameters
                var lp = Vector3.Normalize(Vector3.TransformNormal(LightPosition, view));
                effect.Parameters["LightPosition"].SetValue(lp);
                effect.Parameters["AmbientIntensity"].SetValue(0.8f);
                effect.Parameters["ModelToLight"].SetValue(lightWorldViewProjMatrix);
                effect.Parameters["ModelToView"].SetValue(worldViewMatrix);
                effect.Parameters["NormalToView"].SetValue(worldViewIT);
                effect.Parameters["ModelToScreen"].SetValue(worldViewProjMatrix);
                effect.Parameters["Color"].SetValue(color.ToVector4());
                effect.Parameters["SpecularIntensity"].SetValue(entity.SpecularIntensity);
                effect.Parameters["Shininess"].SetValue(entity.Shininess);
                effect.Parameters["ShadowMapSampler+ShadowMap"].SetValue(_shadowMap);
                effect.Parameters["TextureSampler+Texture"].SetValue(originalEffect.Texture);
                
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
    
    // Optional: Utility method to visualize the shadow map for debugging
    public void DebugDrawShadowMap(Rectangle destination)
    {
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
        _spriteBatch.Draw(_shadowMap, destination, Color.White);
        _spriteBatch.End();
    }
}
