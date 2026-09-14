// CityBuilder.cs — construye la ciudad procedural al arrancar la escena
// Calles con aceras, farolas (poste + brazo + luz), árboles simples
// (cilindro + esfera) y señales de alto.
// Eficiente: los materiales y las mallas se crean UNA vez y se reutilizan.
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
        private Material poleMat;
        private Material lampLightMat;
        private Material trunkMat;
        private Material leafMat;
        private Material signMat;
        private Material signPoleMat;

        // Mallas reutilizadas (una sola de cada tipo)
        private Mesh cubeMesh;
        private Mesh cylMesh;
        private Mesh sphereMesh;

        private Transform cityRoot;

        void Start()
        {
            // Asegurar que existe el StreamingManager
            if (StreamingManager.Instance == null)
                new GameObject("StreamingManager").AddComponent<StreamingManager>();

            CreateSharedAssets();
            cityRoot = new GameObject("City").transform;
            BuildCity();

            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                StreamingManager.Instance.SetPlayer(p.transform);

            Debug.Log("[CityBuilder] Ciudad lista. Objetos en streaming: " +
                      StreamingManager.Instance.RegisteredCount);
        }

        private Material MakeMat(Color c, bool emissive = false)
        {
            Material m = new Material(Shader.Find("Standard"));
            m.color = c;
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 1.5f);
            }
            return m;
        }

        private void CreateSharedAssets()
        {
            asphaltMat   = MakeMat(new Color(0.23f, 0.24f, 0.27f));
            sidewalkMat  = MakeMat(new Color(0.78f, 0.80f, 0.83f));
            curbMat      = MakeMat(new Color(0.62f, 0.64f, 0.67f));
            poleMat      = MakeMat(new Color(0.16f, 0.18f, 0.24f));
            lampLightMat = MakeMat(new Color(1f, 0.90f, 0.45f), true);
            trunkMat     = MakeMat(new Color(0.42f, 0.29f, 0.18f));
            leafMat      = MakeMat(new Color(0.18f, 0.55f, 0.34f));
            signMat      = MakeMat(new Color(0.85f, 0.12f, 0.12f));
            signPoleMat  = MakeMat(new Color(0.55f, 0.56f, 0.60f));

            cubeMesh   = GrabMesh(PrimitiveType.Cube);
            cylMesh    = GrabMesh(PrimitiveType.Cylinder);
            sphereMesh = GrabMesh(PrimitiveType.Sphere);
        }

        // Toma la malla de una primitiva temporal (se comparte entre todos)
        private Mesh GrabMesh(PrimitiveType type)
        {
            GameObject tmp = GameObject.CreatePrimitive(type);
            Mesh m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            return m;
        }

        // Crea una pieza visual reutilizando malla y material.
        // streamable=true -> se oculta cuando está lejos (streaming).
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

            // Suelo base (color acera) con collider para caminar
            GameObject ground = Part("Ground", cubeMesh, sidewalkMat,
                new Vector3(cityW / 2f, -0.55f, cityD / 2f),
                new Vector3(cityW + streetWidth, 1f, cityD + streetWidth),
                cityRoot, false);
            ground.AddComponent<BoxCollider>();

            for (int ix = 0; ix < blocksX; ix++)
                for (int iz = 0; iz < blocksZ; iz++)
                    BuildBlock(ix * blockSize, iz * blockSize);
        }

        private void BuildBlock(float bx, float bz)
        {
            Transform t = cityRoot;
            float cx = bx + blockSize / 2f;
            float cz = bz + blockSize / 2f;

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

            // Bordillos (aceras) a los lados de la calle horizontal
            Part("CurbA", cubeMesh, curbMat,
                new Vector3(cx, 0.12f, bz - streetWidth - 0.15f),
                new Vector3(blockSize + streetWidth, 0.25f, 0.3f), t, false);
            Part("CurbB", cubeMesh, curbMat,
                new Vector3(cx, 0.12f, bz + 0.15f),
                new Vector3(blockSize + streetWidth, 0.25f, 0.3f), t, false);

            // Farolas a lo largo de la calle
            for (int i = 0; i < lampsPerStreet; i++)
            {
                float lz = bz - streetWidth + (i + 0.5f) * (blockSize / lampsPerStreet);
                BuildLamp(bx + streetWidth / 2f + 0.8f, lz, t);
            }

            // Árboles dentro de la manzana
            for (int i = 0; i < treesPerBlock; i++)
            {
                float tx = bx + streetWidth + 5f + (i % 3) * 13f;
                float tz = bz + streetWidth + 5f + (i / 3) * 13f;
                BuildTree(tx, tz, t);
            }

            // Señal de alto en la esquina
            BuildStopSign(bx + streetWidth / 2f + 0.5f, bz - streetWidth / 2f - 0.5f, t);
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

        // Árbol simple: tronco (cilindro) + copa (una sola esfera)
        private void BuildTree(float x, float z, Transform parent)
        {
            Part("Trunk", cylMesh, trunkMat,
                new Vector3(x, 1.1f, z), new Vector3(0.5f, 2.2f, 0.5f), parent, true);
            Part("Leaves", sphereMesh, leafMat,
                new Vector3(x, 3.2f, z), new Vector3(3f, 3.4f, 3f), parent, true);
        }

        // Señal de alto: poste + disco rojo
        private void BuildStopSign(float x, float z, Transform parent)
        {
            Part("SignPole", cylMesh, signPoleMat,
                new Vector3(x, 1.4f, z), new Vector3(0.14f, 2.8f, 0.14f), parent, true);
            GameObject sign = Part("StopSign", cylMesh, signMat,
                new Vector3(x, 3.1f, z), new Vector3(1.3f, 0.12f, 1.3f), parent, true);
            // Acostar el cilindro para que el disco mire a la calle
            sign.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
