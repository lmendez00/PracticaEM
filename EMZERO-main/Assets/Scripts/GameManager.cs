using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public static GameManager Instance;
    private LevelBuilder levelBuilder;

    private NetworkVariable<int> totalCoinsCollected = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(WaitForSceneLoadThenBuild());
        }
    }

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
    }

    [ClientRpc]
    void NotifyClientsMapReadyClientRpc()
    {
        Debug.Log("Mapa generado por el servidor. Cliente listo para jugar.");
        // Aquí puedes activar un flag local si necesitas bloquear UI o controles hasta que esto llegue
    }
}
/*
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public AudioClip pickupSound;

    private NetworkVariable<int> totalCoinsCollected = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CollectCoinServerRpc(ulong clientId)
    {
        totalCoinsCollected.Value++;

        // Notificar al cliente (puedes usar ClientRpc para efectos, sonidos, etc.)
        NotifyCoinPickupClientRpc(clientId);
    }

    [ClientRpc]
    private void NotifyCoinPickupClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            // Solo el jugador que recogió escucha esto
            AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>("pickup"), Vector3.zero);
        }
    }

    public int GetCoinCount()
    {
        return totalCoinsCollected.Value;
    }
}


*/