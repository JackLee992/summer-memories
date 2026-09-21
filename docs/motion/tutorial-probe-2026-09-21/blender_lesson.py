"""BVH import + editable pose-to-pose / two-bone IK lesson in Blender.

No third-party addon or character mesh. Run with installed Blender --background.
The revised action is authored here; it is not claimed to be MoMask output.
"""
from pathlib import Path
import bpy, math, json, sys
from mathutils import Vector, Matrix, Quaternion

OUT=Path(__file__).resolve().parent
scene=bpy.context.scene
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.preferences.addon_enable(module='io_anim_bvh')
bpy.ops.import_anim.bvh(filepath=str(OUT/'momask_raw.bvh'),frame_start=1,update_scene_fps=True,update_scene_duration=True,axis_forward='-Z',axis_up='Y')
raw=bpy.context.object;raw.name='A_MoMask_unedited'
raw.animation_data.action.name='MoMask_first_result_unedited_60f'
raw.show_in_front=True
scene.frame_end=60;scene.render.fps=20
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
raw_samples=[]
for f in range(1,61):
    scene.frame_set(f)
    raw_samples.append({n:raw.matrix_world@raw.pose.bones[n].head for n in raw.pose.bones.keys()})
study=raw.copy();study.data=raw.data.copy();study.animation_data_clear()
bpy.context.collection.objects.link(study);study.name='B_Authored_pose_to_pose'
for pb in study.pose.bones:
    pb.matrix_basis=Matrix.Identity(4)
study.show_in_front=True
scene.frame_set(1)
bpy.context.view_layer.update()

def empty(name,loc=(0,0,0),size=.06):
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o)
    o.location=loc;o.empty_display_type='SPHERE';o.empty_display_size=size
    return o

controls={side:empty('CTRL_'+side+'_grip') for side in ['Left','Right']}
foot_controls={side:empty('CTRL_'+side+'_foot') for side in ['Left','Right']}
poles={side:empty('CTRL_'+side+'_elbow',size=.045) for side in ['Left','Right']}
knee_poles={side:empty('CTRL_'+side+'_knee',size=.045) for side in ['Left','Right']}
for side in ['Left','Right']:
    for name,target,pole in [(side+'ForeArm',controls[side],poles[side]),(side+'Leg',foot_controls[side],knee_poles[side])]:
        c=study.pose.bones[name].constraints.new('IK');c.name='Two_bones_no_stretch'
        c.target=target;c.chain_count=2;c.use_stretch=False;c.iterations=128
        # Pole angle is calibrated against the intended bend below.
        c.pole_target=pole;c.pole_angle=0
        study.pose.bones[name].ik_stretch=0
        study.pose.bones[name].parent.ik_stretch=0
    foot_controls[side].rotation_mode='QUATERNION'
    foot_controls[side].rotation_quaternion=(study.matrix_world@study.data.bones[side+'Foot'].matrix_local).to_quaternion()
    lock=study.pose.bones[side+'Foot'].constraints.new('COPY_ROTATION')
    lock.name='Foot_orientation_lock';lock.target=foot_controls[side];lock.owner_space='WORLD';lock.target_space='WORLD'

# Frame, hip Y/Z, hip pitch, spine pitch, grip Y/Z, hammer angle.
# This is a blocking exercise: few deliberate poses, not model regeneration.
KEYS=[
 (1, .00,.87, 0, 0,-.18,1.27,185),
 (7, .00,.87, 0, 0,-.18,1.27,185),
 (15,.04,.80,-4,-8,-.11,1.36,224),
 (21,.07,.77,-5,-10,-.03,1.48,263),
 (25,.06,.77,-4,-8,-.03,1.48,263),
 (28,-.01,.83,5,13,-.35,1.36,315),
 (31,-.12,.80,10,20,-.43,1.13,400),
 (33,-.12,.80,10,20,-.43,1.13,400),
 (38,-.14,.77,12,22,-.40,1.08,408),
 (44,-.08,.82,6,10,-.32,1.28,320),
 (52,.00,.87,0,0,-.18,1.35,220),
 (57,.00,.87,0,0,-.18,1.27,185),
 (60,.00,.87,0,0,-.18,1.27,185),
]
weapon=empty('CTRL_Hammer_rigid',size=.09)
weapon.rotation_mode='QUATERNION'
rest_hip=(study.matrix_world@study.data.bones['Hips'].matrix_local).to_quaternion()
arm_inv=study.matrix_world.inverted()
axis=(arm_inv.to_3x3()@Vector((1,0,0))).normalized()

def local_axis_rotation(bone,angle):
    q=study.data.bones[bone].matrix_local.to_quaternion()
    return q.inverted()@Quaternion(axis,math.radians(angle))@q

for f,y,z,hip_pitch,spine_pitch,gy,gz,angle in KEYS:
    scene.frame_set(f)
    pelvis=Vector((0,y,z))
    wm=Matrix.Translation(pelvis)@Quaternion(Vector((1,0,0)),math.radians(hip_pitch)).to_matrix().to_4x4()@rest_hip.to_matrix().to_4x4()
    root=study.pose.bones['Hips'];root.rotation_mode='QUATERNION';root.matrix=arm_inv@wm
    root.keyframe_insert('location',frame=f);root.keyframe_insert('rotation_quaternion',frame=f)
    for n,fraction in [('Spine',.3),('Spine1',.3),('Spine2',.4),('Neck',-.5)]:
        p=study.pose.bones[n];p.rotation_mode='QUATERNION';p.rotation_quaternion=local_axis_rotation(n,spine_pitch*fraction)
        p.keyframe_insert('rotation_quaternion',frame=f)
    a=math.radians(angle)
    direction=Vector((0,-math.cos(a),-math.sin(a)))
    center=Vector((0,gy,gz))
    weapon.location=center;weapon.rotation_quaternion=Vector((0,0,1)).rotation_difference(direction)
    weapon.keyframe_insert('location',frame=f);weapon.keyframe_insert('rotation_quaternion',frame=f)
    for side,sign in [('Left',1),('Right',-1)]:
        # Both targets are children of one rigid prop, preventing grip drift
        # between keys when the weapon rotates.
        ctrl=controls[side];ctrl.parent=weapon;ctrl.location=(0,0,sign*.11)
        foot_controls[side].location=(sign*.22, -.03 if side=='Left' else .09, .10)
        poles[side].location=(sign*.7,gy+.10,gz-.32)
        poles[side].keyframe_insert('location',frame=f)
        knee_poles[side].location=(sign*.3,-1.1,.48)
study.animation_data.action.name='Authored_13_poses_body_and_ik'

def fcurves(action):
    for layer in action.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                yield from bag.fcurves

# Linear fast strike, auto-clamped Bezier elsewhere. No full-circle quaternion
# ambiguity: the author provided the directional breakdown at frame 28.
for obj in [study,weapon,*poles.values()]:
    if not obj.animation_data:continue
    for fc in fcurves(obj.animation_data.action):
        for k in fc.keyframe_points:
            k.interpolation='LINEAR' if 25<=k.co.x<31 else 'BEZIER'
            k.handle_left_type='AUTO_CLAMPED';k.handle_right_type='AUTO_CLAMPED'

# Calibrate each two-bone pole angle against a desired elbow/knee location.
scene.frame_set(1);bpy.context.view_layer.update()
calibration={}
for side,sign in [('Left',1),('Right',-1)]:
    for bn,joint,target in [(side+'ForeArm',side+'ForeArm',Vector((sign*.25,-.03,1.05))), (side+'Leg',side+'Leg',Vector((sign*.25,-.18,.48)))]:
        constraint=study.pose.bones[bn].constraints[0]
        candidates=[]
        for i in range(72):
            constraint.pole_angle=-math.pi+i*math.tau/72;bpy.context.view_layer.update()
            p=study.matrix_world@study.pose.bones[joint].head
            candidates.append(((p-target).length,constraint.pole_angle))
        _,constraint.pole_angle=min(candidates)
        calibration[bn]=constraint.pole_angle
bpy.context.view_layer.update()

def material(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1)
    return m
blue=material('Slate_blue',(0.19,.31,.40));ivory=material('Warm_ivory',(.81,.77,.65))
dark=material('Joints',(.07,.11,.14));metal=material('Hammer_head',(.32,.39,.43))
wood=material('Hammer_shaft',(.38,.24,.14));floor=material('Floor',(.88,.88,.83))
rig_objects={raw:[],study:[]}

def weight_to_bone(obj,rig,bone):
    # Mesh starts in armature local rest space, using one-bone rigid weights.
    obj.matrix_world=rig.matrix_world.copy()
    vg=obj.vertex_groups.new(name=bone);vg.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    mod=obj.modifiers.new('Editable_armature_binding','ARMATURE');mod.object=rig
    rig_objects[rig].append(obj)

def mesh_segment(rig,bone,a,b,r1,r2,mat):
    vec=b-a
    if vec.length<.005:return
    q=Vector((0,0,1)).rotation_difference(vec.normalized())
    bpy.ops.mesh.primitive_cone_add(vertices=12,radius1=r1,radius2=r2,depth=vec.length)
    obj=bpy.context.object;obj.name=rig.name+'_'+bone
    transform=Matrix.Translation((a+b)*.5)@q.to_matrix().to_4x4()
    obj.data.transform(transform);obj.data.materials.append(mat)
    weight_to_bone(obj,rig,bone)
    for poly in obj.data.polygons:poly.use_smooth=True

def sphere(rig,bone,center,radius,scale,mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=radius)
    o=bpy.context.object;o.name=rig.name+'_'+bone+'_form'
    o.data.transform(Matrix.Translation(center)@Matrix.Diagonal((*scale,1)));o.data.materials.append(mat)
    weight_to_bone(o,rig,bone)
    for poly in o.data.polygons:poly.use_smooth=True

for rig in [raw,study]:
    for side in ['Left','Right']:
        for bn,r1,r2 in [('UpLeg',.085,.059),('Leg',.060,.039),('Arm',.055,.043),('ForeArm',.045,.032),('Foot',.050,.055)]:
            b=rig.data.bones[side+bn];mesh_segment(rig,b.name,b.head_local,b.tail_local,r1,r2,blue if 'Leg' in bn else ivory)
        for bn in ['Leg','ForeArm','Hand','Foot']:
            b=rig.data.bones[side+bn];sphere(rig,b.name,b.head_local,.043,(1,1,1),dark)
    for bn,r1,r2 in [('Spine',.125,.12),('Spine1',.12,.17),('Spine2',.17,.16),('Neck',.055,.05)]:
        b=rig.data.bones[bn];mesh_segment(rig,bn,b.head_local,b.tail_local,r1,r2,blue)
    b=rig.data.bones['Hips'];sphere(rig,'Hips',b.head_local,.12,(1.3,.7,1),blue)
    # BVH head is at base of head; original coordinate system is Y-up.
    b=rig.data.bones['Head'];sphere(rig,'Head',b.head_local+Vector((0,.075,0)),.10,(.78,1.18,.9),ivory)

def prop_part(name,location,scale,mat,parent):
    bpy.ops.mesh.primitive_cube_add(size=1)
    o=bpy.context.object;o.name=name;o.parent=parent;o.location=location;o.scale=scale;o.data.materials.append(mat)
    bevel=o.modifiers.new('Small_edges','BEVEL');bevel.width=.025;bevel.segments=2
    o.modifiers.new('Normals','WEIGHTED_NORMAL')
    return o

shaft=prop_part('Hammer_rigid_shaft',(0,0,.31),(.033,.033,1.10),wood,weapon)
head=prop_part('Hammer_rigid_head',(0,0,.87),(.24,.14,.14),metal,weapon)
fixed_prop=[shaft,head]

# The unedited motion deliberately shows wrist markers without inventing a
# model-generated prop. Cyan/gold grip markers expose the distinction.
for ctrl in controls.values():
    ctrl.hide_render=True

bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,.0));bpy.context.object.name='Ground';bpy.context.object.data.materials.append(floor)
bpy.ops.object.camera_add(location=(5,-4,2.9));camera=bpy.context.object
target=Vector((0,-.05,1.05));camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=2.8;scene.camera=camera
scene.render.engine='BLENDER_WORKBENCH';scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100
scene.display.shading.light='STUDIO';scene.display.shading.studio_light='paint.sl'
scene.display.shading.color_type='MATERIAL';scene.display.shading.show_shadows=True
scene.display.shading.show_cavity=True;scene.display.shading.cavity_type='BOTH';scene.display.shading.show_object_outline=True
scene.display.shading.background_type='WORLD';scene.world.color=(.88,.88,.83)
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False
scene.view_settings.view_transform='Standard'

checks=[]
for f in range(1,61):
    scene.frame_set(f);bpy.context.view_layer.update()
    wrist={s:study.matrix_world@study.pose.bones[s+'Hand'].head for s in ['Left','Right']}
    checks.append({'frame':f,'grip_error_m':{s:(wrist[s]-controls[s].matrix_world.translation).length for s in wrist},
        'grip_spacing_m':(wrist['Left']-wrist['Right']).length,
        'foot_error_m':{s:(study.matrix_world@study.pose.bones[s+'Foot'].head-foot_controls[s].matrix_world.translation).length for s in wrist}})
raw_spacing=[(x['LeftHand']-x['RightHand']).length for x in raw_samples]
report={'source_bvh':'momask_raw.bvh','raw_bones':len(raw.pose.bones),'fps':20,'frames':60,
 'raw_grip_spacing_min_m':min(raw_spacing),'raw_grip_spacing_max_m':max(raw_spacing),
 'revised_max_grip_error_m':max(v for x in checks for v in x['grip_error_m'].values()),
 'revised_max_foot_error_m':max(v for x in checks for v in x['foot_error_m'].values()),
 'revised_grip_spacing_range_m':[min(x['grip_spacing_m'] for x in checks),max(x['grip_spacing_m'] for x in checks)],
 'authored_keys':[x[0] for x in KEYS],'pole_calibration_rad':calibration,'frame_checks':checks,
 'limits':['Revised animation is manually specified key poses, not repaired AI performance.','Neutral mannequin only, no production character art.','No weapon collision or finger articulation.','No verified commercial model-output rights.']}
(OUT/'blender_verification.json').write_text(json.dumps(report,indent=2)+'\n')

def set_variant(variant):
    for rig,objects in rig_objects.items():
        for obj in objects:
            hidden=(rig==study if variant=='raw' else rig==raw)
            obj.hide_render=hidden;obj.hide_set(hidden)
    for obj in fixed_prop:
        obj.hide_render=(variant=='raw');obj.hide_set(variant=='raw')
    raw.hide_set(variant!='raw');study.hide_set(variant=='raw')

scene.frame_set(21);set_variant('authored')
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
        area.spaces.active.shading.type='MATERIAL' if False else 'SOLID'
bpy.ops.object.select_all(action='DESELECT');study.hide_set(False);study.select_set(True);bpy.context.view_layer.objects.active=study
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'hammer_lesson.blend'))
args=sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else []
mode=args[0] if args else 'keys'
for variant in ['raw','authored']:
    set_variant(variant)
    dest=OUT/(variant+'_frames');dest.mkdir(exist_ok=True)
    frames=range(1,61) if mode=='all' else [1,15,21,28,31,38,44,57]
    for f in frames:
        scene.frame_set(f);scene.render.filepath=str(dest/f'{f:04d}.png');bpy.ops.render.render(write_still=True)
print('RESULT',json.dumps({k:v for k,v in report.items() if k!='frame_checks'}))
