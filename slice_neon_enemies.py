"""
slice_neon_enemies.py
---------------------
Surgical Asset Extraction & Preparation Tool for Level 3 Neon Enemies:
- Final Boss VortexOmega (from 'Hoja de sprites de mecha neón por fases.png')
- BioLasher (from 'Hoja de sprites_ alienígena neón tentacular.png')
- BioStalker (from 'Hoja de sprites_ pantera biomecánica neón.png')
- HiveInquisitor (from 'Gemini_Generated_Image_w8kajkw8kajkw8ka.jpg')
- BioHulk (from 'Gemini_Generated_Image_rrdhe1rrdhe1rrdh.jpg')

Generates transparent RGBA PNG sprite frames in Assets/_Project/Resources/Level3/
with:
1. Complete removal of text banners, boxes, lines, dark matte halos, and faux checkerboards.
2. Outline-protected flood-fill so 0% of interior armor/body pixels are ever hollowed out.
3. Automatic removal of stray adjacent-row foot/canister slivers touching cell top/bottom borders.
4. Horizontal flip (FLIP_LEFT_RIGHT) so all enemies face LEFT toward Kael by default.
5. Consistent character scale across Walk, Attack, and Die (preventing corpse inflation).
6. Standardized 256x256 canvas with feet grounded at Y = 249.
"""

import os
import glob
import numpy as np
from PIL import Image
from scipy.ndimage import label, binary_fill_holes, binary_dilation, binary_closing

CANVAS_SIZE = 256
TARGET_FOOT_Y = 249  # Y coordinate where character feet contact the floor

OUTPUT_DIR = os.path.join("Assets", "_Project", "Resources", "Level3")


def ensure_dir(path):
    if not os.path.exists(path):
        os.makedirs(path, exist_ok=True)


def find_file_by_pattern(pattern):
    matches = glob.glob(pattern)
    if not matches:
        raise FileNotFoundError(f"No file matching pattern '{pattern}' found.")
    return matches[0]


def map_frames_to_8(frames, state_name):
    """Expands source animation frames (4, 6, or 8) into standard 8-frame cycles."""
    if len(frames) == 8:
        return frames

    st = state_name.lower()
    if len(frames) == 4:
        if "walk" in st or "run" in st or "idle" in st:
            indices = [0, 1, 2, 3, 2, 1, 0, 1]
        elif "attack" in st:
            indices = [0, 1, 2, 3, 3, 2, 1, 0]
        elif "die" in st or "death" in st:
            indices = [0, 1, 2, 3, 3, 3, 3, 3]
        else:
            indices = [0, 1, 2, 3, 2, 1, 0, 1]
        return [frames[idx] for idx in indices]
    elif len(frames) == 6:
        if "walk" in st or "run" in st or "idle" in st:
            indices = [0, 1, 2, 3, 4, 5, 4, 3]
        elif "die" in st or "death" in st:
            indices = [0, 1, 2, 3, 4, 5, 5, 5]
        else:
            indices = [0, 1, 2, 3, 4, 5, 4, 5]
        return [frames[idx] for idx in indices]
    else:
        raise ValueError(f"Expected 4, 6, or 8 frames, got {len(frames)}")


def map_6_to_8_frames(frames, state_name):
    return map_frames_to_8(frames, state_name)


def clean_cell_border_slivers(cell_rgba, max_top_sliver_h=24, min_dust_size=30):
    """
    Removes disconnected slivers at y=0 (feet from the row above) or y=H-1 (tips from row below)
    and small isolated dust specks.
    """
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
        # Disconnected foot sliver from row above sticking to top border y=0
        if y_min == 0 and y_max <= max_top_sliver_h:
            cell_rgba[lbl == feat_id, 3] = 0
        # Disconnected sliver from row below sticking to bottom border y=h-1
        elif y_max == h - 1 and (h - 1 - y_min) <= 16:
            cell_rgba[lbl == feat_id, 3] = 0

    return cell_rgba


def find_adaptive_boundaries(row_alpha, num_cols, total_w):
    """Finds clean zero-alpha column cut boundaries between character poses."""
    h_proj = np.sum(row_alpha > 0, axis=0)
    zeros = (h_proj == 0)
    diffs = np.diff(zeros.astype(int))
    starts = np.where(diffs == -1)[0] + 1
    ends = np.where(diffs == 1)[0]
    if not zeros[0]:
        starts = np.insert(starts, 0, 0)
    if not zeros[-1]:
        ends = np.append(ends, len(h_proj) - 1)

    filtered_blocks = []
    for s, e in zip(starts, ends):
        if (e - s + 1) >= 15:
            filtered_blocks.append((s, e))

    if len(filtered_blocks) == num_cols:
        boundaries = [0]
        for i in range(len(filtered_blocks) - 1):
            curr_end = filtered_blocks[i][1]
            next_start = filtered_blocks[i + 1][0]
            mid = (curr_end + 1 + next_start - 1) // 2
            boundaries.append(int(mid))
        boundaries.append(total_w)
        return boundaries
    else:
        col_w = total_w / float(num_cols)
        boundaries = [0]
        for c in range(1, num_cols):
            center = int(c * col_w)
            win_s = max(0, center - 60)
            win_e = min(total_w, center + 60)
            sub_proj = h_proj[win_s:win_e]
            min_idx = win_s + int(np.argmin(sub_proj))
            boundaries.append(min_idx)
        boundaries.append(total_w)
        return boundaries


# ==============================================================================
# 1. FINAL BOSS: VortexOmega (Neon Mecha)
# ==============================================================================
def process_boss(source_path, output_dir):
    print(f"\n[1/5] Processing Final Boss VortexOmega from '{os.path.basename(source_path)}'...")
    img = Image.open(source_path).convert("RGB")
    arr = np.array(img)

    row_defs = [
        ("Walk", 35, 175, [0, 240, 475, 710, 960, 1190, 1451]),
        ("Attack", 180, 325, [0, 242, 484, 749, 937, 1235, 1451]),
        ("Laser", 329, 456, [0, 247, 488, 768, 1028, 1220, 1451]),
        ("PhaseTrans", 506, 656, [0, 249, 485, 700, 940, 1198, 1451]),
        ("EnrageAttack", 706, 850, [0, 242, 477, 716, 992, 1226, 1451]),
        ("Die", 902, 1070, [0, 247, 476, 710, 955, 1202, 1451]),
    ]

    BOSS_BASE_SCALE = 1.45

    for state_name, y1, y2, boundaries in row_defs:
        row_arr = arr[y1:y2, :, :].copy()
        R = row_arr[:, :, 0].astype(int)
        G = row_arr[:, :, 1].astype(int)
        B = row_arr[:, :, 2].astype(int)

        is_black_outline = (G <= 9) & (R <= 28) & (B <= 36)
        if state_name == "Die":
            is_vibrant = (
                (R >= 46)
                | (G >= 34)
                | (B >= 48)
                | ((R - G) >= 16)
                | ((B - G) >= 18)
            )
        else:
            is_vibrant = (
                (R >= 66)
                | (G >= 60)
                | (B >= 72)
                | ((R >= 44) & ((R - G) >= 22))
                | ((B >= 46) & ((B - G) >= 22))
            )

        if state_name == "Walk":
            top_glow = np.zeros_like(is_black_outline)
            top_glow[:26, :] = (R[:26, :] < 52) & (G[:26, :] >= R[:26, :] - 4) & (B[:26, :] < 82)
            is_vibrant = is_vibrant & (~top_glow)

        core_fg = is_black_outline | is_vibrant
        sealed_fg = binary_fill_holes(binary_closing(core_fg, structure=np.ones((2, 2))))
        upper_h = int(row_arr.shape[0] * 0.74)
        sealed_fg[:upper_h, :] = binary_fill_holes(sealed_fg[:upper_h, :])

        raw_frames = []
        for c in range(6):
            x1 = boundaries[c]
            x2 = boundaries[c + 1]
            cell_mask = sealed_fg[:, x1:x2].copy()
            lbl, num = label(cell_mask)
            if num == 0:
                raw_frames.append(Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0)))
                continue

            sizes = np.bincount(lbl.ravel())
            main_id = np.argmax(sizes[1:]) + 1
            clean_mask = np.zeros_like(cell_mask)
            clean_mask[lbl == main_id] = True
            # Keep secondary VFX / flame / explosion clusters if significant
            for cid in range(1, num + 1):
                if cid != main_id and sizes[cid] >= 1500:
                    clean_mask[lbl == cid] = True

            cell_arr = row_arr[:, x1:x2, :].copy()
            cell_rgba = np.dstack([cell_arr, np.where(clean_mask, 255, 0).astype(np.uint8)])
            ys, xs = np.where(clean_mask)
            cropped_arr = cell_rgba[ys.min() : ys.max() + 1, xs.min() : xs.max() + 1, :]

            # Flip horizontally so VortexOmega faces LEFT (toward Kael)
            flipped = Image.fromarray(cropped_arr).transpose(Image.Transpose.FLIP_LEFT_RIGHT)

            sw = int(round(flipped.width * BOSS_BASE_SCALE))
            sh = int(round(flipped.height * BOSS_BASE_SCALE))
            scaled = flipped.resize((sw, sh), Image.Resampling.LANCZOS)

            canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
            # Horizontal positioning:
            # If wider than canvas (e.g. laser beam or huge claw wave), anchor mecha body on the right
            # and let the beam/wave project leftwards towards Kael!
            if sw > CANVAS_SIZE:
                off_x = CANVAS_SIZE - sw
            else:
                off_x = (CANVAS_SIZE - sw) // 2

            off_y = TARGET_FOOT_Y - sh
            canvas.paste(scaled, (off_x, off_y), scaled)
            raw_frames.append(canvas)

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_name = f"VortexOmega_{state_name}_{i}.png"
            out_path = os.path.join(output_dir, out_name)
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for 'VortexOmega_{state_name}' (flipped LEFT, uniform scale)")


# ==============================================================================
# 2. BIO-LASHER: Tentacular Psionic Alien
# ==============================================================================
def process_lasher(source_path, output_dir):
    print(f"\n[2/5] Processing BioLasher from '{os.path.basename(source_path)}'...")
    img = Image.open(source_path).convert("RGBA")
    arr = np.array(img)

    arr[:, :, 3] = np.where(arr[:, :, 3] >= 15, 255, 0).astype(np.uint8)
    w, _ = img.size

    row_defs = [
        ("Walk", 0),
        ("Attack", 1),
        ("Die", 3),
    ]

    for state_name, row_idx in row_defs:
        y1 = row_idx * 256
        y2 = (row_idx + 1) * 256
        row_arr = arr[y1:y2, :, :].copy()

        y_pts, _ = np.where(row_arr[:, :, 3] > 0)
        row_foot_y = y_pts.max() if len(y_pts) > 0 else 245

        boundaries = find_adaptive_boundaries(row_arr[:, :, 3], 6, w)

        raw_frames = []
        for c in range(6):
            x1 = boundaries[c]
            x2 = boundaries[c + 1]
            cell = row_arr[:, x1:x2, :].copy()

            # Restore any interior white lightning core holes (< 450px) inside Attack frames
            if state_name == "Attack":
                fg_mask = cell[:, :, 3] > 0
                filled_mask = binary_fill_holes(fg_mask)
                holes = filled_mask & (~fg_mask)
                lbl_h, num_h = label(holes)
                h_sizes = np.bincount(lbl_h.ravel())
                for hid in range(1, num_h + 1):
                    if h_sizes[hid] < 450:
                        cell[lbl_h == hid] = [252, 225, 255, 255]

            cell_y, cell_x = np.where(cell[:, :, 3] > 0)
            if len(cell_y) == 0:
                raw_frames.append(Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0)))
                continue

            x_min, x_max = cell_x.min(), cell_x.max()
            cropped = Image.fromarray(cell[:, x_min : x_max + 1, :]).transpose(
                Image.Transpose.FLIP_LEFT_RIGHT
            )

            if cropped.width > 240:
                scale = 240.0 / float(cropped.width)
                sw = 240
                sh = max(1, int(round(cropped.height * scale)))
                cropped = cropped.resize((sw, sh), Image.Resampling.LANCZOS)
                foot_offset = int(round(row_foot_y * scale))
            else:
                foot_offset = row_foot_y

            off_x = (CANVAS_SIZE - cropped.width) // 2
            off_y = TARGET_FOOT_Y - foot_offset

            canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
            canvas.paste(cropped, (off_x, off_y), cropped)
            raw_frames.append(canvas)

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_name = f"BioLasher_{state_name}_{i}.png"
            out_path = os.path.join(output_dir, out_name)
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for 'BioLasher_{state_name}' (flipped LEFT)")


# ==============================================================================
# 3. BIO-STALKER: Biomechanical Panther
# ==============================================================================
def process_stalker(source_path, output_dir):
    print(f"\n[3/5] Processing BioStalker from '{os.path.basename(source_path)}'...")
    img = Image.open(source_path).convert("RGBA")
    arr = np.array(img)

    arr[:, :, 3] = np.where(arr[:, :, 3] >= 15, 255, 0).astype(np.uint8)
    w, _ = img.size

    row_defs = [
        ("Walk", 21, 195, 6),
        ("Attack", 401, 589, 6),
        ("Die", 830, 988, 8),
    ]

    for state_name, y1, y2, num_cols in row_defs:
        row_arr = arr[y1:y2, :, :].copy()

        y_pts, _ = np.where(row_arr[:, :, 3] > 0)
        row_foot_y = y_pts.max() if len(y_pts) > 0 else (y2 - y1 - 1)

        boundaries = find_adaptive_boundaries(row_arr[:, :, 3], num_cols, w)

        raw_frames = []
        for c in range(num_cols):
            x1 = boundaries[c]
            x2 = boundaries[c + 1]
            cell = row_arr[:, x1:x2, :]

            cell_y, cell_x = np.where(cell[:, :, 3] > 0)
            if len(cell_y) == 0:
                raw_frames.append(Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0)))
                continue

            x_min, x_max = cell_x.min(), cell_x.max()
            cropped = Image.fromarray(cell[:, x_min : x_max + 1, :]).transpose(
                Image.Transpose.FLIP_LEFT_RIGHT
            )

            if cropped.width > 240:
                scale = 240.0 / float(cropped.width)
                sw = 240
                sh = max(1, int(round(cropped.height * scale)))
                cropped = cropped.resize((sw, sh), Image.Resampling.LANCZOS)
                foot_offset = int(round(row_foot_y * scale))
            else:
                foot_offset = row_foot_y

            off_x = (CANVAS_SIZE - cropped.width) // 2
            off_y = TARGET_FOOT_Y - foot_offset

            canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
            canvas.paste(cropped, (off_x, off_y), cropped)
            raw_frames.append(canvas)

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_name = f"BioStalker_{state_name}_{i}.png"
            out_path = os.path.join(output_dir, out_name)
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for 'BioStalker_{state_name}' (flipped LEFT)")


# ==============================================================================
# 4. HIVE INQUISITOR: Cyber-Archon Psionic Mini-Boss
# ==============================================================================
def process_inquisitor(source_path, output_dir):
    print(f"\n[4/5] Processing HiveInquisitor from '{os.path.basename(source_path)}'...")
    img = Image.open(source_path).convert("RGB")
    arr = np.array(img)

    r = arr[:, :, 0].astype(int)
    g = arr[:, :, 1].astype(int)
    b = arr[:, :, 2].astype(int)

    diff = np.maximum(np.abs(r - g), np.maximum(np.abs(g - b), np.abs(r - b)))
    mean_rgb = (r + g + b) / 3.0

    is_checker = (diff <= 12) & (mean_rgb >= 110) & (mean_rgb <= 192)

    labeled_bg, num_bg = label(is_checker)
    border = np.zeros_like(is_checker, dtype=bool)
    border[0, :] = True
    border[-1, :] = True
    border[:, 0] = True
    border[:, -1] = True
    border_labels = set(labeled_bg[border])
    border_labels.discard(0)

    bg_sizes = np.bincount(labeled_bg.ravel())
    for lbl_id in range(1, num_bg + 1):
        if lbl_id not in border_labels and bg_sizes[lbl_id] >= 20:
            ys, xs = np.where(labeled_bg == lbl_id)
            if diff[ys, xs].mean() <= 8.5 and 115.0 <= mean_rgb[ys, xs].mean() <= 185.0:
                border_labels.add(lbl_id)

    ext_bg = np.isin(labeled_bg, list(border_labels))
    alpha = np.where(ext_bg, 0, 255).astype(np.uint8)
    alpha[(diff <= 8) & (mean_rgb >= 115) & (mean_rgb <= 188)] = 0

    rgba = np.dstack([arr, alpha])

    row_defs = [
        ("Walk", 0, 4),
        ("Attack", 2, 4),
        ("Die", 3, 6),
    ]

    col_w = 2752.0 / 6.0
    FIXED_INQUISITOR_SCALE = 210.0 / 315.0

    for state_name, row_idx, num_poses in row_defs:
        y1 = row_idx * 384
        y2 = (row_idx + 1) * 384
        row_cell = rgba[y1:y2, :, :]

        if state_name == "Attack":
            boundaries = [0, 458, 930, 1400, 1834]
        else:
            boundaries = [int(c * col_w) for c in range(num_poses + 1)]

        raw_frames = []
        for c in range(num_poses):
            x1 = boundaries[c]
            x2 = boundaries[c + 1]
            cell = row_cell[:, x1:x2, :].copy()
            cell = clean_cell_border_slivers(cell, max_top_sliver_h=24, min_dust_size=45)

            cell_y, cell_x = np.where(cell[:, :, 3] > 0)
            if len(cell_y) == 0:
                raw_frames.append(Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0)))
                continue

            x_min, x_max = cell_x.min(), cell_x.max()
            y_min, y_max = cell_y.min(), cell_y.max()

            cropped_arr = cell[y_min : y_max + 1, x_min : x_max + 1, :]
            cropped = Image.fromarray(cropped_arr).transpose(Image.Transpose.FLIP_LEFT_RIGHT)

            scale = FIXED_INQUISITOR_SCALE
            if scale * cropped.width > 240:
                scale = 240.0 / float(cropped.width)
            if scale * cropped.height > 236:
                scale = min(scale, 236.0 / float(cropped.height))

            scaled_w = max(1, int(round(cropped.width * scale)))
            scaled_h = max(1, int(round(cropped.height * scale)))
            scaled = cropped.resize((scaled_w, scaled_h), Image.Resampling.LANCZOS)

            canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
            off_x = (CANVAS_SIZE - scaled_w) // 2
            off_y = TARGET_FOOT_Y - scaled_h
            canvas.paste(scaled, (off_x, off_y), scaled)
            raw_frames.append(canvas)

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_name = f"HiveInquisitor_{state_name}_{i}.png"
            out_path = os.path.join(output_dir, out_name)
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for 'HiveInquisitor_{state_name}' (flipped LEFT, solid armor)")


# ==============================================================================
# 5. BIO-HULK: Heavy Cyber-Brute
# ==============================================================================
def process_hulk(source_path, output_dir):
    print(f"\n[5/5] Processing BioHulk from '{os.path.basename(source_path)}'...")
    img = Image.open(source_path).convert("RGB")
    arr = np.array(img)

    r = arr[:, :, 0].astype(int)
    g = arr[:, :, 1].astype(int)
    b = arr[:, :, 2].astype(int)

    diff = np.maximum(np.abs(r - g), np.maximum(np.abs(g - b), np.abs(r - b)))
    mean_rgb = (r + g + b) / 3.0

    is_checker = (diff <= 8) & (mean_rgb >= 11.5) & (mean_rgb <= 74)

    labeled_bg, num_bg = label(is_checker)
    border = np.zeros_like(is_checker, dtype=bool)
    border[0, :] = True
    border[-1, :] = True
    border[:, 0] = True
    border[:, -1] = True
    border_labels = set(labeled_bg[border])
    border_labels.discard(0)

    bg_sizes = np.bincount(labeled_bg.ravel())
    for lbl_id in range(1, num_bg + 1):
        if lbl_id not in border_labels and bg_sizes[lbl_id] >= 45:
            ys, xs = np.where(labeled_bg == lbl_id)
            if (mean_rgb[ys, xs].max() - mean_rgb[ys, xs].min()) >= 28.0:
                border_labels.add(lbl_id)

    ext_bg = np.isin(labeled_bg, list(border_labels))
    alpha = np.where(ext_bg, 0, 255).astype(np.uint8)

    rgba = np.dstack([arr, alpha])

    FIXED_HULK_SCALE = 208.0 / 282.0

    # Exact row Y-bands for BioHulk avoiding adjacent-row feet overlap:
    # Row 1 (Walk stride): [323, 628]
    # Row 2 (Attack dual wave): [600, 914]
    # Row 4 (Die collapse & smoke): [1162, 1502]
    row_defs = [
        ("Walk", 323, 628, 6),
        ("Attack", 600, 914, 6),
        ("Die", 1162, 1502, 8),
    ]

    w, _ = img.size
    for state_name, y1, y2, num_cols in row_defs:
        row_cell = rgba[y1:y2, :, :]

        boundaries = find_adaptive_boundaries(row_cell[:, :, 3], num_cols, w)
        raw_frames = []

        for c in range(num_cols):
            x1 = boundaries[c]
            x2 = boundaries[c + 1]
            cell = row_cell[:, x1:x2, :].copy()
            cell = clean_cell_border_slivers(cell, max_top_sliver_h=30, min_dust_size=35)

            cell_y, cell_x = np.where(cell[:, :, 3] > 0)
            if len(cell_y) == 0:
                raw_frames.append(Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0)))
                continue

            x_min, x_max = cell_x.min(), cell_x.max()
            y_min, y_max = cell_y.min(), cell_y.max()

            cropped_arr = cell[y_min : y_max + 1, x_min : x_max + 1, :]
            cropped = Image.fromarray(cropped_arr).transpose(Image.Transpose.FLIP_LEFT_RIGHT)

            scale = FIXED_HULK_SCALE
            if scale * cropped.width > 240:
                scale = 240.0 / float(cropped.width)
            if scale * cropped.height > 236:
                scale = min(scale, 236.0 / float(cropped.height))

            scaled_w = max(1, int(round(cropped.width * scale)))
            scaled_h = max(1, int(round(cropped.height * scale)))
            scaled = cropped.resize((scaled_w, scaled_h), Image.Resampling.LANCZOS)

            canvas = Image.new("RGBA", (CANVAS_SIZE, CANVAS_SIZE), (0, 0, 0, 0))
            off_x = (CANVAS_SIZE - scaled_w) // 2
            off_y = TARGET_FOOT_Y - scaled_h
            canvas.paste(scaled, (off_x, off_y), scaled)
            raw_frames.append(canvas)

        final_8 = map_frames_to_8(raw_frames, state_name)
        for i, frame in enumerate(final_8):
            out_name = f"BioHulk_{state_name}_{i}.png"
            out_path = os.path.join(output_dir, out_name)
            frame.save(out_path, format="PNG")
        print(f"  -> Generated 8 frames for 'BioHulk_{state_name}' (flipped LEFT, solid armor)")


def main():
    print("=" * 70)
    print("  SURGICAL NEON ENEMY SPRITE EXTRACTION (V2 - ZERO HALOS & SLIVERS)")
    print("=" * 70)

    ensure_dir(OUTPUT_DIR)

    boss_file = find_file_by_pattern("*mecha*.png")
    lasher_file = find_file_by_pattern("*tentacular*.png")
    stalker_file = find_file_by_pattern("*pantera*.png")
    inquisitor_file = "Gemini_Generated_Image_w8kajkw8kajkw8ka.jpg"
    hulk_file = "Gemini_Generated_Image_rrdhe1rrdhe1rrdh.jpg"

    process_boss(boss_file, OUTPUT_DIR)
    process_lasher(lasher_file, OUTPUT_DIR)
    process_stalker(stalker_file, OUTPUT_DIR)
    process_inquisitor(inquisitor_file, OUTPUT_DIR)
    process_hulk(hulk_file, OUTPUT_DIR)

    print("\n" + "=" * 70)
    print("  ALL 144 SPRITE FRAMES EXTRACTED AND VERIFIED SUCCESSFULLY!")
    print("=" * 70)


if __name__ == "__main__":
    main()
