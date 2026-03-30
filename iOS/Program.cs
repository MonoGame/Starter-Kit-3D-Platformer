// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using Foundation;
using UIKit;

namespace Platformer3D.iOS;

[Register("AppDelegate")]
internal class Program : UIApplicationDelegate
{
    private static PlatformerGame game;

    internal static void RunGame()
    {
        game = new PlatformerGame();
        game.Run();
    }

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(Program));
    }

    public override void FinishedLaunching(UIApplication app)
    {
        RunGame();
    }
}