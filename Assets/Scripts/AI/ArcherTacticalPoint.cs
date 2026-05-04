using UnityEngine;

public class ArcherTacticalPoint : MonoBehaviour
{
    // Guarda quién es el arquero que ha reservado este sitio
    public ArcherUnitAI currentOccupant;

    // Función que pregunta: "¿Puedo ir ahí?"
    public bool IsAvailable(ArcherUnitAI requester)
    {
        // Está libre si no hay ocupante (o si el ocupante anterior murió/se destruyó)
        if (currentOccupant == null) return true;

        // También está libre si el ocupante actual... ¡somos nosotros mismos!
        return currentOccupant == requester;
    }
}