using UnityEngine;

public class EchoArchitectBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void WireScene()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        MicSpectrum mic = Object.FindObjectOfType<MicSpectrum>();
        MonsterAI monster = Object.FindObjectOfType<MonsterAI>();

        if (player != null)
        {
            PlayerNoiseEmitter noiseEmitter = player.GetComponent<PlayerNoiseEmitter>();
            if (noiseEmitter == null)
                noiseEmitter = player.AddComponent<PlayerNoiseEmitter>();

            noiseEmitter.micSpectrum = mic;

            PlayerMove move = player.GetComponent<PlayerMove>();
            if (move != null)
                move.noiseEmitter = noiseEmitter;

            VoiceVisibility visibility = player.GetComponent<VoiceVisibility>();
            if (visibility != null)
            {
                visibility.micSpectrum = mic;
                visibility.noiseEmitter = noiseEmitter;
            }
        }

        if (monster != null)
        {
            if (monster.player == null && player != null)
                monster.player = player.transform;

            if (monster.noiseEmitter == null && player != null)
                monster.noiseEmitter = player.GetComponent<PlayerNoiseEmitter>();
        }
    }
}
