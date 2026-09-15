// CharacterBuilder.cs — arma cada personaje de la familia en 3D con primitivas.
// Todo usa el shader "UI/Default" (es el único que sobrevive al build de Android).
// Los personajes miran hacia +Z; el controlador los gira al caminar.
using System.Collections.Generic;
using UnityEngine;

namespace Geayi.Characters
{
    public static class CharacterBuilder
    {
        private static Dictionary<string, Material> matCache = new Dictionary<string, Material>();
        private static Shader uiShader;

        // Material por color (se comparte entre partes iguales)
        public static Material Mat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#ffffff";
            Material m;
            if (matCache.TryGetValue(hex, out m) && m != null) return m;
            if (uiShader == null)
            {
                uiShader = Shader.Find("UI/Default");
                if (uiShader == null)
                {
                    GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    uiShader = tmp.GetComponent<Renderer>().sharedMaterial.shader;
                    Object.Destroy(tmp);
                }
            }
            Color c = Color.white;
            ColorUtility.TryParseHtmlString(hex, out c);
            m = new Material(uiShader);
            m.color = c;
            matCache[hex] = m;
            return m;
        }

        // Arma el personaje completo. Raíz llamada "Body", pies en y=0.
        public static GameObject Build(CharacterDef def)
        {
            if (def == null) def = FamilyData.Get("gerson");
            GameObject root = new GameObject("Body");
            if (def.species == "dog") BuildDog(root, def);
            else if (def.species == "cavy") BuildCavy(root, def);
            else BuildHuman(root, def);
            root.transform.localScale = Vector3.one * def.scale;
            return root;
        }

        // Una pieza: primitiva con color, sin colisionador
        // (las colisiones las maneja el CharacterController del jugador).
        private static GameObject Part(GameObject parent, PrimitiveType type, string name,
            string hex, Vector3 pos, Vector3 scale, Vector3 rot)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(rot.x, rot.y, rot.z);
            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = Mat(hex);
            return go;
        }

        private static GameObject Part(GameObject parent, PrimitiveType type, string name,
            string hex, Vector3 pos, Vector3 scale)
        {
            return Part(parent, type, name, hex, pos, scale, Vector3.zero);
        }

        // ---------------- Humano (~1.75 de alto) ----------------
        private static void BuildHuman(GameObject root, CharacterDef def)
        {
            if (def.dress)
            {
                // Vestido: una sola pieza acampanada en vez de piernas
                Part(root, PrimitiveType.Cylinder, "Legs", def.dressColor,
                    new Vector3(0f, 0.38f, 0f), new Vector3(0.52f, 0.38f, 0.52f));
            }
            else
            {
                Part(root, PrimitiveType.Cylinder, "LegL", def.pants,
                    new Vector3(-0.11f, 0.375f, 0f), new Vector3(0.18f, 0.375f, 0.18f));
                Part(root, PrimitiveType.Cylinder, "LegR", def.pants,
                    new Vector3(0.11f, 0.375f, 0f), new Vector3(0.18f, 0.375f, 0.18f));
            }
            // Torso (la camisa; ApplyBodyColor lo puede repintar)
            Part(root, PrimitiveType.Capsule, "Torso", def.shirt,
                new Vector3(0f, 1.15f, 0f), new Vector3(0.4f, 0.4f, 0.4f));
            // Brazos y manos
            Part(root, PrimitiveType.Capsule, "ArmL", def.shirt,
                new Vector3(-0.30f, 1.12f, 0f), new Vector3(0.14f, 0.275f, 0.14f));
            Part(root, PrimitiveType.Capsule, "ArmR", def.shirt,
                new Vector3(0.30f, 1.12f, 0f), new Vector3(0.14f, 0.275f, 0.14f));
            Part(root, PrimitiveType.Sphere, "HandL", def.skin,
                new Vector3(-0.30f, 0.80f, 0f), new Vector3(0.14f, 0.14f, 0.14f));
            Part(root, PrimitiveType.Sphere, "HandR", def.skin,
                new Vector3(0.30f, 0.80f, 0f), new Vector3(0.14f, 0.14f, 0.14f));
            // Cabeza y ojos
            Part(root, PrimitiveType.Sphere, "Head", def.skin,
                new Vector3(0f, 1.62f, 0f), new Vector3(0.44f, 0.44f, 0.44f));
            Part(root, PrimitiveType.Sphere, "EyeL", "#101010",
                new Vector3(-0.08f, 1.66f, 0.185f), new Vector3(0.07f, 0.07f, 0.07f));
            Part(root, PrimitiveType.Sphere, "EyeR", "#101010",
                new Vector3(0.08f, 1.66f, 0.185f), new Vector3(0.07f, 0.07f, 0.07f));

            bool hasCap = !string.IsNullOrEmpty(def.cap);
            if (!hasCap)
            {
                // Pelo corto: casquete
                Part(root, PrimitiveType.Sphere, "Hair", def.hair,
                    new Vector3(0f, 1.70f, -0.02f), new Vector3(0.46f, 0.285f, 0.46f));
            }
            if (def.hairStyle == "long")
            {
                // Pelo largo: cae por la espalda
                Part(root, PrimitiveType.Cube, "HairBack", def.hair,
                    new Vector3(0f, 1.42f, -0.20f), new Vector3(0.36f, 0.55f, 0.14f));
            }
            else if (def.hairStyle == "bun")
            {
                // Recogido: moño atrás
                Part(root, PrimitiveType.Sphere, "HairBun", def.hair,
                    new Vector3(0f, 1.86f, -0.12f), new Vector3(0.2f, 0.2f, 0.2f));
            }
            if (hasCap)
            {
                // Gorra: copa + visera
                Part(root, PrimitiveType.Cylinder, "Cap", def.cap,
                    new Vector3(0f, 1.80f, 0f), new Vector3(0.46f, 0.06f, 0.46f));
                Part(root, PrimitiveType.Cube, "CapBrim", def.cap,
                    new Vector3(0f, 1.75f, 0.22f), new Vector3(0.34f, 0.05f, 0.30f));
            }
            if (def.beard)
            {
                // Barba / bigote
                Part(root, PrimitiveType.Cube, "Beard", def.hair,
                    new Vector3(0f, 1.48f, 0.15f), new Vector3(0.28f, 0.20f, 0.12f));
            }
        }

        // ---------------- Perro (mirando a +Z) ----------------
        private static void BuildDog(GameObject root, CharacterDef def)
        {
            string legs = string.IsNullOrEmpty(def.belly) ? def.body : def.belly;
            // Cuerpo horizontal
            Part(root, PrimitiveType.Capsule, "Torso", def.body,
                new Vector3(0f, 0.42f, 0f), new Vector3(0.44f, 0.375f, 0.44f),
                new Vector3(90f, 0f, 0f));
            // Cabeza
            Part(root, PrimitiveType.Sphere, "Head", def.body,
                new Vector3(0f, 0.60f, 0.42f), new Vector3(0.4f, 0.4f, 0.4f));
            // Hocico + nariz
            Part(root, PrimitiveType.Cube, "Snout", legs,
                new Vector3(0f, 0.54f, 0.60f), new Vector3(0.18f, 0.13f, 0.20f));
            Part(root, PrimitiveType.Sphere, "Nose", "#101010",
                new Vector3(0f, 0.57f, 0.70f), new Vector3(0.09f, 0.09f, 0.09f));
            // Ojos
            Part(root, PrimitiveType.Sphere, "EyeL", "#101010",
                new Vector3(-0.085f, 0.66f, 0.56f), new Vector3(0.07f, 0.07f, 0.07f));
            Part(root, PrimitiveType.Sphere, "EyeR", "#101010",
                new Vector3(0.085f, 0.66f, 0.56f), new Vector3(0.07f, 0.07f, 0.07f));
            // Orejas paradas
            Part(root, PrimitiveType.Cube, "EarL", def.body,
                new Vector3(-0.13f, 0.80f, 0.38f), new Vector3(0.09f, 0.18f, 0.06f),
                new Vector3(0f, 0f, 12f));
            Part(root, PrimitiveType.Cube, "EarR", def.body,
                new Vector3(0.13f, 0.80f, 0.38f), new Vector3(0.09f, 0.18f, 0.06f),
                new Vector3(0f, 0f, -12f));
            // 4 patas
            Part(root, PrimitiveType.Cylinder, "LegFL", legs,
                new Vector3(-0.13f, 0.19f, 0.24f), new Vector3(0.12f, 0.19f, 0.12f));
            Part(root, PrimitiveType.Cylinder, "LegFR", legs,
                new Vector3(0.13f, 0.19f, 0.24f), new Vector3(0.12f, 0.19f, 0.12f));
            Part(root, PrimitiveType.Cylinder, "LegBL", legs,
                new Vector3(-0.13f, 0.19f, -0.24f), new Vector3(0.12f, 0.19f, 0.12f));
            Part(root, PrimitiveType.Cylinder, "LegBR", legs,
                new Vector3(0.13f, 0.19f, -0.24f), new Vector3(0.12f, 0.19f, 0.12f));
            // Cola
            Part(root, PrimitiveType.Cylinder, "Tail", def.body,
                new Vector3(0f, 0.52f, -0.42f), new Vector3(0.09f, 0.175f, 0.09f),
                new Vector3(-50f, 0f, 0f));
            if (def.face == "mask")
            {
                // Máscara oscura de pastor belga
                Part(root, PrimitiveType.Sphere, "Mask", "#1a1a1a",
                    new Vector3(0f, 0.62f, 0.55f), new Vector3(0.34f, 0.26f, 0.10f));
            }
            else if (def.face == "schnauzer")
            {
                // Barba blanca de schnauzer
                Part(root, PrimitiveType.Cube, "Beard", "#f5f5f5",
                    new Vector3(0f, 0.46f, 0.62f), new Vector3(0.16f, 0.14f, 0.10f));
            }
            if (!string.IsNullOrEmpty(def.spots))
            {
                // Manchas en el lomo
                Part(root, PrimitiveType.Sphere, "Spot1", def.spots,
                    new Vector3(0.10f, 0.58f, 0.10f), new Vector3(0.3f, 0.10f, 0.3f));
                Part(root, PrimitiveType.Sphere, "Spot2", def.spots,
                    new Vector3(-0.12f, 0.55f, -0.10f), new Vector3(0.26f, 0.10f, 0.26f));
                Part(root, PrimitiveType.Sphere, "Spot3", def.spots,
                    new Vector3(0.05f, 0.60f, -0.25f), new Vector3(0.22f, 0.10f, 0.22f));
            }
            if (!string.IsNullOrEmpty(def.collar))
            {
                // Collar
                Part(root, PrimitiveType.Cylinder, "Collar", def.collar,
                    new Vector3(0f, 0.52f, 0.30f), new Vector3(0.44f, 0.05f, 0.44f));
            }
        }

        // ---------------- Cuyo (mirando a +Z) ----------------
        private static void BuildCavy(GameObject root, CharacterDef def)
        {
            // Cuerpo redondo achatado
            Part(root, PrimitiveType.Sphere, "Torso", def.body,
                new Vector3(0f, 0.21f, 0f), new Vector3(0.532f, 0.403f, 0.70f));
            // Cabeza
            Part(root, PrimitiveType.Sphere, "Head", def.body,
                new Vector3(0f, 0.30f, 0.30f), new Vector3(0.3f, 0.3f, 0.3f));
            // Orejas
            Part(root, PrimitiveType.Sphere, "EarL", def.body,
                new Vector3(-0.09f, 0.44f, 0.28f), new Vector3(0.1f, 0.1f, 0.1f));
            Part(root, PrimitiveType.Sphere, "EarR", def.body,
                new Vector3(0.09f, 0.44f, 0.28f), new Vector3(0.1f, 0.1f, 0.1f));
            // Ojos y nariz
            Part(root, PrimitiveType.Sphere, "EyeL", "#101010",
                new Vector3(-0.065f, 0.33f, 0.42f), new Vector3(0.06f, 0.06f, 0.06f));
            Part(root, PrimitiveType.Sphere, "EyeR", "#101010",
                new Vector3(0.065f, 0.33f, 0.42f), new Vector3(0.06f, 0.06f, 0.06f));
            Part(root, PrimitiveType.Sphere, "Nose", "#e88a8a",
                new Vector3(0f, 0.28f, 0.45f), new Vector3(0.05f, 0.05f, 0.05f));
            if (!string.IsNullOrEmpty(def.spots))
            {
                Part(root, PrimitiveType.Sphere, "Spot1", def.spots,
                    new Vector3(0.12f, 0.36f, 0.05f), new Vector3(0.25f, 0.08f, 0.25f));
                Part(root, PrimitiveType.Sphere, "Spot2", def.spots,
                    new Vector3(-0.10f, 0.34f, -0.12f), new Vector3(0.22f, 0.08f, 0.22f));
            }
        }
    }
}
