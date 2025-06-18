using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private LevelBuilder levelBuilder;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(WaitForSceneLoadThenBuild());
        }
    }

    private IEnumerator WaitForSceneLoadThenBuild()
    {
        // Esperar a que la escena esté completamente cargada
        yield return new WaitForSeconds(1f); // Ajustable según necesidad

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

