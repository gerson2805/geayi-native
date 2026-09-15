// FamilyData.cs — los 22 personajes de la familia GEAYI (diseños originales)
// Cada personaje se describe con colores y rasgos; CharacterBuilder los arma en 3D.
using System.Collections.Generic;

namespace Geayi.Characters
{
    public class CharacterDef
    {
        public string id;
        public string name;
        public string species = "human"; // "human", "dog", "cavy"
        public float scale = 1f;
        // Humanos
        public string skin = "#c68e5e";
        public string hairStyle = "short"; // "short", "long", "bun", "none"
        public string hair = "#1a1a1a";
        public string shirt = "#888888";
        public string pants = "#2e4a7a";
        public bool dress = false;
        public string dressColor = "#f2a0c8";
        public string cap = "";    // "" = sin gorra
        public bool beard = false;
        // Perros y cuyos
        public string body = "#9aa0a8";
        public string spots = "";  // manchas
        public string belly = "";  // panza / patas claras
        public string face = "";   // "mask" (máscara oscura), "schnauzer" (barba blanca)
        public string collar = ""; // color del collar
    }

    public static class FamilyData
    {
        public static readonly List<CharacterDef> All = new List<CharacterDef>
        {
            // ---- Núcleo: Gerson, Esmeralda y los 3 niños ----
            new CharacterDef { id="gerson", name="GERSON", scale=1f,
                skin="#b97a4e", hairStyle="short", hair="#141414",
                shirt="#cfd2d6", pants="#2e4a7a", cap="#f2f2f2", beard=true },
            new CharacterDef { id="esmeralda", name="ESMERALDA", scale=0.97f,
                skin="#c68e5e", hairStyle="bun", hair="#5a3a22",
                shirt="#ffffff", pants="#2e4a7a" },
            new CharacterDef { id="ian", name="IAN", scale=0.70f,
                skin="#c68e5e", hairStyle="short", hair="#141414",
                shirt="#d42a2a", pants="#7a9cc4" },
            new CharacterDef { id="yael", name="YAEL", scale=0.70f,
                skin="#c68e5e", hairStyle="short", hair="#141414",
                shirt="#d42a2a", pants="#2e5aa8" },
            new CharacterDef { id="audrey", name="AUDREY", scale=0.65f,
                skin="#d0a075", hairStyle="long", hair="#141414",
                shirt="#f5f5f5", pants="#7a9cc4" },
            // ---- Honduras ----
            new CharacterDef { id="adelina", name="ADELINA", scale=0.95f,
                skin="#b97a4e", hairStyle="bun", hair="#4a2e18",
                shirt="#d42a2a", pants="#333333" },
            new CharacterDef { id="damaris", name="DAMARIS", scale=0.97f,
                skin="#d0a075", hairStyle="long", hair="#d8b25a",
                shirt="#2a2a3a", pants="#8aa8cc" },
            new CharacterDef { id="ander", name="ANDER", scale=0.72f,
                skin="#b97a4e", hairStyle="short", hair="#141414",
                shirt="#1a1a1a", pants="#4a6a9a" },
            new CharacterDef { id="aitana", name="AITANA", scale=0.60f,
                skin="#b97a4e", hairStyle="bun", hair="#141414",
                dress=true, dressColor="#f2a0c8" },
            // ---- España ----
            new CharacterDef { id="fani", name="FANI", scale=0.97f,
                skin="#d0a075", hairStyle="long", hair="#3a2415",
                shirt="#c22a2a", pants="#1a1a1a" },
            new CharacterDef { id="yurem", name="YUREM", scale=0.90f,
                skin="#d0a075", hairStyle="short", hair="#141414",
                shirt="#b9bec6", pants="#2e4a7a" },
            new CharacterDef { id="dylan", name="DYLAN", scale=0.70f,
                skin="#d0a075", hairStyle="short", hair="#141414",
                shirt="#ffffff", pants="#1a1a1a" },
            // ---- México ----
            new CharacterDef { id="robert", name="ROBERT", scale=0.90f,
                skin="#c68e5e", hairStyle="short", hair="#141414",
                shirt="#4a7ac2", pants="#2a2a2a" },
            new CharacterDef { id="suegra", name="SUEGRA", scale=0.93f,
                skin="#d0a075", hairStyle="bun", hair="#b8b8b8",
                shirt="#7ac2e8", pants="#4a4a4a" },
            new CharacterDef { id="roberto", name="ROBERTO", scale=1f,
                skin="#b97a4e", hairStyle="short", hair="#8a8a8a",
                shirt="#1a1a1a", pants="#3a3a3a", cap="#5a6b3a", beard=true },
            // ---- Immokalee ----
            new CharacterDef { id="juan", name="JUAN", scale=1f,
                skin="#a86a3e", hairStyle="short", hair="#141414",
                shirt="#5a6b3a", pants="#2a2a2a", beard=true },
            new CharacterDef { id="abi", name="ABI", scale=0.97f,
                skin="#c68e5e", hairStyle="long", hair="#0f0f0f",
                dress=true, dressColor="#7a5230" },
            // ---- Perros ----
            new CharacterDef { id="mily", name="MILY", species="dog", scale=0.45f,
                body="#9aa0a8", belly="#f5f5f5", face="schnauzer" },
            new CharacterDef { id="kiara", name="KIARA", species="dog", scale=0.40f,
                body="#f5f5f5", spots="#8a5a2e", collar="#f27ab0" },
            new CharacterDef { id="whini", name="WHINI", species="dog", scale=0.55f,
                body="#c89858", face="mask" },
            // ---- Cuyos ----
            new CharacterDef { id="gorda", name="GORDA", species="cavy", scale=0.30f,
                body="#f5f5f5", spots="#8a5a2e" },
            new CharacterDef { id="nina", name="NIÑA", species="cavy", scale=0.28f,
                body="#f5f5f5", spots="#8a5a2e" },
        };

        private static Dictionary<string, CharacterDef> byId;

        public static CharacterDef Get(string id)
        {
            if (byId == null)
            {
                byId = new Dictionary<string, CharacterDef>();
                foreach (var c in All) byId[c.id] = c;
            }
            if (string.IsNullOrEmpty(id) || !byId.ContainsKey(id))
                return byId["gerson"];
            return byId[id];
        }
    }
}
