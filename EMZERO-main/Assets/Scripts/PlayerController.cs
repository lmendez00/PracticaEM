// PLAYERMANAGER DEL PROYECTO
using TMPro;
using Unity.Netcode;
using UnityEngine;
// using UnityEngine.Windows;

public class PlayerController : NetworkBehaviour
{

    [Header("Stats")]
    public int CoinsCollected = 0;

    [Header("Character settings")]
    public bool isZombie = false; // Añadir una propiedad para el estado del jugador
    public string uniqueID; // Añadir una propiedad para el identificador único

    [Header("Movement Settings")]
    public float moveSpeed = 5f;                // Velocidad de movimiento
    public float zombieSpeedModifier = 0.8f;    // Modificador de velocidad para zombies
    public Animator animator;                   // Referencia al Animator
    public Transform cameraTransform;           // Referencia a la cámara

    private float horizontalInput;              // Entrada horizontal (A/D o flechas)
    private float verticalInput;                // Entrada vertical (W/S o flechas)

    public NetworkVariable<bool> zombificados = new(writePerm: NetworkVariableWritePermission.Server, readPerm: NetworkVariableReadPermission.Everyone);

    private TextMeshProUGUI coinTextValue;
    private TextMeshProUGUI coinRemainingText;

    void Start()
    {

        if (!IsOwner)
        {
            this.GetComponent<PlayerController>().enabled = false;
            // enabled = false;
        }

        // Buscar el objeto "CanvasPlayer" en la escena
        GameObject canvas = GameObject.Find("CanvasPlayer");

        if (canvas != null)
        {
            Debug.Log("Canvas encontrado");

            // Buscar el Panel dentro del CanvasHud
            Transform panel = canvas.transform.Find("PanelHud");
            if (panel != null)
            {
                // Buscar el TextMeshProUGUI llamado "CoinsValue" dentro del Panel

                Transform coinTextTransform = panel.Find("CoinsValue");
                if (coinTextTransform != null)
                {
                    coinTextValue = coinTextTransform.GetComponent<TextMeshProUGUI>();
                    coinTextValue.enabled = false;
                }

                coinRemainingText = panel.Find("Coins").GetComponent<TextMeshProUGUI>();
                coinRemainingText.enabled = false;
                
            }
        }

        if(LevelManager.gameMode == GameMode.Monedas) UpdateCoinUI();
    }

    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    /*void InitializeOwner()
    {
        // GetComponent<PlayerInput>().enabled = true;
        Debug.Log("Player iniciado como dueño local: " + uniqueID);
    }*/
    

    void Update()
    {
        if (!IsOwner) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Debug.DrawRay(rayOrigin, Vector3.down * 1f, Color.red);

        // Leer entrada del teclado
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        // Mover el jugador
        MovePlayer();

        // Manejar las animaciones del jugador
        HandleAnimationsRpc(horizontalInput, verticalInput);

    }

    void MovePlayer()
    {
        if (cameraTransform == null) return;
        //Debug.Log("Entra en Move");
        // Calcular dirección en base a input + cámara
        Vector3 moveDirection = (cameraTransform.forward * verticalInput + cameraTransform.right * horizontalInput).normalized;
        moveDirection.y = 0f;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            float adjustedSpeed = isZombie ? moveSpeed * zombieSpeedModifier : moveSpeed;
            //Debug.Log("MovimientoBien");
            // Calcular nueva posición y rotación
            Vector3 newPosition = transform.position + moveDirection * adjustedSpeed * Time.deltaTime;
            Quaternion newRotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);

            // Enviar al servidor
            SubmitMovementServerRpc(newPosition, newRotation);
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    void SubmitMovementServerRpc(Vector3 position, Quaternion rotation)
    {
        
        transform.position = position;
        transform.rotation = rotation;
    }
    ///////
    


    [Rpc(SendTo.ClientsAndHost)]
    void HandleAnimationsRpc(float horizontalInput, float verticalInput)
    {
        //Debug.Log("Animaciones bien");
        // Animaciones basadas en la dirección del movimiento
        animator.SetFloat("Speed", Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));  // Controla el movimiento (caminar/correr)
    }

    public void CoinCollected()
    {
        if (!isZombie)
        {
            CoinsCollected++;

            if (IsServer)
            {
                // Actualiza el contador en el servidor (opcional si sincronizas globalmente)
                GameManager.Instance.AddCoin();

                // Llama al RPC para actualizar el texto en el cliente dueño
                UpdateCoinUIClientRpc(OwnerClientId, CoinsCollected);
            }
        }
    }

    [ClientRpc]
    void UpdateCoinUIClientRpc(ulong targetClientId, int newValue)
    {
        if (IsOwner && NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            CoinsCollected = newValue;
            UpdateCoinUI();
        }
    }
    void UpdateCoinUI()
    {
        
        if (coinTextValue != null)
        {
            coinTextValue.enabled = true;
            coinRemainingText.enabled = true;
            coinTextValue.text = $"{CoinsCollected}";
        }
    }

    


    ////////////////////////////////

    // [Rpc(SendTo.Server)]    // Manda las actualizaciones al servidor (!!!)
    /*[Rpc(SendTo.ClientsAndHost)]
    public void OnMoveRpc(Vector3 playerTransform, Quaternion playerRotation)
    {
        this.transform.position = playerTransform;
        this.transform.rotation = playerRotation;
    }*/
}



