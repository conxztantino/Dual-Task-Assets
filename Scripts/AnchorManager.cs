using System.Collections;
using UnityEngine;

public class AnchorManager : MonoBehaviour
{
    [Header("Arraste o GameWorldRoot aqui")]
    public Transform raizDoCenario;
    
    private OVRSpatialAnchor ancoraEspacial;

    void Start()
    {
        StartCoroutine(ConfigurarAncora());
    }

    private IEnumerator ConfigurarAncora()
    {
        // 1. Aguarda 1 segundo para garantir que o tracking de câmeras do Quest 3S esteja ativo
        yield return new WaitForSeconds(1.0f);

        // 2. Trava de segurança: só adiciona o componente se ele ainda NÃO existir
        ancoraEspacial = GetComponent<OVRSpatialAnchor>();
        if (ancoraEspacial == null)
        {
            ancoraEspacial = gameObject.AddComponent<OVRSpatialAnchor>();
            Debug.Log("Criando componente OVRSpatialAnchor...");
        }

        // 3. Aguarda até que o Quest confirme a criação da âncora no mundo real
        while (!ancoraEspacial.Created)
        {
            yield return null; 
        }

        Debug.Log("Âncora criada e posicionada no ambiente!");

        // 4. Coloca o Mundo do Jogo como filho desta âncora travada
        if (raizDoCenario != null)
        {
            raizDoCenario.SetParent(transform, true);
            Debug.Log("Cenário travado na âncora!");
        }
        else
        {
            Debug.LogError("Faltou referenciar o GameWorldRoot no Inspector!");
        }
    }

    // Função pública caso você queira acionar a fixação manualmente via botão
    public void FixarAncoraEspacial()
    {
        if (GetComponent<OVRSpatialAnchor>() == null)
        {
            ancoraEspacial = gameObject.AddComponent<OVRSpatialAnchor>();
            Debug.Log("Âncora criada manualmente via botão!");
        }
    }
}