// GameManager.cs — cerebro del juego GEAYI: Obby Xtreme 3D (versión nativa Unity)
// Maneja: estados del juego, monedas, cambio de mundos y calidad gráfica automática.
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Geayi.Core
{
    // Estados principales del juego
    public enum GameState
    {
        Menu,       // menú principal
        Playing,    // jugando normal
        BuildMode,  // modo construir
        Paused      // pausado
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Estado actual")]
        public GameState State = GameState.Menu;

        [Header("Mundos (nombres de escenas en Build Settings)")]
        public string[] worldScenes = { "Immokalee", "Honduras", "Mexico", "Espana" };
        public string menuScene = "MainMenu";

        [Header("Monedas del jugador")]
        public int Coins = 0;
        private const string CoinsKey = "geayi_coins";

        // Si es true, al cargar el mundo arranca directo en modo construir
        public static bool StartInBuild = false;

        void Awake()
        {
            // Singleton: solo uno en toda la app
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Cargar monedas guardadas
            Coins = PlayerPrefs.GetInt(CoinsKey, 0);

            // Calidad automática según el teléfono
            // (respeta el modo rápido si el jugador lo activó antes)
            if (PlayerPrefs.GetInt("geayi_fastmode", -1) == 1)
                SetFastMode(true);
            else
                ApplyAutoQuality();
        }

        // ---------------- Monedas ----------------
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Coins += amount;
            SaveCoins();
            if (UI.HUD.Instance != null) UI.HUD.Instance.RefreshCoins();
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (Coins < amount) return false;
            Coins -= amount;
            SaveCoins();
            if (UI.HUD.Instance != null) UI.HUD.Instance.RefreshCoins();
            return true;
        }

        private void SaveCoins()
        {
            PlayerPrefs.SetInt(CoinsKey, Coins);
            PlayerPrefs.Save();
        }

        // ---------------- Estados ----------------
        public void SetState(GameState newState)
        {
            State = newState;
            // Pausar congela el tiempo del juego
            Time.timeScale = (newState == GameState.Paused) ? 0f : 1f;
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        // ---------------- Mundos / escenas ----------------
        public void LoadWorld(int index)
        {
            if (worldScenes == null || worldScenes.Length == 0)
            {
                Debug.LogWarning("[GameManager] No hay mundos configurados.");
                return;
            }
            index = Mathf.Clamp(index, 0, worldScenes.Length - 1);
            StartInBuild = false;
            SetState(GameState.Playing);
            SceneManager.LoadScene(worldScenes[index]);
        }

        // Carga el mundo y arranca directo en modo construir
        public void LoadWorldBuild(int index)
        {
            if (worldScenes == null || worldScenes.Length == 0)
            {
                Debug.LogWarning("[GameManager] No hay mundos configurados.");
                return;
            }
            index = Mathf.Clamp(index, 0, worldScenes.Length - 1);
            StartInBuild = true;
            SetState(GameState.BuildMode);
            SceneManager.LoadScene(worldScenes[index]);
        }

        public void GoToMenu()
        {
            StartInBuild = false;
            SetState(GameState.Menu);
            SceneManager.LoadScene(menuScene);
        }

        // ---------------- Calidad gráfica automática ----------------
        // Estilo Roblox: si el teléfono es de gama baja, baja la calidad solo.
        private void ApplyAutoQuality()
        {
            if (DetectLowEnd())
            {
                // Gama baja: sin sombras, sin luces extra, 30 FPS
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.pixelLightCount = 0;
                QualitySettings.antiAliasing = 0;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
                Debug.Log("[GameManager] Teléfono gama baja: calidad reducida.");
            }
            else
            {
                // Gama media/alta: calidad normal, 60 FPS
                QualitySettings.shadows = ShadowQuality.All;
                QualitySettings.pixelLightCount = 2;
                QualitySettings.antiAliasing = 2;
                Application.targetFrameRate = 60;
                Debug.Log("[GameManager] Calidad normal activada.");
            }
        }

        private bool DetectLowEnd()
        {
            // Poca memoria RAM (< 3 GB) = gama baja
            if (SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize < 3072)
                return true;
            // Pocos núcleos de CPU = gama baja
            if (SystemInfo.processorCount > 0 && SystemInfo.processorCount <= 4)
                return true;
            return false;
        }

        // Modo rápido manual desde Ajustes (true = sin sombras, más fluido)
        public void SetFastMode(bool on)
        {
            if (on)
            {
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.pixelLightCount = 0;
                QualitySettings.antiAliasing = 0;
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = 30;
            }
            else
            {
                ApplyAutoQuality();
            }
            PlayerPrefs.SetInt("geayi_fastmode", on ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
