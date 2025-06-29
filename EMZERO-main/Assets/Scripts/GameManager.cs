using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public static GameManager Instance;
    private LevelBuilder levelBuilder;

    public NetworkVariable<int> TotalCoinsCollected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayersConnected = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<GameMode> CurrentGameMode = new(writePerm: NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            //StartCoroutine(WaitForSceneLoadThenBuild());
            PlayersConnected.Value = NetworkManager.ConnectedClients.Count;
        }
    }

    public void AddCoin()
    {
        if (IsServer)
        {
            TotalCoinsCollected.Value++;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        int totalPlayers = NetworkManager.Singleton.ConnectedClients.Count;

        if (totalPlayers > 4 && IsServer)//1 host+3 clientes
        {
            Debug.Log($"Se ha alcanzado el límite de jugadores. Rechazando al cliente {clientId}");
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
        else
        {
            PlayersConnected.Value = totalPlayers;
            Debug.Log($"Jugador conectado. Total ahora: {totalPlayers}");
        }
    }
    private void OnClientDisconnected(ulong clientId)
    {
        PlayersConnected.Value = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"Jugador desconectado. Total ahora: {PlayersConnected.Value}");
    }

    [ClientRpc]
    void NotifyClientsMapReadyClientRpc()
    {
        Debug.Log("Mapa generado por el servidor. Cliente listo para jugar.");
        // Aquí puedes activar un flag local si necesitas bloquear UI o controles hasta que esto llegue
    }
}
