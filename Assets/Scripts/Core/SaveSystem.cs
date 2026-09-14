// SaveSystem.cs — guardado del progreso con PlayerPrefs + JSON
// Guarda: monedas, personaje, poderes comprados/equipado, bloques, mascotas.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geayi.Core
{
    [Serializable]
    public class BlockData
    {
        public float x, y, z;
        public float size = 1f;
        public string color = "#ff5e8a";
    }

    [Serializable]
    public class SaveData
    {
        public int coins = 0;
        public string characterId = "gerson";
        public List<string> ownedPowers = new List<string>();
        public string equippedPower = "";
        public List<BlockData> blocks = new List<BlockData>();
        public List<string> pets = new List<string>();
        public int version = 1;
    }

    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }
        private const string SaveKey = "geayi_save_v1";

        [Header("Datos del jugador")]
        public SaveData Data = new SaveData();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // Guarda todo a PlayerPrefs como JSON
        public void Save()
        {
            try
            {
                if (GameManager.Instance != null)
                    Data.coins = GameManager.Instance.Coins;

                string json = JsonUtility.ToJson(Data);
                PlayerPrefs.SetString(SaveKey, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveSystem] Progreso guardado.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Error al guardar: " + e.Message);
            }
        }

        // Carga el progreso (si no hay, usa valores por defecto)
        public void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(SaveKey))
                {
                    string json = PlayerPrefs.GetString(SaveKey);
                    SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                    if (loaded != null)
                    {
                        Data = loaded;
                        // Proteger listas nulas (por si el JSON viene incompleto)
                        if (Data.ownedPowers == null) Data.ownedPowers = new List<string>();
                        if (Data.blocks == null) Data.blocks = new List<BlockData>();
                        if (Data.pets == null) Data.pets = new List<string>();
                    }
                }
                if (GameManager.Instance != null)
                    GameManager.Instance.Coins = Data.coins;
                Debug.Log("[SaveSystem] Progreso cargado.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveSystem] Error al cargar: " + e.Message);
            }
        }

        // Borra todo el progreso (útil para pruebas)
        public void Wipe()
        {
            Data = new SaveData();
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Coins = 0;
            }
        }

        // Guardar automáticamente al minimizar o cerrar la app
        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationQuit() { Save(); }
    }
}
