"""Reopen the saved scene; render an exact side view and previous-work baseline."""
from pathlib import Path
import bpy, sys, json
from mathutils import Vector
OUT=Path(__file__).resolve().parent
mode=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'side'
if mode=='baseline':
    bpy.ops.wm.open_mainfile(filepath=str(OUT.parent/'tutorial-probe-2026-09-21/hammer_lesson.blend'))
    sc=bpy.context.scene;cam=sc.camera
    cam.location=(6,-4,2.6);cam.rotation_euler=(Vector((0,-.19,1.20))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=3.05
    sc.render.resolution_x=720;sc.render.resolution_y=720;sc.render.resolution_percentage=100
    dest=OUT/'baseline_frames'
else:
    bpy.ops.wm.open_mainfile(filepath=str(OUT/'st_hizuru_weight_study.blend'))
    sc=bpy.context.scene;cam=sc.camera
    if mode=='side':
        cam.location=(6,0,1.2);cam.rotation_euler=(Vector((0,0,1.2))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=3.05
        dest=OUT/'side_frames'
    elif mode=='portrait':
        sc.frame_set(1);cam.location=(3,-6,2.3);cam.rotation_euler=(Vector((0,0,1.05))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.20
        sc.render.filepath=str(OUT/'character_study.png');bpy.ops.render.render(write_still=True);raise SystemExit(0)
    else: raise ValueError(mode)
dest.mkdir(exist_ok=True)
for f in range(1,61):
    sc.frame_set(f);sc.render.filepath=str(dest/f'{f:04d}.png');bpy.ops.render.render(write_still=True)
