"""
slice_neon_enemies_v2.py
-------------------------
Master Asset Extraction Script for Level 3 Neon Enemies:
1. VortexOmega (Boss) - 6 states: Walk, Attack, Laser, PhaseTrans, EnrageAttack, Die (Clean flood-fill, 384x256 canvas)
2. HiveInquisitor (Vorst) - 5 states: Walk, Attack (Laser), Missiles, VortexFlame, Die (Full laser beam, 768x256 canvas)
3. BioHulk - 4 states: Walk, Attack (Slam), Laser (Chest Cannon), Die (512x256 canvas for Laser)
4. BioLasher - 4 states: Walk, Attack (Whip), Tornado (Cyclone), Die (256x256 canvas)
5. BioStalker - 3 states: Walk, Attack, Die (256x256 canvas)

Enforces:
- 100% transparent 1px outer perimeter border on every single PNG.
- Horizontal flip so all enemies face LEFT toward Kael by default.
- Floor contact at TARGET_FOOT_Y = 249.
"""

import os
import glob
import numpy as np
from PIL import Image
from scipy.ndimage import label, binary_fill_holes, binary_dilation

CANVAS_H = 256
TARGET_FOOT_Y = 249

OUTPUT_DIR = os.path.join("Assets", "_Project", "Resources", "Level3")
os.makedirs(OUTPUT_DIR, exist_ok=True)


def clean_perimeter_border(img_rgba):
    """Enforces 100% transparency on the 1px outer perimeter border."""
    arr = np.array(img_rgba)
    arr[0, :, 3] = 0
    arr[-1, :, 3] = 0
    arr[:, 0, 3] = 0
    arr[:, -1, 3] = 0
    return Image.fromarray(arr)


def map_frames_to_8(frames, state_name):
    """Expands source frames (4, 5, or 6) into a standardized 8-frame cycle."""
    if len(frames) == 8:
        return frames

    st = state_name.lower()
    n = len(frames)
    if n == 4:
        if "walk" in st or "run" in st or "idle" in st:
            indices = [0, 1, 2, 3, 2, 1, 0, 1]
        elif "attack" in st or "laser" in st or "missile" in st or "vortex" in st:
            indices = [0, 1, 2, 3, 3, 2, 1, 0]
        elif "die" in st or "death" in st:
            indices = [0, 1, 2, 3, 3, 3, 3, 3]
        else:
            indices = [0, 1, 2, 3, 2, 1, 0, 1]
    elif n == 5:
        if "walk" in st:
            indices = [0, 1, 2, 3, 4, 3, 2, 1]
        elif "die" in st:
            indices = [0, 1, 2, 3, 4, 4, 4, 4]
        else:
            indices = [0, 1, 2, 3, 3, 4, 4, 0]
    elif n == 6:
        if "walk" in st or "run" in st or "idle" in st:
            indices = [0, 1, 2, 3, 4, 5, 4, 3]
        elif "die" in st or "death" in st:
            indices = [0, 1, 2, 3, 4, 5, 5, 5]
        elif "phasetrans" in st:
            indices = [0, 1, 2, 3, 4, 5, 5, 4]
        else:
            indices = [0, 1, 2, 3, 4, 5, 4, 3]
    else:
        # Generic resample to 8
        indices = [int(round(i * (n - 1) / 7.0)) for i in range(8)]

    return [frames[idx] for idx in indices]


def clean_cell_slivers(cell_rgba, max_top_sliver_h=24, min_dust_size=35):
    """Removes stray pixels touching top/bottom borders from adjacent rows."""
    h = cell_rgba.shape[0]
    lbl, n_feat = label(cell_rgba[:, :, 3] > 0)
    if n_feat == 0:
        return cell_rgba

    sizes = np.bincount(lbl.ravel())
    for feat_id in range(1, n_feat + 1):
        if sizes[feat_id] < min_dust_size:
            cell_rgba[lbl == feat_id, 3] = 0
            continue
        ys, _ = np.where(lbl == feat_id)
        y_min, y_max = ys.min(), ys.max()
        if y_min == 0 and y_max <= max_top_sliver_h:
            cell_rgba[lbl == feat_id, 3] = 0
        elif y_max == h - 1 and (h - 1 - y_min) <= 16:
            cell_rgba[lbl == feat_id, 3] = 0

    return cell_rgba


# ==============================================================================
# 1. BOSS VORTEX-OMEGA (Clean Flood-fill, 384x256 Canvas)
# ==============================================================================
def process_boss():
    print("\n--- [1/5] Extracting Boss VortexOmega ---")
    source_path = glob.glob("*mecha*.png")[0]
    img = Image.open(source_path).convert("RGB")
    arr = np.array(img)

    row_defs = [
        ("Walk", 35, 175, [0, 240, 475, 710, 960, 1190, 1451], 256),
        ("Attack", 180, 325, [0, 242, 484, 749, 937, 1235, 1451], 384),
        ("Laser", 329, 456, [0, 247, 488, 768, 1028, 1220, 1451], 384),
        ("PhaseTrans", 506, 656, [0, 249, 485, 700, 940, 1198, 1451], 384),
        ("EnrageAttack", 706, 850, [0, 242, 477, 716, 992, 1226, 1451], 384),
        ("Die", 902, 1070, [0, 247, 476, 710, 955, 1202, 1451], 384),
    ]

    BOSS_SCALE = 1.22  # Consistent scale across all states

    for state_name, y1, y2, boundaries, canvas_w in row_defs:
        row_arr = arr[y1:y2, :, :].copy()
        R = row_arr[:, :, 0].astype(int)
        G = row_arr[:, :, 1].astype(int)
        B = row_arr[:, :, 2].astype(int)

        lum = 0.299 * R + 0.587 * G + 0.114 * B
        # Background is dark navy / slate:
        if state_name == "Die":
            is_bg = (lum < 32) & (R < 48) & (G < 40) & (B < 65)
        else:
            is_bg = (lum < 36) & (R < 55) & (G < 45) & (B < 75)

        lbl, num = label(is_bg)
        border = np.zeros_like(is_bg, dtype=bool)
        border[0, :] = True
        border[-1, :] = True
        border[:, 0] = True
        border[:, -1] = True
        border_lbls = set(lbl[border])
        border_lbls.discard(0)

        ext_bg = np.isin(lbl, list(border_lbls))
        alpha = np.where(ext_bg, 0, 255).astype(np.uint8)

        # For Die state, also eliminate dark dust smoke edges touching borders
        row_rgba = np.dstack([row_arr, alpha])

        raw_frames = []
        for c in range(6):
            x1 = boundaries[c]
            x2 = boundaries[c + 1]
            cell = row_rgba[:, x1:x2, :].copy()
            cell = clean_cell_slivers(cell, max_top_sliver_h=20, min_dust_size=40)

            ys, xs = np.where(cell[:, :, 3] > 0)
            if len(ys) == 0:
                raw_frames.append(Image.new("RGBA", (canvas_w, CANVAS_H), (0, 0, 0, 0)))
                continue

            # Keep main character component and large energy bursts
            cell_mask = cell[:, :, 3] > 0
            lbl_c, num_c = label(cell_mask)
            sizes_c = np.bincount(lbl_c.ravel())
            main_id = np.argmax(sizes_c[1:]) + 1
            keep_mask = (lbl_c == main_id)
            for cid in range(1, num_c + 1):
                if cid != main_id and sizes_c[cid] >= 400:
                    keep_mask |= (lbl_c == cid)
            cell[~keep_mask, 3] = 0

            ys, xs = np.where(cell[:, :, 3] > 0)
            cropped_arr = cell[ys.min() : ys.max() + 1, xs.min() : xs.max() + 1, :]
            # Flip horizontally so boss faces LEFT toward Kael
            flipped = Image.fromarray(cropped_arr).transpose(Image.Transpose.FLIP_LEFT_RIGHT)

            sw = int(round(flipped.width * BOSS_SCALE))
            sh = int(round(flipped.height * BOSS_SCALE))
            scaled = flipped.resize((sw, sh), Image.Resampling.LANCZOS)

            canvas = Image.new("RGBA", (canvas_w, CANVAS_H), (0, 0, 0, 0))

            if canvas_w == 256:
                off_x = (canvas_w - sw) // 2
            else:
                # On 384 canvas: anchor spine at x = 256 (128px from right border)
                # Flipped sprite has boss body on the right and attack/claws/flames on the left!
                # So off_x = 256 - (sw - right_body_margin)
                # To guarantee 1px margin on both sides:
                off_x = min(canvas_w - sw - 4, max(4, (canvas_w - sw) // 2))

            off_y = TARGET_FOOT_Y - sh
            # Ensure off_y doesn't exceed bounds
            if off_y < 2:
                # Scale down slightly if needed to preserve 1px top margin
                scale_adj = (TARGET_FOOT_Y - 4) / float(sh)
                sw = int(round(sw * scale_adj))
                sh = int(round(sh * scale_adj))
                scaled = flipped.resize((sw, sh), Image.Resampling.LANCZOS)
                off_y = TARGET_FOOT_Y - sh

            canvas.paste(scaled, (off_x, off_y), scaled)
            canvas = clean_perimeter_border(canvas)
            raw_frames.append(canvas)

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_path = os.path.join(OUTPUT_DIR, f"VortexOmega_{state_name}_{i}.png")
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for VortexOmega_{state_name} ({canvas_w}x{CANVAS_H})")


# ==============================================================================
# 2. HIVE INQUISITOR: Vorst (Full Laser Beam, Missiles, Vortex Flame, Walk, Die)
# ==============================================================================
def process_inquisitor():
    print("\n--- [2/5] Extracting HiveInquisitor Vorst ---")
    source_path = "Gemini_Generated_Image_w8kajkw8kajkw8ka.jpg"
    img = Image.open(source_path).convert("RGB")
    arr = np.array(img)

    r = arr[:, :, 0].astype(int)
    g = arr[:, :, 1].astype(int)
    b = arr[:, :, 2].astype(int)
    diff = np.maximum(np.abs(r - g), np.maximum(np.abs(g - b), np.abs(r - b)))
    mean_rgb = (r + g + b) / 3.0

    # Checkerboard background removal
    is_checker = (diff <= 14) & (mean_rgb >= 105) & (mean_rgb <= 195)
    lbl, num = label(is_checker)
    border = np.zeros_like(is_checker, dtype=bool)
    border[0, :] = True
    border[-1, :] = True
    border[:, 0] = True
    border[:, -1] = True
    border_lbls = set(lbl[border])
    border_lbls.discard(0)

    # Secondary interior checker patches
    sizes = np.bincount(lbl.ravel())
    for cid in range(1, num + 1):
        if cid not in border_lbls and sizes[cid] >= 30:
            ys, xs = np.where(lbl == cid)
            if diff[ys, xs].mean() <= 9.0 and 110.0 <= mean_rgb[ys, xs].mean() <= 190.0:
                border_lbls.add(cid)

    ext_bg = np.isin(lbl, list(border_lbls))
    alpha = np.where(ext_bg, 0, 255).astype(np.uint8)
    alpha[(diff <= 9) & (mean_rgb >= 112) & (mean_rgb <= 188)] = 0

    rgba = np.dstack([arr, alpha])
    INQ_SCALE = 0.58  # Perfectly matches 185px height on 256 canvas

    # --- 2A. WALK (Row 0 Idle & Row 1 Pose 0) ---
    walk_poses = []
    # Row 0 cluster 0, Row 0 cluster 1, Row 1 cluster 0, Row 2 cluster 0
    row0 = rgba[0:384, :, :]
    walk_bounds = [(115, 360), (575, 837), (115, 360), (575, 837)]
    for x1, x2 in walk_bounds:
        cell = row0[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=20, min_dust_size=40)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * INQ_SCALE))
        sh = int(round(cropped.height * INQ_SCALE))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        canvas.paste(scaled, ((256 - sw) // 2, TARGET_FOOT_Y - sh), scaled)
        walk_poses.append(clean_perimeter_border(canvas))

    final_walk = map_frames_to_8(walk_poses, "Walk")
    for i, frame in enumerate(final_walk):
        frame.save(os.path.join(OUTPUT_DIR, f"HiveInquisitor_Walk_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for HiveInquisitor_Walk (256x256)")

    # --- 2B. ATTACK: THE FULL-LENGTH LASER BEAM (Row 2, 768x256 Canvas) ---
    row2 = rgba[768:1152, :, :]
    laser_bounds = [(103, 440), (557, 919), (1015, 1377), (1460, 2707)]
    laser_frames = []
    for pose_idx, (x1, x2) in enumerate(laser_bounds):
        cell = row2[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=20, min_dust_size=40)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        
        sw = int(round(cropped.width * INQ_SCALE))
        sh = int(round(cropped.height * INQ_SCALE))
        # Ensure it fits within 768x256
        if sw > 758:
            adj = 758.0 / float(sw)
            sw = 758
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)

        canvas = Image.new("RGBA", (768, 256), (0, 0, 0, 0))
        # Flipped sprite: Vorst body is on the right, laser shoots left!
        # Anchor body spine at x = 640 (128px from right border)
        off_x = min(768 - sw - 4, max(4, 768 - 128 - sw + 30))
        if pose_idx == 3: # Frame 3 is the giant beam
            off_x = 4 # Project all the way to the left border with 4px margin!
        off_y = TARGET_FOOT_Y - sh
        canvas.paste(scaled, (off_x, off_y), scaled)
        laser_frames.append(clean_perimeter_border(canvas))

    final_laser = map_frames_to_8(laser_frames, "Attack")
    for i, frame in enumerate(final_laser):
        frame.save(os.path.join(OUTPUT_DIR, f"HiveInquisitor_Attack_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for HiveInquisitor_Attack (Full Laser, 768x256)")

    # --- 2C. MISSILES: 5 PSIONIC MISSILE DARTS (Row 0, 512x256 Canvas) ---
    missile_bounds = [(115, 360), (575, 837), (1049, 1362), (1479, 2435), (2440, 2703)]
    missile_frames = []
    for pose_idx, (x1, x2) in enumerate(missile_bounds):
        cell = row0[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=20, min_dust_size=40)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * INQ_SCALE))
        sh = int(round(cropped.height * INQ_SCALE))
        if sw > 504:
            adj = 504.0 / float(sw)
            sw = 504
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)

        canvas = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
        off_x = min(512 - sw - 4, max(4, 512 - 128 - sw + 30))
        if pose_idx == 3: off_x = 4 # Missile spread reaches left!
        off_y = TARGET_FOOT_Y - sh
        canvas.paste(scaled, (off_x, off_y), scaled)
        missile_frames.append(clean_perimeter_border(canvas))

    final_missiles = map_frames_to_8(missile_frames, "Missiles")
    for i, frame in enumerate(final_missiles):
        frame.save(os.path.join(OUTPUT_DIR, f"HiveInquisitor_Missiles_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for HiveInquisitor_Missiles (512x256)")

    # --- 2D. VORTEX & GROUND FLAME (Row 1, 768x256 Canvas) ---
    row1 = rgba[384:768, :, :]
    flame_bounds = [(102, 357), (561, 815), (986, 1325), (1484, 2698)]
    flame_frames = []
    for pose_idx, (x1, x2) in enumerate(flame_bounds):
        cell = row1[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=20, min_dust_size=40)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * INQ_SCALE))
        sh = int(round(cropped.height * INQ_SCALE))
        if sw > 758:
            adj = 758.0 / float(sw)
            sw = 758
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)

        canvas = Image.new("RGBA", (768, 256), (0, 0, 0, 0))
        off_x = min(768 - sw - 4, max(4, 768 - 128 - sw + 30))
        if pose_idx == 3: off_x = 4 # Ground flame sweeps all the way left!
        off_y = TARGET_FOOT_Y - sh
        canvas.paste(scaled, (off_x, off_y), scaled)
        flame_frames.append(clean_perimeter_border(canvas))

    final_flame = map_frames_to_8(flame_frames, "VortexFlame")
    for i, frame in enumerate(final_flame):
        frame.save(os.path.join(OUTPUT_DIR, f"HiveInquisitor_VortexFlame_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for HiveInquisitor_VortexFlame (768x256)")

    # --- 2E. DIE (Row 3, 256x256 Canvas) ---
    row3 = rgba[1152:1536, :, :]
    die_bounds = [(117, 356), (550, 800), (1040, 1244), (1447, 1749), (1876, 2241), (2340, 2690)]
    die_frames = []
    for x1, x2 in die_bounds:
        cell = row3[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=20, min_dust_size=35)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * INQ_SCALE))
        sh = int(round(cropped.height * INQ_SCALE))
        if sw > 248:
            adj = 248.0 / float(sw)
            sw = 248
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        canvas.paste(scaled, ((256 - sw) // 2, TARGET_FOOT_Y - sh), scaled)
        die_frames.append(clean_perimeter_border(canvas))

    final_die = map_frames_to_8(die_frames, "Die")
    for i, frame in enumerate(final_die):
        frame.save(os.path.join(OUTPUT_DIR, f"HiveInquisitor_Die_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for HiveInquisitor_Die (256x256)")


# ==============================================================================
# 3. BIO-HULK: Heavy Cyber-Brute (Walk, Slam, Laser, Die)
# ==============================================================================
def process_hulk():
    print("\n--- [3/5] Extracting BioHulk ---")
    source_path = "Gemini_Generated_Image_rrdhe1rrdhe1rrdh.jpg"
    img = Image.open(source_path).convert("RGB")
    arr = np.array(img)

    r = arr[:, :, 0].astype(int)
    g = arr[:, :, 1].astype(int)
    b = arr[:, :, 2].astype(int)
    diff = np.maximum(np.abs(r - g), np.maximum(np.abs(g - b), np.abs(r - b)))
    mean_rgb = (r + g + b) / 3.0

    is_checker = (diff <= 10) & (mean_rgb >= 10) & (mean_rgb <= 75)
    lbl, num = label(is_checker)
    border = np.zeros_like(is_checker, dtype=bool)
    border[0, :] = True
    border[-1, :] = True
    border[:, 0] = True
    border[:, -1] = True
    border_lbls = set(lbl[border])
    border_lbls.discard(0)

    sizes = np.bincount(lbl.ravel())
    for cid in range(1, num + 1):
        if cid not in border_lbls and sizes[cid] >= 45:
            ys, xs = np.where(lbl == cid)
            if (mean_rgb[ys, xs].max() - mean_rgb[ys, xs].min()) >= 28.0:
                border_lbls.add(cid)

    ext_bg = np.isin(lbl, list(border_lbls))
    alpha = np.where(ext_bg, 0, 255).astype(np.uint8)
    rgba = np.dstack([arr, alpha])

    HULK_SCALE = 208.0 / 282.0

    # Row 0: Walk (6 frames)
    walk_bounds = [(70, 393), (545, 847), (1008, 1311), (1442, 1738), (1905, 2205), (2328, 2640)]
    row0 = rgba[0:384, :, :]
    walk_frames = []
    for x1, x2 in walk_bounds:
        cell = row0[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=24, min_dust_size=35)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * HULK_SCALE))
        sh = int(round(cropped.height * HULK_SCALE))
        if sw > 248:
            adj = 248.0 / float(sw)
            sw = 248
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        canvas.paste(scaled, ((256 - sw) // 2, TARGET_FOOT_Y - sh), scaled)
        walk_frames.append(clean_perimeter_border(canvas))

    final_walk = map_frames_to_8(walk_frames, "Walk")
    for i, frame in enumerate(final_walk):
        frame.save(os.path.join(OUTPUT_DIR, f"BioHulk_Walk_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for BioHulk_Walk (256x256)")

    # Row 1: Attack Ground Slam (6 frames)
    slam_bounds = [(67, 394), (533, 870), (972, 1369), (1436, 1828), (1906, 2292), (2339, 2651)]
    row1 = rgba[384:768, :, :]
    slam_frames = []
    for x1, x2 in slam_bounds:
        cell = row1[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=24, min_dust_size=35)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * HULK_SCALE))
        sh = int(round(cropped.height * HULK_SCALE))
        if sw > 248:
            adj = 248.0 / float(sw)
            sw = 248
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        canvas.paste(scaled, ((256 - sw) // 2, TARGET_FOOT_Y - sh), scaled)
        slam_frames.append(clean_perimeter_border(canvas))

    final_slam = map_frames_to_8(slam_frames, "Attack")
    for i, frame in enumerate(final_slam):
        frame.save(os.path.join(OUTPUT_DIR, f"BioHulk_Attack_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for BioHulk_Attack (Slam, 256x256)")

    # Row 2: Attack Chest Laser Cannon (5 poses -> 8 frames, 512x256 Canvas!)
    laser_bounds = [(59, 392), (519, 901), (970, 1360), (1440, 2292), (2371, 2694)]
    row2 = rgba[768:1152, :, :]
    laser_frames = []
    for pose_idx, (x1, x2) in enumerate(laser_bounds):
        cell = row2[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=24, min_dust_size=35)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * HULK_SCALE))
        sh = int(round(cropped.height * HULK_SCALE))
        if sw > 504:
            adj = 504.0 / float(sw)
            sw = 504
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)

        canvas = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
        off_x = min(512 - sw - 4, max(4, 512 - 128 - sw + 30))
        if pose_idx == 3: off_x = 4 # Laser blast extends all the way left!
        off_y = TARGET_FOOT_Y - sh
        canvas.paste(scaled, (off_x, off_y), scaled)
        laser_frames.append(clean_perimeter_border(canvas))

    final_laser = map_frames_to_8(laser_frames, "Laser")
    for i, frame in enumerate(final_laser):
        frame.save(os.path.join(OUTPUT_DIR, f"BioHulk_Laser_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for BioHulk_Laser (Chest Laser Cannon, 512x256)")

    # Row 3: Die (8 frames)
    die_bounds = [(25, 306), (363, 629), (687, 951), (996, 1305), (1349, 1618), (1679, 2017), (2050, 2346), (2383, 2730)]
    row3 = rgba[1152:1536, :, :]
    die_frames = []
    for x1, x2 in die_bounds:
        cell = row3[:, x1:x2, :].copy()
        cell = clean_cell_slivers(cell, max_top_sliver_h=24, min_dust_size=35)
        ys, xs = np.where(cell[:, :, 3] > 0)
        if len(ys) == 0: continue
        cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
        sw = int(round(cropped.width * HULK_SCALE))
        sh = int(round(cropped.height * HULK_SCALE))
        if sw > 248:
            adj = 248.0 / float(sw)
            sw = 248
            sh = int(round(sh * adj))
        scaled = cropped.resize((sw, sh), Image.Resampling.LANCZOS)
        canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        canvas.paste(scaled, ((256 - sw) // 2, TARGET_FOOT_Y - sh), scaled)
        die_frames.append(clean_perimeter_border(canvas))

    final_die = map_frames_to_8(die_frames, "Die")
    for i, frame in enumerate(final_die):
        frame.save(os.path.join(OUTPUT_DIR, f"BioHulk_Die_{i}.png"), format="PNG")
    print("  -> Generated 8 frames for BioHulk_Die (256x256)")


# ==============================================================================
# 4. BIO-LASHER: Tentacular Psionic Alien (Walk, Whip, Tornado, Die)
# ==============================================================================
def process_lasher():
    print("\n--- [4/5] Extracting BioLasher ---")
    source_path = glob.glob("*tentacular*.png")[0]
    img = Image.open(source_path).convert("RGBA")
    arr = np.array(img)
    arr[:, :, 3] = np.where(arr[:, :, 3] >= 15, 255, 0).astype(np.uint8)

    row_names = ["Walk", "Attack", "Tornado", "Die"]
    for row_idx, state_name in enumerate(row_names):
        row_arr = arr[row_idx * 256 : (row_idx + 1) * 256, :, :].copy()
        raw_frames = []
        for c in range(6):
            cell = row_arr[:, c * 256 : (c + 1) * 256, :].copy()
            # Retain lightning core holes in whip and tornado
            if "attack" in state_name.lower() or "tornado" in state_name.lower():
                fg_mask = cell[:, :, 3] > 0
                filled = binary_fill_holes(fg_mask)
                holes = filled & (~fg_mask)
                lbl_h, num_h = label(holes)
                h_sizes = np.bincount(lbl_h.ravel())
                for hid in range(1, num_h + 1):
                    if h_sizes[hid] < 350:
                        cell[lbl_h == hid] = [252, 225, 255, 255]

            cell = clean_cell_slivers(cell, max_top_sliver_h=16, min_dust_size=30)
            ys, xs = np.where(cell[:, :, 3] > 0)
            if len(ys) == 0:
                raw_frames.append(Image.new("RGBA", (256, 256), (0, 0, 0, 0)))
                continue

            cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            sw = cropped.width
            sh = cropped.height
            if sw > 248:
                scale = 248.0 / float(sw)
                sw = 248
                sh = int(round(sh * scale))
                cropped = cropped.resize((sw, sh), Image.Resampling.LANCZOS)

            canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
            canvas.paste(cropped, ((256 - sw) // 2, TARGET_FOOT_Y - sh), cropped)
            raw_frames.append(clean_perimeter_border(canvas))

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_path = os.path.join(OUTPUT_DIR, f"BioLasher_{state_name}_{i}.png")
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for BioLasher_{state_name} (256x256)")


# ==============================================================================
# 5. BIO-STALKER: Biomechanical Panther (Walk, Attack, Die)
# ==============================================================================
def process_stalker():
    print("\n--- [5/5] Extracting BioStalker ---")
    source_path = glob.glob("*pantera*.png")[0]
    img = Image.open(source_path).convert("RGBA")
    arr = np.array(img)
    arr[:, :, 3] = np.where(arr[:, :, 3] >= 15, 255, 0).astype(np.uint8)

    row_defs = [
        ("Walk", 21, 195, 6),
        ("Attack", 401, 589, 6),
        ("Die", 830, 988, 8),
    ]

    for state_name, y1, y2, num_cols in row_defs:
        row_arr = arr[y1:y2, :, :].copy()
        col_w = row_arr.shape[1] / float(num_cols)
        raw_frames = []
        for c in range(num_cols):
            x1 = int(round(c * col_w))
            x2 = int(round((c + 1) * col_w))
            cell = row_arr[:, x1:x2, :].copy()
            cell = clean_cell_slivers(cell, max_top_sliver_h=16, min_dust_size=30)
            ys, xs = np.where(cell[:, :, 3] > 0)
            if len(ys) == 0:
                raw_frames.append(Image.new("RGBA", (256, 256), (0, 0, 0, 0)))
                continue

            cropped = Image.fromarray(cell[ys.min():ys.max()+1, xs.min():xs.max()+1, :]).transpose(Image.Transpose.FLIP_LEFT_RIGHT)
            sw = cropped.width
            sh = cropped.height
            if sw > 248:
                scale = 248.0 / float(sw)
                sw = 248
                sh = int(round(sh * scale))
                cropped = cropped.resize((sw, sh), Image.Resampling.LANCZOS)

            canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
            canvas.paste(cropped, ((256 - sw) // 2, TARGET_FOOT_Y - sh), cropped)
            raw_frames.append(clean_perimeter_border(canvas))

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_path = os.path.join(OUTPUT_DIR, f"BioStalker_{state_name}_{i}.png")
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for BioStalker_{state_name} (256x256)")


def main():
    print("=" * 70)
    print("  SURGICAL NEON ENEMY ASSET EXTRACTION V2 (MASTER HIGH QUALITY)")
    print("=" * 70)
    process_boss()
    process_inquisitor()
    process_hulk()
    process_lasher()
    process_stalker()
    print("\n" + "=" * 70)
    print("  ALL NEON ENEMY SPRITES GENERATED WITH 100% CLEAN PERIMETERS!")
    print("=" * 70)


if __name__ == "__main__":
    main()
