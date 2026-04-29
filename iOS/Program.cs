// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Foundation;
using UIKit;

[Register("AppDelegate")]
class AppDelegate : UIApplicationDelegate
{
    private PlatformerGame _game;

    public override void FinishedLaunching(UIApplication app)
    {
        _game = new PlatformerGame();
        _game.Run();
    }

    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
