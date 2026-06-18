using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform targetCamera; // CenterEyeAnchor
    [SerializeField] private float yOffset = -0.5f;

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // Usa LOCAL position para não gerar conflito com o reposicionamento global do Rig
        Vector3 localTargetPos = targetCamera.localPosition;
        localTargetPos.y += yOffset;
        
        // Zera o deslocamento local em X e Z se você quer que o esqueleto fique sempre centralizado abaixo da cabeça
        localTargetPos.x = targetCamera.localPosition.x;
        localTargetPos.z = targetCamera.localPosition.z;

        transform.localPosition = localTargetPos;

        // Alinha a rotação apenas no eixo Y local
        Vector3 localRotation = targetCamera.localEulerAngles;
        transform.localRotation = Quaternion.Euler(0f, localRotation.y, 0f);
    }
}