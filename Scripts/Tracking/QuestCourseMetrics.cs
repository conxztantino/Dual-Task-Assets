using UnityEngine;

/// <summary>
/// Extrai métricas geométricas simples da trajetória mundial da cabeça do Quest.
///
/// Não realiza fusão com SlimeVR. O Quest é a única fonte da translação global.
/// Para um percurso de ida e volta, a coordenada longitudinal é projetada no eixo
/// definido no início do ensaio, enquanto a distância horizontal acumulada soma
/// os deslocamentos filtrados no plano do chão.
/// </summary>
public class QuestCourseMetrics : MonoBehaviour
{
    [Header("Referência")]
    [SerializeField] private Transform centerEyeAnchor;

    [Header("Filtragem")]
    [Tooltip("Quanto maior, menor a suavização e menor o atraso.")]
    [Min(0f)]
    [SerializeField] private float positionSharpness = 15f;

    [Tooltip("Incrementos horizontais menores que este valor são ignorados no acumulador para reduzir ruído.")]
    [Min(0f)]
    [SerializeField] private float minimumIncrementMeters = 0.002f;

    [Header("Estado somente para leitura")]
    [SerializeField] private bool running;
    [SerializeField] private Vector3 courseOrigin;
    [SerializeField] private Vector3 courseDirection = Vector3.forward;
    [SerializeField] private float horizontalPathMeters;
    [SerializeField] private float courseCoordinateMeters;
    [SerializeField] private float maximumOutboundCoordinateMeters;
    [SerializeField] private float returnProgressMeters;

    private Vector3 filteredPosition;
    private Vector3 previousFilteredPosition;
    private bool filterInitialized;

    public bool Running { get { return running; } }
    public float HorizontalPathMeters { get { return horizontalPathMeters; } }
    public float CourseCoordinateMeters { get { return courseCoordinateMeters; } }
    public float MaximumOutboundCoordinateMeters { get { return maximumOutboundCoordinateMeters; } }
    public float ReturnProgressMeters { get { return returnProgressMeters; } }
    public Vector3 CourseOrigin { get { return courseOrigin; } }
    public Vector3 CourseDirection { get { return courseDirection; } }

    private void Update()
    {
        if (centerEyeAnchor == null)
        {
            return;
        }

        Vector3 rawPosition = centerEyeAnchor.position;

        if (!filterInitialized)
        {
            filteredPosition = rawPosition;
            previousFilteredPosition = rawPosition;
            filterInitialized = true;
        }
        else
        {
            float alpha = ExponentialAlpha(positionSharpness, Time.unscaledDeltaTime);
            filteredPosition = Vector3.Lerp(filteredPosition, rawPosition, alpha);
        }

        if (!running)
        {
            previousFilteredPosition = filteredPosition;
            return;
        }

        Vector3 horizontalIncrement = filteredPosition - previousFilteredPosition;
        horizontalIncrement.y = 0f;

        float increment = horizontalIncrement.magnitude;
        if (increment >= minimumIncrementMeters)
        {
            horizontalPathMeters += increment;
        }

        Vector3 fromOrigin = filteredPosition - courseOrigin;
        fromOrigin.y = 0f;
        courseCoordinateMeters = Vector3.Dot(fromOrigin, courseDirection);
        maximumOutboundCoordinateMeters = Mathf.Max(
            maximumOutboundCoordinateMeters,
            courseCoordinateMeters);
        returnProgressMeters = Mathf.Max(
            0f,
            maximumOutboundCoordinateMeters - courseCoordinateMeters);

        previousFilteredPosition = filteredPosition;
    }

    /// <summary>
    /// Inicia um ensaio usando a direção horizontal atual do HMD como eixo do percurso.
    /// Execute com a pessoa no ponto inicial, ereta e olhando para o destino da ida.
    /// </summary>
    [ContextMenu("Iniciar percurso na direção atual")]
    public void StartCourse()
    {
        if (centerEyeAnchor == null)
        {
            Debug.LogError("QuestCourseMetrics: atribua Center Eye Anchor.", this);
            return;
        }

        Vector3 forward = centerEyeAnchor.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.000001f)
        {
            forward = Vector3.forward;
        }

        courseDirection = forward.normalized;
        courseOrigin = centerEyeAnchor.position;
        courseOrigin.y = 0f;

        filteredPosition = centerEyeAnchor.position;
        previousFilteredPosition = filteredPosition;
        filterInitialized = true;

        horizontalPathMeters = 0f;
        courseCoordinateMeters = 0f;
        maximumOutboundCoordinateMeters = 0f;
        returnProgressMeters = 0f;
        running = true;

        Debug.Log(
            "QuestCourseMetrics: percurso iniciado. Direção=" + courseDirection,
            this);
    }

    [ContextMenu("Encerrar percurso")]
    public void StopCourse()
    {
        running = false;
        Debug.Log(
            "QuestCourseMetrics: percurso encerrado. Caminho horizontal=" +
            horizontalPathMeters.ToString("F3") + " m; avanço máximo=" +
            maximumOutboundCoordinateMeters.ToString("F3") + " m.",
            this);
    }

    [ContextMenu("Zerar métricas")]
    public void ResetMetrics()
    {
        running = false;
        horizontalPathMeters = 0f;
        courseCoordinateMeters = 0f;
        maximumOutboundCoordinateMeters = 0f;
        returnProgressMeters = 0f;
        filterInitialized = false;
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
