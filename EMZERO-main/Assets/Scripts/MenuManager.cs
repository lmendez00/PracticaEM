using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MenuManager : NetworkBehaviour
{
    public void Awake()
    {
        Time.timeScale = 1f; // Aseg�rate de que el tiempo est� restaurado al cargar la escena
    }

    public void StartGame()
    {
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");

        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count <= 1)
        {
            //Bloquea el inicio si s�lo est� el host
            Debug.Log("Debes esperar a que al menos un cliente se conecte antes de empezar");
            return;
        }

        foreach (var player in allPlayers)
        {
            player.GetComponent<NetworkObject>().Despawn();
        }

        GameManager.Instance.CurrentGameMode.Value = GameMode.Monedas;
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void ModoTiempo()
    {
        var allPlayers = GameObject.FindGameObjectsWithTag("Player");

        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count <= 1)
        {
            //Bloquea el inicio si s�lo est� el host
            Debug.Log("Debes esperar a que al menos un cliente se conecte antes de empezar");
            return;
        }
            foreach (var player in allPlayers)
        {
            player.GetComponent<NetworkObject>().Despawn();
        }

        //GameMode gameMode = GameMode.Tiempo;
        GameManager.Instance.CurrentGameMode.Value = GameMode.Tiempo;
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
