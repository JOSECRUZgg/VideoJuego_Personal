"""Convierte la malla principal de un archivo Rhino .3dm a OBJ para Unity."""
from pathlib import Path
import rhino3dm

source = Path(r"C:\Users\JOSEC\Downloads\jarron.3dm")
target = Path(r"C:\Users\JOSEC\UnityShooter\Assets\Resources\Megabonk\Stage\Vase\Jarron.obj")
target.parent.mkdir(parents=True, exist_ok=True)

model = rhino3dm.File3dm.Read(str(source))
mesh = next(obj.Geometry for obj in model.Objects if isinstance(obj.Geometry, rhino3dm.Mesh))
vertices = list(mesh.Vertices)
center_x = (min(v.X for v in vertices) + max(v.X for v in vertices)) * 0.5
center_y = (min(v.Y for v in vertices) + max(v.Y for v in vertices)) * 0.5

with target.open("w", encoding="utf-8", newline="\n") as obj:
    obj.write("# Vasija convertida desde jarron.3dm\n")
    # Rhino: Z es altura. Unity: Y es altura.
    for vertex in vertices:
        obj.write(f"v {vertex.X - center_x:.6f} {vertex.Z:.6f} {vertex.Y - center_y:.6f}\n")
    for face in mesh.Faces:
        a, b, c, d = face
        if c == d:
            obj.write(f"f {a + 1} {b + 1} {c + 1}\n")
        else:
            obj.write(f"f {a + 1} {b + 1} {c + 1} {d + 1}\n")
