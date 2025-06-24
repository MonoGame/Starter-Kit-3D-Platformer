using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class Cloud : BobingEntity
{
    
    public Cloud(Model model, ContentManager contentManager) : base(model, contentManager)
    {
        SpecularIntensity = 0.1f;
        Shininess = 0.5f;
    }
}