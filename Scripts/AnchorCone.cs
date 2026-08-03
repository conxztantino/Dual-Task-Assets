using UnityEngine;
using System.Collections;

public class AnchorCone : MonoBehaviour
{
    private OVRSpatialAnchor anchor;

    void Start()
    {
        StartCoroutine(CriarAnchorComDelay());
    }

    private IEnumerator CriarAnchorComDelay()
    {
        // Espera alguns segundos para garantir que a sessão XR e o
        // mapeamento espacial já estejam totalmente inicializados
        yield return new WaitForSeconds(3f);

        Debug.Log("AnchorCone: tentando criar o Spatial Anchor agora...");

        anchor = gameObject.AddComponent<OVRSpatialAnchor>();

        // Espera até 10 segundos por um resultado (sucesso OU falha).
        // Isso evita que a coroutine trave para sempre se a criação falhar,
        // já que anchor.Created nunca vira true nesse caso.
        float tempoLimite = 10f;
        float tempoDecorrido = 0f;

        while (!anchor.Created && tempoDecorrido < tempoLimite)
        {
            tempoDecorrido += Time.deltaTime;
            yield return null;
        }

        if (anchor.Created)
        {
            Debug.Log("AnchorCone: Spatial Anchor criado com sucesso: " + anchor.Uuid);
        }
        else
        {
            Debug.LogError("AnchorCone: FALHA ao criar o Spatial Anchor (timeout de " + tempoLimite + "s atingido). anchor.Created continua false.");
        }
    }
}