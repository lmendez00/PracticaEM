using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    /* private LevelBuilder levelBuilder;

     public override void OnNetworkSpawn()
     {
         base.OnNetworkSpawn();

         // Ejecutar solo en el servidor (host o dedicado)
         if (!IsServer) return;

         Debug.Log("GameManager: OnNetworkSpawn en servidor.");

         // Buscar y asignar el LevelBuilder
         levelBuilder = FindObjectOfType<LevelBuilder>();

         if (levelBuilder != null)
         {
             levelBuilder.Build();
         }
         else
         {
             Debug.LogError("GameManager: No se encontr� LevelBuilder en la escena.");
         }

         // Aqu� podr�as agregar m�s l�gica de inicio, como:
         // - Contadores
         // - Inicio de rondas
         // - Sincronizaci�n de datos de juego
     }*/

    /*private LevelBuilder levelBuilder;

    // Posici�n de spawn compartida
    private Vector3 spawnPosition;

    [SerializeField] private GameObject playerPrefab;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            levelBuilder = FindObjectOfType<LevelBuilder>();
            if (levelBuilder != null)
            {
                levelBuilder.Build();

                // Puedes tomar un spawn point espec�fico o el centro del mapa
                spawnPosition = new Vector3(5, 1, 5); // ejemplo

                // Guardar en una NetworkVariable si quieres que todos la conozcan
                SpawnAllPlayers();
            }
        }
    }


    private void SpawnAllPlayers()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        GameObject playerPrefab = NetworkManager.Singleton.NetworkConfig.PlayerPrefab;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

    }*/
    [SerializeField] private GameObject playerPrefab; // Asigna este prefab desde el inspector

    [SerializeField] private Vector3 spawnPosition = new Vector3(5, 1, 5); // o desde LevelBuilder

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnAllConnectedPlayers();
        }
    }

    private void SpawnAllConnectedPlayers()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            SpawnPlayerForClient(clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);

        // Asigna la propiedad de ownership al cliente, sin ser PlayerPrefab
        playerInstance.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }

    // Para spawnear jugadores nuevos que se conectan despu�s
    private void OnEnable()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnPlayerForClient;
        }
    }

    private void OnDisable()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnPlayerForClient;
        }
    }
}
