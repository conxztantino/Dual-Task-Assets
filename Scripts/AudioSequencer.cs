using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSequencer : MonoBehaviour
{
    [Header("Configure a ordem dos áudios aqui:")]
    [SerializeField] private List<AudioSource> audioSources;

    [Header("Intervalo em segundos")]
    [SerializeField] private float intervalo = 30f;

    void Start()
    {
        // Inicia a sequência assim que o jogo começa
        StartCoroutine(TocarAudiosEmSequencia());
    }

    private IEnumerator TocarAudiosEmSequencia()
    {
        // Loop que passa por cada AudioSource adicionado na lista
        for (int i = 0; i < audioSources.Count; i++)
        {
            if (audioSources[i] != null)
            {
                audioSources[i].Play();
                
                // Se for o último áudio, não precisa esperar mais 30 segundos após ele tocar
                if (i < audioSources.Count - 1)
                {
                    yield return new WaitForSeconds(intervalo);
                }
            }
        }
    }
}
