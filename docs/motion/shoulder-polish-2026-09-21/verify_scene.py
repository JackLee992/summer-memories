"""Reopen saved .blend and verify full-loop contacts, skin support and controller editing."""
from pathlib import Path
import bpy,json
from mathutils import Vector
OUT=Path(__file__).resolve().parent
bpy.ops.wm.open_mainfile(filepath=str(OUT/'st_hizuru_shoulder_polish.blend'))
s=bpy.context.scene;r=bpy.data.objects['ST_Hizuru_polished_rig'];c=bpy.data.objects['CTRL_Left_grip']
s.frame_set(33);bpy.context.view_layer.update()
before=r.matrix_world@r.pose.bones['LeftHand'].head
saved=c.location.copy();c.location.x+=.015;bpy.context.view_layer.update()
after=r.matrix_world@r.pose.bones['LeftHand'].head
err=(after-c.matrix_world.translation).length
c.location=saved;bpy.context.view_layer.update()
positions={}
for f in [1,72]:
    s.frame_set(f);bpy.context.view_layer.update()
    positions[f]=[tuple(r.matrix_world@b.head) for b in r.pose.bones]
loop=max((Vector(a)-Vector(b)).length for a,b in zip(positions[1],positions[72]))
report=json.loads((OUT/'verification.json').read_text())
results={'reopened_scene':True,'editable_left_grip_move_m':(after-before).length,'wrist_error_after_edit_m':err,'loop_joint_position_error_m':loop,'max_wrist_error_m':report['max_wrist_error_m'],'max_ankle_error_m':report['max_ankle_error_m'],'head_contact_gap_m':report['contact_gap_m'],'head_minimum_floor_clearance_m':report['minimum_head_floor_clearance_m'],'engine':s.render.engine,'mesh_count':len([o for o in bpy.data.objects if o.type=='MESH'])}
assert abs(results['editable_left_grip_move_m']-.015)<.001
assert err<.001 and loop<.001
assert report['max_wrist_error_m']<.001 and report['max_ankle_error_m']<.001
assert abs(report['contact_gap_m'])<.0001
(OUT/'editability_check.json').write_text(json.dumps(results,indent=2)+'\n')
print('VERIFIED',json.dumps(results))
# At the exact catch/release frame, toggling follow must not alter the target matrix.
far=bpy.data.objects['CTRL_Right_grip'];socket=bpy.data.objects['SOCKET_far_grip'];follow=far.constraints['Grasp_weapon_after_contact']
switches=[]
for f in [13,61]:
    s.frame_set(f);bpy.context.view_layer.update();original=follow.influence
    follow.influence=0;bpy.context.view_layer.update();free=far.matrix_world.copy()
    follow.influence=1;bpy.context.view_layer.update();bound=far.matrix_world.copy()
    switches.append({'frame':f,'position_error_m':(free.translation-bound.translation).length,'angle_error_radians':free.to_quaternion().rotation_difference(bound.to_quaternion()).angle})
    follow.influence=original;bpy.context.view_layer.update()
    assert switches[-1]['position_error_m']<.0001 and switches[-1]['angle_error_radians']<.001
spacing=[]
for f in range(13,61):
    s.frame_set(f);bpy.context.view_layer.update();spacing.append((far.matrix_world.translation-socket.matrix_world.translation).length)
assert max(spacing)<.0001
results['switch_checks']=switches;results['max_bound_socket_error_m']=max(spacing)
results['near_fingers_closed']=bpy.data.objects['Left_finger_0'].data.shape_keys.key_blocks['Open_fingers'].value==0
assert results['near_fingers_closed']
(OUT/'editability_check.json').write_text(json.dumps(results,indent=2)+'\n')
print('SWITCH_VERIFIED',json.dumps(results))
