using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : NetworkBehaviour
{
    [SerializeField]
    NetworkManager m_NetworkManager;

    public static UIManager Instance { get; private set; }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /*public void CrearPartida()
    {
        m_NetworkManager.StartHost();

        //SceneManager.LoadScene("MenuScene");
        m_NetworkManager.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
    }
    

    public void EntrarPartida()
    {
        m_NetworkManager.StartClient();

        //Entrar en el menú, pero dejar claro que es como cliente, antes de eso hay que hacer que ponga el código (en esta misma escena se puede hacer)
        //m_NetworkManager.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);

    }
    */

    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        GUILayout.EndArea();
    }

    void StartButtons()
    {
    if (GUILayout.Button("Host"))
    {
        m_NetworkManager.StartHost();

        //SceneManager.LoadScene("MenuScene");
        m_NetworkManager.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
    }
    if (GUILayout.Button("Client")) m_NetworkManager.StartClient();
        if (GUILayout.Button("Server")) m_NetworkManager.StartServer();
    }

    void StatusLabels()
    {
        var mode = m_NetworkManager.IsHost ?
            "Host" : m_NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
    
}