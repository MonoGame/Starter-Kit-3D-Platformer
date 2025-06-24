using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Platform : Entity
{
    public Platform(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        SpecularIntensity = 0.1f;
        Shininess = 0.5f;
    }
}