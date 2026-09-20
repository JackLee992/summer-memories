"""Original stylized meshes, authored procedurally in Blender; meters, Z-up, face -Y.
Run: Blender -b --python-exit-code 1 --python tools/art/build_squad_models.py -- REPO_ROOT
Exports editable segmented characters with named animation pivots and coastal prop kits.
"""
import bpy, bmesh, math, sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(sys.argv[sys.argv.index('--')+1]);OUT=ROOT/'Assets/Resources/Art/Models';OUT.mkdir(parents=True,exist_ok=True)

def reset():
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
def mat(name,hex):
    rgb=tuple(int(hex[i:i+2],16)/255 for i in (0,2,4));m=bpy.data.materials.new(name);m.diffuse_color=(*rgb,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*rgb,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.72;return m
def coords(p):return (p[0],-p[2],p[1])
def parent(o,p):
    if p:
        bpy.context.view_layer.update()
        mw=o.matrix_world.copy();o.parent=p;o.matrix_world=mw
    return o
def empty(name,pos=(0,0,0),par=None):
    o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=coords(pos);return parent(o,par)
def uv(name,pos,scale,m,par=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=coords(pos));o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);o.data.materials.append(m)
    for f in o.data.polygons:f.use_smooth=True
    return parent(o,par)
def cube(name,pos,scale,m,par=None,bevel=.03):
    bpy.ops.mesh.primitive_cube_add(size=1,location=coords(pos));o=bpy.context.object;o.name=name;o.scale=(scale[0],scale[2],scale[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
    if bevel:
        b=o.modifiers.new('SoftEdges','BEVEL');b.width=bevel;b.segments=2;o.modifiers.new('Normals','WEIGHTED_NORMAL')
    return parent(o,par)
def tube(name,points,radii,m,par=None,sides=12):
    verts=[];faces=[]
    for (x,y,z),r in zip(points,radii):
        if not isinstance(r,tuple):r=(r,r)
        for k in range(sides):
            a=2*math.pi*k/sides;verts.append(coords((x+math.cos(a)*r[0],y,z+math.sin(a)*r[1])))
    for j in range(len(points)-1):
        for k in range(sides):a=j*sides+k;b=j*sides+(k+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.append(tuple(reversed(range(sides))));faces.append(tuple(range((len(points)-1)*sides,len(points)*sides)))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in faces]);mesh.materials.append(m);mesh.update();o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    for f in mesh.polygons:f.use_smooth=True
    # Profiles may run upward (torso) or downward (sleeves/skirt).
    # Orient each closed tube by its signed volume instead of assuming ring order.
    bm=bmesh.new();bm.from_mesh(mesh);volume=bm.calc_volume(signed=True)
    if abs(volume) < 1e-10:
        bm.free();raise RuntimeError('Degenerate mesh: '+name)
    if volume < 0:
        bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.normal_update();bm.to_mesh(mesh)
    bm.free();mesh.update()
    return parent(o,par)
def beam(name,a,b,r,m,par=None):
    aa,bb=Vector(coords(a)),Vector(coords(b));d=bb-aa;bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=r,depth=d.length,location=(aa+bb)/2);o=bpy.context.object;o.name=name;o.rotation_mode='QUATERNION';o.rotation_quaternion=d.to_track_quat('Z','Y');o.data.materials.append(m);return parent(o,par)
def export(name):
    bpy.context.view_layer.update()
    verts=[o.matrix_world@Vector(c) for o in bpy.context.scene.objects if o.type=="MESH" for c in o.bound_box]
    print(name,"bounds",[round(max(v[k] for v in verts)-min(v[k] for v in verts),3) for k in range(3)])
    bpy.ops.object.select_all(action='SELECT');bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,path_mode='AUTO')

def ribbon(name, points, widths, m, par):
    # A lenticular strip gives hair a broad flowing surface and a tapered edge.
    verts=[];faces=[]
    for (x,y,z),w in zip(points,widths):
        for across,depth in [(-1,0),(-.55,.010),(0,.015),(.55,.010),(1,0),(0,-.014)]:
            verts.append(coords((x+across*w,y,z+depth)))
    for j in range(len(points)-1):
        for k in range(6):
            a=j*6+k;b=j*6+(k+1)%6;faces.append((a,b,b+6,a+6))
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in faces]);mesh.materials.append(m);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    for f in mesh.polygons:f.use_smooth=True
    return parent(o,par)

def character(who):
    reset();ushio=who=='ushio';hizuru=who=='hizuru';root=empty('Character')
    skin=mat('Skin','EFC4AB');skinShade=mat('SkinShade','BA806F');white=mat('Cloth','D5E5E4');dark=mat('Navy','213540');cloth=mat('Jacket' if hizuru else 'Trouser','222A3B' if hizuru else '30516B');hair=mat('Hair','C6923F' if ushio else '16212E');hairLight=mat('HairLight','E6BA66' if ushio else '253444');eye=mat('Eye','3C7898');black=mat('Ink','101C28');shoe=mat('Shoe','F0E4CF' if ushio else '26303B')
    hips=empty('Hips',(0,.91,0),root);torso=empty('TorsoPivot',(0,1.03,0),hips)
    tube('TailoredTorso',[(0,.91,0),(0,1.03,0),(0,1.17,0),(0,1.31,0),(0,1.4,0)],[(.18,.105),(.16,.096),(.187,.11),(.235,.125),(.235,.10)],skin if ushio else cloth if hizuru else white,torso,sides=24)
    tube('ShoulderLine',[(0,1.4,0),(0,1.435,0),(0,1.45,0)],[(.23,.10),(.14,.077),(.073,.065)],skin if ushio else cloth if hizuru else white,torso,sides=24)
    tube('Neck',[(0,1.40,0),(0,1.53,0)], [.064,.061],skin,torso)
    head=empty('Head',(0,1.49,0),torso)
    # Six-and-a-half head silhouette, tapered jaw rather than overlapping balls.
    tube('Face',[(0,1.49,.032),(0,1.53,.022),(0,1.61,.012),(0,1.70,0),(0,1.76,-.01),(0,1.785,-.014)],[(.045,.067),(.085,.106),(.126,.126),(.13,.127),(.10,.098),(.015,.019)],skin,head,sides=32)
    for side in [-1,1]:
        uv('Ear',(side*.13,1.62,-.003),(.022,.037,.024),skin,head)
        uv('EyeWhite',(side*.053,1.657,.121),(.038,.018,.012),white,head)
        uv('Iris',(side*.053,1.657,.132),(.015,.017,.005),eye,head)
        uv('Pupil',(side*.053,1.657,.137),(.007,.012,.002),black,head)
        uv('Catchlight',(side*.048,1.664,.139),(.004,.004,.002),white,head)
        beam('UpperLid',(side*.023,1.674,.126),(side*.088,1.675,.115),.0045,black,head)
        beam('Brow',(side*.027,1.698,.116),(side*.091,1.696,.102),.005,hair,head)
    uv('Nose',(0,1.610,.133),(.012,.020,.021),skin,head)
    beam('Mouth',(-.024,1.551,.12),(.024,1.551,.12),.003,skinShade,head)
    # Closed curved scalp with a high hairline at the forehead and low nape.
    verts=[];faces=[];rings=10;segments=40
    for j in range(rings):
        t=j/(rings-1)
        for k in range(segments):
            a=2*math.pi*k/segments;front=max(0,math.cos(a));theta=t*(2.1-.95*front)
            verts.append(coords((math.sin(a)*math.sin(theta)*.143,1.655+math.cos(theta)*.156,-.016+math.cos(a)*math.sin(theta)*.14)))
    for j in range(rings-1):
        for k in range(segments):
            a=j*segments+k;b=j*segments+(k+1)%segments;faces.append((a,b,b+segments,a+segments))
    mesh=bpy.data.meshes.new('Scalp');mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in faces]);mesh.materials.append(hair);mesh.update();cap=bpy.data.objects.new('Scalp',mesh);bpy.context.collection.objects.link(cap);parent(cap,head)
    for f in mesh.polygons:f.use_smooth=True
    for i in range(7):
        x=(i-3)*.036
        ribbon('Fringe',[(x*.3,1.797,.035),(x*.75,1.766,.102),(x,1.715,.14),(x+.02,1.676+(i%3)*.012,.136)],[.023,.027,.022,.001],hairLight if i==2 else hair,head)
    if ushio or hizuru:
        # Layered asymmetric hair sheets curve around the shoulders; no cylinder curtain.
        for i in range(11):
            a=math.pi*.52+i*math.pi*.096;x=math.sin(a)*.133;z=math.cos(a)*.139-.012
            strand=empty('HairStrand'+str(i),(x,1.73,z),head)
            length=(.79 if ushio else 1.0)+.09*math.sin(i*.91)
            endY=1.72-length
            ribbon('LongHair',[(x,1.73,z),(x*1.13,1.5,z*1.25-.022),(x*1.5,1.22,z*1.1-.073),(x*1.65,endY+.1,z-.13),(x*1.5+.023*math.sin(i),endY,z-.11)],[.047,.054,.056,.043,.001],hairLight if i%4==1 else hair,strand)
        for side in [-1,1]:
            ribbon('SideLock',[(side*.123,1.72,.031),(side*.151,1.51,.059),(side*.156,1.30,.057),(side*.19,1.19,.088)],[.023,.029,.017,.001],hair,head)
    else:
        for i in range(7):
            x=(i-3)*.034
            ribbon('BackLock',[(x*.5,1.78,-.062),(x,1.72,-.145),(x*1.07,1.61,-.122),(x*1.09,1.55,-.095)],[.028,.032,.025,.001],hairLight if i==1 else hair,head)
    if ushio:
        tube('DressBodice',[(0,1.00,0),(0,1.13,0),(0,1.30,0)],[(.166,.104),(.176,.112),(.204,.13)],white,torso,sides=24)
        tube('DressSkirt',[(0,1.01,0),(0,.87,0),(0,.69,0),(0,.60,0)],[(.17,.106),(.208,.135),(.27,.17),(.295,.18)],white,hips,sides=32)
        for side in [-1,1]:beam('Strap',(side*.145,1.28,.098),(side*.15,1.43,.012),.017,white,torso)
        cube('Belt',(0,1.02,.108),(.32,.021,.012),eye,torso,.003)
        for x in [-.035,.035]:uv('Bow',(x,1.02,.12),(.031,.018,.015),eye,torso)
    elif hizuru:
        cube('ShirtInset',(0,1.28,.123),(.13,.27,.012),white,torso,.006)
        for side in [-1,1]:beam('Lapel',(side*.047,1.1,.123),(side*.14,1.4,.09),.025,cloth,torso)
        for x in [-.058,.058]:
            for y in [1.641,1.683]:beam('GlassFrame',(x-.046,y,.137),(x+.046,y,.137),.0045,black,head)
            for dx in [-.045,.045]:beam('GlassEdge',(x+dx,1.641,.137),(x+dx,1.683,.137),.0045,black,head)
        beam('Bridge',(-.014,1.663,.14),(.014,1.663,.14),.0045,black,head)
    else:
        for side in [-1,1]:
            beam('Suspender',(side*.14,.94,.107),(side*.185,1.4,.09),.018,dark,torso)
            beam('SuspenderBack',(side*.14,.94,-.108),(side*.185,1.4,-.09),.018,dark,torso)
            cube('Collar',(side*.043,1.414,.095),(.065,.068,.025),white,torso,.008)
        for y in [1.10,1.20,1.30]:uv('Button',(0,y,.126),(.006,.007,.004),dark,torso)
    for side in [-1,1]:
        label='Left' if side<0 else 'Right'
        arm=empty(label+'Arm',(side*.23,1.39,0),torso)
        elbow=(side*.285,1.095,.012);wrist=(side*.29,.86,.055)
        armMat=skin if ushio else cloth if hizuru else white
        tube('UpperSleeve',[(side*.22,1.40,0),(side*.265,1.32,0),(side*.282,1.15,.01),(side*.285,1.092,.012)],[(.071,.082),(.077,.077),(.061,.062),(.052,.053)],armMat,arm,sides=16)
        fore=empty(label+'Forearm',elbow,arm)
        tube('Forearm',[(side*.285,1.10,.012),(side*.289,1.025,.023),(side*.29,.875,.054)],[(.053,.054),(.056,.052),(.035,.035)],cloth if hizuru else skin,fore,sides=16)
        if not ushio and not hizuru:tube('RolledCuff',[(side*.286,1.105,.012),(side*.286,1.05,.024)],[(.059,.06),(.06,.058)],white,fore)
        uv('Hand',(side*.29,.837,.06),(.039,.060,.029),skin,fore)
        uv('Thumb',(side*.258,.843,.078),(.018,.034,.022),skin,fore)
        if side<0 and not ushio and not hizuru:
            cube('Watch',(side*.29,.897,.092),(.061,.039,.02),dark,fore,.006);cube('WatchFace',(side*.29,.897,.104),(.04,.027,.004),eye,fore,.002)
        leg=empty(label+'Leg',(side*.10,.91,0),hips)
        knee=(side*.10,.49,.006)
        tube('Thigh',[(side*.099,.94,0),(side*.10,.82,0),(side*.10,.64,0),knee],[(.10,.10),(.094,.096),(.075,.078),(.057,.060)],skin if ushio else cloth,leg,sides=16)
        shin=empty(label+'Shin',knee,leg)
        tube('Calf',[knee,(side*.10,.38,-.016),(side*.10,.22,-.008),(side*.10,.11,.006)],[(.058,.062),(.062,.065),(.045,.045),(.034,.034)],skin if ushio else cloth,shin,sides=16)
        uv('Foot',(side*.10,.063,.063),(.061,.055,.12),shoe,shin)
        if ushio:cube('SandalStrap',(side*.10,.098,.106),(.12,.018,.035),white,shin,.004)
    if hizuru:
        fore=bpy.data.objects.get('RightForearm');wood=mat('WeaponHandle','765644');metal=mat('WeaponHead','52636C')
        beam('HammerShaft',(.29,.85,.074),(.29,.37,.074),.022,wood,fore)
        cube('HammerHead',(.29,.37,.074),(.31,.13,.13),metal,fore,.025)
    export('st_'+who+'_v02')

for who in ['shinpei','ushio','hizuru']:character(who)
# Reusable gabled coastal house, with separate material groups and meters.
reset();r=empty('House');plaster=mat('Plaster','ECE4C9');wood=mat('Wood','644D3B');roof=mat('Roof','314E60');glass=mat('Glass','427B86');trim=mat('Trim','DDD9BB')
cube('Walls',(0,1.6,0),(5,3.2,4),plaster,r,.06)
for x in [-2.1,2.1]:cube('Corner',(x,1.55,2.02),(.14,3.1,.15),wood,r)
for x in [-1.45,1.45]:
    cube('WindowFrame',(x,1.9,2.035),(1.25,1.4,.12),wood,r)
    cube('WindowGlass',(x,1.9,2.105),(1.04,1.20,.02),glass,r)
    cube('WindowBar',(x,1.9,2.13),(.045,1.2,.045),trim,r,.01)
    cube('WindowBar',(x,1.9,2.13),(1.05,.045,.045),trim,r,.01)
cube('Door',(0,1.15,2.04),(.85,2.25,.12),wood,r);cube('DoorGlass',(0,1.5,2.12),(.6,1.05,.03),glass,r)
for side in [-1,1]:
    o=cube('RoofSlope',(side*1.42,3.54,0),(3.2,.18,4.7),roof,r,.035);o.rotation_euler[1]=0;o.rotation_euler[0]=0;o.rotation_euler[2]=0
    # Blender Y rotation tips a slope along X around horizontal Y.
    o.rotation_euler[1]=side*math.radians(21)
    for k in range(12):
        z=-2.24+k*.41;beam('RoofTile',(-2.93,3.03,z),(0,4.13,z),.042,roof,r);beam('RoofTile',(0,4.13,z),(2.93,3.03,z),.042,roof,r)
beam('Ridge',(0,4.13,-2.42),(0,4.13,2.42),.10,wood,r)
for y in [.5,.9,1.3]:cube('TimberBand',(0,y,2.07),(4.9,.065,.055),trim,r,.01)
export('st_house_v02')
reset();r=empty('FishingBoat');hull=mat('Hull','EAEBDA');blue=mat('Stripe','267D92');wood=mat('Deck','AB8658')
# Hull ring profiles run along height, narrow bottom to rim.
tube('Hull',[(0,.05,0),(0,.35,0),(0,.70,0)],[(.35,1.8),(.65,2.2),(.83,2.45)],hull,r,sides=24)
tube('Gunwale',[(0,.60,0),(0,.72,0)],[(.81,2.43),(.84,2.45)],blue,r,sides=24)
cube('Deck',(0,.71,0),(1.3,.08,3.5),wood,r)
for z in [-.8,.7]:cube('Seat',(0,.85,z),(1.4,.14,.24),hull,r)
export('st_boat_v02')
print('EXPORTED',list(OUT.glob('*v02.fbx')))
