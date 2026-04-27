using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!EchoArchitectGameState.IsGameplayActive)
            return;

        if (!other.CompareTag("Player"))
            return;

        EchoArchitectGameState.SetEscaped();
    }
}
