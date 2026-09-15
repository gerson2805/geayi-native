// CameraFollow.cs — cámara suave en tercera persona
// Sigue al jugador con un offset, lo mira siempre y no atraviesa paredes
// (usa un raycast simple para acercarse si hay algo en medio).
// El ángulo de la cámara (yaw) es INDEPENDIENTE del giro del jugador,
// como en la web: si la cámara girara con el jugador, los controles
// relativos a la cámara se realimentan y el muñeco solo da vueltas.
using UnityEngine;
using Geayi.UI;

namespace Geayi.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("Si se deja vacío, busca al objeto con tag 'Player'")]
        public Transform target;

        [Header("Posición")]
        [Tooltip("Offset detrás/arriba del jugador (en su espacio local)")]
        public Vector3 offset = new Vector3(0f, 4.5f, -9.0f);
        public float smoothSpeed = 8f;
        public float lookHeight = 1.5f;

        [Header("Colisiones")]
        [Tooltip("Capas que la cámara no debe atravesar (paredes, edificios)")]
        public LayerMask collisionMask = -1; // todo por defecto
        [Tooltip("La cámara nunca se pega más que esto a la cabeza del jugador")]
        public float minDistance = 2.5f;

        private bool snapped = false; // primer frame: colocarse directo, sin deslizar
        private float camYaw; // ángulo propio de la cámara (no sigue el giro del jugador)

        void Start()
        {
            if (target == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
            if (target != null)
                camYaw = target.rotation.eulerAngles.y; // empieza detrás del jugador
        }

        void Update()
        {
            // Arrastrar con un dedo (que NO sea el del joystick) gira la cámara, como en la web.
            var joy = (HUD.Instance != null) ? HUD.Instance.Joystick : null;
            bool joyActive = joy != null && joy.IsActive;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase != TouchPhase.Moved) continue;
                // REGLA PRINCIPAL: mientras el joystick está activo, ningún dedo
                // en la mitad izquierda de la pantalla gira la cámara. Al hacer
                // el 360° el dedo viaja MUCHO más lejos del centro que la palanca
                // (la palanca se limita a 130px, el dedo no), y antes eso se salía
                // del radio de 320px y hacía girar la cámara sin control: el
                // "arriba" del joystick dejaba de ser arriba en la pantalla.
                // La cámara solo gira con dedos en la mitad derecha (como en la web,
                // el segundo dedo gira la cámara mientras se camina).
                if (joyActive && t.position.x < Screen.width * 0.5f) continue;
                if (joyActive &&
                    Vector2.Distance(t.position, joy.CurrentBaseCenterScreenPos()) < 320f) continue;
                if (joy != null && joy.IsJoystickPointer(t.fingerId)) continue;
                if (HUD.Instance != null && HUD.Instance.IsTouchOnJoystick(t.position)) continue;
                camYaw -= t.deltaPosition.x * 0.25f;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Punto al que mira la cámara (altura de la cabeza)
            Vector3 lookAt = target.position + Vector3.up * lookHeight;

            // Posición deseada: detrás del jugador según el ángulo PROPIO de la cámara
            // (no el giro del jugador: eso causaba que el muñeco diera vueltas sin control)
            Vector3 desired = lookAt + Quaternion.Euler(0f, camYaw, 0f) * offset;

            // Raycast: si hay una pared entre el jugador y la cámara, acercarla
            Vector3 dir = desired - lookAt;
            float dist = dir.magnitude;
            if (dist > 0.001f)
            {
                dir /= dist;
                RaycastHit hit;
                if (Physics.Raycast(lookAt, dir, out hit, dist, collisionMask))
                {
                    float safe = Mathf.Max(hit.distance - 0.3f, minDistance);
                    desired = lookAt + dir * safe;
                }
            }

            // Movimiento suave + mirar al jugador
            // (el primer frame se coloca directo para no arrastrar desde el menú)
            if (!snapped)
            {
                transform.position = desired;
                snapped = true;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            }
            transform.LookAt(lookAt);
        }
    }
}
