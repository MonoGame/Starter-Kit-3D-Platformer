# Starter Kit 3D Platformer

This repository has a basic template for a 3D platformer game.
The [original](https://github.com/KenneyNL/Starter-Kit-3D-Platformer) was written by [Kenney](https://www.kenney.nl/starter-kits) for Godot 4.3 (stable).

Includes features like;

- Character controller (with double jump)
- Collectable coins and falling platforms
- Camera controls (rotate, zoom)
- Gamepad support
- Sprites and 3D Models _(CC0 licensed)_
- Sound effects _(CC0 licensed)_

## Screenshot

![Screenshot of the 3D platformer game](ScreenShots/screenshot.png)

## Controls

- Standard keyboard controls (WASD, Space), plus GamePad support
- Camera controls (Arrows to move), Comma/Period Zoom

## Level Editing

This sample uses Blender to create levels. The entire game is made up of these
building blocks of meshes.

- cloud
- coin
- grass
- platform-falling
- platform-grass
- platform-large
- platform-medium
- platform
- flag
- jumppad

All of these have already been imported into the `level.blend` file.
To layout more platforms, select one of the platforms you want to add and
duplicate it (Shift+D). You can then move it to the desired location.

We use custom properties in blender to all us to flag an object as having
special properties. For example in the example you will now that the `Empty`
which is used for the spawn point has an `IsSpawnPoint` boolean custom property.
When using an `Empty` you can change the Display Type to be a sphere and alter
the radius if you want to define an area. This can be useful for things like
goals or trigger points.

Supported custom properties:

- `IsGoal`: Add this to an `Empty` and set it to `true` to make this the end goal of a level.
- `IsSpawnPoint`: Add this to an `Empty` and set it to `true` to make this the spawn point of the level.
- `IsCollidable`: Add this to any object to control if this mesh should collide with the player. This can be useful for things such as grass which the player can move through.
- `JumpForce`: A float value, this defines the amount of force to apply when the player
touches it. Usually use in conjunction with the `jumppad` mesh.

Once you have organised your level you can go to the scripting tab in blender
and run the provided script. This script will examine all the objects in the
`Scene` collection and export their properties to the `level.json` file.
If you want to export to a different file you can change the name in the script.

## License

MIT License

Copyright (c) 2024 Kenney

Copyright (c) 2025 MonoGame Foundation

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

Assets included in this package (2D sprites, 3D models and sound effects) are [CC0 licensed](https://creativecommons.org/publicdomain/zero/1.0/)
