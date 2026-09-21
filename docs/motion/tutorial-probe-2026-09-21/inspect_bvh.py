from pathlib import Path
import bpy,json
OUT=Path(__file__).resolve().parent
bpy.ops.preferences.addon_enable(module='io_anim_bvh')
bpy.ops.import_anim.bvh(filepath=str(OUT/'momask_raw.bvh'),frame_start=1,update_scene_fps=True,update_scene_duration=True,axis_forward='-Z',axis_up='Y')
rig=bpy.context.object
data=[]
for frame in [1,10,20,30,40,50,60]:
 bpy.context.scene.frame_set(frame)
 data.append({'frame':frame,'joints':{n:list(rig.matrix_world@rig.pose.bones[n].head) for n in ['Hips','Head','LeftHand','RightHand','LeftArm','RightArm','LeftForeArm','RightForeArm','LeftFoot','RightFoot']}})
print(json.dumps({'fps':bpy.context.scene.render.fps,'bones':list(rig.pose.bones.keys()),'samples':data},indent=2))
(OUT/'import_inspection.json').write_text(json.dumps(data,indent=2))
