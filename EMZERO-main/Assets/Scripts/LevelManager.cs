using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;

// using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode
{
    Tiempo,
    Monedas
}

public class LevelManager : NetworkBehaviour
{
    #region Properties

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject zombiePrefab;

    [Header("Team Settings")]
    [Tooltip("N�mero de jugadores humanos")]
    [SerializeField] private int numberOfHumans = 2;

    [Tooltip("N�mero de zombis")]
    [SerializeField] private int numberOfZombies = 2;

    [Header("Game Mode Settings")]
    [Tooltip("Selecciona el modo de juego")]
    [SerializeField] private GameMode gameMode;

    [Tooltip("Tiempo de partida en minutos para el modo tiempo")]
    [SerializeField] private int minutes = 1;

    private List<Vector3> humanSpawnPoints = new List<Vector3>();
    private List<Vector3> zombieSpawnPoints = new List<Vector3>();

    // Referencias a los elementos de texto en el canvas
    private TextMeshProUGUI humansText;
    private TextMeshProUGUI zombiesText;
    private TextMeshProUGUI gameModeText;

    private TextMeshProUGUI winTextHumans;
    private TextMeshProUGUI winTextZombies;
    private TextMeshProUGUI defeatTextHumans;
    private TextMeshProUGUI defeatTextZombies;
    private TextMeshProUGUI semiWinText;

    private int CoinsGenerated = 0;

    public string PlayerPrefabName => playerPrefab.name;
    public string ZombiePrefabName => zombiePrefab.name;

    private UniqueIdGenerator uniqueIdGenerator;
    private LevelBuilder levelBuilder;
    // private LevelBuilder levelBuilderMonedas;
    // private LevelBuilder levelBuilderTiempo;

    private PlayerController playerController;

    private float remainingSeconds;
    private bool isGameOver = false;

    public GameObject gameOverPanel; // Asigna el panel desde el inspector

    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        Debug.Log("Despertando el nivel");

        // Obtener la referencia al UniqueIDGenerator
        uniqueIdGenerator = GetComponent<UniqueIdGenerator>();

        // Obtener la referencia al LevelBuilder
        levelBuilder = GetComponent<LevelBuilder>();
        // levelBuilderMonedas = GetComponent<LevelBuilderMonedas>();
        // levelBuilderTiempo = GetComponent<LevelBuilderTiempo>();

        Time.timeScale = 1f; // Asegurarse de que el tiempo no est� detenido
    }

    private void Start()
    {
        Debug.Log("Iniciando el nivel");
        // Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            // Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            Transform panelGO = canvas.transform.Find("PanelGameOver");

            if (panel != null)
            {
                // Buscar los TextMeshProUGUI llamados "HumansValue" y "ZombiesValue" dentro del Panel
                Transform humansTextTransform = panel.Find("HumansValue");
                Transform zombiesTextTransform = panel.Find("ZombiesValue");

                if (gameMode == GameMode.Tiempo)
                {
                    Transform gameModeTextTransform = panel.Find("GameModeConditionTiempo");

                    if (gameModeTextTransform != null)
                    {
                        gameModeText = gameModeTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
                else if (gameMode == GameMode.Monedas)
                {
                    Transform gameModeTextTransform = panel.Find("GameModeConditionMonedas");

                    if (gameModeTextTransform != null)
                    {
                        gameModeText = gameModeTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
                

                if (humansTextTransform != null)
                {
                    humansText = humansTextTransform.GetComponent<TextMeshProUGUI>();
                }

                if (zombiesTextTransform != null)
                {
                    zombiesText = zombiesTextTransform.GetComponent<TextMeshProUGUI>();
                }

                if (winTextHumans == null)
                {
                    winTextHumans = panelGO.Find("WinConditionHumans").GetComponent<TextMeshProUGUI>();
                    winTextHumans.enabled = false;
                }

                if (winTextZombies == null)
                {
                    winTextZombies = panelGO.Find("WinConditionZombies").GetComponent<TextMeshProUGUI>();
                    winTextZombies.enabled = false;
                }

                if (defeatTextHumans == null)
                {
                    defeatTextHumans = panelGO.Find("DefeatConditionHumans").GetComponent<TextMeshProUGUI>();
                    defeatTextHumans.enabled = false;
                }

                if (defeatTextZombies == null)
                {
                    defeatTextZombies = panelGO.Find("DefeatConditionZombies").GetComponent<TextMeshProUGUI>();
                    defeatTextZombies.enabled = false;
                }

                if (semiWinText == null)
                {
                    semiWinText = panelGO.Find("SemiWinCondition").GetComponent<TextMeshProUGUI>();
                    semiWinText.enabled = false;
                }
            }
        }

        remainingSeconds = minutes * 60;

        // Obtener los puntos de aparici�n y el n�mero de monedas generadas desde LevelBuilder
        if (IsServer && levelBuilder != null)
        {
            levelBuilder.Build();
            humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();
            CoinsGenerated = levelBuilder.GetCoinsGenerated();

            // Sincroniza con los clientes
            SetCoinsGeneratedClientRpc(CoinsGenerated);

            //El numero inicial de players es el numero total de players
            numberOfHumans = NetworkManager.Singleton.ConnectedClientsIds.Count;
            UpdateHumansZombiesClientRpc(numberOfHumans, numberOfZombies);
        }

        SpawnTeams();
        UpdateTeamUI();
    }

    private void Update()
    {
        if (gameMode == GameMode.Tiempo)
        {

            // L�gica para el modo de juego basado en tiempo
            HandleTimeLimitedGameMode();
        }
        else if (gameMode == GameMode.Monedas)
        {
            // L�gica para el modo de juego basado en monedas
            HandleCoinBasedGameMode();
        }

        if (Input.GetKeyDown(KeyCode.Z)) // Presiona "Z" para convertirte en Zombie
        {
            // Comprobar si el jugador actual est� usando el prefab de humano
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(playerPrefab.name))
            {
                ChangeToZombie();
            }
            else
            {
                Debug.Log("El jugador actual no es un humano.");
            }
        }
        else if (Input.GetKeyDown(KeyCode.H)) // Presiona "H" para convertirte en Humano
        {
            // Comprobar si el jugador actual est� usando el prefab de zombie
            GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
            if (currentPlayer != null && currentPlayer.name.Contains(zombiePrefab.name))
            {
                ChangeToHuman();
            }
            else
            {
                Debug.Log("El jugador actual no es un zombie.");
            }
        }
        UpdateTeamUI();

        if (isGameOver)
        {
            ShowGameOverPanel();
        }
    }

    #endregion

    #region Team management methods

    private void ChangeToZombie()
    {
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");
        ChangeToZombie(currentPlayer, true);
    }

    public void ChangeToZombie(GameObject human, bool enabled)
    {
        Debug.Log("Cambiando a Zombie");

        if (human != null)
        {
            // Guardar la posici�n, rotaci�n y uniqueID del humano actual
            Vector3 playerPosition = human.transform.position;
            Quaternion playerRotation = human.transform.rotation;
            string uniqueID = human.GetComponent<PlayerController>().uniqueID;
            ulong id = human.GetComponent<NetworkObject>().OwnerClientId; 

            // Destruir el humano actual
            human.GetComponent<NetworkObject>().Despawn();
            //Destroy(human);

            // Instanciar el prefab del zombie en la misma posici�n y rotaci�n
            GameObject zombie = Instantiate(zombiePrefab, playerPosition, playerRotation);
            
            zombie.GetComponent<NetworkObject>().SpawnAsPlayerObject(id);
            
            if (enabled) { zombie.tag = "Player"; }

            // Obtener el componente PlayerController del zombie instanciado
            PlayerController playerController = zombie.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = enabled;
                playerController.isZombie = true; // Cambiar el estado a zombie
                playerController.uniqueID = uniqueID; // Mantener el identificador �nico
                numberOfHumans--; // Reducir el n�mero de humanos
                numberOfZombies++; // Aumentar el n�mero de zombis

                playerController.zombificados.Value = true;

                if (numberOfHumans == 0)
                {
                    WinCondition_ZombiesRpc(id);
                }

                UpdateHumansZombiesClientRpc(numberOfHumans, numberOfZombies);
                UpdateTeamUI();

                if (enabled)
                {
                    // Obtener la referencia a la c�mara principal
                    Camera mainCamera = Camera.main;

                    if (mainCamera != null)
                    {
                        // Obtener el script CameraController de la c�mara principal
                        CameraController cameraController = mainCamera.GetComponent<CameraController>();

                        if (cameraController != null)
                        {
                            // Asignar el zombie al script CameraController
                            cameraController.player = zombie.transform;
                        }

                        // Asignar el transform de la c�mara al PlayerController
                        playerController.cameraTransform = mainCamera.transform;
                    }
                    else
                    {
                        Debug.LogError("No se encontr� la c�mara principal.");
                    }
                }
            }
            else
            {
                Debug.LogError("PlayerController no encontrado en el zombie instanciado.");
            }
        }
        else
        {
            Debug.LogError("No se encontr� el humano actual.");
        }
    }

    private void ChangeToHuman()
    {
        Debug.Log("Cambiando a Humano");

        // Obtener la referencia al jugador actual
        GameObject currentPlayer = GameObject.FindGameObjectWithTag("Player");

        if (currentPlayer != null)
        {
            // Guardar la posici�n y rotaci�n del jugador actual
            Vector3 playerPosition = currentPlayer.transform.position;
            Quaternion playerRotation = currentPlayer.transform.rotation;

            // Destruir el jugador actual
            Destroy(currentPlayer);

            // Instanciar el prefab del humano en la misma posici�n y rotaci�n
            GameObject human = Instantiate(playerPrefab, playerPosition, playerRotation);
            human.tag = "Player";

            // Obtener la referencia a la c�mara principal
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                // Obtener el script CameraController de la c�mara principal
                CameraController cameraController = mainCamera.GetComponent<CameraController>();

                if (cameraController != null)
                {
                    // Asignar el humano al script CameraController
                    cameraController.player = human.transform;
                }

                // Obtener el componente PlayerController del humano instanciado
                playerController = human.GetComponent<PlayerController>();
                // Asignar el transform de la c�mara al PlayerController
                if (playerController != null)
                {
                    playerController.enabled = true;
                    playerController.cameraTransform = mainCamera.transform;
                    playerController.isZombie = false; // Cambiar el estado a humano
                    numberOfHumans++; // Aumentar el n�mero de humanos
                    numberOfZombies--; // Reducir el n�mero de zombis
                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el humano instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontr� la c�mara principal.");
            }
        }
        else
        {
            Debug.LogError("No se encontr� el jugador actual.");
        }
    }

    private void SpawnPlayer(Vector3 spawnPosition, GameObject prefab, ulong clientId)
    {

        //////////////////////////////// INTENTO DE SPAWN DE JUGADORES /////////////////////////


        //Debug.Log($"Instanciando jugador en {spawnPosition}");
        if (prefab != null)
        {
            Debug.Log($"Instanciando jugador en {spawnPosition}");
            // Crear una instancia del prefab en el punto especificado


            // Verifica si el jugador ya existe en el diccionario
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                // Si ya tiene un objeto de jugador, no hace nada
                if (client.PlayerObject != null)
                    return;
            }
            Debug.Log("Antes del Spawn: " + NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count);
            // Instancia el nuevo jugador y lo asigna al cliente
            GameObject player = Instantiate(prefab, new Vector3(3,3,3), Quaternion.identity);
            NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnWithOwnership(clientId); // Asigna la propiedad al cliente

            player.GetComponent<PlayerController>().OnNetworkSpawn();
            //player.GetComponent<PlayerController>().networkName.Value = playerName; // Asigna el nombre del jugador
            Debug.Log("Despues del Spawn: " + NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count);

            player.tag = "Player";

            // Obtener la referencia a la c�mara principal
            Camera mainCamera = player.transform.GetChild(3).gameObject.GetComponent<Camera>();

            if (mainCamera != null)
            {
                // Obtener el script CameraController de la c�mara principal
                CameraController cameraController = mainCamera.GetComponent<CameraController>();

                if (cameraController != null)
                {
                    //Debug.Log($"CameraController encontrado en la c�mara principal.");
                    // Asignar el jugador al script CameraController
                    cameraController.player = player.transform;
                }

                //Debug.Log($"C�mara principal encontrada en {mainCamera}");
                // Obtener el componente PlayerController del jugador instanciado
                playerController = player.GetComponent<PlayerController>();
                // Asignar el transform de la c�mara al PlayerController
                if (playerController != null)
                {
                    //Debug.Log($"PlayerController encontrado en el jugador instanciado.");
                    playerController.enabled = true;
                    playerController.cameraTransform = mainCamera.transform;
                    playerController.uniqueID = uniqueIdGenerator.GenerateUniqueID(); // Generar un identificador �nico

                }
                else
                {
                    Debug.LogError("PlayerController no encontrado en el jugador instanciado.");
                }
            }
            else
            {
                Debug.LogError("No se encontr� la c�mara principal.");
            }
        }
        else
        {
            Debug.LogError("Faltan referencias al prefab o al punto de aparici�n.");
        }

    }

    private void SpawnTeams()
    {
        Debug.Log("Instanciando equipos");
        foreach (ulong id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (IsHost)
            {
                SpawnPlayer(humanSpawnPoints[0], playerPrefab, id);
            }
        }

        if (humanSpawnPoints.Count <= 0) { return; }
       
        Debug.Log($"Personaje jugable instanciado en {humanSpawnPoints[0]}");

        for (int i = 1; i < numberOfHumans; i++)
        {
            if (i < humanSpawnPoints.Count)
            {
                SpawnNonPlayableCharacter(playerPrefab, humanSpawnPoints[i]);
            }
        }

        for (int i = 0; i < numberOfZombies; i++)
        {
            if (i < zombieSpawnPoints.Count)
            {
                SpawnNonPlayableCharacter(zombiePrefab, zombieSpawnPoints[i]);
            }
        }
    }

    private void SpawnNonPlayableCharacter(GameObject prefab, Vector3 spawnPosition)
    {
        if (prefab != null)
        {
            GameObject npc = Instantiate(prefab, spawnPosition, Quaternion.identity);
            // Desactivar el controlador del jugador en los NPCs
            var playerController = npc.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false; // Desactivar el controlador del jugador
                playerController.uniqueID = uniqueIdGenerator.GenerateUniqueID(); // Asignar un identificador �nico
            }
            Debug.Log($"Personaje no jugable instanciado en {spawnPosition}");
        }
    }

    [ClientRpc]
    void UpdateHumansZombiesClientRpc(int humanos, int zombies)
    {
        this.numberOfHumans = humanos;
        this.numberOfZombies = zombies;
    }

    private void UpdateTeamUI()
    {
        if (humansText != null)
        {
            humansText.text = $"{numberOfHumans}";
        }

        if (zombiesText != null)
        {
            zombiesText.text = $"{numberOfZombies}";
        }
    }

    [ClientRpc]
    void SetCoinsGeneratedClientRpc(int totalCoins)
    {
        CoinsGenerated = totalCoins;
    }

    #endregion

    #region Modo de juego

    private void HandleTimeLimitedGameMode()
    {
        // Implementar la l�gica para el modo de juego basado en tiempo
        if (isGameOver) return;

        // Decrementar remainingSeconds basado en Time.deltaTime
        remainingSeconds -= Time.deltaTime;

        // Comprobar si el tiempo ha llegado a cero
        if (remainingSeconds <= 0)
        {
            isGameOver = true;
            remainingSeconds = 0;
        }

        // Convertir remainingSeconds a minutos y segundos
        int minutesRemaining = Mathf.FloorToInt(remainingSeconds / 60);
        int secondsRemaining = Mathf.FloorToInt(remainingSeconds % 60);

        // Actualizar el texto de la interfaz de usuario
        if (gameModeText != null)
        {
            gameModeText.text = $"{minutesRemaining:D2}:{secondsRemaining:D2}";
        }

    }

    private void HandleCoinBasedGameMode()
    {
        if (isGameOver) return;

        if (gameModeText != null)
        {
            int totalRecogidas = GameManager.Instance.TotalCoinsCollected.Value;
            int totalGeneradas = CoinsGenerated;

            gameModeText.text = $"{totalRecogidas}/{totalGeneradas}";

            if (totalRecogidas >= totalGeneradas)
            {
                // Tener en cuenta si algun jugador se ha desconectado
                WinCondition_HumansRpc();
            }
        }
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            Time.timeScale = 0f;
            gameOverPanel.SetActive(true); // Muestra el panel de pausa

            // Gesti�n del cursor
            Cursor.lockState = CursorLockMode.None; // Desbloquea el cursor
            Cursor.visible = true; // Hace visible el cursor
        }
    }

    public void ReturnToMainMenu()
    {
        // Gesti�n del cursor
        Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor
        Cursor.visible = false; // Oculta el cursor

        // Cargar la escena del men� principal
        SceneManager.LoadScene("MenuScene"); // Cambia "MenuScene" por el nombre de tu escena principal
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void WinCondition_HumansRpc()
    {
        Debug.Log("Ganan los humanos!");

        var allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (var jugador in allPlayers)
        {
            if (jugador.GetComponent<PlayerController>().IsOwner)
            {
                if (jugador.GetComponent<PlayerController>().isZombie)
                {
                    defeatTextZombies.enabled = true;
                }
                else
                {
                    winTextHumans.enabled = true;
                }

                ShowGameOverPanel();
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void WinCondition_ZombiesRpc(ulong id)
    {
        Debug.Log("Ganan los zombies!");

        var jugadores = GameObject.FindGameObjectsWithTag("Player");

        foreach (var jugador in jugadores)
        {
            if (jugador.GetComponent<PlayerController>().IsOwner)
            {
                bool zombificado = jugador.GetComponent<PlayerController>().zombificados.Value;

                if (jugador.GetComponent<NetworkObject>().OwnerClientId == id)
                {
                    defeatTextHumans.enabled = true;
                }
                else if (zombificado)
                {
                    semiWinText.enabled = true;
                }
                else
                {
                    winTextZombies.enabled = true;
                }

                ShowGameOverPanel();
            }
        }
    }
    #endregion
}
