"""Single-hand shoulder carry, deliberate regrip and character polish study. Authored geometry/action, no new AI output.
Uses the previous MoMask skeleton proportions as a scaffold, not its motion.
Run: Blender --background --factory-startup --python build_study.py -- keys|all
"""
from pathlib import Path
import bpy, math, json, sys
from mathutils import Vector, Matrix, Quaternion
OUT=Path(__file__).resolve().parent
BASE=OUT.parent/'tutorial-probe-2026-09-21'
scene=bpy.context.scene
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.preferences.addon_enable(module='io_anim_bvh')
bpy.ops.import_anim.bvh(filepath=str(BASE/'momask_raw.bvh'), frame_start=1, update_scene_fps=True, axis_forward='-Z',axis_up='Y')
rig=bpy.context.object;rig.name='ST_Hizuru_polished_rig';rig.animation_data_clear();rig.show_in_front=True
for pb in rig.pose.bones: pb.matrix_basis=Matrix.Identity(4)
scene.frame_start=1;scene.frame_end=72;scene.render.fps=20
scene.unit_settings.system='METRIC'
scene.frame_set(1);bpy.context.view_layer.update()
INV=rig.matrix_world.inverted(); SHIFT=Vector((0,0,.92-(rig.matrix_world@rig.data.bones['Hips'].head_local).z))
def joint(name,tail=False):
    b=rig.data.bones[name]
    return rig.matrix_world@(b.tail_local if tail else b.head_local)+SHIFT

def empty(name,loc=(0,0,0),size=.04):
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=loc
    o.empty_display_type='SPHERE';o.empty_display_size=size;return o
weapon=empty('CTRL_Hammer_rigid',size=.08);weapon.rotation_mode='QUATERNION'
grips={s:empty('CTRL_'+s+'_grip') for s in ['Left','Right']}
feet={s:empty('CTRL_'+s+'_foot') for s in grips}
elbows={s:empty('CTRL_'+s+'_elbow') for s in grips}
knees={s:empty('CTRL_'+s+'_knee') for s in grips}
for s,sign in [('Left',1),('Right',-1)]:
    grips[s].parent=weapon;grips[s].location=(0,0,sign*.12)
    feet[s].location=(sign*.17,.18 if s=='Left' else -.24,.085)
    feet[s].rotation_mode='QUATERNION';feet[s].rotation_quaternion=(rig.matrix_world@rig.data.bones[s+'Foot'].matrix_local).to_quaternion()
    knees[s].location=(sign*.30,-1.1,.45)
    for bone,control,pole in [(s+'ForeArm',grips[s],elbows[s]),(s+'Leg',feet[s],knees[s])]:
        c=rig.pose.bones[bone].constraints.new('IK');c.name='Two_bones_no_stretch';c.target=control;c.pole_target=pole;c.chain_count=2;c.use_stretch=False;c.iterations=128
        rig.pose.bones[bone].ik_stretch=0;rig.pose.bones[bone].parent.ik_stretch=0
    c=rig.pose.bones[s+'Foot'].constraints.new('COPY_ROTATION');c.target=feet[s];c.owner_space='WORLD';c.target_space='WORLD'
# frame, pelvis forward/z, pelvis pitch, chest pitch, grip forward/z, hammer degrees.
KEYS=[
(1,.02,.865,-2,2,-.27,1.363,182), (7,.02,.865,-2,2,-.27,1.363,182),
(10,.025,.865,-2,2,-.10,1.35,190),
(13,.035,.85,-3,-2,-.10,1.35,190),
(16,.07,.82,-6,-7,-.06,1.37,224),
(22,.13,.82,-8,-10,.015,1.46,265),
(25,.12,.83,-5,-7,.015,1.46,265),
(27,.04,.85,4,3,-.01,1.47,277),
(29,-.05,.865,10,12,-.16,1.43,308),
(31,-.13,.86,15,19,-.32,1.27,354),
(33,-.17,.82,16,22,-.49,1.10,400),
(34,-.18,.80,18,23,-.49,1.10,400),
(36,-.19,.775,20,24,-.48,1.15,393),
(39,-.17,.79,17,20,-.49,1.10,400),
(43,-.14,.83,12,14,-.45,1.14,393),
(48,-.09,.855,7,8,-.29,1.30,341),
(54,.015,.855,-2,0,-.13,1.42,258),
(60,.02,.865,-2,2,-.10,1.35,190),
(66,.02,.865,-2,2,-.27,1.363,182), (72,.02,.865,-2,2,-.27,1.363,182)]
rest_hip=(rig.matrix_world@rig.data.bones['Hips'].matrix_local).to_quaternion()
axis=(INV.to_3x3()@Vector((1,0,0))).normalized()
def axisrot(bn,deg):
    q=rig.data.bones[bn].matrix_local.to_quaternion()
    return q.inverted()@Quaternion(axis,math.radians(deg))@q
for f,y,z,hp,sp,gy,gz,angle in KEYS:
    scene.frame_set(f)
    # Open the silhouette with an offset weapon lane beside the head.
    wm=Matrix.Translation(Vector((-.015,y,z)))@Quaternion(Vector((1,0,0)),math.radians(hp)).to_matrix().to_4x4()@rest_hip.to_matrix().to_4x4()
    pb=rig.pose.bones['Hips'];pb.rotation_mode='QUATERNION';pb.matrix=INV@wm
    pb.keyframe_insert('location',frame=f);pb.keyframe_insert('rotation_quaternion',frame=f)
    for bn,fac in [('Spine',.3),('Spine1',.3),('Spine2',.4),('Neck',-.7),('Head',-.10)]:
        p=rig.pose.bones[bn];p.rotation_mode='QUATERNION';p.rotation_quaternion=axisrot(bn,sp*fac);p.keyframe_insert('rotation_quaternion',frame=f)
    direction=Vector((0,-math.cos(math.radians(angle)),-math.sin(math.radians(angle))))
    lane=.164 if f<=7 or f>=66 else .19 if f<=25 else .23 if f in [27,54] else .27
    weapon.location=(lane,gy,gz);weapon.rotation_quaternion=Vector((0,0,1)).rotation_difference(direction)
    weapon.keyframe_insert('location',frame=f);weapon.keyframe_insert('rotation_quaternion',frame=f)
    for s,sign in [('Left',1),('Right',-1)]:
        elbows[s].location=(sign*.67,gy+.12,gz-.30);elbows[s].keyframe_insert('location',frame=f)
# Independently editable far-hand target: world-space path into contact, then constraint.
# Grip empties keep identity transforms when switching so the wrist never teleports.
near=grips['Left'];far=grips['Right'];far.parent=None;far.rotation_mode='QUATERNION'
far_socket=empty('SOCKET_far_grip');far_socket.parent=weapon
for f,z in [(1,.025),(13,.025),(18,-.10),(54,-.10),(60,.025),(61,.025),(72,.025)]:
    far_socket.location=(0,0,z);far_socket.keyframe_insert('location',frame=f)
follow=far.constraints.new('COPY_TRANSFORMS');follow.name='Grasp_weapon_after_contact';follow.target=far_socket
for f,v in [(1,0),(12,0),(13,1),(60,1),(61,0),(72,0)]:
    follow.influence=v;follow.keyframe_insert('influence',frame=f)
for f,v in [(1,-.12),(7,-.12),(10,.12),(13,.12),(54,.12),(60,.12),(61,.12),(66,-.12),(72,-.12)]:
    near.location=(0,0,v);near.keyframe_insert('location',frame=f)
# Free hand hangs naturally, lifts in an arc, and reaches the socket before binding.
scene.frame_set(13);bpy.context.view_layer.update();catch=far_socket.matrix_world.copy()
scene.frame_set(61);bpy.context.view_layer.update();release=far_socket.matrix_world.copy()
free_q=Quaternion(Vector((1,0,0)),math.radians(90))
for f,position,rot in [(1,(-.255,-.018,.90),free_q),(5,(-.255,-.018,.90),free_q),
                         (8,(-.23,-.18,1.01),free_q.slerp(catch.to_quaternion(),.30)),
                         (10,(-.12,-.27,1.18),free_q.slerp(catch.to_quaternion(),.75)),
                         (12,catch.translation,catch.to_quaternion()),(13,catch.translation,catch.to_quaternion()),
                         (60,release.translation,release.to_quaternion()),(61,release.translation,release.to_quaternion()),
                         (65,(-.19,-.20,1.10),free_q.slerp(release.to_quaternion(),.6)),
                         (69,(-.255,-.018,.90),free_q),(72,(-.255,-.018,.90),free_q)]:
    far.location=position;far.rotation_quaternion=rot;far.keyframe_insert('location',frame=f);far.keyframe_insert('rotation_quaternion',frame=f)
# Keep the free elbow low during the one-handed carry, then open it for the two-hand lift.
for f,loc in [(1,(-.51,.10,1.05)),(5,(-.51,.10,1.05)),(10,(-.57,-.02,1.10)),(13,(-.67,-.06,1.08)),(60,(-.58,-.08,1.08)),(66,(-.51,.10,1.05)),(72,(-.51,.10,1.05))]:
    elbows['Right'].location=loc;elbows['Right'].keyframe_insert('location',frame=f)
rig.animation_data.action.name='ST_single_hand_regrip_strike_release'
def curves(action):
    for l in action.layers:
        for s in l.strips:
            for b in s.channelbags: yield from b.fcurves
for o in [rig,weapon,near,far,far_socket,*elbows.values()]:
    for fc in curves(o.animation_data.action):
        for k in fc.keyframe_points:
            k.interpolation='CONSTANT' if 'influence' in fc.data_path else ('LINEAR' if 27<=k.co.x<34 else 'BEZIER')
            k.handle_left_type='AUTO_CLAMPED';k.handle_right_type='AUTO_CLAMPED'
scene.frame_set(22);bpy.context.view_layer.update()
for s,sign in [('Left',1),('Right',-1)]:
    for bn,target in [(s+'ForeArm',Vector((sign*.26,-.03,1.13))),(s+'Leg',Vector((sign*.24,-.23,.49)))]:
        c=rig.pose.bones[bn].constraints[0];best=(100,0)
        for i in range(72):
            c.pole_angle=-math.pi+i*math.tau/72;bpy.context.view_layer.update()
            err=(rig.matrix_world@rig.pose.bones[bn].head-target).length
            if err<best[0]:best=(err,c.pole_angle)
        c.pole_angle=best[1]

# A compact palette and deliberate large forms. Shader bands come from light, not random textures.
def mat(name,color):
    m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
    ns=m.node_tree.nodes;ns.clear();out=ns.new('ShaderNodeOutputMaterial');diff=ns.new('ShaderNodeBsdfDiffuse')
    diff.inputs['Color'].default_value=(.8,.8,.8,1)
    rgb=ns.new('ShaderNodeShaderToRGB');r=ns.new('ShaderNodeValToRGB');r.color_ramp.interpolation='CONSTANT'
    r.color_ramp.elements.remove(r.color_ramp.elements[1])
    for idx,(pos,fac) in enumerate([(.0,.40),(.30,.73),(.65,1.0)]):
        e=r.color_ramp.elements[0] if idx==0 else r.color_ramp.elements.new(pos);e.position=pos;e.color=(*(v*fac for v in color),1)
    emit=ns.new('ShaderNodeEmission');m.node_tree.links.new(diff.outputs[0],rgb.inputs[0]);m.node_tree.links.new(rgb.outputs[0],r.inputs[0]);m.node_tree.links.new(r.outputs[0],emit.inputs[0]);m.node_tree.links.new(emit.outputs[0],out.inputs[0]);return m
suit=mat('Suit_ink_blue',(.065,.084,.125));lapel=mat('Lapel_graphite',(.10,.135,.18));ivory=mat('Shirt_ivory',(.83,.84,.78))
skin=mat('Skin_warm',(.87,.65,.48));hair=mat('Hair_blue_black',(.032,.041,.067));hair_light=mat('Hair_broad_highlight',(.075,.100,.145))
black=mat('Glasses_ink',(.009,.014,.023));iris=mat('Eyes_muted_brown',(.095,.054,.04));metal=mat('Hammer_blue_steel',(.36,.44,.51));edge=mat('Hammer_endcaps',(.59,.64,.65))
wood=mat('Handle_walnut',(.29,.15,.075));leather=mat('Grip_leather',(.095,.071,.063));sole=mat('Shoe_sole',(.025,.034,.046));gold=mat('Small_brass_details',(.5,.32,.12))
created=[]
def torso_weights(z):
    centers=[(.94,'Hips'),(1.10,'Spine'),(1.24,'Spine1'),(1.36,'Spine2')]
    if z<=centers[0][0]: return {'Hips':1}
    if z>=centers[-1][0]: return {'Spine2':1}
    for (a,an),(b,bn) in zip(centers,centers[1:]):
        if a<=z<=b:
            t=(z-a)/(b-a);return {an:1-t,bn:t}
def bind_mesh(name,verts,faces,material,weights):
    mesh=bpy.data.meshes.new(name);mesh.from_pydata([INV@(Vector(v)-SHIFT) for v in verts],[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o);o.matrix_world=rig.matrix_world.copy();o.data.materials.append(material)
    for i,v in enumerate(verts):
        ws=weights[i] if isinstance(weights,list) else weights(v) if callable(weights) else {weights:1}
        for bn,w in ws.items():
            g=o.vertex_groups.get(bn) or o.vertex_groups.new(name=bn)
            if w>0:g.add([i],w,'REPLACE')
    mod=o.modifiers.new('Skinned_to_editable_rig','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=True
    for p in mesh.polygons:p.use_smooth=True
    created.append(o);return o

def loft(name,rings,material,weights,n=16):
    # ring = x/y/z center, width radius, depth radius; vertical garment cross sections
    vs=[]
    for x,y,z,rx,ry in rings:
        for i in range(n):
            a=math.tau*i/n;vs.append((x+rx*math.cos(a),y+ry*math.sin(a),z))
    fs=[tuple(reversed(range(n)))]
    for j in range(len(rings)-1):
        for i in range(n): fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.append(tuple((len(rings)-1)*n+i for i in range(n)))
    return bind_mesh(name,vs,fs,material,weights)

def ellipsoid(name,center,scale,material,bone,seg=24,rings=14):
    vs=[];fs=[]
    for j in range(rings+1):
        p=math.pi*j/rings
        for i in range(seg):
            a=math.tau*i/seg;vs.append((center[0]+scale[0]*math.sin(p)*math.cos(a),center[1]+scale[1]*math.sin(p)*math.sin(a),center[2]+scale[2]*math.cos(p)))
    for j in range(rings):
        for i in range(seg):fs.append((j*seg+i,j*seg+(i+1)%seg,(j+1)*seg+(i+1)%seg,(j+1)*seg+i))
    return bind_mesh(name,vs,fs,material,bone)

def tube(name,points,radii,material,bone,n=10):
    vs=[]
    for j,p in enumerate(points):
        p=Vector(p);d=Vector(points[min(j+1,len(points)-1)])-Vector(points[max(0,j-1)])
        if d.length<1e-6:d=Vector((0,0,1))
        q=Vector((0,0,1)).rotation_difference(d.normalized())
        for i in range(n):
            a=math.tau*i/n;v=p+q@Vector((radii[j]*math.cos(a),radii[j]*math.sin(a),0));vs.append(v)
    fs=[tuple(reversed(range(n)))]
    for j in range(len(points)-1):
        for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    fs.append(tuple((len(points)-1)*n+i for i in range(n)))
    return bind_mesh(name,vs,fs,material,bone)

# Tailored jacket, tapered trousers and opaque shoes. Keep visible wrists, neckline and lapels.
loft('Jacket_continuous',[(0,.005,.865,.175,.102),(0,.004,.98,.159,.106),(0,.0,1.09,.133,.096),(0,-.006,1.19,.161,.110),(0,.012,1.30,.190,.108),(0,.012,1.36,.210,.086),(0,.014,1.395,.146,.065)],suit,lambda v:torso_weights(v[2]))
ellipsoid('Pelvis_under_jacket',(0,.003,.885),(.158,.100,.14),suit,'Hips')
# shirt and lapel planes sit just in front of jacket, blending with torso.
def patch(name,vs,material):return bind_mesh(name,vs,[tuple(range(len(vs)))],material,lambda v:torso_weights(v[2]))
patch('Shirt_front', [(-.076,-.073,1.38),(.078,-.074,1.38),(.056,-.119,1.27),(0,-.118,1.145),(-.063,-.121,1.27)],ivory)
patch('Lapel_left',[(-.082,-.08,1.385),(-.133,-.083,1.335),(-.108,-.119,1.274),(-.08,-.123,1.267),(-.014,-.121,1.154),(-.040,-.125,1.29)],lapel)
patch('Lapel_right',[(.082,-.08,1.385),(.134,-.083,1.332),(.111,-.119,1.279),(.08,-.123,1.267),(.012,-.122,1.151),(.04,-.125,1.29)],lapel)
patch('Collar_left',[(-.065,-.052,1.405),(-.012,-.07,1.38),(-.041,-.105,1.334),(-.078,-.08,1.374)],ivory)
patch('Collar_right',[(.065,-.052,1.405),(.012,-.07,1.38),(.041,-.105,1.334),(.078,-.08,1.374)],ivory)
for z in [1.09,1.01]:ellipsoid('Jacket_button',(0,-.104,z),(.009,.006,.009),gold,'Spine' if z>1.05 else 'Hips',12,6)
for s,sign in [('Left',1),('Right',-1)]:
    # One continuous skin across the knee/elbow removes open caps and disconnected joints.
    for upper,lower,radii in [('UpLeg','Leg',[.086,.082,.063,.060,.057,.046,.032]),('Arm','ForeArm',[.065,.060,.047,.046,.047,.043,.036])]:
        a=joint(s+upper);m=joint(s+lower);b=joint(s+lower,True)
        points=[a,a.lerp(m,.48),a.lerp(m,.88),m,m.lerp(b,.12),m.lerp(b,.55),b]
        vs=[];ws=[];fs=[];n=16
        for j,(pt,rad) in enumerate(zip(points,radii)):
            d=(points[min(j+1,6)]-points[max(j-1,0)]).normalized()
            ref=Vector((1,0,0)) if upper=='UpLeg' else Vector((0,0,1))
            u=(ref-d*ref.dot(d)).normalized();v=d.cross(u).normalized()
            blend=[0,0,.12,.5,.88,1,1][j]
            for k in range(n):
                ang=math.tau*k/n;vs.append(pt+u*(rad*math.cos(ang))+v*(rad*.91*math.sin(ang)))
                ws.append({s+upper:1-blend,s+lower:blend})
        fs.append(tuple(reversed(range(n))))
        for j in range(6):
            for k in range(n):fs.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
        fs.append(tuple(6*n+k for k in range(n)))
        limb=bind_mesh(s+'_'+upper+'_continuous',vs,fs,suit,ws)
        g=limb.vertex_groups.new(name='Joint_correction_region')
        for j,w in [(1,.25),(2,.75),(3,1),(4,.75),(5,.25)]:g.add(list(range(j*n,(j+1)*n)),w,'REPLACE')
        smooth=limb.modifiers.new('Local_joint_corrective_smooth','CORRECTIVE_SMOOTH');smooth.factor=.22;smooth.iterations=3;smooth.vertex_group=g.name
        # Keep cuff and trouser hem intact; smooth shading handles the broad surface.
    # A skinned shoulder bridge overlaps the jacket and upper sleeve instead of exposing a cap.
    sh=joint(s+'Arm')
    ellipsoid(s+'_shoulder_bridge',sh+Vector((sign*.012,0,-.005)),(.052,.061,.057),suit,lambda v:{'Spine2':.2,s+'Shoulder':.35,s+'Arm':.45})
    a=joint(s+'ForeArm');b=joint(s+'ForeArm',True);tube(s+'_white_cuff',[a.lerp(b,.88),b],[.041,.041],ivory,s+'ForeArm')
    ankle=joint(s+'Foot');footcenter=ankle+Vector((0,-.055,-.040))
    ellipsoid(s+'_shoe',footcenter,(.049,.122,.047),suit,s+'Foot')
    ellipsoid(s+'_sole',footcenter+Vector((0,-.005,-.025)),(.052,.121,.016),sole,s+'Foot')
# Neck and a stylized custom head with a tapered jaw rather than the sphere mannequin.
loft('Neck',[(0,.012,1.368,.043,.041),(0,.004,1.43,.041,.042),(0,-.005,1.475,.041,.045)],skin,'Neck')
headcenter=joint('Head')+Vector((0,0,.075))
hx,hy,hz=headcenter
face=loft('Face_tapered_jaw',[(hx,hy-.005,hz-.112,.017,.030),(hx,hy-.009,hz-.096,.040,.047),(hx,hy-.006,hz-.074,.061,.059),(hx,hy-.002,hz-.041,.077,.071),(hx,hy+.002,hz+.0,.085,.079),(hx,hy+.007,hz+.048,.088,.083),(hx,hy+.015,hz+.091,.074,.071),(hx,hy+.017,hz+.116,.035,.036),(hx,hy+.017,hz+.120,.009,.010)],skin,'Head',48)
face_skin=mat('Face_soft_two_tone',(.87,.66,.50));ramp=next(n for n in face_skin.node_tree.nodes if n.type=='VALTORGB').color_ramp
while len(ramp.elements)>2:ramp.elements.remove(ramp.elements[-1])
ramp.elements[0].position=0;ramp.elements[0].color=(.58,.37,.30,1);ramp.elements[1].position=.42;ramp.elements[1].color=(.87,.66,.50,1)
face.data.materials[0]=face_skin
sub=face.modifiers.new('Face_surface_polish','SUBSURF');sub.levels=2;sub.render_levels=2
for sign in [-1,1]:ellipsoid('Ear',(hx+sign*.083,hy+.010,hz-.004),(.014,.014,.028),skin,'Head')
# Face features are purposely broad enough to read at sprite size.
for sign in [-1,1]:
    x=hx+sign*.038
    ellipsoid('Eye_white',(x,hy-.075,hz+.003),(.022,.006,.011),ivory,'Head',16,8)
    ellipsoid('Eye_iris',(x-sign*.002,hy-.082,hz+.003),(.006,.0025,.0095),iris,'Head',16,10)
    ellipsoid('Eye_catchlight',(x-sign*.002-.002,hy-.085,hz+.007),(.0011,.0007,.0016),ivory,'Head',12,6)
    tube('Lower_lid',[(x-.019,hy-.079,hz-.001),(x,hy-.081,hz-.005),(x+.019,hy-.079,hz-.001)],[.0012]*3,iris,'Head',6)
    tube('Upper_lid',[(x-.020,hy-.079,hz+.007),(x,hy-.085,hz+.012),(x+.020,hy-.079,hz+.007)],[.0025]*3,black,'Head',6)
    tube('Brow',[(x-.020,hy-.076,hz+.032),(x+.005,hy-.080,hz+.032),(x+.020,hy-.075,hz+.036)],[.003]*3,hair,'Head',6)
    # Rounded rectangular glasses; actual geometry, no pasted face texture.
    pts=[]
    for i in range(33):
        a=math.tau*i/32;pts.append((x+.029*math.copysign(abs(math.cos(a))**.6,math.cos(a)),hy-.087,hz+.007+.021*math.copysign(abs(math.sin(a))**.6,math.sin(a))))
    tube('Glasses_frame',pts,[.0024]*len(pts),black,'Head',6)
    tube('Glasses_temple',[(hx+sign*.067,hy-.085,hz+.01),(hx+sign*.086,hy+.004,hz+.016)],[.0024,.0024],black,'Head',6)
tube('Glasses_bridge',[(hx-.009,hy-.086,hz+.012),(hx,hy-.090,hz+.016),(hx+.009,hy-.086,hz+.012)],[.003]*3,black,'Head',6)
# Tiny nose wedge and a quiet mouth. Avoid noisy face detail.
bind_mesh('Nose',[(hx-.011,hy-.071,hz-.018),(hx+.011,hy-.071,hz-.018),(hx,hy-.093,hz-.025),(hx,hy-.075,hz+.016)],[(0,1,2),(0,2,3),(1,3,2)],skin,'Head')
tube('Mouth',[(hx-.013,hy-.062,hz-.063),(hx,hy-.064,hz-.064),(hx+.011,hy-.062,hz-.062)],[.0015]*3,iris,'Head',6)
# Scalp shell (upper portion, plus back); leave the face and glasses exposed.
vs=[];fs=[];n=40;rings=12
for j in range(rings+1):
    for i in range(n):
        a=math.tau*i/n
        # Negative Y = face: higher hairline. Back wraps below the occiput.
        maxp=1.30 if math.sin(a)<-.25 else 2.12
        p=.03+(maxp-.03)*j/rings
        vs.append((hx+.094*math.sin(p)*math.cos(a),hy+.016+.098*math.sin(p)*math.sin(a),hz+.020+.115*math.cos(p)))
for j in range(rings):
    for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
bind_mesh('Hair_scalp',vs,fs,hair,'Head')
# Broad tapered hair ribbons. Shape keys let the tips lag without changing the head mesh.
hair_objects=[]
def ribbon(name,points,widths,material,depth=.018,animate=False):
    vs=[]
    for p,w in zip(points,widths):
        x,y,z=p
        for k in range(8):
            a=math.tau*k/8;vs.append((x+w*math.cos(a),y+depth*math.sin(a),z))
    fs=[]
    for j in range(len(points)-1):
        for i in range(8):fs.append((j*8+i,j*8+(i+1)%8,(j+1)*8+(i+1)%8,(j+1)*8+i))
    fs.extend([tuple(reversed(range(8))),tuple((len(points)-1)*8+i for i in range(8))])
    o=bind_mesh(name,vs,fs,material,'Head')
    sub=o.modifiers.new('Smooth_broad_hair_locks','SUBSURF');sub.levels=2;sub.render_levels=2
    if animate:
        o.shape_key_add(name='Basis');key=o.shape_key_add(name='Tip_lag')
        for i,p in enumerate(key.data):
            t=(i//8)/(len(points)-1);p.co+=INV.to_3x3()@Vector((-.020*t*t,.19*t*t,.06*t*t))
        for f,v in [(1,0),(10,0),(19,-.18),(26,-.10),(29,.0),(33,.8),(36,1),(40,.5),(46,-.18),(52,.25),(61,-.08),(69,0),(72,0)]:
            key.value=v;key.keyframe_insert('value',frame=f)
        hair_objects.append(o)
    return o
for i,(x,endx,y,endz) in enumerate([(-.074,-.15,.07,1.11),(-.040,-.095,.10,1.06),(0,-.02,.115,1.04),(.037,.06,.11,1.08),(.068,.11,.085,1.14)]):
    ribbon('Hair_back_lock_'+str(i),[(hx+x,hy+y,hz+.06),(hx+x*1.2,hy+y+.015,hz-.06),(hx+endx*.9,hy+y+.06,1.31),(hx+endx,hy+y+.07,endz)],[.043,.056,.056,.005],hair,animate=True)
# Continuous back hair mass underneath the individual locks; perimeter opens around the face.
vs=[];fs=[];n=24
hairrings=[(hz+.072,.083,.078,hy+.020),(hz+.004,.092,.092,hy+.020),(hz-.10,.106,.087,hy+.030),(1.30,.145,.060,hy+.073),(1.16,.14,.035,hy+.118),(1.08,.12,.025,hy+.14)]
for j,(z,rx,ry,cy) in enumerate(hairrings):
    for i in range(n+1):
        a=math.pi*i/n;vs.append((hx+rx*math.cos(a),cy+ry*math.sin(a),z+(.023*math.sin(3*a) if j==5 else 0)))
for j in range(len(hairrings)-1):
    for i in range(n):fs.append((j*(n+1)+i,j*(n+1)+i+1,(j+1)*(n+1)+i+1,(j+1)*(n+1)+i))
backhair=bind_mesh('Hair_back_volume',vs,fs,hair,'Head');solid=backhair.modifiers.new('Hair_volume_thickness','SOLIDIFY');solid.thickness=.018
smooth=backhair.modifiers.new('Hair_volume_smooth','SUBSURF');smooth.levels=2;smooth.render_levels=2
backhair.shape_key_add(name='Basis');lag=backhair.shape_key_add(name='Tip_lag')
for i,v in enumerate(lag.data):
    t=max(0,min(1,(hz-vs[i][2])/(hz-1.08)));v.co+=INV.to_3x3()@Vector((-.020*t*t,.19*t*t,.06*t*t))
for f,value in [(1,0),(10,0),(19,-.18),(26,-.10),(29,0),(33,.8),(36,1),(40,.5),(46,-.18),(52,.25),(61,-.08),(69,0),(72,0)]:lag.value=value;lag.keyframe_insert('value',frame=f)
# Side swept fringe, simple overlaps rather than lots of thin strands.
ribbon('Fringe_swept',[(hx+.032,hy-.025,hz+.112),(hx+.027,hy-.081,hz+.086),(hx-.005,hy-.096,hz+.055),(hx-.058,hy-.075,hz+.008)],[.042,.046,.032,.002],hair)
ribbon('Fringe_secondary',[(hx+.054,hy-.014,hz+.105),(hx+.070,hy-.056,hz+.049),(hx+.076,hy-.037,hz-.016)],[.020,.018,.002],hair)
ribbon('Side_lock_near',[(hx+.078,hy-.0,hz+.043),(hx+.090,hy+.003,hz-.09),(hx+.116,hy+.032,1.30),(hx+.14,hy+.07,1.22)],[.029,.032,.032,.003],hair,animate=True)
ribbon('Side_lock_far',[(hx-.08,hy-.0,hz+.05),(hx-.094,hy+.01,hz-.09),(hx-.13,hy+.04,1.26)],[.027,.033,.003],hair,animate=True)

# Rigid hammer geometry, small head with steel caps and a visibly long handle.
def cube(name,loc,scale,material,parent=None,bevel=.015):
    bpy.ops.mesh.primitive_cube_add(size=1);o=bpy.context.object;o.name=name;o.location=loc;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if parent:o.parent=parent
    o.data.materials.append(material)
    if bevel:
        b=o.modifiers.new('Crafted_edge_bevel','BEVEL');b.width=bevel;b.segments=2;o.modifiers.new('Weighted_normals','WEIGHTED_NORMAL')
    return o
shaft=cube('Hammer_handle',(0,0,.34),(.032,.032,1.25),wood,weapon,.009)
head=cube('Hammer_head',(0,0,.96),(.29,.17,.18),metal,weapon,.017)
for sign in [-1,1]:cube('Hammer_steel_endcap',(sign*.142,0,.96),(.035,.183,.19),edge,weapon,.009)
for z in [-.2,-.14,-.08,.03,.09,.15]:cube('Handle_wrap',(0,0,z),(.039,.039,.026),leather,weapon,.003)
for x in [-.09,.09]:
    for z in [.912,1.008]:cube('Hammer_rivet',(x,-.088,z),(.019,.008,.019),black,weapon,.004)
# Hands follow the same target as the wrist. Four curved fingers can open on the free hand.
def finger_mesh(name,z,parent,material,index):
    closed=[(.030,.009,z),(.016,-.030,z),(-.018,-.034,z),(-.028,-.004,z),(-.010,.012,z)]
    opened=[(.030,.009,z),(.028,-.018,z),(.030,-.048,z),(.034,-.071,z),(.035,-.082,z)]
    def meshcoords(points):
        vs=[];n=8
        for j,pt in enumerate(points):
            d=Vector(points[min(j+1,4)])-Vector(points[max(j-1,0)]);q=Vector((0,0,1)).rotation_difference(d.normalized())
            rad=.006*(1-.40*j/4)
            for k in range(n):vs.append(Vector(pt)+q@Vector((rad*math.cos(k*math.tau/n),rad*math.sin(k*math.tau/n),0)))
        return vs
    coords=meshcoords(closed);fs=[]
    for j in range(4):
        for k in range(8):fs.append((j*8+k,j*8+(k+1)%8,(j+1)*8+(k+1)%8,(j+1)*8+k))
    fs.extend([tuple(reversed(range(8))),tuple(32+i for i in range(8))])
    me=bpy.data.meshes.new(name);me.from_pydata(coords,[],fs);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.parent=parent;me.materials.append(material)
    for poly in me.polygons:poly.use_smooth=True
    o.shape_key_add(name='Basis');key=o.shape_key_add(name='Open_fingers');key.value=0
    for v,co in zip(key.data,meshcoords(opened)):v.co=co
    if parent==far:
        for f,v in [(1,.55),(5,.55),(9,1),(11,.65),(12,0),(60,0),(62,.65),(65,1),(69,.55),(72,.55)]:key.value=v;key.keyframe_insert('value',frame=f)
    return o
for side,parent in [('Left',near),('Right',far)]:
    cube(side+'_palm',(.028,.010,0),(.026,.05,.062),skin,parent,.012)
    for index,dz in enumerate([-.024,-.008,.008,.024]):finger_mesh(side+'_finger_'+str(index),dz,parent,skin,index)
    thumb=cube(side+'_thumb',(.014,.024,.03),(.038,.024,.02),skin,parent,.01);thumb.rotation_euler.z=-.5

# Match contact height to the actual rigid head's lowest box corner at impact.
scene.frame_set(33);bpy.context.view_layer.update()
headpoints=[head.matrix_world@Vector(c) for c in head.bound_box]
contact_z=min(v.z for v in headpoints)
headcenter_world=head.matrix_world.translation.copy()
target_center=(headcenter_world.x,headcenter_world.y,contact_z/2)
targetmat=mat('Target_warm_red',(.31,.17,.12));groundmat=mat('Ground_slate',(.24,.31,.34));trim=mat('Target_steel_band',(.23,.27,.28))
cube('Target_block',target_center,(.70,.48,contact_z),targetmat,None,.015)
for x in [headcenter_world.x-.26,headcenter_world.x+.26]:cube('Target_band',(x,headcenter_world.y,contact_z/2),(.035,.49,contact_z+.002),trim,None,.002)
# Mark the surface to judge whether contact really happens.
for dx in [-.1,0,.1]:cube('Contact_mark',(headcenter_world.x+dx,headcenter_world.y,contact_z+.001),(.008,.18,.001),ivory,None,0)
cube('Stage_floor',(0,0,-.045),(200,200,.08),groundmat,None,0)
# A soft continuous floor prevents large toon-light bands from distracting from the actor.
groundmat.node_tree.nodes.clear()
gout=groundmat.node_tree.nodes.new('ShaderNodeOutputMaterial');gbsdf=groundmat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
gbsdf.inputs['Base Color'].default_value=(.19,.245,.26,1);gbsdf.inputs['Roughness'].default_value=1
groundmat.node_tree.links.new(gbsdf.outputs[0],gout.inputs[0])
# Eevee soft lighting + banded shaders. No camera shake, flash or speed lines in this diagnostic pass.
world=bpy.data.worlds.new('Studio_world');scene.world=world;world.use_nodes=True;world.node_tree.nodes['Background'].inputs[0].default_value=(.25,.31,.36,1);world.node_tree.nodes['Background'].inputs[1].default_value=.4
for name,loc,power,size in [('Key',(-3,-4,6),900,4),('Rim',(2,3,4),1100,3),('Fill',(4,-1,3),350,4)]:
    bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.name=name;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(6,-4,2.6));camera=bpy.context.object;camera.name='Camera_presentation';camera.rotation_euler=(Vector((0,-.19,1.20))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=3.05;scene.camera=camera
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=720;scene.render.resolution_y=720;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.film_transparent=False;scene.view_settings.view_transform='Standard'
scene.render.fps=20
# Improve the saved viewport as well as exported frames.
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'

checks=[]
for f in range(1,73):
    scene.frame_set(f);bpy.context.view_layer.update()
    wrists={s:rig.matrix_world@rig.pose.bones[s+'Hand'].head for s in grips}
    checks.append({'frame':f,'wrist_error':max((wrists[s]-grips[s].matrix_world.translation).length for s in grips),
       'ankle_error':max((rig.matrix_world@rig.pose.bones[s+'Foot'].head-feet[s].matrix_world.translation).length for s in feet),
       'hammer_bottom_z':min((head.matrix_world@Vector(c)).z for c in head.bound_box)})
report={'fps':20,'frames':72,'authored_key_frames':[k[0] for k in KEYS], 'max_wrist_error_m':max(x['wrist_error'] for x in checks),'max_ankle_error_m':max(x['ankle_error'] for x in checks),
'contact_frame':33,'contact_surface_z':contact_z,'contact_gap_m':checks[32]['hammer_bottom_z']-contact_z,'far_grip_catch_frame':13,'far_grip_release_frame':61,'minimum_head_floor_clearance_m':min(x['hammer_bottom_z'] for x in checks),
'finger_open_key_default_near':bpy.data.objects['Left_finger_0'].data.shape_keys.key_blocks['Open_fingers'].value,
'method':'Authored character meshes and keyposes over previous skeleton. Eevee cel bands; no AI image/video regeneration.',
'limitations':['Stylized proportion study, not final Hizuru character model.','Mesh seam cleanup and facial likeness need further art direction.','Hands are curved stylized meshes with finger opening keys, not anatomical finger bones.','Target contact is staged, not a physics collision.','Hair tip motion is authored shape-key overlap, not simulation.','Far-hand constraint is deliberately switched at matched contact/release frames.'], 'per_frame':checks}
(OUT/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
scene.frame_set(1);bpy.context.view_layer.update()
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'st_hizuru_shoulder_polish.blend'))
mode=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'keys'
frames=range(1,73) if mode=='all' else [1,10,16,24,28,34,38,48,58,66,72]
dest=OUT/'frames';dest.mkdir(exist_ok=True)
for f in frames:
    scene.frame_set(f);scene.render.filepath=str(dest/f'{f:04d}.png');bpy.ops.render.render(write_still=True)
print('STUDY_REPORT',json.dumps({k:v for k,v in report.items() if k!='per_frame'}))
