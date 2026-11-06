"""Builds the M1.6 player characters into ArtSource/Incoming/<slug>/ (FBX + textures + materials.json + source.txt).

Usage: python -I Tools/Art/build_characters.py <packs_dir>
<packs_dir> holds the extracted Quaternius packs (CC0): ubc/std (Universal Base Characters, Standard) and
fantasy/std (Modular Character Outfits - Fantasy, Standard). Garment textures are turned into a neutral
luminance map so each outfit part can be tinted in Unity (modern colours on the fantasy cuts).
Then run BilliardGame > Art > Import Incoming in Unity.
"""
import json
import os
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageOps

BLENDER = r"C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
ROOT = Path(__file__).resolve().parents[2]
TEX = 2048
GARMENT_SMOOTHNESS = 0.55

# Tints are linear-ish multipliers on a neutral (median 0.75) garment map.
CHARACTERS = [
    {"slug": "char-timur", "name": "Timur", "sex": "Male", "outfit": "Peasant", "hair": ["Hair_SimpleParted"],
     "drop": "", "skin": 1.0, "hair_tint": [0.16, 0.11, 0.08],
     "tints": {"Top": [0.92, 0.92, 0.95], "Bottom": [0.22, 0.29, 0.45], "Shoes": [0.32, 0.22, 0.16], "Acc": [0.3, 0.3, 0.3]}},
    {"slug": "char-aziz", "name": "Aziz", "sex": "Male", "outfit": "Ranger", "hair": ["Hair_Buzzed", "Hair_Beard"],
     "drop": "Hood|Pauldron|Bracer", "skin": 0.82, "hair_tint": [0.08, 0.07, 0.06],
     "tints": {"Top": [0.36, 0.37, 0.4], "Bottom": [0.16, 0.16, 0.18], "Shoes": [0.12, 0.1, 0.09], "Acc": [0.45, 0.33, 0.22]}},
    {"slug": "char-malika", "name": "Malika", "sex": "Female", "outfit": "Peasant", "hair": ["Hair_Long"],
     "drop": "", "skin": 1.0, "hair_tint": [0.1, 0.07, 0.05],
     "tints": {"Top": [0.62, 0.13, 0.2], "Bottom": [0.14, 0.14, 0.16], "Shoes": [0.12, 0.1, 0.1], "Acc": [0.8, 0.65, 0.3]}},
    {"slug": "char-dilnoza", "name": "Dilnoza", "sex": "Female", "outfit": "Ranger", "hair": ["Hair_Buns"],
     "drop": "Hood|Pauldron|Bracer|Belt_2", "skin": 0.9, "hair_tint": [0.22, 0.14, 0.09],
     "tints": {"Top": [0.13, 0.42, 0.47], "Bottom": [0.72, 0.64, 0.52], "Shoes": [0.3, 0.2, 0.14], "Acc": [0.45, 0.33, 0.22]}},
]
PARTS = {"Body": "Top", "Arms": "Top", "Legs": "Bottom", "Feet": "Shoes"}


def save(img: Image.Image, path: Path) -> str:
    img.resize((TEX, TEX), Image.LANCZOS).save(path)
    return path.name


def neutral(src: Path, dst: Path) -> str:
    """Garment base -> grey luminance with its median at 0.75, so a material tint sets the colour."""
    lum = ImageOps.grayscale(Image.open(src).convert("RGB")).resize((TEX, TEX), Image.LANCZOS)
    hist = lum.histogram()
    half, acc, median = sum(hist) / 2, 0, 128
    for i, n in enumerate(hist):
        acc += n
        if acc >= half:
            median = max(i, 1)
            break
    scale = 0.75 * 255 / median
    return save(lum.point(lambda v: min(255, int(v * scale))).convert("RGB"), dst)


def mask_from_orm(src: Path, dst: Path) -> str:
    """ORM (R occlusion, G roughness, B metal) -> URP mask (R metal, G AO, A smoothness).
    Smoothness is scaled down: the fantasy garments read as shiny plate under the hall lights otherwise."""
    o, r, m = Image.open(src).convert("RGB").resize((TEX, TEX), Image.LANCZOS).split()
    zero = Image.new("L", (TEX, TEX), 0)
    smooth = ImageOps.invert(r).point(lambda v: int(v * GARMENT_SMOOTHNESS))
    return save(Image.merge("RGBA", (m, o, zero, smooth)), dst)


def mask_from_rough(src: Path, dst: Path) -> str:
    rough = ImageOps.grayscale(Image.open(src).convert("RGB")).resize((TEX, TEX), Image.LANCZOS)
    zero, one = Image.new("L", (TEX, TEX), 0), Image.new("L", (TEX, TEX), 255)
    return save(Image.merge("RGBA", (zero, one, zero, ImageOps.invert(rough))), dst)


def copy(src: Path, dst: Path) -> str:
    return save(Image.open(src).convert("RGB"), dst)


def material(name, base, normal=None, mask=None, color=(1, 1, 1), roughness=0.6):
    return {"name": name, "base": base, "normal": normal or "", "mask": mask or "", "emission": "",
            "color": [*color, 1.0], "metallic": 0.0, "roughness": roughness, "alpha": "OPAQUE", "doubleSided": False}


def build(packs: Path, c: dict) -> None:
    ubc, fan = packs / "ubc/std", packs / "fantasy/std"
    out = ROOT / "ArtSource/Incoming" / c["slug"]
    out.mkdir(parents=True, exist_ok=True)
    sex, outfit = c["sex"], c["outfit"]
    utex, ftex = ubc / "Base Characters/Textures", fan / "Textures"
    hair_dir = ubc / "Hairstyles/Rigged to Head Bone/FBX (Unity)"
    spec = {
        "slug": c["slug"],
        "outfit": str(fan / f"Exports/FBX (Unity)/Outfits/{sex}_{outfit}.fbx"),
        "base": str(ubc / f"Base Characters/Unity/Superhero_{sex}_FullBody.fbx"),
        "hair": [str(hair_dir / f"{h}.fbx") for h in c["hair"]],
        "drop": c["drop"], "parts": PARTS, "max_part_tris": 3200, "max_tris": 20000,
        "render": str(packs / "previews" / f"{c['slug']}.png"),
    }
    spec_path = out / "spec.json"
    spec_path.write_text(json.dumps(spec, indent=1), encoding="utf-8")
    subprocess.run([BLENDER, "-b", "--factory-startup", "-P", str(ROOT / "Tools/Blender/assemble_character.py"), "--",
                    str(spec_path), str(out)], check=True, stdout=subprocess.DEVNULL)
    used = json.loads((out / "assembly.json").read_text())["materials"]

    skin = [c["skin"]] * 3
    head = "T_Superhero_Male_Dark.png" if sex == "Male" else "T_Superhero_Female_Dark_BaseColor.png"
    garment = neutral(ftex / outfit / f"T_{outfit}_BaseColor.png", out / f"{outfit}_Neutral.png")
    garment_n = copy(ftex / outfit / f"T_{outfit}_Normal.png", out / f"{outfit}_Normal.png")
    garment_m = mask_from_orm(ftex / outfit / f"T_{outfit}_ORM.png", out / f"{outfit}_Mask.png")
    unity_normals = utex / "Normals Unity - Godot"
    mats = {
        f"MI_Superhero_{sex}": material(f"MI_Superhero_{sex}", copy(utex / head, out / "Head_Base.png"),
                                        copy(unity_normals / f"T_Superhero_{sex}_Normal.png", out / "Head_Normal.png"),
                                        mask_from_rough(utex / f"T_Superhero_{sex}_Roughness.png", out / "Head_Mask.png"), skin),
        f"MI_Regular_{sex}": material(f"MI_Regular_{sex}", copy(ftex / f"Base/T_Regular_{sex}_Dark_BaseColor.png", out / "Hands_Base.png"),
                                      copy(ftex / f"Base/T_Regular_{sex}_Normal.png", out / "Hands_Normal.png"),
                                      mask_from_rough(ftex / f"Base/T_Regular_{sex}_Roughness.png", out / "Hands_Mask.png"), skin),
        "MI_Eyes": material("MI_Eyes", copy(utex / "T_Eye_Brown.png", out / "Eye_Base.png"), roughness=0.2),
    }
    for n in (1, 2):
        mats[f"MI_Hair_{n}"] = material(f"MI_Hair_{n}", neutral(utex / f"T_Hair_{n}_BaseColor.png", out / f"Hair{n}_Neutral.png"),
                                        copy(unity_normals / f"T_Hair_{n}_Normal.png", out / f"Hair{n}_Normal.png"),
                                        color=c["hair_tint"], roughness=0.55)
    for part, tint in c["tints"].items():
        mats[f"Outfit_{part}"] = material(f"Outfit_{part}", garment, garment_n, garment_m, tint)
    missing = [m for m in used if m not in mats]
    if missing:
        raise SystemExit(f"{c['slug']}: no texture mapping for {missing}")
    sidecar = {"materials": [mats[m] for m in used], "tris": json.loads((out / "assembly.json").read_text())["tris"]}
    (out / "materials.json").write_text(json.dumps(sidecar, indent=1), encoding="utf-8")
    (out / "source.txt").write_text(
        f"name: {c['name']} (Quaternius Universal Base Characters + Modular Character Outfits - Fantasy, recoloured)\n"
        "source_url: https://quaternius.itch.io/universal-base-characters ; https://quaternius.itch.io/modular-character-outfits-fantasy\n"
        "author: Quaternius\nlicense: CC0\n", encoding="utf-8")
    for f in ("spec.json", "assembly.json"):
        (out / f).unlink()
    print("built", c["slug"], sidecar["tris"], "tris")


if __name__ == "__main__":
    packs_dir = Path(sys.argv[1])
    for character in CHARACTERS:
        build(packs_dir, character)
