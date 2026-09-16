// CoinPickup.cs — monedas doradas flotando en las calles
// Al acercarse el jugador las recoge (+monedas). Con el poder LUZ activo,
// las monedas cercanas vuelan hacia el jugador (imán).
// Se crean con CoinPickup.Spawn(padre, posición); giran y flotan solas.
using UnityEngine;
using Geayi.Core;
using Geayi.Player;

namespace Geayi.World
{
    public class CoinPickup : MonoBehaviour
    {
        public int value = 5;

        private Transform playerTr;
        private PlayerPowers powers;
        private float bobPhase;
        private Vector3 basePos;

        private static Material coinMat;
        private static Mesh coinMesh;

        public static void Spawn(Transform parent, Vector3 pos)
        {
            if (coinMesh == null)
            {
                GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coinMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(tmp);
            }
            if (coinMat == null)
            {
                Shader s = Shader.Find("Standard");
                if (s == null) s = Shader.Find("UI/Default");
                coinMat = new Material(s);
                coinMat.color = new Color(1f, 0.78f, 0.15f);
                coinMat.EnableKeyword("_EMISSION");
                coinMat.SetColor("_EmissionColor", new Color(1f, 0.70f, 0.10f) * 0.9f);
            }
            GameObject go = new GameObject("Coin");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = coinMesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = coinMat;
            var cp = go.AddComponent<CoinPickup>();
            cp.basePos = pos;
        }

        void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTr = p.transform;
                powers = p.GetComponent<PlayerPowers>();
            }
            bobPhase = Random.Range(0f, Mathf.PI * 2f);
            // Moneda acostada como disco, tamaño visible
            transform.localScale = new Vector3(0.9f, 0.16f, 0.9f);
        }

        void Update()
        {
            // Girar como las monedas de los videojuegos + flotar
            transform.Rotate(0f, 180f * Time.deltaTime, 0f);
            transform.position = basePos + new Vector3(0f, 0.25f + Mathf.Sin(Time.time * 2.5f + bobPhase) * 0.15f, 0f);

            if (playerTr == null || GameManager.Instance == null) return;
            Vector3 pp = playerTr.position + new Vector3(0f, 1f, 0f);
            float d = Vector3.Distance(transform.position, pp);

            // Imán del poder LUZ: las cercanas vuelan hacia el jugador
            float magnetR = (powers != null) ? powers.MagnetRadius : 0f;
            if (magnetR > 0f && d < magnetR && d > 0.01f)
            {
                basePos = Vector3.MoveTowards(basePos, playerTr.position, 14f * Time.deltaTime);
                d = Vector3.Distance(transform.position, pp);
            }

            if (d < 1.8f)
            {
                GameManager.Instance.AddCoins(value);
                gameObject.SetActive(false); // recogida
            }
        }
    }
}
