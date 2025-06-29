using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class DetectPlayerCollision : NetworkBehaviour
{
    [SerializeField] private AudioClip pickupSound;

    private void OnTriggerEnter(Collider other)
    {
        // Solo el servidor debe procesar la recogida y sincronizar el resultado
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (player != null && !player.isZombie)
            {
                // Suma a su contador local (opcional si usas contador global)
                player.CoinCollected();

                // Reproduce sonido solo en el servidor (opcional)
                if (pickupSound != null)
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);

                // Despawning correcto de la moneda
                var networkObject = GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Despawn(true); //Elimina en todos los clientes
                }
                else
                {
                    Destroy(gameObject); //Fallback por si no es NetworkObject
                }
            }
        }
    }
}


