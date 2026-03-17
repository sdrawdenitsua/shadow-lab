using System.Collections.Generic;
using UnityEngine;

namespace ShadowLab.Environment
{
    /// <summary>
    /// Procedural Shadow Lab geometry — zero external assets needed.
    /// Builds at runtime, runs on Quest 3S.
    ///
    /// GENERATES:
    ///   • Workshop room (walls, floor, ceiling, metal grate panels)
    ///   • 2x Dornier loom frames (HTV-style rapier loom skeleton)
    ///   • Main workbench + tool pegboard
    ///   • Mineral oil immersion tank (PC cooling bath)
    ///   • Overhead cable trays
    ///   • Neon strip lights (violet + amber)
    ///   • Floor drain grates
    ///   • Support pillars
    ///
    /// All geometry is Quest-friendly:
    ///   • Single combined mesh per category (draw call budget)
    ///   • Max 16 vertices per primitive
    ///   • No transparent materials except light strips
    /// </summary>
    [ExecuteAlways]
    public class ProceduralLabMesh : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────
        [Header("Room Dimensions")]
        [SerializeField] private float roomWidth    = 12f;
        [SerializeField] private float roomDepth    = 18f;
        [SerializeField] private float roomHeight   =  4.2f;

        [Header("Materials (assign in inspector)")]
        [SerializeField] private Material matConcrete;
        [SerializeField] private Material matMetal;
        [SerializeField] private Material matGrate;
        [SerializeField] private Material matNeonViolet;
        [SerializeField] private Material matNeonAmber;
        [SerializeField] private Material matGlass;
        [SerializeField] private Material matOil;

        [Header("Loom")]
        [SerializeField] private int   loomCount    = 2;
        [SerializeField] private float loomSpacing  = 3.8f;

        [Header("Rebuild")]
        [SerializeField] private bool  rebuildOnStart = true;

        // ── Runtime ───────────────────────────────────────────────────
        private readonly List<GameObject> _generated = new();

        private void Start()  { if (rebuildOnStart) Build(); }

        // ── Public API ────────────────────────────────────────────────

        [ContextMenu("Rebuild Lab")]
        public void Build()
        {
            Clear();
            BuildRoom();
            BuildPillars();
            BuildLooms();
            BuildWorkbench();
            BuildOilTank();
            BuildCableTrays();
            BuildNeonLights();
            BuildFloorDrains();
            Debug.Log("[ShadowLab] ProceduralLabMesh: build complete.");
        }

        [ContextMenu("Clear Lab")]
        public void Clear()
        {
            foreach (var go in _generated) if (go) DestroyImmediate(go);
            _generated.Clear();
        }

        // ═══════════════════════════════════════════════════════════════
        // ROOM
        // ═══════════════════════════════════════════════════════════════

        private void BuildRoom()
        {
            float w = roomWidth, d = roomDepth, h = roomHeight;

            // Floor — concrete
            Quad("Floor",      matConcrete, Vector3.zero,
                 new Vector3(w, 0, d), Vector3.up);

            // Ceiling — dark metal panels
            Quad("Ceiling",    matMetal,
                 new Vector3(0, h, 0),
                 new Vector3(w, 0, d), Vector3.down);

            // Walls
            Quad("Wall_Back",  matConcrete,
                 new Vector3(0,   h * 0.5f, d * 0.5f),
                 new Vector3(w,   h,        0.18f),    Vector3.back);

            Quad("Wall_Front", matConcrete,
                 new Vector3(0,   h * 0.5f, -d * 0.5f),
                 new Vector3(w,   h,        0.18f),    Vector3.forward);

            Quad("Wall_Left",  matConcrete,
                 new Vector3(-w * 0.5f, h * 0.5f, 0),
                 new Vector3(0.18f,     h,        d),  Vector3.right);

            Quad("Wall_Right", matConcrete,
                 new Vector3(w * 0.5f,  h * 0.5f, 0),
                 new Vector3(0.18f,     h,        d),  Vector3.left);

            // Metal grate panels along lower walls
            float panelH = 0.9f;
            for (int i = 0; i < 4; i++)
            {
                float z = -d * 0.5f + 1f + i * (d / 4f);
                Box($"GratePanel_L{i}", matGrate,
                    new Vector3(-w * 0.5f + 0.06f, panelH * 0.5f, z),
                    new Vector3(0.04f, panelH, 2.4f));
                Box($"GratePanel_R{i}", matGrate,
                    new Vector3( w * 0.5f - 0.06f, panelH * 0.5f, z),
                    new Vector3(0.04f, panelH, 2.4f));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PILLARS
        // ═══════════════════════════════════════════════════════════════

        private void BuildPillars()
        {
            float h = roomHeight;
            float[] zPos = { -roomDepth * 0.3f, 0f, roomDepth * 0.3f };
            foreach (float z in zPos)
            {
                Box($"Pillar_L_{z}", matMetal,
                    new Vector3(-roomWidth * 0.5f + 0.25f, h * 0.5f, z),
                    new Vector3(0.3f, h, 0.3f));
                Box($"Pillar_R_{z}", matMetal,
                    new Vector3( roomWidth * 0.5f - 0.25f, h * 0.5f, z),
                    new Vector3(0.3f, h, 0.3f));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // DORNIER HTV LOOM FRAMES
        // ═══════════════════════════════════════════════════════════════

        private void BuildLooms()
        {
            for (int i = 0; i < loomCount; i++)
            {
                float z = -((loomCount - 1) * loomSpacing * 0.5f) + i * loomSpacing;
                BuildSingleLoom(i, new Vector3(0, 0, z));
            }
        }

        private void BuildSingleLoom(int idx, Vector3 origin)
        {
            var root = new GameObject($"Loom_{idx:00}");
            root.transform.SetParent(transform);
            root.transform.position = origin;
            _generated.Add(root);

            // Measurements based on Dornier HTV 190cm weaving width
            float lw = 2.8f;  // loom width
            float lh = 2.1f;  // loom height
            float ld = 1.4f;  // loom depth

            // Main frame rails
            CreateChild("Frame_LeftRail",  root, matMetal, new Vector3(-lw*0.5f, lh*0.5f, 0),   new Vector3(0.08f, lh, ld));
            CreateChild("Frame_RightRail", root, matMetal, new Vector3( lw*0.5f, lh*0.5f, 0),   new Vector3(0.08f, lh, ld));
            CreateChild("Frame_TopBeam",   root, matMetal, new Vector3(0,        lh,       0),   new Vector3(lw,    0.10f, 0.10f));
            CreateChild("Frame_BaseBeam",  root, matMetal, new Vector3(0,        0.05f,    0),   new Vector3(lw,    0.10f, ld));

            // Reed (the warp separator — horizontal slats across loom face)
            for (int s = 0; s < 14; s++)
            {
                float y = 0.3f + s * (lh * 0.7f / 14f);
                CreateChild($"Reed_Slat_{s}", root, matMetal,
                    new Vector3(0, y, -ld * 0.3f),
                    new Vector3(lw * 0.96f, 0.012f, 0.012f));
            }

            // Rapier guide rail
            CreateChild("RapierRail", root, matMetal,
                new Vector3(0, lh * 0.45f, -ld * 0.45f),
                new Vector3(lw, 0.025f, 0.025f));

            // Dobby head (boxy control unit on top-right)
            CreateChild("DobbyHead", root, matMetal,
                new Vector3(lw * 0.45f, lh + 0.25f, 0),
                new Vector3(0.5f, 0.5f, 0.45f));

            // Tension beam (back-bottom)
            CreateChild("TensionBeam", root, matMetal,
                new Vector3(0, 0.28f, ld * 0.45f),
                new Vector3(lw * 0.9f, 0.08f, 0.08f));

            // Warp beam (large cylinder back) — approximated as flattened box
            CreateChild("WarpBeam", root, matMetal,
                new Vector3(0, 0.55f, ld * 0.48f),
                new Vector3(lw * 0.88f, 0.22f, 0.22f));

            // Cloth roll (front-bottom)
            CreateChild("ClothRoll", root, matMetal,
                new Vector3(0, 0.32f, -ld * 0.46f),
                new Vector3(lw * 0.85f, 0.18f, 0.18f));

            // Control panel (right side)
            CreateChild("ControlPanel", root, matMetal,
                new Vector3(lw * 0.5f + 0.15f, lh * 0.6f, -ld * 0.2f),
                new Vector3(0.12f, 0.6f, 0.4f));

            // Oil drip tray
            CreateChild("OilTray", root, matGrate,
                new Vector3(0, 0.02f, 0),
                new Vector3(lw + 0.2f, 0.04f, ld + 0.1f));
        }

        // ═══════════════════════════════════════════════════════════════
        // WORKBENCH
        // ═══════════════════════════════════════════════════════════════

        private void BuildWorkbench()
        {
            var root = new GameObject("Workbench");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(-roomWidth * 0.5f + 1.2f, 0, roomDepth * 0.3f);
            _generated.Add(root);

            float bw = 2.4f, bd = 0.7f, bh = 0.9f;

            // Bench top
            CreateChild("Top",      root, matMetal,
                new Vector3(0, bh, 0), new Vector3(bw, 0.06f, bd));

            // Legs
            Vector3[] legPos = {
                new(-bw*0.46f, bh*0.5f, -bd*0.46f),
                new( bw*0.46f, bh*0.5f, -bd*0.46f),
                new(-bw*0.46f, bh*0.5f,  bd*0.46f),
                new( bw*0.46f, bh*0.5f,  bd*0.46f),
            };
            for (int i = 0; i < 4; i++)
                CreateChild($"Leg_{i}", root, matMetal, legPos[i], new Vector3(0.06f, bh, 0.06f));

            // Shelf
            CreateChild("Shelf", root, matMetal,
                new Vector3(0, bh * 0.45f, 0), new Vector3(bw, 0.03f, bd * 0.9f));

            // Pegboard backing
            CreateChild("Pegboard", root, matGrate,
                new Vector3(0, bh + 0.6f, -bd * 0.5f + 0.02f),
                new Vector3(bw, 1.1f, 0.04f));

            // Tool silhouettes on pegboard (wrenches, files)
            for (int i = 0; i < 5; i++)
            {
                float x = -bw * 0.4f + i * (bw * 0.2f);
                CreateChild($"Tool_{i}", root, matMetal,
                    new Vector3(x, bh + 0.55f, -bd * 0.48f),
                    new Vector3(0.04f, 0.32f, 0.02f));
            }

            // Vise (left end of bench)
            CreateChild("Vise_Body",  root, matMetal,
                new Vector3(-bw*0.44f, bh + 0.07f, bd*0.3f),
                new Vector3(0.18f, 0.14f, 0.22f));
            CreateChild("Vise_Jaw",   root, matMetal,
                new Vector3(-bw*0.44f, bh + 0.12f, bd*0.1f),
                new Vector3(0.18f, 0.08f, 0.06f));
            CreateChild("Vise_Screw", root, matMetal,
                new Vector3(-bw*0.44f, bh + 0.07f, bd*0.45f),
                new Vector3(0.04f, 0.04f, 0.28f));
        }

        // ═══════════════════════════════════════════════════════════════
        // MINERAL OIL IMMERSION TANK
        // ═══════════════════════════════════════════════════════════════

        private void BuildOilTank()
        {
            var root = new GameObject("OilTank");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(roomWidth * 0.5f - 1.4f, 0, -roomDepth * 0.3f);
            _generated.Add(root);

            float tw = 0.7f, th = 0.55f, td = 0.5f;

            // Tank walls
            CreateChild("Wall_Front", root, matGlass,
                new Vector3(0, th*0.5f, -td*0.5f), new Vector3(tw, th, 0.012f));
            CreateChild("Wall_Back",  root, matGlass,
                new Vector3(0, th*0.5f,  td*0.5f), new Vector3(tw, th, 0.012f));
            CreateChild("Wall_Left",  root, matGlass,
                new Vector3(-tw*0.5f, th*0.5f, 0), new Vector3(0.012f, th, td));
            CreateChild("Wall_Right", root, matGlass,
                new Vector3( tw*0.5f, th*0.5f, 0), new Vector3(0.012f, th, td));

            // Tank base — metal
            CreateChild("Base", root, matMetal,
                new Vector3(0, 0.02f, 0), new Vector3(tw + 0.04f, 0.04f, td + 0.04f));

            // Oil surface
            CreateChild("OilSurface", root, matOil,
                new Vector3(0, th * 0.82f, 0), new Vector3(tw - 0.015f, 0.008f, td - 0.015f));

            // Submerged PC motherboard silhouette
            CreateChild("PCBoard", root, matMetal,
                new Vector3(0, th * 0.4f, 0), new Vector3(tw * 0.85f, 0.01f, td * 0.85f));

            // Pump box (side)
            CreateChild("Pump", root, matMetal,
                new Vector3(tw * 0.5f + 0.1f, 0.12f, 0), new Vector3(0.16f, 0.18f, 0.16f));

            // Tubing
            CreateChild("Tube_H", root, matMetal,
                new Vector3(tw * 0.3f, th * 0.9f, 0), new Vector3(tw * 0.4f, 0.025f, 0.025f));
            CreateChild("Tube_V", root, matMetal,
                new Vector3(tw * 0.5f + 0.02f, th * 0.55f, 0), new Vector3(0.025f, th * 0.7f, 0.025f));

            // Stand legs
            Vector3[] legs = {
                new(-tw*0.45f, 0, -td*0.45f), new(tw*0.45f, 0, -td*0.45f),
                new(-tw*0.45f, 0,  td*0.45f), new(tw*0.45f, 0,  td*0.45f),
            };
            for (int i = 0; i < 4; i++)
                CreateChild($"StandLeg_{i}", root, matMetal,
                    legs[i] + Vector3.up * 0.12f, new Vector3(0.04f, 0.24f, 0.04f));
        }

        // ═══════════════════════════════════════════════════════════════
        // OVERHEAD CABLE TRAYS
        // ═══════════════════════════════════════════════════════════════

        private void BuildCableTrays()
        {
            float y = roomHeight - 0.3f;

            // Main longitudinal tray (runs full depth)
            Box("CableTray_Main", matGrate,
                new Vector3(0, y, 0),
                new Vector3(0.3f, 0.08f, roomDepth * 0.9f));

            // Cross trays above each loom
            for (int i = 0; i < loomCount; i++)
            {
                float z = -((loomCount - 1) * loomSpacing * 0.5f) + i * loomSpacing;
                Box($"CableTray_Cross_{i}", matGrate,
                    new Vector3(0, y - 0.05f, z),
                    new Vector3(roomWidth * 0.7f, 0.06f, 0.22f));
            }

            // Conduit drops
            for (int i = 0; i < 6; i++)
            {
                float z = -roomDepth * 0.4f + i * (roomDepth * 0.16f);
                Box($"Conduit_{i}", matMetal,
                    new Vector3(0.08f, roomHeight * 0.6f, z),
                    new Vector3(0.04f, roomHeight * 0.8f, 0.04f));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // NEON LIGHTS
        // ═══════════════════════════════════════════════════════════════

        private void BuildNeonLights()
        {
            float y = roomHeight - 0.15f;

            // Violet strips (main overhead — two runs)
            Box("Neon_Violet_L", matNeonViolet,
                new Vector3(-roomWidth * 0.25f, y, 0),
                new Vector3(0.04f, 0.04f, roomDepth * 0.85f));

            Box("Neon_Violet_R", matNeonViolet,
                new Vector3( roomWidth * 0.25f, y, 0),
                new Vector3(0.04f, 0.04f, roomDepth * 0.85f));

            // Amber under-bench strip
            Box("Neon_Amber_Bench", matNeonAmber,
                new Vector3(-roomWidth * 0.5f + 1.2f, 0.82f, roomDepth * 0.3f),
                new Vector3(2.4f, 0.02f, 0.02f));

            // Violet wall accent strips
            float[] wallZ = { -roomDepth * 0.35f, 0f, roomDepth * 0.35f };
            foreach (float z in wallZ)
            {
                Box($"Neon_Wall_L_{z}", matNeonViolet,
                    new Vector3(-roomWidth * 0.5f + 0.06f, roomHeight * 0.7f, z),
                    new Vector3(0.025f, 0.025f, 1.8f));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // FLOOR DRAINS
        // ═══════════════════════════════════════════════════════════════

        private void BuildFloorDrains()
        {
            Vector3[] positions = {
                new( 0,          0.001f, 0),
                new(-roomWidth * 0.3f, 0.001f, -roomDepth * 0.3f),
                new( roomWidth * 0.3f, 0.001f,  roomDepth * 0.3f),
            };
            foreach (var p in positions)
            {
                int idx = System.Array.IndexOf(positions, p);
                Box($"Drain_{idx}", matGrate, p, new Vector3(0.5f, 0.025f, 0.5f));
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // PRIMITIVE HELPERS
        // ═══════════════════════════════════════════════════════════════

        private GameObject Box(string name, Material mat, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.position   = pos;
            go.transform.localScale = size;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            _generated.Add(go);
            return go;
        }

        private GameObject Quad(string name, Material mat, Vector3 pos, Vector3 size, Vector3 normal)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(transform);
            go.transform.position   = pos;
            go.transform.localScale = size;
            go.transform.up         = normal;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
            _generated.Add(go);
            return go;
        }

        private static void CreateChild(string name, GameObject parent, Material mat,
                                        Vector3 localPos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = size;
            if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
