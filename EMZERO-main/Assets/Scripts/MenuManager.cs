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

        LevelManager.gameMode = GameMode.Monedas;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void ModoTiempo()
    {
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");

        foreach (var player in allPlayers)
        {
            player.GetComponent<NetworkObject>().Despawn();
        }

        //GameMode gameMode = GameMode.Tiempo;
        LevelManager.gameMode = GameMode.Tiempo;
        if (LevelManager.gameMode == GameMode.Tiempo)
        {
            Debug.Log("El modo tiempo esta activo");
        }
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
