using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public static GameManager Instance;
    private LevelBuilder levelBuilder;

    public NetworkVariable<int> TotalCoinsCollected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayersConnected = new(writePerm: NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            //StartCoroutine(WaitForSceneLoadThenBuild());
            PlayersConnected.Value = NetworkManager.ConnectedClients.Count;
        }
    }
    /*
    private IEnumerator WaitForSceneLoadThenBuild()
    {
        //Esperar a que la escena esté completamente cargada
        yield return new WaitForSeconds(1f);// Ajustable según necesidad

        levelBuilder = FindObjectOfType<LevelBuilder>();
        if (levelBuilder != null)
        {
            levelBuilder.Build(); // Solo el servidor construye el mapa
            NotifyClientsMapReadyClientRpc();
        }
    }*/

    public void AddCoin()
    {
        if (IsServer)
        {
            TotalCoinsCollected.Value++;
        }
    }

    [ClientRpc]
    void NotifyClientsMapReadyClientRpc()
    {
        Debug.Log("Mapa generado por el servidor. Cliente listo para jugar.");
        // Aquí puedes activar un flag local si necesitas bloquear UI o controles hasta que esto llegue
    }
}
