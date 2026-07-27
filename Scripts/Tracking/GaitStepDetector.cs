using System;
using UnityEngine;

public enum GaitSide
{
    Left,
    Right
}

[Serializable]
public struct GaitStepEvent
{
    public GaitSide side;
    public int totalStepIndex;
    public int sideStepIndex;
    public float unityTimeSeconds;
    public Vector3 footWorldPosition;
}

/// <summary>
/// Detector inicial de eventos de passo a partir dos alvos dos pés.
///
/// IMPORTANTE: é uma heurística de engenharia para prototipagem. Não foi validada
/// clinicamente e não deve ser tratada como referência-ouro de contato do pé.
/// Para pesquisa, salve os sinais e valide os eventos contra vídeo, plataforma de
/// força, sensor plantar ou anotação manual.
/// </summary>
public class GaitStepDetector : MonoBehaviour
{
    [Header("Alvos")]
    [SerializeField] private Transform leftFootTarget;
    [SerializeField] private Transform rightFootTarget;

    [Header("Limiar de balanço")]
    [Tooltip("Velocidade mundial do pé que inicia a fase de balanço.")]
    [Min(0.01f)]
    [SerializeField] private float swingStartSpeed = 0.35f;

    [Tooltip("Elevação acima da altura de apoio calibrada que inicia o balanço.")]
    [Min(0f)]
    [SerializeField] private float swingStartHeight = 0.035f;

    [Header("Limiar de contato")]
    [Tooltip("Velocidade abaixo da qual o pé pode ser considerado em apoio.")]
    [Min(0.01f)]
    [SerializeField] private float contactMaximumSpeed = 0.18f;

    [Tooltip("Tolerância de altura acima do nível de apoio calibrado.")]
    [Min(0f)]
    [SerializeField] private float contactHeightTolerance = 0.025f;

    [Tooltip("Duração mínima de balanço antes de aceitar um novo contato.")]
    [Min(0.05f)]
    [SerializeField] private float minimumSwingDuration = 0.15f;

    [Tooltip("Intervalo mínimo entre dois passos do mesmo pé.")]
    [Min(0.1f)]
    [SerializeField] private float sameFootRefractoryTime = 0.35f;

    [Header("Estado somente para leitura")]
    [SerializeField] private int leftSteps;
    [SerializeField] private int rightSteps;
    [SerializeField] private int totalSteps;
    [SerializeField] private bool floorCalibrated;

    private FootState leftState;
    private FootState rightState;

    public event Action<GaitStepEvent> StepDetected;

    public int LeftSteps { get { return leftSteps; } }
    public int RightSteps { get { return rightSteps; } }
    public int TotalSteps { get { return totalSteps; } }

    [Serializable]
    private class FootState
    {
        public bool initialized;
        public bool inSwing;
        public Vector3 previousPosition;
        public float supportHeight;
        public float swingStartTime;
        public float lastStepTime = -1000f;
    }

    private void Awake()
    {
        leftState = new FootState();
        rightState = new FootState();
    }

    private void Update()
    {
        UpdateFoot(GaitSide.Left, leftFootTarget, leftState);
        UpdateFoot(GaitSide.Right, rightFootTarget, rightState);
    }

    /// <summary>
    /// Execute com os dois pés apoiados no chão e os targets já estabilizados.
    /// </summary>
    [ContextMenu("Calibrar altura de apoio dos pés")]
    public void CalibrateFloorHeights()
    {
        if (leftFootTarget == null || rightFootTarget == null)
        {
            Debug.LogError(
                "GaitStepDetector: atribua os targets dos dois pés.",
                this);
            return;
        }

        leftState.supportHeight = leftFootTarget.position.y;
        rightState.supportHeight = rightFootTarget.position.y;
        leftState.previousPosition = leftFootTarget.position;
        rightState.previousPosition = rightFootTarget.position;
        leftState.initialized = true;
        rightState.initialized = true;
        leftState.inSwing = false;
        rightState.inSwing = false;
        floorCalibrated = true;

        Debug.Log("GaitStepDetector: alturas de apoio calibradas.", this);
    }

    [ContextMenu("Zerar contagem de passos")]
    public void ResetCounts()
    {
        leftSteps = 0;
        rightSteps = 0;
        totalSteps = 0;
        leftState.inSwing = false;
        rightState.inSwing = false;
        leftState.lastStepTime = -1000f;
        rightState.lastStepTime = -1000f;
    }

    private void UpdateFoot(GaitSide side, Transform foot, FootState state)
    {
        if (foot == null)
        {
            return;
        }

        Vector3 currentPosition = foot.position;

        if (!state.initialized)
        {
            state.previousPosition = currentPosition;
            state.supportHeight = currentPosition.y;
            state.initialized = true;
            return;
        }

        float deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float speed = Vector3.Distance(currentPosition, state.previousPosition) / deltaTime;
        float elevation = currentPosition.y - state.supportHeight;
        float now = Time.unscaledTime;

        if (!state.inSwing)
        {
            bool startsBySpeed = speed >= swingStartSpeed;
            bool startsByHeight = elevation >= swingStartHeight;

            if ((startsBySpeed || startsByHeight) &&
                now - state.lastStepTime >= sameFootRefractoryTime)
            {
                state.inSwing = true;
                state.swingStartTime = now;
            }
        }
        else
        {
            bool minimumDurationReached =
                now - state.swingStartTime >= minimumSwingDuration;
            bool isLowEnough = elevation <= contactHeightTolerance;
            bool isSlowEnough = speed <= contactMaximumSpeed;

            if (minimumDurationReached && isLowEnough && isSlowEnough)
            {
                state.inSwing = false;
                state.lastStepTime = now;
                RegisterStep(side, currentPosition, now);
            }
        }

        // Atualização lenta da referência de apoio quando o pé está parado.
        if (!state.inSwing && speed < contactMaximumSpeed * 0.5f)
        {
            state.supportHeight = Mathf.Lerp(
                state.supportHeight,
                currentPosition.y,
                1f - Mathf.Exp(-2f * deltaTime));
        }

        state.previousPosition = currentPosition;
    }

    private void RegisterStep(GaitSide side, Vector3 footPosition, float time)
    {
        totalSteps++;

        int sideIndex;
        if (side == GaitSide.Left)
        {
            leftSteps++;
            sideIndex = leftSteps;
        }
        else
        {
            rightSteps++;
            sideIndex = rightSteps;
        }

        GaitStepEvent stepEvent = new GaitStepEvent
        {
            side = side,
            totalStepIndex = totalSteps,
            sideStepIndex = sideIndex,
            unityTimeSeconds = time,
            footWorldPosition = footPosition
        };

        Debug.Log(
            "Passo detectado: " + side +
            " | total=" + totalSteps +
            " | t=" + time.ToString("F3") + " s",
            this);

        Action<GaitStepEvent> handler = StepDetected;
        if (handler != null)
        {
            handler(stepEvent);
        }
    }
}
