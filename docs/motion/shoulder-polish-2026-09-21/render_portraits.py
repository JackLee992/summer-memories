from pathlib import Path
import bpy
from mathutils import Vector
OUT=Path(__file__).resolve().parent
for label,path in [('previous',OUT.parent/'art-weight-trial-2026-09-21/st_hizuru_weight_study.blend'),('polished',OUT/'st_hizuru_shoulder_polish.blend')]:
    bpy.ops.wm.open_mainfile(filepath=str(path));s=bpy.context.scene;s.frame_set(1);bpy.context.view_layer.update();c=s.camera
    r=bpy.data.objects.get('ST_Hizuru_polished_rig') or bpy.data.objects['ST_Hizuru_study_rig']
    head=r.matrix_world@r.pose.bones['Head'].head;target=head+Vector((0,.01,.03))
    for view,offset in [('front',(0,-5,0)),('threequarter',(3,-5,.25)),('side',(5,0,0))]:
        c.location=target+Vector(offset);c.rotation_euler=(target-c.location).to_track_quat('-Z','Y').to_euler();c.data.ortho_scale=.57
        s.render.resolution_x=600;s.render.resolution_y=600;s.render.resolution_percentage=100
        s.render.filepath=str(OUT/f'{label}_face_{view}.png');bpy.ops.render.render(write_still=True)
