using System.Collections;
using UnityEngine;

/// <summary>
/// Controlador para os botões de calibração da interface.
/// A calibração espacial deve ocorrer com a pessoa ereta, imóvel e olhando para
/// a direção inicial do percurso. A calibração de apoio dos pés ocorre um frame
/// depois, quando os targets já receberam as novas poses.
/// </summary>
public class SlimeCalibrationController : MonoBehaviour
{
    [SerializeField] private QuestSlimeSpaceAligner spaceAligner;
    [SerializeField] private SlimeTrackerTargetDriver targetDriver;
    [SerializeField] private GaitStepDetector stepDetector;

    [Tooltip("Ative somente quando os targets já estiverem posicionados em uma pose neutra que deve ser preservada como offset.")]
    [SerializeField] private bool captureTargetOffsetsAfterCalibration = false;

    [Tooltip("Calibra automaticamente a altura de apoio dos pés após atualizar os targets.")]
    [SerializeField] private bool calibrateFootSupportAfterCalibration = true;

    public void CalibrateAll()
    {
        if (spaceAligner == null || targetDriver == null)
        {
            Debug.LogError(
                "SlimeCalibrationController: atribua Space Aligner e Target Driver.",
                this);
            return;
        }

        spaceAligner.Calibrate();

        if (!spaceAligner.IsCalibrated)
        {
            return;
        }

        if (captureTargetOffsetsAfterCalibration)
        {
            targetDriver.CaptureTargetOffsets();
        }

        if (calibrateFootSupportAfterCalibration && stepDetector != null)
        {
            StartCoroutine(CalibrateFeetAfterTargetsUpdate());
        }
    }

    public void CalibrateFootSupportOnly()
    {
        if (stepDetector == null)
        {
            Debug.LogError(
                "SlimeCalibrationController: atribua Step Detector.",
                this);
            return;
        }

        stepDetector.CalibrateFloorHeights();
    }

    private IEnumerator CalibrateFeetAfterTargetsUpdate()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        stepDetector.CalibrateFloorHeights();
    }
}
