"""Convert the downloaded Sketchfab GLB to a Unity-readable FBX with textures.

Run with Blender in background mode; paths are passed after ``--``.
"""

import bpy
import bmesh
import os
import json
import struct
import sys

source, output = sys.argv[sys.argv.index("--") + 1:][:2]
texture_dir = os.path.join(os.path.dirname(output), "Textures")
os.makedirs(texture_dir, exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=source)

with open(source, "rb") as source_file:
    source_file.read(12)
    json_length, _ = struct.unpack("<II", source_file.read(8))
    gltf = json.loads(source_file.read(json_length))
    source_file.seek((-json_length) % 4, 1)
    binary_length, _ = struct.unpack("<II", source_file.read(8))
    binary = source_file.read(binary_length)

for index, image_info in enumerate(gltf.get("images", [])):
    view = gltf["bufferViews"][image_info["bufferView"]]
    extension = ".jpg" if image_info["mimeType"] == "image/jpeg" else ".png"
    target = os.path.join(texture_dir, "snowy_mountain_texture_%02d" % index + extension)
    with open(target, "wb") as texture_file:
        start = view.get("byteOffset", 0)
        texture_file.write(binary[start:start + view["byteLength"]])
    print("TEXTURE", target)
    for blender_image in bpy.data.images:
        blender_image.filepath = target

bpy.ops.object.select_all(action="DESELECT")
meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH" and len(obj.data.polygons) > 100]
for obj in meshes:
    # La descarga trae las caras del terreno mirando hacia abajo. Unity
    # ocultaba la superficie y tampoco recibía raycasts desde arriba.
    average_up = sum(face.normal.z for face in obj.data.polygons) / len(obj.data.polygons)
    if average_up < -0.3:
        mesh_edit = bmesh.new()
        mesh_edit.from_mesh(obj.data)
        bmesh.ops.reverse_faces(mesh_edit, faces=mesh_edit.faces)
        mesh_edit.to_mesh(obj.data)
        mesh_edit.free()
        obj.data.update()
        print("FLIPPED_TERRAIN_NORMALS", obj.name)
    obj.select_set(True)
if not meshes:
    raise RuntimeError("The GLB contains no meshes")
bpy.context.view_layer.objects.active = meshes[0]

from mathutils import Vector
points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
minimum = [min(point[axis] for point in points) for axis in range(3)]
maximum = [max(point[axis] for point in points) for axis in range(3)]
print("MODEL_BOUNDS", minimum, maximum)

bpy.ops.export_scene.fbx(
    filepath=output,
    use_selection=True,
    object_types={"MESH"},
    path_mode="RELATIVE",
    embed_textures=False,
    apply_unit_scale=True,
    axis_forward="-Z",
    axis_up="Y",
)
print("EXPORTED", output)
