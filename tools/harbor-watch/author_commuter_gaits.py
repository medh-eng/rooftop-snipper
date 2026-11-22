"""Generate two original, in-place commuter gaits for ambient harbor traffic.

Usage (requires Blender + NumPy):
    blender --background --python-exit-code 1 --python tools/harbor-watch/author_commuter_gaits.py

This shares the hand-made 14-bone commuter from author_walker.py. It does not
download third-party animations. Root translation is constant: the Unity traffic
controller owns route positions and uses calibrated clip playback speeds.
"""
from pathlib import Path
import math
import runpy

import bpy
from mathutils import Matrix, Quaternion, Vector

source = Path(__file__).resolve().with_name("author_walker.py")
# Reuse the original rig and mesh source; this also rebuilds HarborWalker.fbx.
g = runpy.run_path(str(source))
rig, arm, bones = g["rig"], g["arm"], g["bones"]
V, Z = Vector, Vector((0, 0, 1))
rest = {n: arm.bones[n].matrix_local.copy() for n in bones}

def make_loop(label, speed_mps, frame_count, stance_share, step_lift, hip_height):
    """Bake an in-place walk or jog with stance feet sliding backwards at
    body speed (so a matched world-moving rig keeps contact feet planted).
    Both feet are offset by half a cycle; endpoints have identical poses.
    """
    rig.animation_data.action = bpy.data.actions.new(label)
    scene = bpy.context.scene
    scene.frame_start = 1
    scene.frame_end = frame_count + 1
    period = frame_count / 24.0
    stride = speed_mps * period
    half_stance = 0.5 * stride * stance_share

    for frame in range(frame_count + 1):
        t = frame / frame_count
        pos, foot_tilt = [], []
        for i, sign in enumerate((-1, 1)):
            phase = (t + i * 0.5) % 1.0
            if phase < stance_share:
                x = half_stance * (1.0 - 2.0 * phase / stance_share)
                lift = 0.0
                pitch = 0.0
            else:
                u = (phase - stance_share) / (1.0 - stance_share)
                smooth = u * u * (3.0 - 2.0 * u)
                x = half_stance * (2.0 * smooth - 1.0)
                lift = step_lift * math.sin(math.pi * u) ** 2
                pitch = -0.12 * math.sin(math.pi * u)
            pos.append(V((sign * .108, x, lift)))
            foot_tilt.append(pitch)

        # Mild pelvis sway and twice-per-cycle vertical bob; no big root shift.
        hip = V((.014 * math.sin(2 * math.pi * t),
                 .010 * math.sin(4 * math.pi * t),
                 hip_height + (.018 if label == "CrowdWalk" else .032)
                 * math.cos(4 * math.pi * t)))
        pelvis_roll = .028 * math.sin(2 * math.pi * t)
        trunk_yaw = .045 * math.sin(2 * math.pi * t)
        poses = {}
        def pose(name, head, rotation):
            poses[name] = (Matrix.Translation(head)
                           @ rotation.to_matrix().to_4x4()
                           @ rest[name].to_quaternion().to_matrix().to_4x4())

        def limb(name, head, tail):
            direction = (tail - head).normalized()
            pole = V((0, 1, 0))
            delta = V((0, 0, -1)).rotation_difference(direction)
            pose(name, head, delta)

        # The scene-model root is in place, allowing route-driven Unity motion.
        pose("RigRoot", hip - bones["Hips"][0], Quaternion(Z, 0))
        pose("Hips", hip, Quaternion(Z, pelvis_roll))
        chest = hip + V((0, 0, .125))
        pose("Chest", chest, Quaternion(Z, trunk_yaw))
        pose("Head", chest + V((0, 0, .44)), Quaternion(Z, 0))
        for i, (sign, suffix) in enumerate(((-1, "L"), (1, "R"))):
            def n(part): return part + "." + suffix
            thigh = hip + V((sign * .108, 0, 0))
            ankle = pos[i] + V((0, 0, .075))
            diff = ankle - thigh
            direction = diff.normalized()
            distance = max(.0001, diff.length)
            pole = V((0, 1, 0))
            bend = (pole - direction * pole.dot(direction)).normalized()
            along = (.428 ** 2 - .422 ** 2 + distance ** 2) / (2 * distance)
            along = max(-.428, min(.428, along))
            knee = thigh + direction * along + bend * math.sqrt(max(0, .428 ** 2 - along ** 2))
            limb(n("Thigh"), thigh, knee)
            limb(n("Shin"), knee, ankle)
            pose(n("Foot"), ankle, Quaternion(V((1, 0, 0)), foot_tilt[i]))

            swing = (.28 if label == "CrowdWalk" else .53) * math.sin(
                2 * math.pi * (t + i * .5))
            shoulder = chest + V((sign * .202, 0, .375))
            upper = Quaternion(V((1, 0, 0)), swing)
            lower = Quaternion(V((1, 0, 0)), swing + .24)
            pose(n("UpperArm"), shoulder, upper)
            pose(n("Forearm"), shoulder + upper @ V((0, 0, -.29)), lower)

        for name, (_, _, parent) in bones.items():
            bind = (rest[name] if parent is None
                    else poses[parent] @ rest[parent].inverted() @ rest[name])
            pb = rig.pose.bones[name]
            pb.matrix_basis = bind.inverted() @ poses[name]
            for channel in ("location", "rotation_quaternion"):
                pb.keyframe_insert(data_path=channel, frame=frame + 1, group=name)

    scene.frame_set(1)
    output = Path(__file__).resolve().parents[2] / "unity/HarborWatch/Assets/HarborWatch/Resources" / (label + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=str(output), object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False, bake_anim_use_all_actions=False,
        bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0,
        axis_forward="-Z", axis_up="Y")
    print("AMBIENT_GAIT", label, frame_count, "frames", speed_mps,
          "m/s", output)

make_loop("CrowdWalk", 1.05, 24, .58, .105, .81)
make_loop("CrowdJog", 2.05, 18, .45, .14, .79)
