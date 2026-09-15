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
        // ID del dedo que maneja el joystick (-1 = ninguno). En Unity el
        // pointerId táctil es igual al fingerId del Touch.
        public int ActivePointerId { get; private set; } = -1;

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
            ActivePointerId = eventData.pointerId;
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
            ActivePointerId = -1;
            ClearInput();
        }

        // ¿Este dedo es el que maneja el joystick? La cámara lo usa para
        // ignorarlo: al girar el dedo en 360° sale del cuadrado del joystick
        // y antes la cámara creía que era un dedo de giro y se iba a otro lado.
        public bool IsJoystickPointer(int pointerId)
        {
            return IsActive && ActivePointerId >= 0 && pointerId == ActivePointerId;
        }

        // Limpia el estado (al volver al menú a mitad de un arrastre)
        public void ClearInput()
        {
            Horizontal = 0f;
            Vertical = 0f;
            IsActive = false;
            ActivePointerId = -1;
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
        // Inicializados explícitos: si quedan nulos, los botones mueren en silencio.
        public UnityEvent onPowerPressed = new UnityEvent();
        public UnityEvent onPetsPressed = new UnityEvent();
        public UnityEvent onJumpPressed = new UnityEvent();

        public VirtualJoystick Joystick { get; private set; }

        private GameObject coinsLabel;
        private GameObject powerLabel;
        private PlayerController player;
        // El jugador puede activarse DESPUÉS que el HUD (al pulsar JUGAR):
        // resolverlo tarde, no solo en Start, o SALTAR nunca lo encuentra.
        private PlayerController Player
        {
            get
            {
                if (player == null) player = FindAnyObjectByType<PlayerController>();
                if (player != null && Joystick != null && player.joystick == null)
                    player.joystick = Joystick;
                return player;
            }
        }
        private GameObject hudCanvasGo; // referencia directa (Find no ve objetos inactivos)
        private RectTransform hudCanvasRt; // rect del canvas (para el joystick flotante)
        private RectTransform joystickBaseRt; // para saber si un dedo está sobre el joystick

        // ¿Este toque de pantalla cae sobre el joystick? (la cámara lo ignora, como en la web)
        public bool IsTouchOnJoystick(Vector2 screenPos)
        {
            if (joystickBaseRt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(joystickBaseRt, screenPos, null);
        }

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
            var p = Player; // resolución tardía por si el jugador aún no está activo
            if (p != null && Joystick != null)
                p.joystick = Joystick;
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

        private GameObject hudToastObj;
        private GameObject hudToastText;
        private float hudToastTimer;
        // Crea el aviso una sola vez al armar el HUD (no en el primer toque)
        private void EnsureHudToast(string msg)
        {
            if (hudCanvasGo == null || hudToastObj != null) return;
            hudToastObj = new GameObject("HudToast");
            hudToastObj.transform.SetParent(hudCanvasGo.transform, false);
            Image bgi = hudToastObj.AddComponent<Image>(); // convierte a RectTransform
            bgi.color = new Color(0f, 0f, 0f, 0.75f);
            bgi.raycastTarget = false; // el aviso no debe bloquear los toques
            RectTransform rt = hudToastObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.10f, 0.80f);
            rt.anchorMax = new Vector2(0.72f, 0.90f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            hudToastText = UILabel.CreateLabel(hudToastObj.transform, msg, 30, Color.white);
            var toastGraphic = hudToastText.GetComponent<MaskableGraphic>();
            if (toastGraphic != null) toastGraphic.raycastTarget = false;
            StretchFull(hudToastText.GetComponent<RectTransform>());
            hudToastObj.SetActive(false);
        }
        // Aviso breve en pantalla (para que cada toque dé respuesta visible)
        private void ShowHudToast(string msg)
        {
            if (hudCanvasGo == null) return;
            EnsureHudToast(msg);
            if (hudToastObj == null) return;
            UILabel.SetText(hudToastText, msg);
            hudToastObj.SetActive(true);
            hudToastTimer = 1.6f;
        }

        void Update()
        {
            if (hudToastObj != null && hudToastObj.activeSelf)
            {
                hudToastTimer -= Time.deltaTime;
                if (hudToastTimer <= 0f) hudToastObj.SetActive(false);
            }
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
            hudCanvasRt = canvasGo.GetComponent<RectTransform>();
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Monedas (arriba a la izquierda) — texto simple, sin emoji
            coinsLabel = UILabel.CreateLabel(canvasGo.transform, "MONEDAS: 0", 32, Color.white);
            var coinsGraphic = coinsLabel.GetComponent<MaskableGraphic>();
            if (coinsGraphic != null) coinsGraphic.raycastTarget = false; // no bloquea toques
            Anchor(coinsLabel.GetComponent<RectTransform>(),
                new Vector2(0f, 0.92f), new Vector2(0.55f, 1f));

            // Botón de poder equipado (lado derecho, tocable)
            GameObject powerBtn = MakeButton(canvasGo.transform, new Color(0.10f, 0.45f, 0.90f),
                new Vector2(0.80f, 0.52f), new Vector2(0.98f, 0.70f));
            powerLabel = UILabel.CreateLabel(powerBtn.transform, "PODER", 30, Color.white);
            StretchFull(powerLabel.GetComponent<RectTransform>());
            powerBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    onPowerPressed.Invoke();
                    ShowHudToast(string.IsNullOrEmpty(CurrentPowerName())
                        ? "Sin poder equipado"
                        : "Poder: " + CurrentPowerName());
                }
                catch (System.Exception e) { ShowHudToast("Error: " + e.Message); }
            });

            // Botón de mascotas (encima del salto)
            GameObject petBtn = MakeButton(canvasGo.transform, new Color(0.35f, 0.20f, 0.60f),
                new Vector2(0.80f, 0.30f), new Vector2(0.98f, 0.46f));
            GameObject petLabel = UILabel.CreateLabel(petBtn.transform, "MASCOTA", 22, Color.white);
            StretchFull(petLabel.GetComponent<RectTransform>());
            petBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    onPetsPressed.Invoke();
                    ShowHudToast("Mascotas: disponible pronto");
                }
                catch (System.Exception e) { ShowHudToast("Error: " + e.Message); }
            });

            // Botón MENÚ (arriba a la derecha): volver para elegir otro personaje
            GameObject menuBtn = MakeButton(canvasGo.transform, new Color(0.45f, 0.45f, 0.50f),
                new Vector2(0.80f, 0.78f), new Vector2(0.98f, 0.92f));
            GameObject menuLabel = UILabel.CreateLabel(menuBtn.transform, "MENÚ", 30, Color.white);
            StretchFull(menuLabel.GetComponent<RectTransform>());
            menuBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    if (GameManager.Instance != null) GameManager.Instance.ReturnToMenu();
                    else ShowHudToast("Error: sin GameManager");
                }
                catch (System.Exception e) { ShowHudToast("Error: " + e.Message); }
            });

            // Botón de salto (abajo a la derecha)
            GameObject jumpBtn = MakeButton(canvasGo.transform, new Color(0.15f, 0.70f, 0.30f),
                new Vector2(0.80f, 0.03f), new Vector2(0.98f, 0.24f));
            GameObject jumpLabel = UILabel.CreateLabel(jumpBtn.transform, "SALTAR", 30, Color.white);
            StretchFull(jumpLabel.GetComponent<RectTransform>());
            jumpBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    onJumpPressed.Invoke();
                    var p = Player; // tardío: el jugador puede haberse activado después del HUD
                    if (p != null) p.TryJump();
                    else ShowHudToast("Error: sin jugador");
                }
                catch (System.Exception e) { ShowHudToast("Error: " + e.Message); }
            });

            // Joystick virtual (abajo a la izquierda)
            // Cuadrado fijo (320x320): con anclas proporcionales se deformaba a óvalo
            GameObject joyBase = new GameObject("Joystick");
            joyBase.transform.SetParent(canvasGo.transform, false);
            Image joyImg = joyBase.AddComponent<Image>();
            joyImg.sprite = UIShape.Circle();
            joyImg.color = new Color(1f, 1f, 1f, 0.25f);
            RectTransform joyRt = joyBase.GetComponent<RectTransform>();
            joystickBaseRt = joyRt;
            joyRt.anchorMin = new Vector2(0f, 0f);
            joyRt.anchorMax = new Vector2(0f, 0f);
            // Pivote al CENTRO: las coordenadas locales (0,0) son el centro del
            // joystick. Con pivote en la esquina, todo delta salía positivo y el
            // muñeco solo caminaba en una dirección diagonal.
            joyRt.pivot = new Vector2(0.5f, 0.5f);
            joyRt.sizeDelta = new Vector2(320f, 320f);
            joyRt.anchoredPosition = new Vector2(200f, 200f); // 40 + 160

            GameObject knob = new GameObject("Knob");
            knob.transform.SetParent(joyBase.transform, false);
            Image knobImg = knob.AddComponent<Image>();
            knobImg.sprite = UIShape.Circle();
            knobImg.color = new Color(1f, 1f, 1f, 0.60f);
            // La palanca no intercepta el toque: el dedo siempre cae en la base
            // (si no, tocar justo el centro no movía el joystick).
            knobImg.raycastTarget = false;
            RectTransform knobRt = knob.GetComponent<RectTransform>();
            knobRt.anchorMin = new Vector2(0.5f, 0.5f);
            knobRt.anchorMax = new Vector2(0.5f, 0.5f);
            knobRt.sizeDelta = new Vector2(90f, 90f);
            knobRt.anchoredPosition = Vector2.zero;

            VirtualJoystick joy = joyBase.AddComponent<VirtualJoystick>();
            joy.radius = 110f;
            joy.Setup(joyBase.GetComponent<RectTransform>(), knobRt);
            Joystick = joy;

            // Zona táctil flotante: el joystick aparece donde cae el dedo
            BuildTouchZone();

            // El aviso se crea desde el inicio (no en el primer toque)
            EnsureHudToast("");
        }

        // Limpia el joystick (al volver al menú a mitad de un arrastre)
        public void ResetInput()
        {
            if (Joystick != null) Joystick.ClearInput();
        }

        // -----------------------------------------------------------
        // Joystick FLOTANTE: una zona invisible en la mitad izquierda hace
        // que el joystick aparezca justo donde cae el dedo. Así los giros
        // de 360° siempre quedan sobre el joystick y la cámara ya no los
        // confunde con gestos de giro (aunque el dedo salga del cuadrito).
        // -----------------------------------------------------------
        private void BuildTouchZone()
        {
            GameObject zone = new GameObject("TouchZone");
            zone.transform.SetParent(hudCanvasGo.transform, false);
            RectTransform rt = zone.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0.55f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = zone.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f); // invisible pero tocable
            img.raycastTarget = true;
            zone.AddComponent<TouchZoneForwarder>().Setup(this);
            // Detrás de todo (joystick y botones tienen prioridad)
            zone.transform.SetAsFirstSibling();
        }

        // Mueve la base del joystick al punto del toque y lo activa
        public void JoystickDownAt(PointerEventData eventData)
        {
            if (joystickBaseRt == null || Joystick == null || hudCanvasRt == null) return;
            Vector2 local;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    hudCanvasRt, eventData.position, eventData.pressEventCamera, out local))
            {
                // 'local' se mide desde el centro del canvas (pivote 0.5,0.5);
                // la base usa anclas (0,0) en la esquina inferior izquierda.
                Vector2 size = hudCanvasRt.rect.size;
                Vector2 pos = local + size * 0.5f;
                float r = 160f; // mitad de la base (320x320): no salir de la pantalla
                pos.x = Mathf.Clamp(pos.x, r, size.x - r);
                pos.y = Mathf.Clamp(pos.y, r, size.y - r);
                joystickBaseRt.anchoredPosition = pos;
            }
            Joystick.OnPointerDown(eventData);
        }

        // Reenvía los toques de la zona invisible al joystick
        private class TouchZoneForwarder : MonoBehaviour,
            IPointerDownHandler, IDragHandler, IPointerUpHandler
        {
            private HUD hud;
            public void Setup(HUD h) { hud = h; }
            public void OnPointerDown(PointerEventData eventData)
            {
                if (hud != null) hud.JoystickDownAt(eventData);
            }
            public void OnDrag(PointerEventData eventData)
            {
                if (hud != null && hud.Joystick != null) hud.Joystick.OnDrag(eventData);
            }
            public void OnPointerUp(PointerEventData eventData)
            {
                if (hud != null && hud.Joystick != null) hud.Joystick.OnPointerUp(eventData);
            }
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
