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

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            gameObject.tag = "Player";
            EnsureBody();               // crea el capsule si no existe
            ApplyBodyColor(bodyColorHex);
        }

        void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            if (joystick == null && HUD.Instance != null)
                joystick = HUD.Instance.Joystick;
        }

        // Crea el cuerpo (capsule) si el prefab viene vacío
        private void EnsureBody()
        {
            Transform body = transform.Find("Body");
            if (body == null)
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.name = "Body";
                capsule.transform.SetParent(transform);
                capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
                // El CharacterController ya maneja las colisiones: quitamos la del capsule
                Destroy(capsule.GetComponent<Collider>());
                body = capsule.transform;
            }
            bodyRenderer = body.GetComponent<Renderer>();
        }

        void Update()
        {
            // --- Entrada: teclado (PC/editor) o joystick táctil (móvil) ---
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (joystick != null && joystick.IsActive)
            {
                h = joystick.Horizontal;
                v = joystick.Vertical;
            }

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

        // Cambia el color del cuerpo (personalización del personaje)
        public void ApplyBodyColor(string hex)
        {
            bodyColorHex = hex;
            if (bodyRenderer == null) return;
            Color c;
            if (ColorUtility.TryParseHtmlString(hex, out c))
            {
                // Material propio para no pintar a otros personajes
                Material m = new Material(Shader.Find("Standard"));
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
