"""Reopen the saved Blender artifact, exercise a grip control without saving."""
from pathlib import Path
import bpy,json
from mathutils import Vector
out=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(out/'hammer_lesson.blend'))
bpy.context.scene.frame_set(21)
rig=bpy.data.objects['B_Authored_pose_to_pose'];target=bpy.data.objects['CTRL_Left_grip']
bpy.context.view_layer.update()
before=rig.matrix_world@rig.pose.bones['LeftHand'].head
original=target.location.copy()
target.location.x+=.03
bpy.context.view_layer.update()
after=rig.matrix_world@rig.pose.bones['LeftHand'].head
goal=target.matrix_world.translation
result={'opened_saved_blend':True,'frame':21,'control_edit_local_m':[.03,0,0],
 'hand_displacement_m':(after-before).length,'target_error_after_edit_m':(goal-after).length,
 'armature_count':sum(o.type=='ARMATURE' for o in bpy.data.objects),
 'source_action_retained':bpy.data.objects['A_MoMask_unedited'].animation_data.action.name,
 'authored_action':rig.animation_data.action.name,'saved_mutation':False}
target.location=original
assert result['hand_displacement_m']>.025
assert result['target_error_after_edit_m']<.002
(out/'editability_check.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result))
