using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectPlayerCollision : MonoBehaviour
{
    [SerializeField] private AudioClip pickupSound; // Sonido al recoger la moneda

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // Verifica si el jugador tocó la moneda
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.isZombie) // Verifica si el jugador no es un zombie
            {
                player.CoinCollected();
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                Destroy(gameObject); // Elimina la moneda de la escena
            }
        }
    }
}
/*
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null && !player.isZombie)
            {
                GameManager.Instance.CollectCoinServerRpc(player.OwnerClientId);

                //Destruir la moneda para todos
                NetworkObject networkObject = GetComponent<NetworkObject>();
                if (networkObject != null)
                    networkObject.Despawn(true);
            }
        }
    }
}
*/
