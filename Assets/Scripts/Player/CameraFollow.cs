// CameraFollow.cs — cámara suave en tercera persona
// Sigue al jugador con un offset, lo mira siempre y no atraviesa paredes
// (usa un raycast simple para acercarse si hay algo en medio).
// El ángulo de la cámara (yaw) es INDEPENDIENTE del giro del jugador,
// como en la web: si la cámara girara con el jugador, los controles
// relativos a la cámara se realimentan y el muñeco solo da vueltas.
using UnityEngine;
using System.Collections.Generic;
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
        [Header("Vista 360")]
        [Tooltip("Sensibilidad horizontal (grados por píxel)")]
        public float yawSpeed = 0.25f;
        [Tooltip("Sensibilidad vertical (grados por píxel)")]
        public float pitchSpeed = 0.22f;
        [Tooltip("Límite mirando hacia arriba (grados, negativo = cámara baja)")]
        public float minPitch = -25f;
        [Tooltip("Límite mirando hacia abajo (grados, cámara alta)")]
        public float maxPitch = 80f;
        private float camPitch = 0f; // 0 = vista actual; + = cámara alta (mira abajo)
        // Dedos que están girando la cámara (se decide cuando el dedo EMPIEZA):
        // el dedo del joystick y los que empiezan en su zona nunca son de cámara.
        private Dictionary<int, bool> camFingers = new Dictionary<int, bool>();

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
            // Vista 360 como en la web: cualquier dedo que NO sea el del joystick
            // gira la cámara (lados + arriba/abajo), INCLUSO mientras el joystick
            // está activo. Así se puede caminar con la izquierda y mirar con la
            // derecha al mismo tiempo.
            var joy = (HUD.Instance != null) ? HUD.Instance.Joystick : null;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began)
                {
                    // Se decide al empezar el toque: el dedo del joystick y los
                    // que caen en su zona (abajo-izquierda) o sobre su base
                    // son para moverse, nunca para girar la cámara.
                    bool esDedoJoystick = joy != null && joy.IsJoystickPointer(t.fingerId);
                    bool sobreJoystick = HUD.Instance != null && HUD.Instance.IsTouchOnJoystick(t.position);
                    bool enZonaJoystick = t.position.x < Screen.width * 0.45f
                                       && t.position.y < Screen.height * 0.60f;
                    camFingers[t.fingerId] = !esDedoJoystick && !sobreJoystick && !enZonaJoystick;
                }
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    camFingers.Remove(t.fingerId);
                    continue;
                }
                if (t.phase != TouchPhase.Moved) continue;
                bool esCamara;
                if (!camFingers.TryGetValue(t.fingerId, out esCamara) || !esCamara) continue;
                camYaw -= t.deltaPosition.x * yawSpeed;
                // Arrastrar arriba = mirar arriba (ver el cielo);
                // arrastrar abajo = mirar abajo.
                camPitch = Mathf.Clamp(camPitch - t.deltaPosition.y * pitchSpeed,
                                       minPitch, maxPitch);
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Punto al que mira la cámara (altura de la cabeza)
            Vector3 lookAt = target.position + Vector3.up * lookHeight;

            // Posición deseada: detrás del jugador según el ángulo PROPIO de la cámara
            // (no el giro del jugador: eso causaba que el muñeco diera vueltas sin control).
            // El pitch (arriba/abajo) se aplica sobre el yaw: vista 360 completa.
            Quaternion rot = Quaternion.Euler(0f, camYaw, 0f) * Quaternion.Euler(camPitch, 0f, 0f);
            Vector3 desired = lookAt + rot * offset;

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
