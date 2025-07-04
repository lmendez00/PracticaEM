using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
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
    public static GameMode gameMode; //Se ha puesto publico porque si no daba problemas a la hora de comprobar el modo de juego

    [Tooltip("Tiempo de partida en minutos para el modo tiempo")]
    public int minutes = 1;

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
    

    private PlayerController playerController;

    private float remainingSeconds;
    public static float GlobalRemainingSeconds = -1f;

    private bool isGameOver = false;

    public bool modoMedio;

    public GameObject gameOverPanel;




    #endregion

    #region Unity game loop methods

    private void Awake()
    {
        Debug.Log("Despertando el nivel");

        // Obtener la referencia al UniqueIDGenerator
        uniqueIdGenerator = GetComponent<UniqueIdGenerator>();

        // Obtener la referencia al LevelBuilder
        levelBuilder = GetComponent<LevelBuilder>();
        

        Time.timeScale = 1f; //Asegurarse de que el tiempo no est� detenido
    }

    private void Start()
    {
        gameMode = GameManager.Instance.CurrentGameMode.Value;
        Debug.Log("Iniciando el nivel");
        //Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");
        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            //Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            Transform panelGO = canvas.transform.Find("PanelGameOver");

            if (panel != null)
            {
                //Buscar los TextMeshProUGUI llamados "HumansValue" y "ZombiesValue" dentro del Panel
                Transform humansTextTransform = panel.Find("HumansValue");
                Transform zombiesTextTransform = panel.Find("ZombiesValue");

                Transform gameModeTextTransform = panel.Find("GameModeCondition");

                if (gameModeTextTransform != null)
                {
                    gameModeText = gameModeTextTransform.GetComponent<TextMeshProUGUI>();
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


        if (IsServer)
        {
            float time = (GlobalRemainingSeconds > 0) ? GlobalRemainingSeconds : minutes * 60;
            remainingSeconds = time;

            // Enviar a todos los clientes
            SetRemainingTimeClientRpc(time);
        }

        // Obtener los puntos de aparici�n y el n�mero de monedas generadas desde LevelBuilder
        if (IsServer && levelBuilder != null)
        {
            //Debug.Log($"Gamemode en builder: {gameMode}");
            levelBuilder.generateCoins = (gameMode == GameMode.Monedas);

            levelBuilder.Build();
            humanSpawnPoints = levelBuilder.GetHumanSpawnPoints();
            zombieSpawnPoints = levelBuilder.GetZombieSpawnPoints();
            CoinsGenerated = levelBuilder.GetCoinsGenerated();

            //El numero inicial de players es el numero total de players
            numberOfHumans = NetworkManager.Singleton.ConnectedClientsIds.Count;
            UpdateHumansZombiesClientRpc(numberOfHumans, numberOfZombies);

            // Sincroniza con los clientes
            SetCoinsGeneratedClientRpc(CoinsGenerated);

        }

        SpawnTeams();
        UpdateTeamUI();
    }

    private void Update()
    {
        //Debug.Log($"Gamemode en update: {gameMode}");
        if (gameMode == GameMode.Tiempo)
        {

            // L�gica para el modo de juego basado en tiempo
            HandleTimeLimitedGameMode();
            
        }
        else if (gameMode == GameMode.Monedas)
        {
            HandleCoinBasedGameMode();
        }

        if (Input.GetKeyDown(KeyCode.Z)) 
        {
            //Comprobar si el jugador actual est� usando el prefab de humano
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
        else if (Input.GetKeyDown(KeyCode.H))
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
                numberOfHumans--;
                numberOfZombies++;

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
                    playerController.isZombie = false; 
                    numberOfHumans++; 
                    numberOfZombies--;
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
            //Crear una instancia del prefab en el punto especificado


            //Verificar si el jugador ya existe
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                //Si ya tiene un objeto de jugador, no hace nada
                if (client.PlayerObject != null)
                    return;
            }
            Debug.Log("Antes del Spawn: " + NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count);
            // Instancia el nuevo jugador y lo asigna al cliente
            GameObject player = Instantiate(prefab, spawnPosition, Quaternion.identity);
            NetworkObject playerNetworkObject = player.GetComponent<NetworkObject>();
            playerNetworkObject.SpawnWithOwnership(clientId); // Asigna la propiedad al cliente

            player.GetComponent<PlayerController>().OnNetworkSpawn();
            //player.GetComponent<PlayerController>().networkName.Value = playerName; // Asigna el nombre del jugador
            Debug.Log("Despues del Spawn: " + NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count);

            player.tag = "Player";

            //Obtener la referencia a la c�mara principal
            Camera mainCamera = player.transform.GetChild(3).gameObject.GetComponent<Camera>();

            if (mainCamera != null)
            {
                // Obtener el script CameraController de la c�mara principal
                CameraController cameraController = mainCamera.GetComponent<CameraController>();

                if (cameraController != null)
                {
                    // Asignar el jugador al script CameraController
                    cameraController.player = player.transform;
                }

                //Obtener el componente PlayerController del jugador instanciado
                playerController = player.GetComponent<PlayerController>();
                //Asignar el transform de la c�mara al PlayerController
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
       

        Debug.Log("Instanciando equipos aleatorios...");

        var connectedClients = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        int totalPlayers = connectedClients.Count;

        //Mezclar los IDs aleatoriamente
        System.Random rnd = new System.Random();
        connectedClients = connectedClients.OrderBy(x => rnd.Next()).ToList();

        //Calcular n�mero de humanos y zombies
        int numHumans = totalPlayers / 2;
        int numZombies = totalPlayers - numHumans; //Igual o uno m�s que humanos

        for (int i = 0; i < numHumans; i++)
        {
            SpawnPlayer(humanSpawnPoints[0], playerPrefab, connectedClients[i]);
        }

        for (int i = 0; i < numZombies; i++)
        {
            int index = i + numHumans;
            SpawnPlayer(zombieSpawnPoints[i], zombiePrefab, connectedClients[index]);
        }

        //Actualizar contadores
        numberOfHumans = numHumans;
        numberOfZombies = numZombies;
        UpdateHumansZombiesClientRpc(numberOfHumans, numberOfZombies);
    }

    private void SpawnNonPlayableCharacter(GameObject prefab, Vector3 spawnPosition)
    {
        if (prefab != null)
        {
            GameObject npc = Instantiate(prefab, spawnPosition, Quaternion.identity);
            var playerController = npc.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = false; 
                playerController.uniqueID = uniqueIdGenerator.GenerateUniqueID(); 
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
        //Implementar la l�gica para el modo de juego basado en tiempo
        if (isGameOver) return;

        
        remainingSeconds -= Time.deltaTime;

        //Si el tiempo es 0 se termina
        if (remainingSeconds <= 0)
        {
            WinCondition_HumansRpc();
            isGameOver = true;
            remainingSeconds = 0;

            SetRemainingTimeClientRpc(0);
        }

        //RemainingSeconds a minutos y segundos
        int minutesRemaining = Mathf.FloorToInt(remainingSeconds / 60);
        int secondsRemaining = Mathf.FloorToInt(remainingSeconds % 60);

        //Actualizar la interfaz
        if (gameModeText != null)
        {
            //Debug.Log($"Entra en gameModeText de Tiempo, minutos {minutesRemaining} segundos{secondsRemaining}");
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
                //Tener en cuenta si algun jugador se ha desconectado
                WinCondition_HumansRpc();
                isGameOver = true;
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
        ResetGameRequestRpc(); //Resetea el GameManager
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var allPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (var player in allPlayers)
            {
                player.GetComponent<NetworkObject>().Despawn();
            }
            CleanupAndReturnToMenu();
        }


    }

    public void DificultadFacil()
    {
        if (IsServer)
        {
            GlobalRemainingSeconds = minutes * 60;
            modoMedio = false;
        }
    }

    public void DificultadMedia()
    {
        if (IsServer)
        {
            Debug.Log("Entra DificultadMedia");
            GlobalRemainingSeconds = (minutes * 60) * 3;
            Debug.Log($"tiempo: {GlobalRemainingSeconds}");
            modoMedio = true;
        }
    }

    [ClientRpc]
    public void SetRemainingTimeClientRpc(float time)
    {
        remainingSeconds = time;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ResetGameRequestRpc()
    {
        minutes = 1; //Valor por defecto
        CoinsGenerated = 0;
        levelBuilder = null;
        isGameOver = false;
        numberOfHumans = 0;
        numberOfZombies = 0;
        Debug.Log("Juego reiniciado");
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void WinCondition_HumansRpc()
    {
        //Debug.Log("Ganan los humanos!");

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
            var pc = jugador.GetComponent<PlayerController>();
            if (pc == null || !pc.IsOwner) continue;

            //Mostrar el mensaje correcto basado en el estado del jugador
            if (pc.isZombie && pc.zombificados.Value)
            {
                semiWinText.enabled = true;
                winTextZombies.enabled = false;
            }
            else if (pc.isZombie)
            {
                winTextZombies.enabled = true;
            }
            else
            {
                defeatTextHumans.enabled = true;
            }

            ShowGameOverPanel();
        }
    }

    public void CleanupAndReturnToMenu()
    {
        if (!IsServer) return;
        var gameManager = FindObjectOfType<GameManager>();

        //Limpiar network objects
        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.ToList())
        {
            if (netObj != null && netObj.IsSpawned)
            {
                if (netObj != null && netObj.gameObject != gameManager.gameObject) //No destruir el GameManager
                {
                    netObj.Despawn();
                    Destroy(netObj.gameObject);
                }
                
            }
        }
        //Cargar menu
        NetworkManager.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
    }

    #endregion
}
