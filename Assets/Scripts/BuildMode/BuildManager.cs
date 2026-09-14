// BuildManager.cs — modo construir estilo Roblox
// Tocar la pantalla coloca bloques alineados a una rejilla.
// SIN LÍMITE de bloques: los bloques lejanos se ocultan solos
// con el StreamingManager (estilo Roblox) para no ponerse lento.
// La construcción se guarda/carga en JSON vía SaveSystem.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Geayi.Core;
using Geayi.World;

namespace Geayi.Build
{
    public class BuildManager : MonoBehaviour
    {
        [Header("Bloques")]
        [Tooltip("Prefab opcional. Si está vacío se usa un cubo.")]
        public GameObject blockPrefab;
        public float gridSize = 1f;
        public float blockSize = 1f;

        [Header("Paleta de colores")]
        public string[] palette =
        {
            "#ff5e8a", "#ff9d00", "#ffe95e", "#59d867",
            "#35b6ff", "#9d6bff", "#ffffff", "#222831"
        };
        private int colorIndex = 0;
        private Material[] paletteMats;

        [Header("Autoguardado")]
        public float autoSaveSeconds = 30f;
        private float saveTimer = 0f;

        private Camera cam;
        private readonly List<GameObject> blocks = new List<GameObject>();

        // Guarda el color de cada bloque colocado
        private class BlockInfo : MonoBehaviour
        {
            public string colorHex = "#ff5e8a";
        }

        void Start()
        {
            cam = Camera.main;
            BuildPalette();
            // Al entrar al modo construir, cargar lo que había guardado
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.BuildMode)
                LoadBuild();
        }

        private void BuildPalette()
        {
            paletteMats = new Material[palette.Length];
            for (int i = 0; i < palette.Length; i++)
                paletteMats[i] = MakeColorMat(palette[i]);
        }

        private Material MakeColorMat(string hex)
        {
            Color c = Color.white;
            ColorUtility.TryParseHtmlString(hex, out c);
            Material m = new Material(Shader.Find("Standard"));
            m.color = c;
            return m;
        }

        void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.BuildMode)
                return;

            // Colocar con toque (móvil) o clic (PC/editor)
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began && !IsOverUI(t.fingerId))
                    TryPlace(t.position);
            }
            else if (Input.GetMouseButtonDown(0) && !IsOverUI(-1))
            {
                TryPlace(Input.mousePosition);
            }

            if (Input.GetKeyDown(KeyCode.C)) NextColor(); // cambiar color (pruebas en PC)
            if (Input.GetKeyDown(KeyCode.Z)) Undo();      // deshacer (pruebas en PC)

            // Autoguardado cada X segundos
            saveTimer += Time.deltaTime;
            if (saveTimer >= autoSaveSeconds)
            {
                saveTimer = 0f;
                SaveBuild();
            }
        }

        // Ignora toques sobre botones de la UI
        private bool IsOverUI(int fingerId)
        {
            if (EventSystem.current == null) return false;
            return fingerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

        private void TryPlace(Vector2 screenPos)
        {
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(screenPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 300f))
            {
                // Encima de lo tocado, alineado a la rejilla
                Vector3 p = hit.point + hit.normal * (blockSize * 0.5f);
                p.x = Mathf.Round(p.x / gridSize) * gridSize;
                p.y = Mathf.Round(p.y / gridSize) * gridSize;
                p.z = Mathf.Round(p.z / gridSize) * gridSize;
                SpawnBlock(p, palette[colorIndex], blockSize);
            }
        }

        // SIN LÍMITE: siempre deja colocar otro bloque.
        public GameObject SpawnBlock(Vector3 pos, string colorHex, float size = 0f)
        {
            if (size <= 0f) size = blockSize;

            GameObject go;
            if (blockPrefab != null)
            {
                go = Instantiate(blockPrefab, pos, Quaternion.identity);
                go.transform.localScale = Vector3.one * size;
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = pos;
                go.transform.localScale = Vector3.one * size;
            }
            go.name = "Block";

            var info = go.AddComponent<BlockInfo>();
            info.colorHex = colorHex;

            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                int idx = System.Array.IndexOf(palette, colorHex);
                r.material = (idx >= 0 && idx < paletteMats.Length)
                    ? paletteMats[idx]
                    : MakeColorMat(colorHex);
            }

            blocks.Add(go);

            // Streaming estilo Roblox: los bloques lejanos se ocultan solos
            if (StreamingManager.Instance != null)
                StreamingManager.Instance.RegisterObject(go, pos);

            return go;
        }

        public void NextColor() { colorIndex = (colorIndex + 1) % palette.Length; }
        public string CurrentColor { get { return palette[colorIndex]; } }
        public int BlockCount { get { return blocks.Count; } }

        // Quita el último bloque (botón "deshacer")
        public void Undo()
        {
            if (blocks.Count == 0) return;
            GameObject last = blocks[blocks.Count - 1];
            blocks.RemoveAt(blocks.Count - 1);
            if (last != null) Destroy(last);
        }

        // Borra todo (y el guardado)
        public void ClearAll()
        {
            foreach (GameObject b in blocks)
                if (b != null) Destroy(b);
            blocks.Clear();
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Data.blocks.Clear();
                SaveSystem.Instance.Save();
            }
        }

        // ---------------- Guardar / cargar (JSON vía SaveSystem) ----------------
        public void SaveBuild()
        {
            if (SaveSystem.Instance == null) return;
            var list = new List<BlockData>();
            foreach (GameObject b in blocks)
            {
                if (b == null) continue;
                BlockInfo info = b.GetComponent<BlockInfo>();
                list.Add(new BlockData
                {
                    x = b.transform.position.x,
                    y = b.transform.position.y,
                    z = b.transform.position.z,
                    size = b.transform.localScale.x,
                    color = (info != null) ? info.colorHex : palette[0]
                });
            }
            SaveSystem.Instance.Data.blocks = list;
            SaveSystem.Instance.Save();
            Debug.Log("[BuildManager] Construcción guardada: " + list.Count + " bloques.");
        }

        public void LoadBuild()
        {
            if (SaveSystem.Instance == null) return;
            foreach (GameObject b in blocks)
                if (b != null) Destroy(b);
            blocks.Clear();
            foreach (BlockData d in SaveSystem.Instance.Data.blocks)
                SpawnBlock(new Vector3(d.x, d.y, d.z), d.color, d.size);
            Debug.Log("[BuildManager] Construcción cargada: " + blocks.Count + " bloques.");
        }

        void OnDisable()
        {
            SaveBuild();
        }
    }
}
