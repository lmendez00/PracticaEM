using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    public void CrearPartida()
    {
        m_NetworkManager.StartHost();

        //SceneManager.LoadScene("MenuScene");
        m_NetworkManager.SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
    }
    

    public void EntrarPartida()
    {
        m_NetworkManager.StartClient();
    }
}