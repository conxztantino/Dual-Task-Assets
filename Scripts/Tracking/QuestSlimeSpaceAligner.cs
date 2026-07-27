using UnityEngine;

/// <summary>
/// Define quem é responsável pela translação global do corpo.
/// </summary>
public enum SlimePositionPolicy
{
    /// <summary>
    /// Recomendado para a aplicação própria no Quest.
    /// O Quest fornece a trajetória global. As posições do SlimeVR são usadas
    /// apenas relativamente ao tracker de referência (normalmente o quadril).
    /// Dessa forma, qualquer translação global presente no OSC é cancelada.
    /// </summary>
    QuestOwnsGlobalTranslation,

    /// <summary>
    /// Use somente depois de confirmar que o servidor SlimeVR já recebe a pose
    /// do mesmo HMD e que as posições OSC já estão no referencial global correto.
    /// Aplica apenas a transformação rígida obtida na calibração.
    /// </summary>
    SlimeAlreadyAnchoredToQuest,

    /// <summary>
    /// Usa /tracking/trackers/head/position como ponto correspondente à cabeça
    /// do Quest e faz uma translação rígida a cada frame. Requer que o SlimeVR
    /// realmente envie o tracker virtual "head".
    /// </summary>
    MatchSlimeHeadPositionToQuest,

    /// <summary>
    /// Não transforma os dados. Serve somente para diagnóstico visual.
    /// </summary>
    RawSlimeDiagnostic
}

/// <summary>
/// Converte poses do espaço SlimeVR para o mundo do Unity sem tratar SlimeVR e
/// Quest como duas odometrias independentes.
///
/// Política recomendada:
/// - Quest: posição global no percurso;
/// - SlimeVR: rotações e geometria relativa dos segmentos;
/// - tracker de referência: quadril/pelve.
///
/// O AvatarWorldRoot e os TrackerTargets NÃO devem ser filhos de TrackingSpace
/// nem de CenterEyeAnchor.
/// </summary>
public class QuestSlimeSpaceAligner : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SlimeOscPoseReceiver receiver;
    [SerializeField] private Transform centerEyeAnchor;

    [Header("Política de posição")]
    [SerializeField] private SlimePositionPolicy positionPolicy =
        SlimePositionPolicy.QuestOwnsGlobalTranslation;

    [Tooltip("Tracker usado como raiz relativa. Normalmente o quadril/pelve.")]
    [SerializeField] private string referenceTrackerId = "5";

    [Tooltip("Tracker virtual da cabeça, quando disponível no OSC.")]
    [SerializeField] private string slimeHeadTrackerId = "head";

    [Header("Relação cabeça-quadril")]
    [Tooltip("Posição aproximada do tracker de referência em relação aos olhos, na pose neutra. X=lateral, Y=vertical, Z=frente.")]
    [SerializeField] private Vector3 referenceOffsetFromQuestHead =
        new Vector3(0f, -0.90f, 0.03f);

    [Tooltip("Quanto da variação vertical da cabeça será aplicada à raiz do corpo. 1 acompanha agachamentos; 0 mantém a altura calibrada.")]
    [Range(0f, 1f)]
    [SerializeField] private float questVerticalMotionWeight = 1f;

    [Tooltip("Suavização da trajetória global fornecida pelo Quest. Zero desativa.")]
    [Min(0f)]
    [SerializeField] private float questAnchorSharpness = 20f;

    [Header("Alinhamento de orientação")]
    [Tooltip("No modo de correspondência pela cabeça, atualizar também o yaw continuamente. Normalmente deixe desativado para não transformar giro da cabeça em giro do corpo.")]
    [SerializeField] private bool continuouslyMatchHeadYaw = false;

    [Header("Estado somente para leitura")]
    [SerializeField] private bool calibrated;

    private Quaternion sourceToWorldRotation = Quaternion.identity;
    private Vector3 sourceToWorldTranslation = Vector3.zero;
    private Vector3 questHeadPositionAtCalibration;
    private Vector3 filteredQuestHeadPosition;
    private bool filteredQuestInitialized;

    public bool IsCalibrated
    {
        get { return calibrated; }
    }

    public SlimePositionPolicy PositionPolicy
    {
        get { return positionPolicy; }
        set { positionPolicy = value; }
    }

    public string ReferenceTrackerId
    {
        get { return referenceTrackerId; }
    }

    private void LateUpdate()
    {
        if (centerEyeAnchor == null)
        {
            return;
        }

        if (!filteredQuestInitialized)
        {
            filteredQuestHeadPosition = centerEyeAnchor.position;
            filteredQuestInitialized = true;
            return;
        }

        float alpha = ExponentialAlpha(questAnchorSharpness, Time.unscaledDeltaTime);
        filteredQuestHeadPosition = Vector3.Lerp(
            filteredQuestHeadPosition,
            centerEyeAnchor.position,
            alpha);
    }

    /// <summary>
    /// Execute com a pessoa ereta, olhando para a direção inicial do percurso,
    /// após a calibração e o reset do SlimeVR.
    /// </summary>
    [ContextMenu("Calibrar espaço Quest-SlimeVR")]
    public void Calibrate()
    {
        if (receiver == null || centerEyeAnchor == null)
        {
            Debug.LogError(
                "QuestSlimeSpaceAligner: atribua Receiver e Center Eye Anchor.",
                this);
            return;
        }

        SlimeTrackerPose referencePose;
        if (!receiver.TryGetPose(referenceTrackerId, out referencePose) ||
            !referencePose.hasPosition ||
            !referencePose.hasRotation)
        {
            Debug.LogError(
                "QuestSlimeSpaceAligner: o tracker de referência '" +
                referenceTrackerId +
                "' precisa ter posição e rotação antes da calibração.",
                this);
            return;
        }

        questHeadPositionAtCalibration = centerEyeAnchor.position;
        filteredQuestHeadPosition = centerEyeAnchor.position;
        filteredQuestInitialized = true;

        Quaternion questYaw = ExtractYaw(centerEyeAnchor.rotation);
        Quaternion sourceReferenceYaw = ExtractYaw(referencePose.rotation);

        sourceToWorldRotation =
            questYaw * Quaternion.Inverse(sourceReferenceYaw);

        Quaternion referenceWorldYaw =
            sourceToWorldRotation * sourceReferenceYaw;

        Vector3 expectedReferenceWorldPosition =
            ComputeQuestReferenceAnchor(referenceWorldYaw);

        sourceToWorldTranslation =
            expectedReferenceWorldPosition -
            sourceToWorldRotation * referencePose.position;

        calibrated = true;

        Debug.Log(
            "QuestSlimeSpaceAligner: calibração concluída. Política: " +
            positionPolicy + ". Referência: '" + referenceTrackerId + "'.",
            this);
    }

    public Vector3 ToWorldPosition(Vector3 sourcePosition)
    {
        if (!calibrated || positionPolicy == SlimePositionPolicy.RawSlimeDiagnostic)
        {
            return sourcePosition;
        }

        if (positionPolicy == SlimePositionPolicy.SlimeAlreadyAnchoredToQuest)
        {
            return sourceToWorldTranslation + sourceToWorldRotation * sourcePosition;
        }

        if (positionPolicy == SlimePositionPolicy.MatchSlimeHeadPositionToQuest)
        {
            SlimeTrackerPose headPose;
            if (receiver != null &&
                receiver.TryGetPose(slimeHeadTrackerId, out headPose) &&
                headPose.hasPosition)
            {
                Quaternion currentRotation = ComputeCurrentSourceToWorldRotation(headPose);
                Vector3 questHead = GetFilteredQuestHeadPosition();
                Vector3 translation = questHead - currentRotation * headPose.position;
                return translation + currentRotation * sourcePosition;
            }

            // Fallback seguro: não inventar uma segunda odometria.
            return QuestAnchoredRelativePosition(sourcePosition);
        }

        // Política padrão: QuestOwnsGlobalTranslation.
        return QuestAnchoredRelativePosition(sourcePosition);
    }

    public Quaternion ToWorldRotation(Quaternion sourceRotation)
    {
        if (!calibrated || positionPolicy == SlimePositionPolicy.RawSlimeDiagnostic)
        {
            return sourceRotation;
        }

        if (positionPolicy == SlimePositionPolicy.MatchSlimeHeadPositionToQuest &&
            continuouslyMatchHeadYaw)
        {
            SlimeTrackerPose headPose;
            if (receiver != null &&
                receiver.TryGetPose(slimeHeadTrackerId, out headPose) &&
                headPose.hasRotation)
            {
                return ComputeCurrentSourceToWorldRotation(headPose) * sourceRotation;
            }
        }

        return sourceToWorldRotation * sourceRotation;
    }

    private Vector3 QuestAnchoredRelativePosition(Vector3 sourcePosition)
    {
        if (receiver == null || centerEyeAnchor == null)
        {
            return sourceToWorldTranslation + sourceToWorldRotation * sourcePosition;
        }

        SlimeTrackerPose referencePose;
        if (!receiver.TryGetPose(referenceTrackerId, out referencePose) ||
            !referencePose.hasPosition)
        {
            return sourceToWorldTranslation + sourceToWorldRotation * sourcePosition;
        }

        Quaternion referenceWorldYaw = referencePose.hasRotation
            ? ExtractYaw(sourceToWorldRotation * referencePose.rotation)
            : ExtractYaw(centerEyeAnchor.rotation);

        Vector3 referenceAnchorWorld =
            ComputeQuestReferenceAnchor(referenceWorldYaw);

        // A subtração da posição atual da referência cancela qualquer translação
        // global existente no fluxo SlimeVR. Permanecem apenas as posições relativas
        // entre os trackers, produzidas pela cinemática do esqueleto.
        Vector3 relativeToReference = sourcePosition - referencePose.position;

        return referenceAnchorWorld +
               sourceToWorldRotation * relativeToReference;
    }

    private Vector3 ComputeQuestReferenceAnchor(Quaternion referenceWorldYaw)
    {
        Vector3 questHead = GetFilteredQuestHeadPosition();

        float verticalDelta =
            questHead.y - questHeadPositionAtCalibration.y;

        questHead.y =
            questHeadPositionAtCalibration.y +
            questVerticalMotionWeight * verticalDelta;

        Vector3 horizontalOffset = new Vector3(
            referenceOffsetFromQuestHead.x,
            0f,
            referenceOffsetFromQuestHead.z);

        return questHead +
               referenceWorldYaw * horizontalOffset +
               Vector3.up * referenceOffsetFromQuestHead.y;
    }

    private Quaternion ComputeCurrentSourceToWorldRotation(SlimeTrackerPose headPose)
    {
        if (!continuouslyMatchHeadYaw || centerEyeAnchor == null || !headPose.hasRotation)
        {
            return sourceToWorldRotation;
        }

        Quaternion questYaw = ExtractYaw(centerEyeAnchor.rotation);
        Quaternion slimeHeadYaw = ExtractYaw(headPose.rotation);
        return questYaw * Quaternion.Inverse(slimeHeadYaw);
    }

    private Vector3 GetFilteredQuestHeadPosition()
    {
        if (filteredQuestInitialized)
        {
            return filteredQuestHeadPosition;
        }

        return centerEyeAnchor != null
            ? centerEyeAnchor.position
            : Vector3.zero;
    }

    private static Quaternion ExtractYaw(Quaternion rotation)
    {
        Vector3 forward = rotation * Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.000001f)
        {
            return Quaternion.identity;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
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
