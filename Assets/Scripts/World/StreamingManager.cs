// StreamingManager.cs — streaming estilo Roblox
// Los objetos decorativos se registran con su posición. Cada 0.5 segundos
// se ocultan los que están a más de 90 m del jugador y se muestran los cercanos.
// Así el juego corre fluido aunque la ciudad sea gigante.
using System.Collections.Generic;
using UnityEngine;

namespace Geayi.World
{
    public class StreamingManager : MonoBehaviour
    {
        public static StreamingManager Instance { get; private set; }

        [Header("Radio de streaming en metros")]
        [Tooltip("Los objetos decorativos más lejos que esto se ocultan")]
        public float radius = 90f;

        [Header("Cada cuántos segundos revisa")]
        public float checkInterval = 0.5f;

        private struct StreamItem
        {
            public GameObject go;
            public Vector3 pos;
        }

        private readonly List<StreamItem> items = new List<StreamItem>();
        private Transform player;
        private float timer = 0f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Registra un objeto decorativo con su posición en el mundo
        public void RegisterObject(GameObject go, Vector3 worldPos)
        {
            if (go == null) return;
            items.Add(new StreamItem { go = go, pos = worldPos });
        }

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }

        public int RegisteredCount { get { return items.Count; } }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < checkInterval) return;
            timer = 0f;

            if (player == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                else return;
            }

            float r2 = radius * radius;
            Vector3 pp = player.position;

            // Al revés para poder quitar de la lista los objetos destruidos
            for (int i = items.Count - 1; i >= 0; i--)
            {
                StreamItem it = items[i];
                if (it.go == null) { items.RemoveAt(i); continue; }

                // Solo distancia horizontal (los edificios altos no cuentan)
                float dx = it.pos.x - pp.x;
                float dz = it.pos.z - pp.z;
                bool visible = (dx * dx + dz * dz) <= r2;
                if (it.go.activeSelf != visible)
                    it.go.SetActive(visible);
            }
        }

        // Limpia el registro (al cambiar de mundo)
        public void Clear()
        {
            items.Clear();
        }
    }
}
