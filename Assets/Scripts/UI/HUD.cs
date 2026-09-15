// HUD.cs — interfaz durante el juego
// Muestra: monedas, botón de poder equipado (tocable), joystick virtual,
// botón de salto y botón de mascotas. Todo se construye por código.
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Geayi.Core;
using Geayi.Player;

namespace Geayi.UI
{
    // ---------------------------------------------------------------
    // Joystick virtual: se arrastra con el dedo, devuelve valores -1..1
    // ---------------------------------------------------------------
    public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public bool IsActive { get; private set; }

        [Tooltip("Distancia máxima de la palanca en píxeles")]
        public float radius = 110f;

        private RectTransform baseRt;
        private RectTransform knobRt;

        public void Setup(RectTransform baseRect, RectTransform knobRect)
        {
            baseRt = baseRect;
            knobRt = knobRect;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsActive = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (baseRt == null) return;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    baseRt, eventData.position, eventData.pressEventCamera, out local))
                return;
            // El centro del joystick es (0,0) en coordenadas locales
            Vector2 delta = local;
            if (delta.magnitude > radius)
                delta = delta.normalized * radius;
            if (knobRt != null)
                knobRt.anchoredPosition = delta;
            Horizontal = delta.x / radius;
            Vertical = delta.y / radius;
            IsActive = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Horizontal = 0f;
            Vertical = 0f;
            IsActive = false;
            if (knobRt != null)
                knobRt.anchoredPosition = Vector2.zero;
        }
    }

    // ---------------------------------------------------------------
    // HUD principal del juego
    // ---------------------------------------------------------------
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("Eventos (se conectan en el Inspector o por código)")]
        public UnityEvent onPowerPressed;
        public UnityEvent onPetsPressed;
        public UnityEvent onJumpPressed;

        public VirtualJoystick Joystick { get; private set; }

        private GameObject coinsLabel;
        private GameObject powerLabel;
        private PlayerController player;
        private GameObject hudCanvasGo; // referencia directa (Find no ve objetos inactivos)

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            EnsureEventSystem();
            BuildHUD();
            player = FindAnyObjectByType<PlayerController>();
            if (player != null && Joystick != null)
                player.joystick = Joystick;
            RefreshCoins();
            RefreshPower();
            // En el menú principal el HUD arranca oculto
            if (GameManager.Instance != null && GameManager.Instance.State == GameState.Menu)
                SetVisible(false);
        }

        // Muestra u oculta todo el HUD
        public void SetVisible(bool visible)
        {
            if (hudCanvasGo != null) hudCanvasGo.SetActive(visible);
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

        // ---------------- construcción de la interfaz ----------------
        private void BuildHUD()
        {
            GameObject canvasGo = new GameObject("HUDCanvas");
            hudCanvasGo = canvasGo; // guardar referencia directa
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Monedas (arriba a la izquierda) — texto simple, sin emoji
            coinsLabel = UILabel.CreateLabel(canvasGo.transform, "MONEDAS: 0", 32, Color.white);
            Anchor(coinsLabel.GetComponent<RectTransform>(),
                new Vector2(0f, 0.92f), new Vector2(0.55f, 1f));

            // Botón de poder equipado (lado derecho, tocable)
            GameObject powerBtn = MakeButton(canvasGo.transform, new Color(0.10f, 0.45f, 0.90f),
                new Vector2(0.80f, 0.52f), new Vector2(0.98f, 0.70f));
            powerLabel = UILabel.CreateLabel(powerBtn.transform, "PODER", 30, Color.white);
            StretchFull(powerLabel.GetComponent<RectTransform>());
            powerBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                onPowerPressed.Invoke();
                Debug.Log("[HUD] Poder activado: " + CurrentPowerName());
            });

            // Botón de mascotas (encima del salto)
            GameObject petBtn = MakeButton(canvasGo.transform, new Color(0.35f, 0.20f, 0.60f),
                new Vector2(0.80f, 0.30f), new Vector2(0.98f, 0.46f));
            GameObject petLabel = UILabel.CreateLabel(petBtn.transform, "MASCOTA", 22, Color.white);
            StretchFull(petLabel.GetComponent<RectTransform>());
            petBtn.GetComponent<Button>().onClick.AddListener(() => onPetsPressed.Invoke());

            // Botón de salto (abajo a la derecha)
            GameObject jumpBtn = MakeButton(canvasGo.transform, new Color(0.15f, 0.70f, 0.30f),
                new Vector2(0.80f, 0.03f), new Vector2(0.98f, 0.24f));
            GameObject jumpLabel = UILabel.CreateLabel(jumpBtn.transform, "SALTAR", 30, Color.white);
            StretchFull(jumpLabel.GetComponent<RectTransform>());
            jumpBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                onJumpPressed.Invoke();
                if (player != null) player.TryJump();
            });

            // Joystick virtual (abajo a la izquierda)
            // Cuadrado fijo (320x320): con anclas proporcionales se deformaba a óvalo
            GameObject joyBase = new GameObject("Joystick");
            joyBase.transform.SetParent(canvasGo.transform, false);
            Image joyImg = joyBase.AddComponent<Image>();
            joyImg.sprite = UIShape.Circle();
            joyImg.color = new Color(1f, 1f, 1f, 0.25f);
            RectTransform joyRt = joyBase.GetComponent<RectTransform>();
            joyRt.anchorMin = new Vector2(0f, 0f);
            joyRt.anchorMax = new Vector2(0f, 0f);
            joyRt.pivot = new Vector2(0f, 0f);
            joyRt.sizeDelta = new Vector2(320f, 320f);
            joyRt.anchoredPosition = new Vector2(40f, 40f);

            GameObject knob = new GameObject("Knob");
            knob.transform.SetParent(joyBase.transform, false);
            Image knobImg = knob.AddComponent<Image>();
            knobImg.sprite = UIShape.Circle();
            knobImg.color = new Color(1f, 1f, 1f, 0.60f);
            RectTransform knobRt = knob.GetComponent<RectTransform>();
            knobRt.anchorMin = new Vector2(0.5f, 0.5f);
            knobRt.anchorMax = new Vector2(0.5f, 0.5f);
            knobRt.sizeDelta = new Vector2(90f, 90f);
            knobRt.anchoredPosition = Vector2.zero;

            VirtualJoystick joy = joyBase.AddComponent<VirtualJoystick>();
            joy.radius = 110f;
            joy.Setup(joyBase.GetComponent<RectTransform>(), knobRt);
            Joystick = joy;
        }

        private GameObject MakeButton(Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject("UIButton");
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.sprite = UIShape.Rounded();
            img.color = color;
            go.AddComponent<Button>();
            Anchor(go.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return go;
        }

        private void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ---------------- datos ----------------
        public void RefreshCoins()
        {
            int coins = (GameManager.Instance != null) ? GameManager.Instance.Coins : 0;
            UILabel.SetText(coinsLabel, "MONEDAS: " + coins);
        }

        public void RefreshPower()
        {
            UILabel.SetText(powerLabel, PowerShortName(CurrentPowerName()));
        }

        private string CurrentPowerName()
        {
            if (SaveSystem.Instance != null && !string.IsNullOrEmpty(SaveSystem.Instance.Data.equippedPower))
                return SaveSystem.Instance.Data.equippedPower;
            return "";
        }

        // Nombre corto del poder para el botón (texto: los emoji no se ven en Android)
        private string PowerShortName(string powerId)
        {
            switch (powerId)
            {
                case "giro": return "GIRO";
                case "bola": return "BOLA";
                case "telarana": return "RED";
                case "estrella": return "LUZ";
                case "onda": return "ONDA";
                case "vuelo": return "VUELO";
                default: return "PODER";
            }
        }
    }
}
