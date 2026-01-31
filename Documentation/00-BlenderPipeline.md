# Blender Pipeline

This document describes the Blender export pipeline process for this project, focusing on how to export FBX assets (including meshes and animations) with the correct settings.

## Level Editing

The entire game is made up of these building blocks of meshes.

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
When you open the `.blend` file it will ask if you want to run the `AddMenu` script.
This script adds a `Platformer->Run Platformer Export` menu to the Blender menu to make it easy to export to the game.

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
`Scene` collection and export their properties to a `.json` file.
The name of the file is taken from the collection nanme. The script will also produce
a `levels.json` file which contains a list of all the levels in the game.
This will allow you to easily add new levels.

## Exporting FBX from Blender

Exporting FBX files from Blender can be a bit complicated. However, if you know which settings to use, things get a lot easier. When you have the object you want to export selected, go to `File->Export->FBX` and use the following settings

|Step|Blender Option|Value|Details|
|---|---|---|---|
|1|Path Mode|Strip Path|This will remove the path completely and only leave the texture filename. This means the texture MUST be in the same directory as the .fbx. But it is better than trying to figure out how to deal with a relative path.|
|2|Selected Objects|Checked|Check Selected Objects to only export the things you have selected **(optional)**.|
|3|Object Types|Only export "Mesh" objects|Unless you have animations in which case select "Mesh" and "Armature", you can ignore everything else. Only check the "Custom Properties" if you want to use custom properties in your Model Processor.|
|4|Geometry/Smoothing|Edge|Set the Smoothing to Edge (depends on personal preference).|
|5|Geometry/Triangulate Faces|Checked|You probably have Quads on your model. Use this option to convert them to triangles on export.|
|6|Armature FBX/Add Leaf Bones|Unchecked|If you are exporting animations, be sure to uncheck "Add Leaf Bones".|
|7|Animation/NLA Strips|Checked|For animations you only need to export "NLA Strips". Be sure that your animations have been pushed to the NLA Strip.|

> [!NOTE]
> Make sure to check the `Object Types` setting depending on what you want to export, as shown above:
>
> - Only "Mesh" for No Animation
> - Select "Mesh and Armature" to include Animation in the export.

Those are the settings that were used by this project. See the screenshots below for details.

![Blender Export Settings](/Images/BlenderFBXSettings.png)
![Blender Animation Export Settings](/Images/BlenderFBXSettingsAnim.png)

## Debugging Blender Export Script

First install the required [extension](https://marketplace.visualstudio.com/items?itemName=JacquesLucke.blender-development).

The run Ctrl+Shift+P (Cmd+Shift+P on Mac), `Blender: Start` to start blender and attach the debugger.
Note: You will be asked for the path to Blender.

Then open the `ExportScript.py` in VSCode and place a breakpoint. Then run Ctrl+Shift+P,`Blender: Run Script`. You will now be debugging the script.
