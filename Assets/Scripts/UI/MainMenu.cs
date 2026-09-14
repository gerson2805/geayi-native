// MainMenu.cs — menú principal del juego (se construye solo por código)
// Botones: JUGAR, MODO CONSTRUIR, PERSONALIZAR, TIENDA, AJUSTES.
// Usa TextMeshPro si el proyecto lo tiene, si no usa Text normal.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Geayi.Core;

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

    public class MainMenu : MonoBehaviour
    {
        [Header("Título del juego")]
        public string gameTitle = "GEAYI: Obby Xtreme 3D";

        private GameObject toastObj;
        private float toastTimer = 0f;

        void Start()
        {
            EnsureEventSystem();
            BuildMenu();
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
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

            // Botones principales
            string[] names = { "▶ JUGAR", "🧱 MODO CONSTRUIR", "🎨 PERSONALIZAR", "🛒 TIENDA", "⚙️ AJUSTES" };
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
        }

        private Button CreateButton(Transform parent, string text, float yCenter)
        {
            GameObject go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.15f, 0.45f, 0.95f);
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
                default: // PERSONALIZAR, TIENDA, AJUSTES
                    ShowToast("Disponible pronto 🔧");
                    break;
            }
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
