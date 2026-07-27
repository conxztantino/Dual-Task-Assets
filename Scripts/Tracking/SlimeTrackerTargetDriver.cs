using System;
using UnityEngine;

[Serializable]
public class SlimeTrackerBinding
{
    [Tooltip("ID presente no endereço OSC, por exemplo 1, 2, 5, 6 ou head.")]
    public string trackerId;

    [Tooltip("Transform alvo, normalmente um objeto vazio dentro de TrackerTargets.")]
    public Transform target;

    public bool applyPosition = true;
    public bool applyRotation = true;

    [Tooltip("Offset manual adicional no espaço local do tracker.")]
    public Vector3 manualPositionOffset;

    [Tooltip("Correção dos eixos do objeto/modelo, em graus.")]
    public Vector3 manualRotationOffsetEuler;

    [HideInInspector] public Quaternion capturedRotationOffset = Quaternion.identity;
    [HideInInspector] public Vector3 capturedPositionOffset = Vector3.zero;
    [HideInInspector] public bool hasCapturedOffset;
}

/// <summary>
/// Aplica as poses recebidas a objetos-alvo no espaço mundial.
///
/// Recomendação: os alvos devem ser objetos vazios de depuração/IK, não diretamente
/// os ossos de um modelo final. O avatar visual deve seguir esses alvos por constraints.
/// </summary>
public class SlimeTrackerTargetDriver : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SlimeOscPoseReceiver receiver;
    [SerializeField] private QuestSlimeSpaceAligner spaceAligner;
    [SerializeField] private SlimeTrackerBinding[] bindings;

    [Header("Filtragem")]
    [Tooltip("Quanto maior, menor a suavização e menor o atraso.")]
    [Min(0f)]
    [SerializeField] private float positionSharpness = 20f;

    [Tooltip("Quanto maior, menor a suavização e menor o atraso.")]
    [Min(0f)]
    [SerializeField] private float rotationSharpness = 25f;

    [Tooltip("Após esse tempo sem dados, o alvo deixa de ser atualizado.")]
    [Min(0.01f)]
    [SerializeField] private float staleTimeoutSeconds = 0.5f;

    private void LateUpdate()
    {
        if (receiver == null || spaceAligner == null || bindings == null)
        {
            return;
        }

        float positionAlpha = ExponentialAlpha(positionSharpness, Time.unscaledDeltaTime);
        float rotationAlpha = ExponentialAlpha(rotationSharpness, Time.unscaledDeltaTime);

        for (int i = 0; i < bindings.Length; i++)
        {
            SlimeTrackerBinding binding = bindings[i];
            if (binding == null ||
                binding.target == null ||
                string.IsNullOrEmpty(binding.trackerId))
            {
                continue;
            }

            SlimeTrackerPose pose;
            if (!receiver.TryGetPose(binding.trackerId, out pose))
            {
                continue;
            }

            float newestTimestamp = Mathf.Max(pose.lastPositionTime, pose.lastRotationTime);
            if (Time.unscaledTime - newestTimestamp > staleTimeoutSeconds)
            {
                continue;
            }

            Quaternion trackerWorldRotation = pose.hasRotation
                ? spaceAligner.ToWorldRotation(pose.rotation)
                : binding.target.rotation;

            Quaternion desiredRotation =
                trackerWorldRotation *
                Quaternion.Euler(binding.manualRotationOffsetEuler);

            if (binding.hasCapturedOffset)
            {
                desiredRotation = desiredRotation * binding.capturedRotationOffset;
            }

            if (binding.applyPosition && pose.hasPosition)
            {
                Vector3 desiredPosition = spaceAligner.ToWorldPosition(pose.position);

                Vector3 totalLocalOffset = binding.manualPositionOffset;
                if (binding.hasCapturedOffset)
                {
                    totalLocalOffset += binding.capturedPositionOffset;
                }

                desiredPosition += trackerWorldRotation * totalLocalOffset;

                binding.target.position = positionSharpness <= 0f
                    ? desiredPosition
                    : Vector3.Lerp(binding.target.position, desiredPosition, positionAlpha);
            }

            if (binding.applyRotation && pose.hasRotation)
            {
                binding.target.rotation = rotationSharpness <= 0f
                    ? desiredRotation
                    : Quaternion.Slerp(binding.target.rotation, desiredRotation, rotationAlpha);
            }
        }
    }

    /// <summary>
    /// Preserva a orientação/posição atual de cada alvo como offset em relação ao tracker.
    /// Execute depois de calibrar o espaço, com a pessoa e o avatar em pose neutra.
    /// </summary>
    [ContextMenu("Capturar offsets dos alvos")]
    public void CaptureTargetOffsets()
    {
        if (receiver == null || spaceAligner == null || !spaceAligner.IsCalibrated)
        {
            Debug.LogError(
                "SlimeTrackerTargetDriver: calibre o QuestSlimeSpaceAligner antes de capturar offsets.",
                this);
            return;
        }

        if (bindings == null)
        {
            return;
        }

        int capturedCount = 0;

        for (int i = 0; i < bindings.Length; i++)
        {
            SlimeTrackerBinding binding = bindings[i];
            if (binding == null || binding.target == null)
            {
                continue;
            }

            SlimeTrackerPose pose;
            if (!receiver.TryGetPose(binding.trackerId, out pose) || !pose.hasRotation)
            {
                continue;
            }

            Quaternion trackerWorldRotation = spaceAligner.ToWorldRotation(pose.rotation);
            Quaternion manualRotation = Quaternion.Euler(binding.manualRotationOffsetEuler);
            Quaternion rotationBeforeCapturedOffset = trackerWorldRotation * manualRotation;

            binding.capturedRotationOffset =
                Quaternion.Inverse(rotationBeforeCapturedOffset) * binding.target.rotation;

            if (pose.hasPosition)
            {
                Vector3 trackerWorldPosition = spaceAligner.ToWorldPosition(pose.position);
                binding.capturedPositionOffset =
                    Quaternion.Inverse(trackerWorldRotation) *
                    (binding.target.position - trackerWorldPosition) -
                    binding.manualPositionOffset;
            }
            else
            {
                binding.capturedPositionOffset = Vector3.zero;
            }

            binding.hasCapturedOffset = true;
            capturedCount++;
        }

        Debug.Log(
            "SlimeTrackerTargetDriver: offsets capturados para " + capturedCount + " alvos.",
            this);
    }

    [ContextMenu("Limpar offsets capturados")]
    public void ClearCapturedOffsets()
    {
        if (bindings == null)
        {
            return;
        }

        for (int i = 0; i < bindings.Length; i++)
        {
            SlimeTrackerBinding binding = bindings[i];
            if (binding == null)
            {
                continue;
            }

            binding.capturedRotationOffset = Quaternion.identity;
            binding.capturedPositionOffset = Vector3.zero;
            binding.hasCapturedOffset = false;
        }
    }

    private static float ExponentialAlpha(float sharpness, float deltaTime)
    {
        if (sharpness <= 0f)
        {
            return 1f;
        }

        return 1f - Mathf.Exp(-sharpness * Mathf.Max(0f, deltaTime));
    }
}
