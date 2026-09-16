// ============================================================
// GEAYI: Obby Xtreme 3D — Constructor de ciudad (versión web)
// Ciudad bonita y colorida como la web: cielo azul vivo, calles
// con doble línea amarilla, banquetas blancas, señal de ALTO con
// letras, tiendas con nombres, semáforo, nubes, monedas grandes.
// Versión 2 — 2026-09-16
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace Geayi.World
{
    public class CityBuilder : MonoBehaviour
    {
        public static CityBuilder Instance;

        private readonly List<GameObject> streamables = new List<GameObject>();

        private Material asphaltMat, sidewalkMat, curbMat, grassMat;
        private Material laneMat, crossMat;
        private Material poleMat, lampHeadMat, lampLightMat;
        private Material trunkMat, leafMat, flowerMat, flowerMat2, planterMat;
        private Material altoMat, signBoardMat;
        private Material cloudMat, benchMat, trafficBoxMat;
        private Material trafficRedMat, trafficYelMat, trafficGrnMat;
        private Material waterTankMat, waterLegMat, roofOrangeMat, doorMat;
        private Material[] buildingMats, roofMats, storeMats;
        private Mesh cubeMesh, cylMesh, sphMesh;

        private float blockSize = 60f;
        private float streetWidth = 10f;
        private float cityW, cityD;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CreateSharedAssets();
            BuildCity();
        }

        // ---------------- materiales ----------------
        private Material MakeMat(Color c, float emission = 0f)
        {
            Material m = new Material(Shader.Find("Standard"));
            if (m.shader == null || m.shader.name == "Hidden/InternalErrorShader")
                m = new Material(Shader.Find("UI/Default"));
            m.color = c;
            if (emission > 0f && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emission);
            }
            return m;
        }

        private void CreateSharedAssets()
        {
            cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            cylMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
            sphMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");

            asphaltMat = MakeMat(new Color(0.33f, 0.34f, 0.39f));       // calle gris (como la web)
            sidewalkMat = MakeMat(new Color(0.93f, 0.93f, 0.95f));      // banqueta casi blanca
            curbMat = MakeMat(new Color(0.80f, 0.81f, 0.84f));
            grassMat = MakeMat(new Color(0.30f, 0.72f, 0.32f));         // pasto verde vivo
            laneMat = MakeMat(new Color(1.0f, 0.84f, 0.10f), 0.55f);    // doble línea amarilla
            crossMat = MakeMat(new Color(0.98f, 0.98f, 0.98f));

            poleMat = MakeMat(new Color(0.22f, 0.24f, 0.30f));
            lampHeadMat = MakeMat(new Color(0.25f, 0.27f, 0.33f));
            lampLightMat = MakeMat(new Color(1.0f, 0.95f, 0.75f), 1.2f);

            trunkMat = MakeMat(new Color(0.45f, 0.30f, 0.18f));
            leafMat = MakeMat(new Color(0.25f, 0.68f, 0.30f));
            flowerMat = MakeMat(new Color(0.95f, 0.30f, 0.35f));
            flowerMat2 = MakeMat(new Color(0.95f, 0.55f, 0.85f));
            planterMat = MakeMat(new Color(0.55f, 0.38f, 0.24f));

            altoMat = MakeMat(new Color(0.85f, 0.15f, 0.15f));          // señal ALTO roja
            signBoardMat = MakeMat(new Color(1.0f, 0.80f, 0.15f), 0.25f); // letreros amarillos

            cloudMat = MakeMat(new Color(1f, 1f, 1f));                  // nubes blancas
            benchMat = MakeMat(new Color(0.60f, 0.42f, 0.25f));         // banca de madera
            trafficBoxMat = MakeMat(new Color(0.10f, 0.10f, 0.12f));
            trafficRedMat = MakeMat(new Color(1f, 0.2f, 0.2f), 1.5f);
            trafficYelMat = MakeMat(new Color(1f, 0.85f, 0.2f), 0.4f);
            trafficGrnMat = MakeMat(new Color(0.2f, 0.9f, 0.3f), 0.4f);

            waterTankMat = MakeMat(new Color(0.92f, 0.93f, 0.95f));     // tinaco blanco
            waterLegMat = MakeMat(new Color(0.55f, 0.57f, 0.60f));
            roofOrangeMat = MakeMat(new Color(0.90f, 0.45f, 0.20f));    // techos naranja
            doorMat = MakeMat(new Color(0.30f, 0.20f, 0.12f));

            buildingMats = new Material[]
            {
                MakeMat(new Color(0.95f, 0.55f, 0.65f)),  // rosa
                MakeMat(new Color(0.55f, 0.80f, 0.95f)),  // celeste
                MakeMat(new Color(1.00f, 0.85f, 0.45f)),  // amarillo
                MakeMat(new Color(0.65f, 0.90f, 0.60f)),  // verde
                MakeMat(new Color(0.75f, 0.60f, 0.95f)),  // morado
                MakeMat(new Color(1.00f, 0.65f, 0.45f)),  // naranja
            };
            roofMats = new Material[]
            {
                MakeMat(new Color(0.55f, 0.58f, 0.62f)),
                MakeMat(new Color(0.45f, 0.48f, 0.52f)),
            };
            storeMats = new Material[]
            {
                MakeMat(new Color(0.95f, 0.45f, 0.35f)),  // paletería
                MakeMat(new Color(0.95f, 0.70f, 0.30f)),  // hamburguesas
                MakeMat(new Color(0.45f, 0.75f, 0.95f)),  // taquería
            };
        }

        // ---------------- ciudad ----------------
        private void BuildCity()
        {
            // Ambiente como la web: cielo azul vivo, sin niebla pesada
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = new Color(0.52f, 0.80f, 0.98f);
                cam.clearFlags = CameraClearFlags.SolidColor;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.83f, 0.97f);
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 520f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.80f, 0.86f);

            Light sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.90f);
            sun.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.transform.SetParent(transform, false);

            int blocks = 3;
            cityW = blocks * blockSize;
            cityD = blocks * blockSize;

            // Base de pasto
            GameObject ground = Part("Ground", cubeMesh, grassMat,
                new Vector3(cityW / 2f, -0.55f, cityD / 2f),
                new Vector3(cityW + streetWidth, 1f, cityD + streetWidth), null);
            ground.GetComponent<Collider>().enabled = true;

            for (int ix = 0; ix < blocks; ix++)
                for (int iz = 0; iz < blocks; iz++)
                    BuildBlock(ix * blockSize, iz * blockSize, ix, iz);

            BuildWaterTower(cityW * 0.72f, cityD * 0.78f);
            BuildClouds();

            var sm = GetComponent<StreamingManager>();
            if (sm == null) sm = gameObject.AddComponent<StreamingManager>();
            sm.streamables = streamables;
        }

        private void BuildBlock(float bx, float bz, int ix, int iz)
        {
            GameObject block = new GameObject("Block_" + ix + "_" + iz);
            block.transform.SetParent(transform, false);

            // Calle horizontal (z de bz-10 a bz) con banquetas blancas
            Part("StreetH", cubeMesh, asphaltMat, new Vector3(bx + blockSize / 2f, 0f, bz - streetWidth / 2f),
                new Vector3(blockSize, 0.1f, streetWidth), block, true);
            Part("WalkN", cubeMesh, sidewalkMat, new Vector3(bx + blockSize / 2f, 0.02f, bz + 1.2f),
                new Vector3(blockSize, 0.14f, 3.4f), block, true);
            Part("WalkS", cubeMesh, sidewalkMat, new Vector3(bx + blockSize / 2f, 0.02f, bz - streetWidth - 1.2f),
                new Vector3(blockSize, 0.14f, 3.4f), block, true);
            // Calle vertical (x de bx-10 a bx)
            Part("StreetV", cubeMesh, asphaltMat, new Vector3(bx - streetWidth / 2f, 0f, bz + blockSize / 2f),
                new Vector3(streetWidth, 0.1f, blockSize), block, true);

            // Doble línea amarilla continua (como la web)
            for (int s = -1; s <= 1; s += 2)
                Part("Lane", cubeMesh, laneMat, new Vector3(bx + blockSize / 2f, 0.09f, bz - streetWidth / 2f + s * 0.45f),
                    new Vector3(blockSize - 4f, 0.04f, 0.28f), block);

            // Cruce peatonal
            for (int i = 0; i < 5; i++)
                Part("Cross", cubeMesh, crossMat, new Vector3(bx - streetWidth + 2f + i * 1.9f, 0.09f, bz - streetWidth / 2f),
                    new Vector3(1.1f, 0.04f, streetWidth - 2.4f), block);

            // Farolas bajas con collider (la cámara ya no las atraviesa)
            BuildLamp(bx + 12f, bz + 1.5f, block);
            BuildLamp(bx + 48f, bz + 1.5f, block);
            BuildLamp(bx + 30f, bz - streetWidth - 1.5f, block);

            // Banca, flores y señal de ALTO en la banqueta norte
            BuildBench(bx + 30f, bz + 2.4f, block);
            BuildFlowers(bx + 22f, bz + 1.2f, block);
            BuildFlowers(bx + 38f, bz + 1.2f, block);
            BuildAltoSign(bx + 6f, bz + 1.2f, block);

            // Semáforo en la esquina del cruce
            BuildTrafficLight(bx + 1.5f, bz + 1.5f, block);

            // Árboles
            BuildTree(bx + 18f, bz + 18f, block);
            BuildTree(bx + 31f, bz + 18f, block);
            BuildTree(bx + 44f, bz + 18f, block);
            BuildTree(bx + 18f, bz + 31f, block);
            BuildTree(bx + 44f, bz + 44f, block);

            // Edificios (unos con techo plano, otros con techo naranja como la web)
            BuildBuilding(bx + 20f, bz + 26f, 0, block, false);
            BuildBuilding(bx + 42f, bz + 44f, 3, block, true);

            // Tiendas con nombres
            BuildStore(bx + blockSize / 2f - 14f, bz + 9f, 0, "DELICIAS\nMICHOACANAS", block);
            BuildStore(bx + blockSize / 2f + 2f, bz + 9f, 1, "HAMBURGUESAS", block);
            BuildStore(bx + blockSize / 2f + 18f, bz + 9f, 2, "TAQUERÍA", block);

            // Monedas grandes a lo largo de la calle
            for (int i = 0; i < 4; i++)
                CoinPickup.Spawn(block.transform, new Vector3(bx + 10f + i * 14f, 0.9f, bz - streetWidth / 2f), 5);

            streamables.Add(block);
        }

        // ---------------- piezas ----------------
        private GameObject Part(string name, Mesh mesh, Material mat, Vector3 pos, Vector3 scale,
                                GameObject parent, bool solid = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent.transform : transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.material = mat;
            if (solid)
            {
                BoxCollider bc = go.AddComponent<BoxCollider>();
                bc.size = Vector3.one;
            }
            return go;
        }

        private void BuildLamp(float x, float z, GameObject parent)
        {
            GameObject lamp = new GameObject("Lamp");
            lamp.transform.SetParent(parent.transform, false);
            lamp.transform.localPosition = new Vector3(x, 0f, z);
            // Poste bajo (4.2m) con collider: la cámara ya no lo atraviesa
            Part("Pole", cylMesh, poleMat, new Vector3(0f, 2.1f, 0f),
                new Vector3(0.28f, 4.2f, 0.28f), lamp, true);
            Part("Arm", cubeMesh, poleMat, new Vector3(0.8f, 4.05f, 0f),
                new Vector3(1.7f, 0.16f, 0.16f), lamp);
            Part("Head", cubeMesh, lampHeadMat, new Vector3(1.55f, 3.95f, 0f),
                new Vector3(0.55f, 0.28f, 0.35f), lamp);
            Part("Light", sphMesh, lampLightMat, new Vector3(1.55f, 3.78f, 0f),
                new Vector3(0.32f, 0.22f, 0.32f), lamp);
        }

        private void BuildTree(float x, float z, GameObject parent)
        {
            GameObject tree = new GameObject("Tree");
            tree.transform.SetParent(parent.transform, false);
            tree.transform.localPosition = new Vector3(x, 0f, z);
            Part("Trunk", cylMesh, trunkMat, new Vector3(0f, 1.1f, 0f),
                new Vector3(0.5f, 2.2f, 0.5f), tree, true);
            Part("Leaves", sphMesh, leafMat, new Vector3(0f, 3.1f, 0f),
                new Vector3(2.6f, 2.4f, 2.6f), tree);
            Part("Leaves2", sphMesh, leafMat, new Vector3(0.8f, 2.5f, 0.4f),
                new Vector3(1.7f, 1.5f, 1.7f), tree);
        }

        private void BuildFlowers(float x, float z, GameObject parent)
        {
            GameObject f = new GameObject("Flowers");
            f.transform.SetParent(parent.transform, false);
            f.transform.localPosition = new Vector3(x, 0f, z);
            Part("Planter", cubeMesh, planterMat, new Vector3(0f, 0.35f, 0f),
                new Vector3(2.4f, 0.7f, 1.2f), f, true);
            Part("Bush", cubeMesh, leafMat, new Vector3(-0.55f, 1.05f, 0f),
                new Vector3(1.0f, 0.9f, 0.9f), f);
            Part("Bush2", cubeMesh, leafMat, new Vector3(0.55f, 1.05f, 0f),
                new Vector3(1.0f, 0.9f, 0.9f), f);
            Part("Flower", sphMesh, flowerMat, new Vector3(-0.55f, 1.6f, 0f),
                new Vector3(0.35f, 0.35f, 0.35f), f);
            Part("Flower2", sphMesh, flowerMat2, new Vector3(0.55f, 1.6f, 0f),
                new Vector3(0.35f, 0.35f, 0.35f), f);
        }

        private void BuildBench(float x, float z, GameObject parent)
        {
            GameObject b = new GameObject("Bench");
            b.transform.SetParent(parent.transform, false);
            b.transform.localPosition = new Vector3(x, 0f, z);
            Part("LegL", cubeMesh, benchMat, new Vector3(-0.7f, 0.25f, 0f),
                new Vector3(0.15f, 0.5f, 0.5f), b, true);
            Part("LegR", cubeMesh, benchMat, new Vector3(0.7f, 0.25f, 0f),
                new Vector3(0.15f, 0.5f, 0.5f), b, true);
            Part("Seat", cubeMesh, benchMat, new Vector3(0f, 0.55f, 0f),
                new Vector3(1.8f, 0.12f, 0.55f), b);
            Part("Back", cubeMesh, benchMat, new Vector3(0f, 1.0f, 0.26f),
                new Vector3(1.8f, 0.55f, 0.12f), b);
        }

        private void BuildTrafficLight(float x, float z, GameObject parent)
        {
            GameObject t = new GameObject("TrafficLight");
            t.transform.SetParent(parent.transform, false);
            t.transform.localPosition = new Vector3(x, 0f, z);
            Part("Pole", cylMesh, trafficBoxMat, new Vector3(0f, 1.75f, 0f),
                new Vector3(0.25f, 3.5f, 0.25f), t, true);
            Part("Box", cubeMesh, trafficBoxMat, new Vector3(0f, 4.1f, 0f),
                new Vector3(0.8f, 2.0f, 0.8f), t);
            Part("Red", sphMesh, trafficRedMat, new Vector3(0f, 4.7f, 0.42f),
                new Vector3(0.4f, 0.4f, 0.2f), t);
            Part("Yel", sphMesh, trafficYelMat, new Vector3(0f, 4.1f, 0.42f),
                new Vector3(0.4f, 0.4f, 0.2f), t);
            Part("Grn", sphMesh, trafficGrnMat, new Vector3(0f, 3.5f, 0.42f),
                new Vector3(0.4f, 0.4f, 0.2f), t);
        }

        private void BuildAltoSign(float x, float z, GameObject parent)
        {
            GameObject s = new GameObject("AltoSign");
            s.transform.SetParent(parent.transform, false);
            s.transform.localPosition = new Vector3(x, 0f, z);
            Part("Pole", cylMesh, poleMat, new Vector3(0f, 1.1f, 0f),
                new Vector3(0.18f, 2.2f, 0.18f), s, true);
            // Tablero rojo con "ALTO" en blanco por los dos lados (como la web)
            Part("Board", cubeMesh, altoMat, new Vector3(0f, 2.6f, 0f),
                new Vector3(2.4f, 1.5f, 0.15f), s);
            WorldLabel(s.transform, "ALTO", 64, Color.white, 2.3f, 1.4f,
                new Vector3(0f, 2.6f, 0.09f), 0f);
            WorldLabel(s.transform, "ALTO", 64, Color.white, 2.3f, 1.4f,
                new Vector3(0f, 2.6f, -0.09f), 180f);
        }

        private void BuildBuilding(float cx, float cz, int colorIdx, GameObject parent, bool gableRoof)
        {
            GameObject b = new GameObject("Building");
            b.transform.SetParent(parent.transform, false);
            // Edificios ALTOS como los del video (3-5 pisos)
            float w = 12f, d = 10f, h = 14f + (colorIdx % 3) * 3.5f;
            Material bodyMat = MakeMat(new Color(0.72f, 0.45f, 0.35f)); // ladrillo
            if (colorIdx % 2 == 0) bodyMat = buildingMats[colorIdx % buildingMats.Length];
            Part("Body", cubeMesh, bodyMat,
                new Vector3(cx, h / 2f, cz), new Vector3(w, h, d), b, true);
            // Base de concreto
            Part("Base", cubeMesh, curbMat,
                new Vector3(cx, 0.6f, cz), new Vector3(w + 0.4f, 1.2f, d + 0.4f), b, true);
            // Puerta de vidrio al frente
            Material glassMat = MakeMat(new Color(0.65f, 0.85f, 0.95f), 0.45f);
            Part("Door", cubeMesh, glassMat, new Vector3(cx, 1.5f, cz - d / 2f - 0.06f),
                new Vector3(2.4f, 3.0f, 0.12f), b);
            // Franjas de vidrio en los 4 lados, piso por piso (como el video)
            int floors = Mathf.FloorToInt(h / 3.4f);
            for (int f = 0; f < floors; f++)
            {
                float wy = 4.6f + f * 3.4f;
                // Sur y norte
                Part("WinS", cubeMesh, glassMat, new Vector3(cx, wy, cz - d / 2f - 0.06f),
                    new Vector3(w * 0.86f, 1.9f, 0.12f), b);
                Part("WinN", cubeMesh, glassMat, new Vector3(cx, wy, cz + d / 2f + 0.06f),
                    new Vector3(w * 0.86f, 1.9f, 0.12f), b);
                // Este y oeste
                Part("WinE", cubeMesh, glassMat, new Vector3(cx + w / 2f + 0.06f, wy, cz),
                    new Vector3(0.12f, 1.9f, d * 0.86f), b);
                Part("WinW", cubeMesh, glassMat, new Vector3(cx - w / 2f - 0.06f, wy, cz),
                    new Vector3(0.12f, 1.9f, d * 0.86f), b);
            }
            if (gableRoof)
            {
                // Techo naranja a dos aguas (como las casas de la web)
                GameObject r1 = Part("RoofL", cubeMesh, roofOrangeMat,
                    new Vector3(cx - w / 4f, h + 1.1f, cz), new Vector3(w / 2f + 0.6f, 0.35f, d + 0.8f), b);
                r1.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
                GameObject r2 = Part("RoofR", cubeMesh, roofOrangeMat,
                    new Vector3(cx + w / 4f, h + 1.1f, cz), new Vector3(w / 2f + 0.6f, 0.35f, d + 0.8f), b);
                r2.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            }
            else
            {
                Part("Roof", cubeMesh, roofMats[colorIdx % roofMats.Length],
                    new Vector3(cx, h + 0.25f, cz), new Vector3(w + 0.8f, 0.5f, d + 0.8f), b);
                Part("RoofTop", cubeMesh, roofMats[(colorIdx + 1) % roofMats.Length],
                    new Vector3(cx, h + 0.7f, cz), new Vector3(w * 0.5f, 0.4f, d * 0.5f), b);
            }
        }

        private void BuildStore(float cx, float cz, int kind, string name, GameObject parent)
        {
            GameObject s = new GameObject("Store");
            s.transform.SetParent(parent.transform, false);
            float w = 13f, h = 4.6f, d = 5f;
            Part("Body", cubeMesh, storeMats[kind % storeMats.Length],
                new Vector3(cx, h / 2f, cz), new Vector3(w, h, d), s, true);
            Part("Roof", cubeMesh, curbMat,
                new Vector3(cx, h + 0.2f, cz), new Vector3(w + 0.6f, 0.4f, d + 0.6f), s);
            // Vitrina y puerta al frente (lado sur, hacia la calle)
            Material glassMat = MakeMat(new Color(0.70f, 0.88f, 1.0f), 0.4f);
            Part("Glass", cubeMesh, glassMat, new Vector3(cx - 2.5f, 1.8f, cz - d / 2f - 0.06f),
                new Vector3(5.5f, 2.4f, 0.12f), s);
            Part("Door", cubeMesh, doorMat, new Vector3(cx + 3.5f, 1.3f, cz - d / 2f - 0.06f),
                new Vector3(1.8f, 2.6f, 0.12f), s);
            // Letrero amarillo con el nombre de la tienda
            Part("Sign", cubeMesh, signBoardMat, new Vector3(cx, h + 1.35f, cz - 1.0f),
                new Vector3(w * 0.8f, 1.5f, 0.3f), s);
            Part("SignPoleL", cylMesh, poleMat, new Vector3(cx - w * 0.32f, h + 0.55f, cz - 1.0f),
                new Vector3(0.18f, 1.6f, 0.18f), s);
            Part("SignPoleR", cylMesh, poleMat, new Vector3(cx + w * 0.32f, h + 0.55f, cz - 1.0f),
                new Vector3(0.18f, 1.6f, 0.18f), s);
            WorldLabel(s.transform, name, 56, new Color(0.6f, 0.1f, 0.1f), w * 0.75f, 1.35f,
                new Vector3(cx, h + 1.35f, cz - 1.18f), 180f);
        }

        private void BuildWaterTower(float x, float z)
        {
            GameObject t = new GameObject("WaterTower");
            t.transform.SetParent(transform, false);
            t.transform.localPosition = new Vector3(x, 0f, z);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f * Mathf.Deg2Rad;
                Part("Leg" + i, cylMesh, waterLegMat,
                    new Vector3(Mathf.Cos(a) * 2.6f, 6f, Mathf.Sin(a) * 2.6f),
                    new Vector3(0.45f, 12f, 0.45f), t, true);
            }
            Part("Tank", cylMesh, waterTankMat, new Vector3(0f, 14.5f, 0f),
                new Vector3(7.5f, 5.5f, 7.5f), t);
            Part("TankRoof", cylMesh, roofOrangeMat, new Vector3(0f, 17.5f, 0f),
                new Vector3(8.0f, 1.0f, 8.0f), t);
        }

        private void BuildClouds()
        {
            GameObject clouds = new GameObject("Clouds");
            clouds.transform.SetParent(transform, false);
            System.Random rng = new System.Random(7);
            for (int i = 0; i < 8; i++)
            {
                GameObject c = new GameObject("Cloud" + i);
                c.transform.SetParent(clouds.transform, false);
                float cx = (float)(rng.NextDouble() * (cityW + 80) - 40);
                float cz = (float)(rng.NextDouble() * (cityD + 80) - 40);
                float cy = 42f + (float)rng.NextDouble() * 22f;
                c.transform.localPosition = new Vector3(cx, cy, cz);
                float s = 6f + (float)rng.NextDouble() * 6f;
                Part("P1", cubeMesh, cloudMat, new Vector3(0f, 0f, 0f),
                    new Vector3(s * 2.2f, s * 0.55f, s * 1.1f), c);
                Part("P2", cubeMesh, cloudMat, new Vector3(s * 0.7f, s * 0.35f, s * 0.2f),
                    new Vector3(s * 1.2f, s * 0.5f, s * 0.9f), c);
                Part("P3", cubeMesh, cloudMat, new Vector3(-s * 0.7f, s * 0.3f, -s * 0.15f),
                    new Vector3(s * 1.1f, s * 0.45f, s * 0.85f), c);
            }
        }

        // Etiqueta de texto flotando en el mundo (ALTO, nombres de tiendas).
        // Usa el mismo UILabel del HUD (probado en Android).
        private void WorldLabel(Transform parent, string text, int fontSize, Color color,
                                float widthMeters, float heightMeters, Vector3 localPos, float yawDeg)
        {
            GameObject cgo = new GameObject("WorldLabel");
            cgo.transform.SetParent(parent, false);
            cgo.transform.localPosition = localPos;
            cgo.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);
            Canvas c = cgo.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            const float PPU = 100f; // 100 unidades de canvas = 1 metro
            RectTransform rt = cgo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(widthMeters * PPU, heightMeters * PPU);
            cgo.transform.localScale = Vector3.one / PPU;
            GameObject label = Geayi.UI.UILabel.CreateLabel(cgo.transform, text, fontSize, color);
            RectTransform lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var txt = label.GetComponent<UnityEngine.UI.Text>();
            if (txt != null) txt.alignment = TextAnchor.MiddleCenter;
        }
    }
}
