using UnityEngine;

public class ConfigPT : MonoBehaviour
{
    public void passthroughLayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        // Tenta encontrar o componente de Passthrough no próprio Rig
        passthroughLayer = GetComponent<OVRPassthroughLayer>();

        if (passthroughLayer == null)
        {
            // Se não encontrar, adiciona automaticamente para evitar erros
            passthroughLayer = gameObject.AddComponent<OVRPassthroughLayer>();
        }

        // Ativa a oclusão baseada na malha de profundidade do ambiente (Quest 3S)
        passthroughLayer.environmentDepthEnabled = true;
        
        Debug.Log("Profundidade do Passthrough (Environment Depth) ativada via OVRPassthroughLayer!");
    }
}
