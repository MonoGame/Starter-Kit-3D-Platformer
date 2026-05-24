import bpy
import os
import math
import mathutils
import json
import subprocess

blend_file_path = bpy.data.filepath
blend_file_dir = os.path.dirname(blend_file_path)
repo_root = os.path.abspath(os.path.join(blend_file_dir, ".."))
asset_dir = os.path.join(repo_root, "Content", "Assets", "Levels")
os.makedirs(asset_dir, exist_ok=True)

# Example function to get the object name of the instancer
def get_instancer_object_name(obj):
    if obj.is_instancer:
        if obj.parent:
            if obj.parent.is_instancer:
                return obj.parent.name
    return None
    
def is_linked_duplicate(obj):
    # Check if the object is an instancer
    if obj.is_instancer:
        # Check if the object has a parent
        if obj.parent:
            # Check if the parent is an instancer
            if obj.parent.is_instancer:
                return True
    return False

def get_rotation_degrees(obj):
    if obj:
        # Get rotation mode
        if obj.rotation_mode == 'QUATERNION':
            # Convert quaternion to euler
            euler = obj.rotation_quaternion.to_euler()
        else:
            euler = obj.rotation_euler
            
        # Convert from radians to degrees
        x = math.degrees(euler.x)
        y = math.degrees(euler.y)
        z = math.degrees(euler.z)
        
        return (x, y, z)
    return None

# Convert to HEX
def rgb_to_hex(rgb):
    return "#{:02x}{:02x}{:02x}".format(
        int(rgb[0]*255),
        int(rgb[1]*255),
        int(rgb[2]*255)
    )
    
world = bpy.context.scene.world
if world.use_nodes and world.node_tree:
    # For rendered backgrounds using nodes (Cycles/Eevee)
    bg_node = None
    for node in world.node_tree.nodes:
        if node.type == 'BACKGROUND':
            bg_node = node
            break
    if bg_node:
        wcolor = bg_node.inputs['Color'].default_value[:3]  # (R, G, B)
    else:
        wcolor = world.color[:3]
else:
    # For simple viewport backgrounds
    wcolor = world.color[:3]  # (R, G, B)

levels_data = []

for collection in bpy.data.collections:

    #if not collection.hide_viewport:
    #    continue
    if collection.name.startswith("Models"):
            continue

    print (collection.name)
    
    if collection.name.startswith("Level"):
        levels_data.append(collection.name.lower())
    
    objects_data = []

    # NOTE: Blender uses a Z up coordinate system
    # MonoGame used a Y up coordinate system, so we need to switch the values.
    background = collection.get("Background")
    if not background:
        background = rgb_to_hex(rgb = wcolor)
    scene_data = {
        "type": 'SCENE',
        "background": background
    }
    objects_data.append(scene_data)

    for obj in collection.all_objects:
        name = obj.name
        print (name)
        position = obj.location * 100
        rotation = get_rotation_degrees(obj)
        matrix_world = obj.matrix_world
        scale = obj.scale
        if obj.type == 'CURVE':
            continue
        if obj.parent:
            continue
        if obj.type == 'EMPTY' and name.split('.')[0] == 'character':
            object_data = {
                "name" : name,
                "type": 'MESH',
                "instanceof": 'character',
                "position": [position.x, position.z, -position.y],
                "rotation": [round (rotation[0]), round (rotation[1]), round (rotation[2])],
            }
            objects_data.append (object_data)
            continue
        if obj.type == 'CAMERA':
            # Get the rotation as a matrix
            rot_matrix = obj.matrix_world.to_3x3()

            # Blender's camera looks down its local -Z axis in object space
            forward_local = (0.0, 0.0, -1.0)
            forward_world = rot_matrix @ mathutils.Vector(forward_local)

            # Normalize to get a unit vector
            direction = forward_world.normalized()
            object_data = {
                "name" : name,
                "type": 'CAMERA',
                "position": [position.x, position.z, -position.y],
                "direction" : [direction.x, direction.z, -direction.y],
                "rotation": [round (rotation[0]), round (rotation[1]), round (rotation[2])],
                "fov": math.degrees(obj.data.angle)
            }
            objects_data.append (object_data)
            continue
        if obj.type == 'LIGHT':
            object_data = {
                "name" : name,
                "type": 'LIGHT',
                "position": [position.x, position.z, -position.y],
                "color": rgb_to_hex(rgb = obj.data.color),
                "intensity": obj.data.energy
            }
            objects_data.append (object_data)
            continue
        if obj.type == 'EMPTY':
            isSpawnPoint = obj.get('IsSpawnPoint', False)
            if isSpawnPoint:
                object_data = {
                    "name" : name,
                    "type": 'SPAWNPOINT',
                    "position": [position.x, position.z, -position.y],
                    "rotation": [round (rotation[0]), round (rotation[1]), round (rotation[2])],
                }
                objects_data.append (object_data)
            isGoal = obj.get('IsGoal', False)
            if isGoal:
                radius = 100
                if obj.empty_display_type == 'SPHERE':
                    radius = obj.empty_display_size * 100
                object_data = {
                    "name" : name,
                    "type": 'GOAL',
                    "position": [position.x, position.z, -position.y],
                    "radius" : radius
                }
                objects_data.append (object_data)
            continue
        isCollidable = obj.get('IsCollidable', True)
        jumpForce = obj.get('JumpForce', 0.0)
        object_data = {
            "name": name,
            "type": obj.type,
            "instanceof" : name.split('.')[0],
            "position": [position.x, position.z, -position.y],
            "rotation": [round (rotation[0]), round (-rotation[1]), round (rotation[2])],
            "scale": [scale.x, scale.z, scale.y],
            "collidable": isCollidable,
        }
        if jumpForce != 0.0:
            object_data["jumpforce"] = jumpForce
            
        if name.split('.')[0].startswith("platform-moving"):
            maxMove = obj.get("MaxMove")
            minMove = obj.get("MinMove")
            if minMove:
                object_data["minmove"] = [minMove[0] * 100, minMove[1] * 100, minMove[2] * 100]
            if maxMove:
                object_data["maxmove"] = [maxMove[0] * 100, maxMove[1] * 100, maxMove[2] * 100]

        moveSpeed = obj.get("MoveSpeed")
        if moveSpeed:
            object_data["movespeed"] = moveSpeed * 10
        curve = obj.get("Path")
        if curve:
            splines = []
            for spline in curve.splines:
                points = []
                for sp in spline.points:
                    x, y, z, w = sp.co
                    local_co = mathutils.Vector((x, y, z))
                    world_co = obj.matrix_world @ local_co
                    lp = world_co * 100
                    point = {
                        "point": [lp.x, lp.z, -lp.y],
                    }
                    points.append(point)
                spline_data = {
                    "type" : spline.type,
                    "points" : points,
                }
                splines.append(spline_data)
            object_data["splines"] = splines
        objects_data.append(object_data)

    output_path = os.path.join(asset_dir, collection.name.lower() + ".json")
    with open(output_path, 'w') as file:
        json.dump(objects_data, file, indent=4)

levels_path = os.path.join(asset_dir, "levels.json")
with open(levels_path, 'w') as file:
    json.dump(levels_data, file, indent=4)

# Optional: run the content and game build so the new level is compiled immediately.
command = ["dotnet", "build", "Platforms/Desktop/Desktop.csproj"]
workingdir = repo_root
my_env = os.environ.copy()
dotnet_dir = "/usr/local/share/dotnet"
if os.path.isdir(dotnet_dir):
    my_env["PATH"] = dotnet_dir + os.pathsep + my_env.get("PATH", "")

# Run the command
try:
    print("Executing:", command)
    print("Writing level assets to:", asset_dir)
    result = subprocess.run(command, env=my_env, cwd=workingdir, check=True, capture_output=True, text=True)
    print("STDOUT:", result.stdout)
    print("STDERR:", result.stderr)
except subprocess.CalledProcessError as e:
    print(f"Command failed with exit code {e.returncode}")
    print("STDERR:", e.stderr)
except subprocess.FileNotFoundError as fnf:
    print(f"Command failed: {fnf}")