// CityBuilder.cs — construye la ciudad procedural al arrancar la escena.
// Versión "bonita": materiales con luz real (Standard con respaldo seguro),
// paleta viva estilo web (pasto verde, edificios de colores, tiendas),
// calle con líneas amarillas y cruces peatonales, palmeras, flores,
// monedas doradas y las tiendas de la familia (Los Hermanos Tamps, etc.).
// Eficiente: materiales y mallas se crean UNA vez y se reutilizan.
// La decoración se registra en el StreamingManager (solo se muestra lo cercano).
using UnityEngine;

namespace Geayi.World
{
    public class CityBuilder : MonoBehaviour
    {
        [Header("Tamaño de la ciudad")]
        public int blocksX = 3;
        public int blocksZ = 3;
        public float blockSize = 60f;
        public float streetWidth = 10f;

        [Header("Decoración")]
        public int lampsPerStreet = 4;
        public int treesPerBlock = 6;

        // Materiales reutilizados (se crean una sola vez)
        private Material asphaltMat;
        private Material sidewalkMat;
        private Material curbMat;
        private Material grassMat;
        private Material laneMat;
        private Material crosswalkMat;
        private Material poleMat;
        private Material lampLightMat;
        private Material trunkMat;
        private Material leafMat;
        private Material palmLeafMat;
        private Material signMat;
        private Material signPoleMat;
        private Material windowMat;
        private Material planterMat;
        private Material[] buildingMats;
        private Material[] flowerMats;
        private Material truckRedMat;
        private Material truckWhiteMat;
        private Material wheelMat;
        private Material signBoardMat;

        // Shader con luz real: "Standard" casi siempre está en el build
        // (los carros ya lo usan). Si faltara, se usa el respaldo seguro.
        private static Shader litShader;
        private static Shader safeShader;
        private static Shader LitShader()
        {
            if (litShader == null)
            {
                litShader = Shader.Find("Standard");
                if (litShader == null) litShader = SafeShader();
            }
            return litShader;
        }
        private static Shader SafeShader()
        {
            if (safeShader == null)
            {
                safeShader = Shader.Find("UI/Default");
                if (safeShader == null)
                {
                    GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    safeShader = tmp.GetComponent<Renderer>().sharedMaterial.shader;
                    Destroy(tmp);
                }
            }
            return safeShader;
        }

        // Mallas reutilizadas (una sola de cada tipo)
        private Mesh cubeMesh;
        private Mesh cylMesh;
        private Mesh sphereMesh;

        private Transform cityRoot;

        void Start()
        {
            // Cielo con profundidad: niebla del color del cielo + luz ambiental
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.53f, 0.81f, 0.98f);
            RenderSettings.fogStartDistance = 90f;
            RenderSettings.fogEndDistance = 280f;
            RenderSettings.ambientLight = new Color(0.72f, 0.75f, 0.82f);

            if (StreamingManager.Instance == null)
                new GameObject("StreamingManager").AddComponent<StreamingManager>();

            CreateSharedAssets();
            cityRoot = new GameObject("City").transform;
            BuildCity();

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                StreamingManager.Instance.SetPlayer(p.transform);

            Debug.Log("[CityBuilder] Ciudad bonita lista. Objetos en streaming: " +
                      StreamingManager.Instance.RegisteredCount);
        }

        private Material MakeMat(Color c, bool emissive = false, float emissionBoost = 1.5f)
        {
            Material m = new Material(LitShader());
            m.color = c;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * emissionBoost);
            }
            return m;
        }

        private void CreateSharedAssets()
        {
            asphaltMat   = MakeMat(new Color(0.20f, 0.22f, 0.28f));
            sidewalkMat  = MakeMat(new Color(0.82f, 0.83f, 0.86f));
            curbMat      = MakeMat(new Color(0.68f, 0.70f, 0.73f));
            grassMat     = MakeMat(new Color(0.25f, 0.70f, 0.30f)); // pasto vivo
            laneMat      = MakeMat(new Color(1f, 0.85f, 0.15f), true, 0.6f);
            crosswalkMat = MakeMat(new Color(0.95f, 0.95f, 0.96f));
            poleMat      = MakeMat(new Color(0.16f, 0.18f, 0.24f));
            lampLightMat = MakeMat(new Color(1f, 0.90f, 0.45f), true);
            trunkMat     = MakeMat(new Color(0.45f, 0.30f, 0.18f));
            leafMat      = MakeMat(new Color(0.16f, 0.62f, 0.30f)); // verde vivo
            palmLeafMat  = MakeMat(new Color(0.20f, 0.72f, 0.35f));
            signMat      = MakeMat(new Color(0.85f, 0.12f, 0.12f));
            signPoleMat  = MakeMat(new Color(0.55f, 0.56f, 0.60f));
            windowMat    = MakeMat(new Color(0.80f, 0.92f, 1f), true, 0.35f);
            planterMat   = MakeMat(new Color(0.50f, 0.33f, 0.20f));
            truckRedMat   = MakeMat(new Color(0.85f, 0.15f, 0.15f)); // camión rojo Tamps
            truckWhiteMat = MakeMat(new Color(0.96f, 0.96f, 0.97f));
            wheelMat      = MakeMat(new Color(0.12f, 0.12f, 0.14f));
            signBoardMat  = MakeMat(new Color(1f, 0.82f, 0.15f), true, 0.5f);

            // Paleta de edificios (colores fuertes estilo web)
            Color[] palette = {
                new Color(1f, 0.45f, 0.35f),   // coral
                new Color(0.15f, 0.75f, 0.75f),// turquesa
                new Color(1f, 0.85f, 0.25f),   // amarillo sol
                new Color(0.55f, 0.38f, 0.85f),// morado
                new Color(0.45f, 0.80f, 0.30f),// lima
                new Color(1f, 0.50f, 0.65f),   // rosa
                new Color(0.35f, 0.62f, 1f),   // azul cielo
                new Color(1f, 0.60f, 0.20f),    // naranja
            };
            buildingMats = new Material[palette.Length];
            for (int i = 0; i < palette.Length; i++)
                buildingMats[i] = MakeMat(palette[i]);

            Color[] flowers = {
                new Color(1f, 0.25f, 0.30f),
                new Color(1f, 0.85f, 0.20f),
                new Color(1f, 0.55f, 0.80f),
                new Color(0.95f, 0.95f, 1f),
            };
            flowerMats = new Material[flowers.Length];
            for (int i = 0; i < flowers.Length; i++)
                flowerMats[i] = MakeMat(flowers[i], true, 0.4f);

            cubeMesh   = GrabMesh(PrimitiveType.Cube);
            cylMesh    = GrabMesh(PrimitiveType.Cylinder);
            sphereMesh = GrabMesh(PrimitiveType.Sphere);
        }

        private Mesh GrabMesh(PrimitiveType type)
        {
            GameObject tmp = GameObject.CreatePrimitive(type);
            Mesh m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            return m;
        }

        private GameObject Part(string name, Mesh mesh, Material mat,
                                Vector3 pos, Vector3 scale, Transform parent,
                                bool streamable)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (streamable)
                StreamingManager.Instance.RegisterObject(go, pos);
            return go;
        }

        private void BuildCity()
        {
            float cityW = blocksX * blockSize;
            float cityD = blocksZ * blockSize;

            GameObject ground = Part("Ground", cubeMesh, sidewalkMat,
                new Vector3(cityW / 2f, -0.55f, cityD / 2f),
                new Vector3(cityW + streetWidth, 1f, cityD + streetWidth),
                cityRoot, false);
            ground.AddComponent<BoxCollider>();

            for (int ix = 0; ix < blocksX; ix++)
                for (int iz = 0; iz < blocksZ; iz++)
                    BuildBlock(ix, iz, ix * blockSize, iz * blockSize);
        }

        // Variedad determinista por manzana (siempre igual en cada arranque)
        private int BlockSeed(int ix, int iz) { return ix * 73 + iz * 149 + 7; }

        private void BuildBlock(int ix, int iz, float bx, float bz)
        {
            Transform t = cityRoot;
            float cx = bx + blockSize / 2f;
            float cz = bz + blockSize / 2f;
            int seed = BlockSeed(ix, iz);

            // Pasto verde dentro de la manzana
            Part("Grass", cubeMesh, grassMat,
                new Vector3(cx, 0.0f, cz),
                new Vector3(blockSize, 0.1f, blockSize), t, false);

            // Calle horizontal (asfalto) al borde sur de la manzana
            GameObject streetH = Part("StreetH", cubeMesh, asphaltMat,
                new Vector3(cx, 0.02f, bz - streetWidth / 2f),
                new Vector3(blockSize + streetWidth, 0.12f, streetWidth), t, false);
            streetH.AddComponent<BoxCollider>();

            // Calle vertical al borde oeste
            GameObject streetV = Part("StreetV", cubeMesh, asphaltMat,
                new Vector3(bx - streetWidth / 2f, 0.02f, cz),
                new Vector3(streetWidth, 0.12f, blockSize + streetWidth), t, false);
            streetV.AddComponent<BoxCollider>();

            // Línea amarilla central (segmentos)
            for (float x = bx + 4f; x < bx + blockSize - 2f; x += 6f)
                Part("Lane", cubeMesh, laneMat,
                    new Vector3(x, 0.095f, bz - streetWidth / 2f),
                    new Vector3(2.6f, 0.02f, 0.28f), t, false);

            // Cruce peatonal en la esquina (franjas blancas)
            for (int s = 0; s < 5; s++)
                Part("Cross", cubeMesh, crosswalkMat,
                    new Vector3(bx - streetWidth / 2f, 0.095f, bz - streetWidth + 1.2f + s * 1.7f),
                    new Vector3(streetWidth - 1f, 0.02f, 0.8f), t, false);

            // Bordillos a los lados de la calle horizontal
            Part("CurbA", cubeMesh, curbMat,
                new Vector3(cx, 0.12f, bz - streetWidth - 0.15f),
                new Vector3(blockSize + streetWidth, 0.25f, 0.3f), t, false);
            Part("CurbB", cubeMesh, curbMat,
                new Vector3(cx, 0.12f, bz + 0.15f),
                new Vector3(blockSize + streetWidth, 0.25f, 0.3f), t, false);

            // Tienda de la manzana (rota entre 4 negocios de la familia)
            BuildStore(seed % 4, cx, bz + 7f, t);

            // Edificios de colores dentro de la manzana (2 por manzana)
            BuildBuilding(bx + 17f, bz + 24f, 10f, 9f + (seed % 3) * 2.5f, 10f,
                buildingMats[seed % buildingMats.Length], t);
            BuildBuilding(bx + 42f, bz + 42f, 12f, 11f + ((seed / 3) % 3) * 2.5f, 9f,
                buildingMats[(seed + 3) % buildingMats.Length], t);

            // Farolas a lo largo de la calle
            for (int i = 0; i < lampsPerStreet; i++)
            {
                float lz = bz - streetWidth + (i + 0.5f) * (blockSize / lampsPerStreet);
                BuildLamp(bx + streetWidth / 2f + 0.8f, lz, t);
            }

            // Árboles y palmeras dentro de la manzana (alternados; sin chocar edificios)
            for (int i = 0; i < treesPerBlock; i++)
            {
                float tx = bx + streetWidth + 8f + (i % 3) * 13f;
                float tz = bz + streetWidth + 8f + (i / 3) * 13f;
                if ((i + seed) % 2 == 0) BuildPalm(tx, tz, t);
                else BuildTree(tx, tz, t);
            }

            // Flores en macetas junto a la acera
            for (int i = 0; i < 4; i++)
            {
                float fx = bx + 10f + i * 14f;
                BuildFlowers(fx, bz + 1.2f, flowerMats[(seed + i) % flowerMats.Length], t);
            }

            // Monedas doradas sobre la calle (para comprar poderes)
            for (float x = bx + 8f; x < bx + blockSize - 4f; x += 13f)
                CoinPickup.Spawn(t, new Vector3(x, 0.6f, bz - streetWidth / 2f));

            // Señal de alto en la esquina
            BuildStopSign(bx + streetWidth / 2f + 0.5f, bz - streetWidth / 2f - 0.5f, t);
        }

        // Edificio de color: cuerpo + banda de ventanas + techo
        private void BuildBuilding(float x, float z, float w, float h, float d,
                                   Material bodyMat, Transform parent)
        {
            GameObject body = Part("Building", cubeMesh, bodyMat,
                new Vector3(x, h / 2f, z), new Vector3(w, h, d), parent, true);
            body.AddComponent<BoxCollider>();
            // Banda de ventanas al frente (cara sur)
            Part("Windows", cubeMesh, windowMat,
                new Vector3(x, h * 0.62f, z - d / 2f - 0.06f),
                new Vector3(w * 0.8f, h * 0.28f, 0.12f), parent, true);
            Part("Windows2", cubeMesh, windowMat,
                new Vector3(x, h * 0.30f, z - d / 2f - 0.06f),
                new Vector3(w * 0.8f, h * 0.18f, 0.12f), parent, true);
            // Techo (borde más oscuro)
            Part("Roof", cubeMesh, curbMat,
                new Vector3(x, h + 0.25f, z), new Vector3(w + 0.6f, 0.5f, d + 0.6f), parent, true);
        }

        // Tiendas: 0 = camión rojo LOS HERMANOS TAMPS, 1 = paletería
        // DELICIAS MICHOACANAS, 2 = HAMBURGUESAS, 3 = TAQUERÍA
        private void BuildStore(int kind, float x, float z, Transform parent)
        {
            if (kind == 0)
            {
                // Camión rojo de Los Hermanos Tamps
                GameObject body = Part("Truck", cubeMesh, truckRedMat,
                    new Vector3(x, 1.9f, z), new Vector3(7f, 3.2f, 3f), parent, true);
                body.AddComponent<BoxCollider>();
                Part("TruckRoof", cubeMesh, truckWhiteMat,
                    new Vector3(x, 3.65f, z), new Vector3(7.3f, 0.3f, 3.3f), parent, true);
                Part("TruckWindow", cubeMesh, windowMat,
                    new Vector3(x, 2.1f, z - 1.56f), new Vector3(5.5f, 1.1f, 0.12f), parent, true);
                Part("TruckSign", cubeMesh, signBoardMat,
                    new Vector3(x, 4.5f, z), new Vector3(5f, 1f, 0.4f), parent, true);
                // Ruedas
                for (int w = 0; w < 2; w++)
                    for (int s = 0; s < 2; s++)
                    {
                        GameObject wheel = Part("Wheel", cylMesh, wheelMat,
                            new Vector3(x - 2.2f + w * 4.4f, 0.55f, z - 1.4f + s * 2.8f),
                            new Vector3(1.1f, 0.5f, 1.1f), parent, true);
                        wheel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    }
            }
            else
            {
                // Local de colores según el negocio
                Material bodyM, trimM;
                if (kind == 1)      { bodyM = MakeMat(new Color(1f, 0.55f, 0.70f)); trimM = truckWhiteMat; } // paletería rosa
                else if (kind == 2) { bodyM = MakeMat(new Color(1f, 0.60f, 0.20f)); trimM = MakeMat(new Color(0.25f, 0.45f, 1f)); } // hamburguesas
                else                { bodyM = MakeMat(new Color(1f, 0.85f, 0.25f)); trimM = MakeMat(new Color(0.25f, 0.70f, 0.30f)); } // tacos
                GameObject body = Part("Store", cubeMesh, bodyM,
                    new Vector3(x, 1.75f, z), new Vector3(9f, 3.5f, 5f), parent, true);
                body.AddComponent<BoxCollider>();
                Part("StoreTrim", cubeMesh, trimM,
                    new Vector3(x, 3.7f, z), new Vector3(9.4f, 0.4f, 5.4f), parent, true);
                Part("StoreSign", cubeMesh, signBoardMat,
                    new Vector3(x, 4.5f, z - 1.8f), new Vector3(6.5f, 1.1f, 0.3f), parent, true);
                Part("StoreDoor", cubeMesh, windowMat,
                    new Vector3(x - 2.5f, 1.1f, z - 2.56f), new Vector3(1.6f, 2.2f, 0.12f), parent, true);
                Part("StoreWindow", cubeMesh, windowMat,
                    new Vector3(x + 1.5f, 1.9f, z - 2.56f), new Vector3(3.5f, 1.4f, 0.12f), parent, true);
                // Toldo a rayas (paletería) o simple
                if (kind == 1)
                {
                    for (int s = 0; s < 6; s++)
                        Part("Awning", cubeMesh, s % 2 == 0 ? truckWhiteMat : bodyM,
                            new Vector3(x - 3.1f + s * 1.25f, 3.1f, z - 3.1f),
                            new Vector3(1.2f, 0.12f, 1.6f), parent, true);
                }
            }
        }

        // Farola: poste + brazo hacia la calle + foco encendido
        private void BuildLamp(float x, float z, Transform parent)
        {
            Part("LampPole", cylMesh, poleMat,
                new Vector3(x, 2.8f, z), new Vector3(0.25f, 5.6f, 0.25f), parent, true);
            Part("LampArm", cubeMesh, poleMat,
                new Vector3(x - 0.8f, 5.5f, z), new Vector3(1.7f, 0.12f, 0.12f), parent, true);
            Part("LampLight", cubeMesh, lampLightMat,
                new Vector3(x - 1.6f, 5.38f, z), new Vector3(0.55f, 0.15f, 0.3f), parent, true);
        }

        // Árbol: tronco + copa verde viva
        private void BuildTree(float x, float z, Transform parent)
        {
            Part("Trunk", cylMesh, trunkMat,
                new Vector3(x, 1.1f, z), new Vector3(0.5f, 2.2f, 0.5f), parent, true);
            Part("Leaves", sphereMesh, leafMat,
                new Vector3(x, 3.4f, z), new Vector3(3.2f, 3.6f, 3.2f), parent, true);
        }

        // Palmera: tronco alto + copa achatada
        private void BuildPalm(float x, float z, Transform parent)
        {
            Part("PalmTrunk", cylMesh, trunkMat,
                new Vector3(x, 2f, z), new Vector3(0.4f, 4f, 0.4f), parent, true);
            Part("PalmTop", sphereMesh, palmLeafMat,
                new Vector3(x, 4.6f, z), new Vector3(4.2f, 1.8f, 4.2f), parent, true);
        }

        // Maceta con flores de colores
        private void BuildFlowers(float x, float z, Material flowerMat, Transform parent)
        {
            Part("Planter", cubeMesh, planterMat,
                new Vector3(x, 0.3f, z), new Vector3(1.6f, 0.6f, 0.8f), parent, true);
            Part("Flowers", sphereMesh, flowerMat,
                new Vector3(x, 0.85f, z), new Vector3(1.3f, 0.7f, 0.6f), parent, true);
        }

        // Señal de alto: poste + disco rojo
        private void BuildStopSign(float x, float z, Transform parent)
        {
            Part("SignPole", cylMesh, signPoleMat,
                new Vector3(x, 1.4f, z), new Vector3(0.14f, 2.8f, 0.14f), parent, true);
            GameObject sign = Part("StopSign", cylMesh, signMat,
                new Vector3(x, 3.1f, z), new Vector3(1.3f, 0.12f, 1.3f), parent, true);
            sign.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
