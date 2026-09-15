// PlayerController.cs — personaje 3D para móvil (estilo Roblox)
// Joystick virtual izquierdo para moverse, botón para saltar,
// cámara en tercera persona, caminar/correr según qué tan a fondo va el joystick.
using UnityEngine;
using Geayi.UI;

namespace Geayi.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Velocidades")]
        public float walkSpeed = 5f;
        public float runSpeed = 9f;
        [Tooltip("Si el joystick pasa este valor (0-1), el personaje corre")]
        [Range(0.5f, 1f)]
        public float runThreshold = 0.85f;

        [Header("Salto y gravedad")]
        public float jumpHeight = 2.2f;
        public float gravity = -22f;

        [Header("Referencias")]
        public Transform cameraTransform;
        [Tooltip("Se asigna solo desde el HUD si se deja vacío")]
        public VirtualJoystick joystick;
        public string bodyColorHex = "#ff5533";

        private CharacterController cc;
        private Vector3 verticalVel;
        private bool jumpQueued = false;
        private Renderer bodyRenderer;
        private Vector3 spawnPos;
        private Quaternion spawnRot;
        // Joystick suavizado estilo la web (evita que el personaje dé vueltas al caminar)
        private Vector2 joySmooth = Vector2.zero;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (cc == null) cc = gameObject.AddComponent<CharacterController>();
            gameObject.tag = "Player";
            spawnPos = transform.position;   // punto de inicio (para volver al entrar)
            spawnRot = transform.rotation;
            EnsureBody();               // crea el avatar del personaje elegido
            ApplyBodyColor(bodyColorHex);
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (joystick == null && HUD.Instance != null)
                joystick = HUD.Instance.Joystick;
        }

        // Crea el avatar 3D del personaje de la familia elegido en el menú
        // (se guarda en SaveSystem.Data.characterId; por defecto: gerson).
        private void EnsureBody()
        {
            if (transform.Find("Body") != null) return; // ya existe
            string charId = "gerson";
            if (Geayi.Core.SaveSystem.Instance != null &&
                !string.IsNullOrEmpty(Geayi.Core.SaveSystem.Instance.Data.characterId))
                charId = Geayi.Core.SaveSystem.Instance.Data.characterId;
            var def = Geayi.Characters.FamilyData.Get(charId);
            GameObject avatar = Geayi.Characters.CharacterBuilder.Build(def);
            avatar.transform.SetParent(transform, false);
            avatar.transform.localPosition = Vector3.zero;
            avatar.transform.localRotation = Quaternion.identity;
            // bodyRenderer apunta al torso para que ApplyBodyColor repinte la camisa
            Transform torso = avatar.transform.Find("Torso");
            if (torso == null && avatar.transform.childCount > 0)
                torso = avatar.transform.GetChild(0);
            if (torso != null)
                bodyRenderer = torso.GetComponent<Renderer>();
            bodyColorHex = def.shirt;
        }

        void Update()
        {
            // Por si el HUD se creó después que el jugador: tomar el joystick tarde
            if (joystick == null && Geayi.UI.HUD.Instance != null)
                joystick = Geayi.UI.HUD.Instance.Joystick;

            // --- Entrada: teclado (PC/editor) o joystick táctil (móvil) ---
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (joystick != null && joystick.IsActive)
            {
                // Igual que la web: zona muerta (ignora temblores) + reescala 0..1
                // + suavizado pasa-bajos (el personaje camina estable en vez de girar brusco)
                Vector2 raw = new Vector2(joystick.Horizontal, joystick.Vertical);
                float m = raw.magnitude;
                const float DEAD = 0.22f;
                if (m < DEAD) raw = Vector2.zero;
                else raw = raw.normalized * Mathf.Min(1f, (m - DEAD) / (1f - DEAD));
                float k = 1f - Mathf.Pow(1f - 0.35f, Time.deltaTime * 60f); // 0.35 como la web a 60fps
                joySmooth += (raw - joySmooth) * k;
                if (joySmooth.magnitude < 0.04f) joySmooth = Vector2.zero;
                h = joySmooth.x;
                v = joySmooth.y;
            }
            else { joySmooth = Vector2.zero; }

            Vector2 input = new Vector2(h, v);
            if (input.sqrMagnitude > 1f) input.Normalize();

            bool running = input.magnitude > runThreshold;
            float speed = running ? runSpeed : walkSpeed;

            // --- Dirección relativa a la cámara ---
            Vector3 dir = new Vector3(input.x, 0f, input.y);
            if (cameraTransform != null && dir.sqrMagnitude > 0.001f)
            {
                Vector3 camFwd = cameraTransform.forward; camFwd.y = 0f; camFwd.Normalize();
                Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
                dir = camFwd * input.y + camRight * input.x;
                if (dir.sqrMagnitude > 1f) dir.Normalize();
            }

            // --- Mover ---
            cc.Move(dir * speed * Time.deltaTime);

            // --- Girar el personaje hacia donde camina ---
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, 12f * Time.deltaTime);
            }

            // --- Gravedad y salto ---
            if (cc.isGrounded)
            {
                if (verticalVel.y < 0f) verticalVel.y = -2f; // pegado al suelo
                if (jumpQueued || Input.GetButtonDown("Jump"))
                {
                    verticalVel.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            jumpQueued = false;
            verticalVel.y += gravity * Time.deltaTime;
            cc.Move(verticalVel * Time.deltaTime);
        }

        // Lo llama el botón de salto del HUD
        public void TryJump()
        {
            if (cc != null && cc.isGrounded)
                jumpQueued = true;
        }

        // Vuelve a armar el avatar con el personaje elegido en FAMILIA
        // (se llama al entrar a jugar, por si cambió la elección).
        public void RebuildBody()
        {
            Transform old = transform.Find("Body");
            if (old != null) Destroy(old.gameObject);
            bodyRenderer = null;
            EnsureBody();
        }

        // Regresa al punto de inicio del mundo
        public void GoHome()
        {
            transform.rotation = spawnRot;
            Teleport(spawnPos);
        }

        // Cambia el color del cuerpo (personalización del personaje)
        public void ApplyBodyColor(string hex)
        {
            bodyColorHex = hex;
            if (bodyRenderer == null) return;
            Color c;
            if (ColorUtility.TryParseHtmlString(hex, out c))
            {
                // Material propio para no pintar a otros personajes.
                // "UI/Default" siempre está en el build (los shaders 3D fueron optimizados fuera).
                Shader s = Shader.Find("UI/Default");
                if (s == null)
                {
                    GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    s = tmp.GetComponent<Renderer>().sharedMaterial.shader;
                    Destroy(tmp);
                }
                Material m = new Material(s);
                m.color = c;
                bodyRenderer.material = m;
            }
        }

        // Teletransportar (cambiar de mundo, reaparecer, etc.)
        public void Teleport(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            verticalVel = Vector3.zero;
            cc.enabled = true;
        }
    }
}
