using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MenuManager : NetworkBehaviour
{
    public void Awake()
    {
        Time.timeScale = 1f; // Asegúrate de que el tiempo está restaurado al cargar la escena
    }

    public void StartGame()
    {
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in allPlayers)
        {
            player.GetComponent<NetworkObject>().Despawn();
        }

        GameMode gameMode = GameMode.Monedas;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void ModoTiempo()
    {
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in allPlayers)
        {
            player.GetComponent<NetworkObject>().Despawn();
        }

        GameMode gameMode = GameMode.Tiempo;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Salir en el editor
#else
            Application.Quit(); // Salir en una build
#endif
    }
}
