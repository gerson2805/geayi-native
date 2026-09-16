// PlayerPowers.cs — poderes del jugador (se compran en TIENDA con monedas)
// GIRO: turbo de velocidad x1.6 por 12 s
// BOLA: super salto x1.7 por 15 s
// LUZ (estrella): imán de monedas (8 m) por 20 s
// El poder equipado se guarda en SaveSystem (equippedPower) y el botón
// PODER del HUD lo activa.
using UnityEngine;
using Geayi.Core;

namespace Geayi.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerPowers : MonoBehaviour
    {
        public float SpeedMultiplier { get; private set; } = 1f;
        public float JumpMultiplier { get; private set; } = 1f;
        public float MagnetRadius { get; private set; } = 0f;
        public string ActivePowerId { get; private set; } = "";
        public float ActiveTimeLeft { get; private set; } = 0f;

        public bool IsActive => ActiveTimeLeft > 0f && !string.IsNullOrEmpty(ActivePowerId);

        // Catálogo: id, nombre corto, descripción, precio en monedas, duración (s)
        public struct PowerDef
        {
            public string id, shortName, description;
            public int price;
            public float duration;
        }

        public static readonly PowerDef[] Catalog = new PowerDef[]
        {
            new PowerDef { id = "giro", shortName = "GIRO",
                description = "Turbo de velocidad x1.6", price = 30, duration = 12f },
            new PowerDef { id = "bola", shortName = "BOLA",
                description = "Super salto x1.7", price = 60, duration = 15f },
            new PowerDef { id = "estrella", shortName = "LUZ",
                description = "Imán de monedas por 20 s", price = 90, duration = 20f },
        };

        public static PowerDef? DefOf(string id)
        {
            foreach (var d in Catalog)
                if (d.id == id) return d;
            return null;
        }

        void Update()
        {
            if (!IsActive) return;
            ActiveTimeLeft -= Time.deltaTime;
            if (ActiveTimeLeft <= 0f)
            {
                ActivePowerId = "";
                ActiveTimeLeft = 0f;
                SpeedMultiplier = 1f;
                JumpMultiplier = 1f;
                MagnetRadius = 0f;
            }
        }

        // Activa el poder equipado. Devuelve mensaje para mostrar en pantalla.
        public string Activate()
        {
            if (IsActive)
                return ActivePowerId.ToUpper() + ": " + Mathf.CeilToInt(ActiveTimeLeft) + " s";
            string id = (SaveSystem.Instance != null) ? SaveSystem.Instance.Data.equippedPower : "";
            if (string.IsNullOrEmpty(id))
                return "Elige un poder en TIENDA";
            var def = DefOf(id);
            if (def == null)
                return "Elige un poder en TIENDA";
            ActivePowerId = id;
            ActiveTimeLeft = def.Value.duration;
            SpeedMultiplier = 1f;
            JumpMultiplier = 1f;
            MagnetRadius = 0f;
            switch (id)
            {
                case "giro": SpeedMultiplier = 1.6f; break;
                case "bola": JumpMultiplier = 1.7f; break;
                case "estrella": MagnetRadius = 8f; break;
            }
            return def.Value.shortName + " activado!";
        }

        // ¿El jugador ya compró este poder?
        public static bool IsOwned(string id)
        {
            return SaveSystem.Instance != null
                && SaveSystem.Instance.Data.ownedPowers != null
                && SaveSystem.Instance.Data.ownedPowers.Contains(id);
        }

        // Comprar con monedas. Devuelve mensaje para mostrar.
        public static string Buy(string id)
        {
            var def = DefOf(id);
            if (def == null) return "Poder no válido";
            if (IsOwned(id)) return "Ya lo tienes";
            if (GameManager.Instance == null || SaveSystem.Instance == null) return "Error";
            if (!GameManager.Instance.SpendCoins(def.Value.price))
                return "Te faltan monedas";
            SaveSystem.Instance.Data.ownedPowers.Add(id);
            SaveSystem.Instance.Data.equippedPower = id; // se equipa al comprar
            SaveSystem.Instance.Save();
            return def.Value.shortName + " comprado!";
        }

        public static void Equip(string id)
        {
            if (SaveSystem.Instance == null || !IsOwned(id)) return;
            SaveSystem.Instance.Data.equippedPower = id;
            SaveSystem.Instance.Save();
        }
    }
}
