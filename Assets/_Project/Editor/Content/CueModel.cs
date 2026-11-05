using System.Collections.Generic;
using UnityEngine;

namespace BilliardGame.Editor.Content
{
    /// <summary>
    /// Lathed cue mesh: pivot at the tip, butt along +Z, 1.47 m. Submesh 0 = decorated butt (textured body material),
    /// submesh 1 = trim from an 8-cell colour atlas (ferrule, tip, accents, shaft, wrap). ~1k triangles.
    /// </summary>
    internal static class CueModelBuilder
    {
        public const float Length = 1.47f;
        public const int AtlasCells = 8;

        // Atlas cells.
        public const int Ferrule = 0, Tip = 1, AccentA = 2, AccentB = 3, Shaft = 4, Wrap = 5, Cap = 6;

        private const int Sides = 16;

        public readonly struct Segment
        {
            public Segment(float z0, float z1, float r0, float r1, int cell)
            {
                Z0 = z0;
                Z1 = z1;
                R0 = r0;
                R1 = r1;
                Cell = cell;
            }

            public float Z0 { get; }
            public float Z1 { get; }
            public float R0 { get; }
            public float R1 { get; }
            /// <summary>Atlas cell, or -1 for the textured body.</summary>
            public int Cell { get; }
        }

        /// <summary>Standard two-piece profile; <paramref name="rings"/> adds accent rings on the forearm and sleeve.</summary>
        public static List<Segment> Profile(int rings, bool wrap)
        {
            var s = new List<Segment>
            {
                new Segment(0.000f, 0.008f, 0.0062f, 0.0064f, Tip),
                new Segment(0.008f, 0.030f, 0.0065f, 0.0066f, Ferrule),
                new Segment(0.030f, 0.700f, 0.0066f, 0.0086f, Shaft),
                new Segment(0.700f, 0.712f, 0.0094f, 0.0100f, AccentB),
                new Segment(0.712f, 0.740f, 0.0102f, 0.0104f, AccentA),
            };

            float forearmStart = 0.740f, forearmEnd = 1.020f;
            AddBody(s, forearmStart, forearmEnd, 0.0105f, 0.0124f, rings);
            s.Add(new Segment(1.020f, 1.030f, 0.0126f, 0.0126f, AccentA));
            s.Add(new Segment(1.030f, 1.300f, 0.0127f, 0.0136f, wrap ? Wrap : -1));
            s.Add(new Segment(1.300f, 1.310f, 0.0138f, 0.0138f, AccentA));
            AddBody(s, 1.310f, 1.452f, 0.0139f, 0.0145f, Mathf.Max(0, rings - 1));
            s.Add(new Segment(1.452f, Length, 0.0145f, 0.0138f, Cap));
            return s;
        }

        private static void AddBody(List<Segment> s, float z0, float z1, float r0, float r1, int rings)
        {
            if (rings <= 0)
            {
                s.Add(new Segment(z0, z1, r0, r1, -1));
                return;
            }

            // Thin accent rings spread over the section; the body shows between them.
            const float ringWidth = 0.006f;
            float span = (z1 - z0 - rings * ringWidth) / (rings + 1);
            float z = z0;
            for (int i = 0; i <= rings; i++)
            {
                float zb = z + span;
                s.Add(new Segment(z, zb, Mathf.Lerp(r0, r1, (z - z0) / (z1 - z0)), Mathf.Lerp(r0, r1, (zb - z0) / (z1 - z0)), -1));
                z = zb;
                if (i == rings) break;
                float rr = Mathf.Lerp(r0, r1, (z - z0) / (z1 - z0)) + 0.0004f;
                s.Add(new Segment(z, z + ringWidth, rr, rr, i % 2 == 0 ? AccentA : AccentB));
                z += ringWidth;
            }
        }

        public static Mesh Build(string name, List<Segment> profile)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var body = new List<int>();
            var trim = new List<int>();

            foreach (Segment seg in profile)
            {
                List<int> target = seg.Cell < 0 ? body : trim;
                int start = vertices.Count;
                for (int ring = 0; ring < 2; ring++)
                {
                    float z = ring == 0 ? seg.Z0 : seg.Z1;
                    float r = ring == 0 ? seg.R0 : seg.R1;
                    for (int i = 0; i <= Sides; i++)
                    {
                        float a = i * Mathf.PI * 2f / Sides;
                        var n = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                        vertices.Add(n * r + Vector3.forward * z);
                        normals.Add(n);
                        uvs.Add(seg.Cell < 0 ? new Vector2(z * 2f, i / (float)Sides) : CellUv(seg.Cell));
                    }
                }

                for (int i = 0; i < Sides; i++)
                {
                    int a = start + i, b = start + i + 1, c = start + Sides + 1 + i, d = c + 1;
                    target.Add(a); target.Add(c); target.Add(b);
                    target.Add(b); target.Add(c); target.Add(d);
                }
            }

            AddCap(vertices, normals, uvs, trim, profile[0].Z0, profile[0].R0, -1f, Tip);
            Segment last = profile[profile.Count - 1];
            AddCap(vertices, normals, uvs, trim, last.Z1, last.R1, 1f, Cap);

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(body, 0);
            mesh.SetTriangles(trim, 1);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddCap(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> target, float z, float r, float facing, int cell)
        {
            int centre = vertices.Count;
            var n = new Vector3(0f, 0f, facing);
            vertices.Add(new Vector3(0f, 0f, z));
            normals.Add(n);
            uvs.Add(CellUv(cell));
            for (int i = 0; i <= Sides; i++)
            {
                float a = i * Mathf.PI * 2f / Sides;
                vertices.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, z));
                normals.Add(n);
                uvs.Add(CellUv(cell));
            }

            for (int i = 0; i < Sides; i++)
            {
                target.Add(centre);
                if (facing > 0) { target.Add(centre + 1 + i); target.Add(centre + 2 + i); }
                else { target.Add(centre + 2 + i); target.Add(centre + 1 + i); }
            }
        }

        private static Vector2 CellUv(int cell) => new Vector2((cell + 0.5f) / AtlasCells, 0.5f);

        /// <summary>8×1 colour atlas, upscaled so mip levels keep cells apart.</summary>
        public static Color32[] Atlas(Color[] cells, out int width, out int height)
        {
            const int cellSize = 8;
            width = AtlasCells * cellSize;
            height = cellSize;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++) pixels[y * width + x] = cells[x / cellSize];
            }

            return pixels;
        }
    }
}
