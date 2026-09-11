// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Foundation;
using Microsoft.Xna.Framework.Content;
using UIKit;

[Register("AppDelegate")]
class AppDelegate : UIApplicationDelegate
{
    private PlatformerGame _game;

    public override void FinishedLaunching(UIApplication app)
    {
        // With iOS, due to AOT (Ahead-Of-Time) compilation, we need to manually register all content type readers.
        ContentTypeReaderManager.AddTypeCreator(typeof(ReflectiveReader<AnimationData>).FullName,
            () => new ReflectiveReader<AnimationData>());
        ContentTypeReaderManager.AddTypeCreator(typeof(ReflectiveReader<ConvexHull>).FullName,
            () => new ReflectiveReader<ConvexHull>());
        ContentTypeReaderManager.AddTypeCreator(typeof(ReflectiveReader<ModelData>).FullName,
            () => new ReflectiveReader<ModelData>());
            
        _game = new PlatformerGame();
        _game.Run();
    }

    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
