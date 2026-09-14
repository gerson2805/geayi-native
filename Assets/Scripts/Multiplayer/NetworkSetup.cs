// NetworkSetup.cs — BASE de multijugador con Netcode for GameObjects
// ============================================================================
// ⚠️ ESTO ES SOLO LA ESTRUCTURA BASE, no es un multijugador funcional todavía.
// Para activarlo hay que:
//   1) Instalar el paquete "Netcode for GameObjects" (Package Manager).
//   2) Agregar UNITY_NETCODE_PRESENT en:
//      Project Settings > Player > Scripting Define Symbols.
//   3) Crear un prefab de jugador con NetworkObject + PlayerNetwork.
//   4) Configurar un servidor o servicio de relay para conectar jugadores.
// Sin el paquete instalado, este script compila pero solo muestra avisos.
// ============================================================================
using UnityEngine;

#if UNITY_NETCODE_PRESENT
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
#endif

namespace Geayi.Net
{
    public class NetworkSetup : MonoBehaviour
    {
        [Header("Red")]
        public string defaultAddress = "127.0.0.1";
        public ushort defaultPort = 7777;
        [Tooltip("En el editor arranca como host para probar rápido")]
        public bool startAsHostInEditor = true;

#if UNITY_NETCODE_PRESENT
        private NetworkManager netManager;

        void Start()
        {
            // NetworkManager (lo crea si no existe en la escena)
            netManager = GetComponent<NetworkManager>();
            if (netManager == null)
                netManager = gameObject.AddComponent<NetworkManager>();

            // Transporte oficial de Unity (UTP)
            UnityTransport transport = GetComponent<UnityTransport>();
            if (transport == null)
                transport = gameObject.AddComponent<UnityTransport>();
            transport.SetConnectionData(defaultAddress, defaultPort);

#if UNITY_EDITOR
            if (startAsHostInEditor && !netManager.IsListening)
            {
                netManager.StartHost();
                Debug.Log("[NetworkSetup] Host iniciado (editor).");
            }
#endif
        }

        // Llamar desde botones de la UI
        public void StartHost()
        {
            if (!netManager.IsListening) netManager.StartHost();
        }

        public void StartClient()
        {
            if (!netManager.IsListening) netManager.StartClient();
        }

        public void StartServer()
        {
            if (!netManager.IsListening) netManager.StartServer();
        }

        public void Shutdown()
        {
            if (netManager.IsListening) netManager.Shutdown();
        }

        public bool IsListening { get { return netManager != null && netManager.IsListening; } }
#else
        void Start()
        {
            Debug.LogWarning("[NetworkSetup] BASE sin activar: instala el paquete " +
                "'Netcode for GameObjects' y define UNITY_NETCODE_PRESENT en " +
                "Project Settings > Player > Scripting Define Symbols.");
        }

        // Versiones vacías para que el proyecto compile sin el paquete
        public void StartHost() { Debug.LogWarning("[NetworkSetup] Netcode no instalado."); }
        public void StartClient() { Debug.LogWarning("[NetworkSetup] Netcode no instalado."); }
        public void StartServer() { Debug.LogWarning("[NetworkSetup] Netcode no instalado."); }
        public void Shutdown() { }
        public bool IsListening { get { return false; } }
#endif
    }

#if UNITY_NETCODE_PRESENT
    // ------------------------------------------------------------------
    // Jugador en red: el dueño lo controla, los demás solo lo ven moverse.
    // Poner este script + NetworkObject en el prefab del jugador.
    // ------------------------------------------------------------------
    public class PlayerNetwork : NetworkBehaviour
    {
        // Posición sincronizada por la red
        private readonly NetworkVariable<Vector3> netPos = new NetworkVariable<Vector3>();

        public override void OnNetworkSpawn()
        {
            // Solo el dueño controla su personaje; los demás lo ven
            var pc = GetComponent<Geayi.Player.PlayerController>();
            if (pc != null) pc.enabled = IsOwner;
        }

        void Update()
        {
            if (IsOwner)
            {
                // El dueño publica su posición
                netPos.Value = transform.position;
            }
            else
            {
                // Los demás la reciben con suavizado
                transform.position = Vector3.Lerp(transform.position, netPos.Value, 10f * Time.deltaTime);
            }
        }
    }
#endif
}
