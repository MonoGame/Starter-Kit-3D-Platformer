// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.md', which is part of this source code package.

using System;

public static class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new PlatformerGame();
        game.Run();
    }
}
