"""Build the original Harbor Watch pedestrian and baked gait. Run with Blender -b -P script."""
import math
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 24
V = Vector
Z = V((0, 0, 1))
bones = {}
verts, faces, face_materials, weights = [], [], [], []
palette = {
    "Denim": (.075, .16, .22), "Cuff": (.105, .21, .26),
    "Jacket": (.68, .26, .095), "Hem": (.43, .145, .055),
    "Skin": (.62, .365, .235), "Hair": (.12, .065, .045),
    "Leather": (.18, .12, .085), "Sole": (.72, .67, .54),
    "Metal": (.78, .63, .39), "Shirt": (.73, .72, .62),
}
materials = list(palette)

def bone(name, head, tail, parent=None):
    bones[name] = (V(head), V(tail), parent)

def rings(b, mat, layers, sides=16):
    """Elliptical sections (centre x/y, height, x/y radii) in bind pose."""
    first = len(verts)
    for x, y, z, rx, ry in layers:
        for i in range(sides):
            t = 2 * math.pi * i / sides
            verts.append((x + rx * math.cos(t), y + ry * math.sin(t), z))
            weights.append(b)
    new = [tuple(first + i for i in reversed(range(sides)))]
    for j in range(len(layers) - 1):
        for i in range(sides):
            a = first + j * sides + i
            c = first + j * sides + (i + 1) % sides
            new.append((a, c, c + sides, a + sides))
    new.append(tuple(first + (len(layers)-1)*sides+i for i in range(sides)))
    faces.extend(new)
    face_materials.extend([materials.index(mat)] * len(new))

def shell(b, mat, x, y, sections):
    rings(b, mat, [(x,y,z,rx,ry) for z,rx,ry in sections])

def oval(b, mat, center, size):
    x,y,z = center
    rx,ry,rz = size
    layers = []
    for i in range(11):
        a = math.pi * (-.499 + .998*i/10)
        layers.append((x,y,z+rz*math.sin(a),rx*math.cos(a),ry*math.cos(a)))
    rings(b,mat,layers)

bone("RigRoot",(0,0,0),(0,0,.12))
bone("Hips",(0,0,.925),(0,0,1.05),"RigRoot")
bone("Chest",(0,0,1.05),(0,0,1.47),"Hips")
bone("Head",(0,0,1.49),(0,0,1.74),"Chest")
shell("Hips","Denim",0,0,[(.825,.139,.103),(.91,.168,.119),(1.035,.155,.115)])
shell("Chest","Jacket",0,0,[(.985,.158,.118),(1.02,.178,.131),(1.24,.18,.138),(1.40,.206,.127),(1.46,.173,.109),(1.482,.086,.081)])
shell("Chest","Hem",0,0,[(.983,.16,.12),(1.016,.177,.134)])
shell("Chest","Shirt",0,0,[(1.46,.094,.088),(1.489,.077,.074)])
oval("Chest","Metal",(0,.134,1.226),(.006,.006,.202))
oval("Chest","Skin",(0,0,1.505),(.067,.066,.080))
rings("Head","Skin",[(0,.018,1.515,.05,.063),(0,.014,1.543,.078,.084),(0,.006,1.595,.088,.09),(0,.004,1.657,.09,.094),(0,-.004,1.707,.084,.086),(0,0,1.735,.058,.058)])
oval("Head","Hair",(0,-.017,1.69),(.095,.092,.072))
oval("Head","Skin",(0,.094,1.615),(.017,.023,.023))
oval("Head","Hair",(0,.085,1.572),(.026,.004,.003))
for s,label in [(-1,"L"),(1,"R")]:
    def n(part): return part+"."+label
    x = s*.108
    bone(n("Thigh"),(x,0,.925),(x,0,.497),"Hips")
    bone(n("Shin"),(x,0,.497),(x,0,.075),n("Thigh"))
    bone(n("Foot"),(x,0,.075),(x,.155,.075),n("Shin"))
    shell(n("Thigh"),"Denim",x,0,[(.466,.07,.073),(.53,.074,.078),(.75,.086,.094),(.893,.098,.112),(.941,.087,.093)])
    shell(n("Shin"),"Denim",x,0,[(.104,.056,.057),(.17,.06,.067),(.34,.074,.078),(.48,.072,.077),(.521,.062,.068)])
    shell(n("Shin"),"Cuff",x,0,[(.099,.057,.061),(.139,.06,.064)])
    shell(n("Foot"),"Sole",x,.065,[(.002,.064,.135),(.019,.067,.139),(.029,.064,.139)])
    rings(n("Foot"),"Leather",[(x,.064,.027,.065,.138),(x,.06,.06,.065,.134),(x,.011,.096,.054,.078),(x,0,.117,.046,.049)])
    a=s*.202
    bone(n("UpperArm"),(a,0,1.425),(a,0,1.135),"Chest")
    bone(n("Forearm"),(a,0,1.135),(a,0,.91),n("UpperArm"))
    shell(n("UpperArm"),"Jacket",a,0,[(1.105,.054,.054),(1.18,.061,.064),(1.36,.071,.075),(1.424,.071,.076),(1.466,.038,.045)])
    shell(n("Forearm"),"Jacket",a,0,[(.95,.044,.046),(1.015,.052,.053),(1.13,.059,.062),(1.162,.051,.053)])
    shell(n("Forearm"),"Hem",a,0,[(.929,.042,.045),(.961,.047,.048)])
    oval(n("Forearm"),"Skin",(a,.004,.885),(.039,.03,.061))
    oval("Head","Skin",(s*.087,-.004,1.623),(.018,.022,.033))
    oval("Head","Hair",(s*.036,.089,1.643),(.011,.005,.005))
    oval("Head","Hair",(s*.036,.087,1.656),(.017,.005,.004))

arm = bpy.data.armatures.new("Harbor skeleton")
rig = bpy.data.objects.new("HarborWalker",arm)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active=rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
for name,(head,tail,parent) in bones.items():
    b=arm.edit_bones.new(name)
    b.head,b.tail=head,tail
    if parent: b.parent=arm.edit_bones[parent]
bpy.ops.object.mode_set(mode="OBJECT")
mesh=bpy.data.meshes.new("Harbor commuter")
mesh.from_pydata(verts,[],faces)
body=bpy.data.objects.new("Commuter",mesh)
scene.collection.objects.link(body)
for name,color in palette.items():
    mat=bpy.data.materials.new(name)
    mat.diffuse_color=(*color,1)
    mesh.materials.append(mat)
for polygon,index in zip(mesh.polygons,face_materials):
    polygon.material_index=index
    polygon.use_smooth=True
for name in bones:
    group=body.vertex_groups.new(name=name)
    indices=[i for i,b in enumerate(weights) if b==name]
    if indices: group.add(indices,1,"REPLACE")
body.parent=rig
mod=body.modifiers.new("Skeleton","ARMATURE")
mod.object=rig

def yaw(a): return Quaternion(Z,a)
def ease(t): return t*t*(3-2*t)
# Each step moves one shoe between explicit world-space contacts. Both shoes
# stay fixed during pauses; corner turns use three short stepping contacts.
# A smooth 90-degree arc replaces the old five-step corner pivot.
# 0.62 m radius gives about 0.32 m of progress per curved step (three
# weight-bearing contacts). There is no 18-frame standstill before corners.
# The whole route is closed and all 36 footsteps have alternating feet.
# We author the route twice so the last planted contacts become the first
# contacts, removing the loop seam without adding a fourth corner step.
center=V((-.95,-.95,0))
angle=0.
feet=[]
beats=[]
def beat(frames, dest=None, side=0, target_angle=None):
    global angle
    start=feet[side].copy()
    end=start if dest is None else dest
    end_angle=angle if target_angle is None else target_angle
    beats.append((frames,[p.copy() for p in feet],side,end.copy(),angle,end_angle,dest is not None))
    feet[side]=end.copy()
    angle=end_angle

def build_rounded_route(start_contacts):
    global center, angle, feet, beats
    center=V((-.95,-.95,0))
    angle=0.
    feet=[p.copy() for p in start_contacts]
    beats=[]
    beat(12)
    side=0
    for edge in range(4):
        origin=center.copy()
        for distance in (.32,.64,.96,1.28,1.60,1.92):
            center=origin+yaw(angle)@V((0,distance,0))
            beat(11,center+yaw(angle)@V(((-1,1)[side]*.108,0,0)),side)
            side=1-side
        arc_start=center.copy()
        entry_angle=angle
        for k in range(1,4):
            theta=k*math.pi/6  # three 30-degree forward steps around the bend
            delta=V((.62*(1-math.cos(theta)),.62*math.sin(theta),0))
            center=arc_start+yaw(entry_angle)@delta
            target=entry_angle-theta
            beat(10,center+yaw(target)@V(((-1,1)[side]*.108,0,0)),side,target)
            side=1-side
    beat(12)
    return [p.copy() for p in feet]

first_contacts=[center+V((s*.108,0,0)) for s in (-1,1)]
last_contacts=build_rounded_route(first_contacts)
build_rounded_route(last_contacts)
samples=[]
foot_yaw=[0.,0.]
for frames,start,side,end,a,b,moving in beats:
    foot_start=foot_yaw[side]
    for j in range(frames):
        t=j/frames
        u=ease(t)
        pos=[p.copy() for p in start]
        pos[side]=pos[side].lerp(end,u)
        lift=math.sin(math.pi*t)**2 if moving else 0
        pos[side].z += .087*lift
        angles=foot_yaw.copy()
        angles[side]=foot_start+(b-foot_start)*u
        samples.append((pos,angles,a+(b-a)*u,lift,side,-.24*math.sin(2*math.pi*t)*lift))
    foot_yaw[side]=b
# Feet and body end at their initial position, so the loop seam is stationary.
samples.append(([p.copy() for p in feet],list(foot_yaw),angle,0,0,0))
rest={n:arm.bones[n].matrix_local.copy() for n in bones}
for frame,(pos,angles,heading,lift,swing,pitch) in enumerate(samples):
    rot=yaw(heading)
    centre=V((0,0,0))
    for d,w in [(-4,.12),(-2,.2),(0,.36),(2,.2),(4,.12)]:
        p=samples[max(0,min(len(samples)-1,frame+d))][0]
        centre+=(p[0]+p[1])*(w*.5)
    centre.z=0
    stride=(rot.inverted()@(pos[0]-pos[1])).y
    hip=centre+rot@V(((-1,1)[1-swing]*.024*lift,0,.881+.017*lift))
    poses={}
    def pose(name,head,rotation):
        poses[name]=Matrix.Translation(head)@rotation.to_matrix().to_4x4()@rest[name].to_quaternion().to_matrix().to_4x4()
    def limb(name,head,tail):
        direction=(tail-head).normalized()
        local=rot.inverted()@direction
        delta=V((0,0,-1)).rotation_difference(local)
        pose(name,head,rot@delta)
    pose("RigRoot",hip-bones["Hips"][0],yaw(0))
    pose("Hips",hip,rot)
    chest=hip+V((0,0,.125))
    twist=yaw(heading+stride*.13)
    pose("Chest",chest,twist)
    pose("Head",chest+V((0,0,.44)),rot)
    for i,(s,label) in enumerate([(-1,"L"),(1,"R")]):
        def n(part): return part+"."+label
        thigh=hip+rot@V((s*.108,0,0))
        ankle=pos[i]+V((0,0,.075))
        diff=ankle-thigh
        axis=diff.normalized()
        pole=rot@V((0,1,0))
        bend=(pole-axis*pole.dot(axis)).normalized()
        along=(.428**2-.422**2+diff.length_squared)/(2*diff.length)
        knee=thigh+axis*along+bend*math.sqrt(max(0,.428**2-along**2))
        limb(n("Thigh"),thigh,knee)
        limb(n("Shin"),knee,ankle)
        roll = pitch if i==swing else 0
        pose(n("Foot"),ankle,yaw(angles[i])@Quaternion(V((1,0,0)),roll))
        shoulder=chest+twist@V((s*.202,0,.375))
        arm_angle=s*stride*.82
        upper=twist@Quaternion(V((1,0,0)),arm_angle)
        lower=twist@Quaternion(V((1,0,0)),arm_angle+.27)
        pose(n("UpperArm"),shoulder,upper)
        pose(n("Forearm"),shoulder+upper@V((0,0,-.29)),lower)
    for name,(_,_,parent) in bones.items():
        bind=rest[name] if parent is None else poses[parent]@rest[parent].inverted()@rest[name]
        pb=rig.pose.bones[name]
        prev=pb.rotation_quaternion.copy()
        pb.matrix_basis=bind.inverted()@poses[name]
        quat=pb.rotation_quaternion.copy()
        quat.make_compatible(prev)
        pb.rotation_quaternion=quat
        for channel in ("location","rotation_quaternion"):
            pb.keyframe_insert(data_path=channel,frame=frame+1,group=name)
rig.animation_data.action.name="HarborWalk"
scene.frame_start=1
scene.frame_end=len(samples)
scene.frame_set(1)
out=Path(__file__).resolve().parents[2]/"unity/HarborWatch/Assets/HarborWatch/Resources/HarborWalker.fbx"
bpy.ops.export_scene.fbx(filepath=str(out),object_types={"ARMATURE","MESH"},add_leaf_bones=False,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.15,axis_forward="-Z",axis_up="Y")
print("WALKER",len(samples),"frames",len(verts),"vertices",out)
