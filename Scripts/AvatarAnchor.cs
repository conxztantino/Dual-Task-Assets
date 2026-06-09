using UnityEngine;

public class AvatarAnchor : MonoBehaviour
{
    [Header("Referências do VR")]
    public Transform centroDaCameraVR; // Arraste a 'CenterEyeAnchor' do OVRCameraRig aqui

    [Header("Membro Raiz do Avatar")]
    public Transform quadrilCubo;      // Arraste o cubo do 'Quadril' aqui

    [Header("Ajuste de Altura")]
    public float offsetAlturaChao = 0.0f; // Caso queira subir ou descer o esqueleto

    void Update()
    {
        if (centroDaCameraVR != null && quadrilCubo != null)
        {
            // Captura a posição do óculos no espaço real (X e Z)
            float posX = centroDaCameraVR.position.x;
            float posZ = centroDaCameraVR.position.z;

            // Mantém a altura estável do quadril baseada na altura da cabeça (calculada pelo esqueleto)
            // Se preferir fixar a altura, substitua por um valor fixo (ex: 0.9f)
            float posY = centroDaCameraVR.position.y * 0.55f + offsetAlturaChao; 

            // Aplica o deslocamento global no quadril kinematicamente
            quadrilCubo.position = new Vector3(posX, posY, posZ);
        }
    }
}