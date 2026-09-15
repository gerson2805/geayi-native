// MainMenu.cs — menú principal del juego (se construye solo por código)
// Botones: JUGAR, MODO CONSTRUIR, PERSONALIZAR, TIENDA, AJUSTES.
// Usa TextMeshPro si el proyecto lo tiene, si no usa Text normal.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Geayi.Core;
using Geayi.Characters;

namespace Geayi.UI
{
    // Ayudante: usa TextMeshPro si está disponible, si no Text normal.
    public static class UILabel
    {
        // Cambia el texto de un label ya creado (TMP o Text)
        public static void SetText(GameObject go, string text)
        {
            if (go == null) return;
            Component tmp = go.GetComponent("TMPro.TextMeshProUGUI");
            if (tmp != null)
            {
                var prop = tmp.GetType().GetProperty("text");
                if (prop != null) { prop.SetValue(tmp, text, null); return; }
            }
            Text t = go.GetComponent<Text>();
            if (t != null) t.text = text;
        }

        // Crea un label nuevo: TMP si existe el paquete, si no Text
        public static GameObject CreateLabel(Transform parent, string text, int size, Color color)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            // ¿Existe TextMeshPro en el proyecto?
            System.Type tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
            if (tmpType != null)
            {
                Component tmp = go.AddComponent(tmpType);
                tmp.GetType().GetProperty("text").SetValue(tmp, text, null);
                tmp.GetType().GetProperty("fontSize").SetValue(tmp, (float)size, null);
                tmp.GetType().GetProperty("color").SetValue(tmp, color, null);
                var alignProp = tmp.GetType().GetProperty("alignment");
                alignProp.SetValue(tmp,
                    System.Enum.Parse(alignProp.PropertyType, "Center"), null);
            }
            else
            {
                Text t = go.AddComponent<Text>();
                t.text = text;
                t.fontSize = size;
                t.color = color;
                t.alignment = TextAnchor.MiddleCenter;
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return go;
        }
    }

    // Ayudante: sprites procedurales para que los botones se vean redondos.
    // Se generan una vez con Texture2D (blancos) y se tiñen con Image.color.
    // (La fuente de Unity en Android no trae emoji de colores, por eso los
    // botones usan texto en vez de 🌀🐾🪙.)
    public static class UIShape
    {
        private static Sprite rounded;
        private static Sprite circle;

        // Rectángulo con esquinas redondeadas
        public static Sprite Rounded()
        {
            if (rounded != null) return rounded;
            int s = 128; float r = 30f;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Max(Mathf.Max(r - x, x - (s - 1 - r)), 0f);
                    float dy = Mathf.Max(Mathf.Max(r - y, y - (s - 1 - r)), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f;
                    if (d > r) a = 0f;
                    else if (d > r - 2f) a = (r - d) / 2f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            rounded = Sprite.Create(tex, new Rect(0, 0, s, s),
                new Vector2(0.5f, 0.5f), s, 0, SpriteMeshType.FullRect);
            rounded.name = "GeayiRounded";
            return rounded;
        }

        // Círculo (para el joystick)
        public static Sprite Circle()
        {
            if (circle != null) return circle;
            int s = 128; float r = 62f;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2((s - 1) / 2f, (s - 1) / 2f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    float a = 1f;
                    if (d > r) a = 0f;
                    else if (d > r - 2f) a = (r - d) / 2f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, s, s),
                new Vector2(0.5f, 0.5f), s, 0, SpriteMeshType.FullRect);
            circle.name = "GeayiCircle";
            return circle;
        }
    }

    public class MainMenu : MonoBehaviour
    {
        [Header("Título del juego")]
        public string gameTitle = "GEAYI: Obby Xtreme 3D";

        private GameObject toastObj;
        private float toastTimer = 0f;

        // Panel de familia (elegir personaje)
        private GameObject familyPanel;
        private GameObject familyGrid;
        private GameObject familyPageLabel;
        private int familyPage = 0;
        private const int FamilyPerPage = 8;

        // Previsualización 3D de los personajes (una foto por celda)
        private const int PREVIEW_LAYER = 30; // capa que la cámara principal no ve
        private Camera previewCam;
        private readonly List<RenderTexture> previewRTs = new List<RenderTexture>();

        void Start()
        {
            EnsureEventSystem();
            BuildMenu();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildMenu()
        {
            // Canvas principal
            GameObject canvasGo = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280); // diseño vertical (móvil)
            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo oscuro
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(canvasGo.transform, false);
            bg.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.20f);
            StretchFull(bg.GetComponent<RectTransform>());

            // Título
            GameObject title = UILabel.CreateLabel(canvasGo.transform, gameTitle, 54, Color.white);
            RectTransform trt = title.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.05f, 0.78f);
            trt.anchorMax = new Vector2(0.95f, 0.93f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            // Botones principales (texto simple: la fuente de Android no trae emoji)
            string[] names = { "JUGAR", "MODO CONSTRUIR", "FAMILIA", "TIENDA", "AJUSTES" };
            for (int i = 0; i < names.Length; i++)
            {
                int idx = i; // copia para el listener
                Button b = CreateButton(canvasGo.transform, names[i], 0.68f - i * 0.11f);
                b.onClick.AddListener(() => OnButton(idx));
            }

            // Mensaje corto ("toast")
            toastObj = UILabel.CreateLabel(canvasGo.transform, "", 30, Color.yellow);
            RectTransform tort = toastObj.GetComponent<RectTransform>();
            tort.anchorMin = new Vector2(0.1f, 0.05f);
            tort.anchorMax = new Vector2(0.9f, 0.12f);
            tort.offsetMin = Vector2.zero;
            tort.offsetMax = Vector2.zero;
            toastObj.SetActive(false);

            BuildFamilyPanel(canvasGo.transform);
        }

        private Button CreateButton(Transform parent, string text, float yCenter)
        {
            GameObject go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = UIShape.Rounded();
            img.color = new Color(0.15f, 0.45f, 0.95f);
            Button b = go.AddComponent<Button>();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, yCenter - 0.045f);
            rt.anchorMax = new Vector2(0.8f, yCenter + 0.045f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject label = UILabel.CreateLabel(go.transform, text, 34, Color.white);
            StretchFull(label.GetComponent<RectTransform>());
            return b;
        }

        private void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void OnButton(int idx)
        {
            if (GameManager.Instance == null) return;
            switch (idx)
            {
                case 0: // JUGAR
                    GameManager.Instance.LoadWorld(0);
                    break;
                case 1: // MODO CONSTRUIR
                    GameManager.Instance.LoadWorldBuild(0);
                    break;
                case 2: // FAMILIA (elegir personaje)
                    OpenFamilyPanel();
                    break;
                default: // TIENDA, AJUSTES
                    ShowToast("Disponible pronto");
                    break;
            }
        }

        // ---------------- Panel FAMILIA: elegir personaje ----------------
        private void BuildFamilyPanel(Transform parent)
        {
            familyPanel = new GameObject("FamilyPanel");
            familyPanel.transform.SetParent(parent, false);
            Image bg = familyPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.88f);
            StretchFull(familyPanel.GetComponent<RectTransform>());
            familyPanel.SetActive(false); // oculto desde el inicio

            try
            {
                GameObject title = UILabel.CreateLabel(familyPanel.transform, "ELIGE TU PERSONAJE", 40, Color.white);
                RectTransform trt = title.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(0.05f, 0.87f);
                trt.anchorMax = new Vector2(0.95f, 0.95f);
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;

                // Botones de navegación PRIMERO: CERRAR siempre existe aunque algo falle después
                MakePanelButton(familyPanel.transform, "<", 0.04f, 0.06f, 0.30f, 0.14f,
                    new Color(0.25f, 0.25f, 0.35f), 40, () => ShowFamilyPage(familyPage - 1));
                MakePanelButton(familyPanel.transform, "CERRAR", 0.35f, 0.06f, 0.65f, 0.14f,
                    new Color(0.70f, 0.25f, 0.25f), 32, () => CloseFamilyPanel());
                MakePanelButton(familyPanel.transform, ">", 0.70f, 0.06f, 0.96f, 0.14f,
                    new Color(0.25f, 0.25f, 0.35f), 40, () => ShowFamilyPage(familyPage + 1));

                familyGrid = new GameObject("FamilyGrid");
                familyGrid.transform.SetParent(familyPanel.transform, false);
                // OJO: un GameObject nuevo trae Transform normal; hay que convertirlo
                // a RectTransform o los botones hijos salen con tamaño cero (invisibles).
                RectTransform gridRt = familyGrid.AddComponent<RectTransform>();
                StretchFull(gridRt);

                familyPageLabel = UILabel.CreateLabel(familyPanel.transform, "", 28, Color.yellow);
                RectTransform prt = familyPageLabel.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.30f, 0.165f);
                prt.anchorMax = new Vector2(0.70f, 0.215f);
                prt.offsetMin = Vector2.zero;
                prt.offsetMax = Vector2.zero;
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FAMILIA] Error armando panel: " + e);
            }
            familyPanel.SetActive(false); // por si algo falló, nunca queda trabado abierto
        }

        private void OpenFamilyPanel()
        {
            if (familyPanel == null) return;
            familyPanel.SetActive(true);
            ShowFamilyPage(0);
        }

        private void CloseFamilyPanel()
        {
            familyPanel.SetActive(false);
        }

        private void ShowFamilyPage(int page)
        {
            try
            {
                var all = Geayi.Characters.FamilyData.All;
                if (all == null || all.Count == 0)
                    throw new System.Exception("lista de personajes vacía");
                int pages = (all.Count + FamilyPerPage - 1) / FamilyPerPage;
                if (pages < 1) pages = 1;
                if (page < 0) page = pages - 1;
                if (page >= pages) page = 0;
                familyPage = page;

                // Limpiar cuadrícula
                if (familyGrid != null)
                {
                    for (int i = familyGrid.transform.childCount - 1; i >= 0; i--)
                        Destroy(familyGrid.transform.GetChild(i).gameObject);
                }
                ReleasePreviews(); // liberar las fotos 3D de la página anterior

                string selectedId = "";
                if (SaveSystem.Instance != null) selectedId = SaveSystem.Instance.Data.characterId;

                // 8 por página: 4 columnas x 2 filas (celdas altas para la foto 3D)
                int start = page * FamilyPerPage;
                int end = Mathf.Min(start + FamilyPerPage, all.Count);
                for (int i = start; i < end; i++)
                {
                    var def = all[i];
                    int slot = i - start;
                    int col = slot % 4;
                    int row = slot / 4;
                    float x0 = 0.03f + col * 0.235f;
                    float x1 = x0 + 0.225f;
                    float y1 = 0.82f - row * 0.30f;
                    float y0 = y1 - 0.28f;
                    MakeFamilyCell(familyGrid.transform, def, x0, y0, x1, y1,
                        def.id == selectedId);
                }
                UILabel.SetText(familyPageLabel, (page + 1) + "/" + pages);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FAMILIA] Error mostrando página: " + e);
                // Mostrar el error EN PANTALLA para diagnosticar con una captura
                if (familyGrid != null)
                {
                    for (int i = familyGrid.transform.childCount - 1; i >= 0; i--)
                        Destroy(familyGrid.transform.GetChild(i).gameObject);
                }
                if (familyPageLabel != null)
                    UILabel.SetText(familyPageLabel, "Error: " + e.Message);
                else
                    ShowToast("Error FAMILIA: " + e.Message);
            }
        }

        // ---------------- Foto 3D de cada personaje ----------------
        // Cámara dedicada que dibuja los avatares en una capa que la cámara
        // principal no ve: la foto sale limpia, sin la ciudad detrás.
        private void EnsurePreviewRig()
        {
            if (previewCam != null) return;
            GameObject camGo = new GameObject("FamilyPreviewCam");
            previewCam = camGo.AddComponent<Camera>();
            previewCam.cullingMask = 1 << PREVIEW_LAYER;
            previewCam.clearFlags = CameraClearFlags.SolidColor;
            previewCam.backgroundColor = new Color(0.10f, 0.14f, 0.28f); // azul del menú
            previewCam.fieldOfView = 38f;
            previewCam.nearClipPlane = 0.1f;
            previewCam.farClipPlane = 50f;
            previewCam.enabled = false; // solo dibuja cuando se le pide
            camGo.transform.position = new Vector3(0f, 1.05f, -2.8f);
            camGo.transform.LookAt(new Vector3(0f, 0.95f, 0f));
            // La cámara del juego no debe ver esta capa
            if (Camera.main != null)
                Camera.main.cullingMask &= ~(1 << PREVIEW_LAYER);
        }

        private static void SetLayerRec(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++)
                SetLayerRec(go.transform.GetChild(i).gameObject, layer);
        }

        private void ReleasePreviews()
        {
            for (int i = 0; i < previewRTs.Count; i++)
            {
                if (previewRTs[i] != null)
                {
                    previewRTs[i].Release();
                    Destroy(previewRTs[i]);
                }
            }
            previewRTs.Clear();
        }

        // Dibuja el avatar del personaje en la textura de su celda
        private void RenderPreview(CharacterDef def, RenderTexture rt)
        {
            try
            {
                EnsurePreviewRig();
                GameObject avatar = CharacterBuilder.Build(def);
                SetLayerRec(avatar, PREVIEW_LAYER);
                avatar.transform.position = Vector3.zero;
                avatar.transform.rotation = Quaternion.identity;
                previewCam.targetTexture = rt;
                previewCam.Render();
                previewCam.targetTexture = null;
                Destroy(avatar);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[FAMILIA] Error en foto 3D: " + e);
            }
        }

        // Celda de personaje: foto 3D arriba + nombre abajo. Toda la celda se toca.
        private void MakeFamilyCell(Transform parent, CharacterDef def,
            float x0, float y0, float x1, float y1, bool selected)
        {
            GameObject go = new GameObject("Cell");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = UIShape.Rounded();
            img.color = selected
                ? new Color(0.15f, 0.65f, 0.30f)   // elegido: verde
                : new Color(0.15f, 0.45f, 0.95f);  // normal: azul
            Button b = go.AddComponent<Button>();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Foto 3D del personaje (arriba)
            RenderTexture rtex = new RenderTexture(160, 180, 16);
            rtex.Create();
            previewRTs.Add(rtex);
            RenderPreview(def, rtex);
            GameObject photoGo = new GameObject("Photo");
            photoGo.transform.SetParent(go.transform, false);
            RawImage photo = photoGo.AddComponent<RawImage>();
            photo.texture = rtex;
            RectTransform prt = photoGo.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.04f, 0.30f);
            prt.anchorMax = new Vector2(0.96f, 0.97f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            // Nombre (abajo)
            GameObject label = UILabel.CreateLabel(go.transform, def.name, 26, Color.white);
            RectTransform lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.02f, 0.02f);
            lrt.anchorMax = new Vector2(0.98f, 0.28f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            string id = def.id;   // copias para el listener
            string nm = def.name;
            b.onClick.AddListener(() => SelectCharacter(id, nm));
        }

        private void SelectCharacter(string id, string name)
        {
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.Data.characterId = id;
                SaveSystem.Instance.Save();
            }
            CloseFamilyPanel();
            ShowToast("Juegas como " + name);
        }

        private void MakePanelButton(Transform parent, string text,
            float x0, float y0, float x1, float y1, Color color, int fontSize, UnityAction onClick)
        {
            GameObject go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = UIShape.Rounded();
            img.color = color;
            Button b = go.AddComponent<Button>();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            GameObject label = UILabel.CreateLabel(go.transform, text, fontSize, Color.white);
            StretchFull(label.GetComponent<RectTransform>());
            if (onClick != null) b.onClick.AddListener(onClick);
        }

        private void ShowToast(string msg)
        {
            UILabel.SetText(toastObj, msg);
            toastObj.SetActive(true);
            toastTimer = 2f;
        }

        void Update()
        {
            if (toastObj != null && toastObj.activeSelf)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f) toastObj.SetActive(false);
            }
        }
    }
}
