// CameraFollow.cs — cámara suave en tercera persona
// Sigue al jugador con un offset, lo mira siempre y no atraviesa paredes
// (usa un raycast simple para acercarse si hay algo en medio).
using UnityEngine;

namespace Geayi.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Objetivo")]
        [Tooltip("Si se deja vacío, busca al objeto con tag 'Player'")]
        public Transform target;

        [Header("Posición")]
        [Tooltip("Offset detrás/arriba del jugador (en su espacio local)")]
        public Vector3 offset = new Vector3(0f, 3.5f, -6.5f);
        public float smoothSpeed = 8f;
        public float lookHeight = 1.5f;

        [Header("Colisiones")]
        [Tooltip("Capas que la cámara no debe atravesar (paredes, edificios)")]
        public LayerMask collisionMask = -1; // todo por defecto
        public float minDistance = 1f;

        void Start()
        {
            if (target == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;

            // Punto al que mira la cámara (altura de la cabeza)
            Vector3 lookAt = target.position + Vector3.up * lookHeight;

            // Posición deseada: detrás del jugador según hacia dónde mira
            Vector3 desired = lookAt + target.rotation * offset;

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
            transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
            transform.LookAt(lookAt);
        }
    }
}
