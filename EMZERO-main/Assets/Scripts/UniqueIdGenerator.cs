using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Clase para generar identificadores �nicos basados en combinaciones de adjetivos, monstruos y obras.
/// </summary>
public class UniqueIdGenerator : MonoBehaviour
{
    // Listas de adjetivos, monstruos y obras
    private List<string> adjectives = new List<string>
    {
        "Terrible", "Misterioso", "Oscuro", "Poderoso", "Inmortal",
        "Temible", "Fant�stico", "Despiadado", "Legendario", "Siniestro"
    };
    private List<string> monsters = new List<string>
    {
        "Frankenstein", "Dr�cula", "Hombre Lobo", "Momia", "Fantasma",
        "Zombi", "Vampiro", "Bruja", "Espectro", "G�rgola"
    };
    private List<string> works = new List<string>
    {
        "El Mago de Oz", "1984", "La Guerra de los Mundos", "Dune", "El Se�or de los Anillos",
        "Willow", "Blade Runner", "Star Wars", "Matrix", "Jurassic Park"
    };

    // HashSet para almacenar los IDs generados
    private HashSet<string> generatedIDs = new HashSet<string>();

    /// <summary>
    /// M�todo para generar un identificador �nico.
    /// </summary>
    /// <returns>Identificador �nico generado.</returns>
    public string GenerateUniqueID()
    {
        string uniqueID = null;
        bool isUnique = false;
        int attempts = 0;

        while (attempts < 3 && !isUnique)
        {
            uniqueID = GenerateRandomID();

            if (!generatedIDs.Contains(uniqueID))
            {
                isUnique = true;
            }
            attempts++;
        }

        if (!isUnique)
        {
            uniqueID = System.Guid.NewGuid().ToString();
        }

        // A�adir el ID generado al HashSet
        generatedIDs.Add(uniqueID);

        return uniqueID;
    }

    /// <summary>
    /// M�todo para generar un identificador aleatorio basado en combinaciones de adjetivos, monstruos y obras.
    /// </summary>
    /// <returns>Identificador aleatorio generado.</returns>
    private string GenerateRandomID()
    {
        string adjective = adjectives[Random.Range(0, adjectives.Count)];
        string monster = monsters[Random.Range(0, monsters.Count)];
        string work = works[Random.Range(0, works.Count)];
        return $"El {adjective} {monster} de {work}";
    }

    /// <summary>
    /// M�todo para obtener la lista de IDs generados.
    /// </summary>
    /// <returns>Lista de IDs generados.</returns>
    public List<string> GetGeneratedIDs()
    {
        return new List<string>(generatedIDs);
    }
}





