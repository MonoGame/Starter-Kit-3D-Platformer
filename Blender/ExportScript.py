import bpy
import os
import math
import json

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

def get_rotation_degrees(obk):
    if obj:
        # Get rotation modew
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

for collection in bpy.data.collections:

    #if not collection.hide_viewport:
    #    continue
    if not collection.name.startswith("Level"):
            continue

    print (collection.name)
    objects_data = []

    # NOTE: Blender uses a Z up coordinate system
    # MonoGame used a Y up coordinate system, so we need to switch the values.

    for obj in collection.all_objects:
        name = obj.name
        print (name)
        position = obj.location * 100
        rotation = get_rotation_degrees(obj)
        matrix_world = obj.matrix_world
        scale = obj.scale
        if obj.type == 'CURVE':
            continue
        if obj.type == 'LIGHT':
            object_data = {
                "name" : name,
                "type": 'LIGHT',
                "position": [position.x, position.z, -position.y],
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
                    local_co = sp.co
                    world_co = matrix_world @ local_co
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
        
    blend_file_path = bpy.data.filepath
    blend_file_dir = os.path.dirname(blend_file_path)

    with open(blend_file_dir + "/../Content/" + collection.name.lower() + ".json", 'w') as file:
        json.dump(objects_data, file, indent=4)