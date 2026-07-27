using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Mostra no Console quais IDs estão chegando, se existe posição e se existe rotação.
/// Use este componente antes de preencher os bindings no Inspector.
/// </summary>
public class SlimeTrackingDiagnostics : MonoBehaviour
{
    [SerializeField] private SlimeOscPoseReceiver receiver;
    [Min(0.1f)]
    [SerializeField] private float reportIntervalSeconds = 1f;

    private readonly List<string> trackerIds = new List<string>();
    private float nextReportTime;

    private void Update()
    {
        if (receiver == null || Time.unscaledTime < nextReportTime)
        {
            return;
        }

        nextReportTime = Time.unscaledTime + reportIntervalSeconds;
        receiver.GetKnownTrackerIds(trackerIds);

        if (trackerIds.Count == 0)
        {
            Debug.LogWarning("SlimeVR: nenhum tracker OSC recebido até agora.", this);
            return;
        }

        StringBuilder report = new StringBuilder("SlimeVR trackers: ");
        for (int i = 0; i < trackerIds.Count; i++)
        {
            SlimeTrackerPose pose;
            receiver.TryGetPose(trackerIds[i], out pose);

            if (i > 0)
            {
                report.Append(" | ");
            }

            report.Append(trackerIds[i]);
            report.Append(" [P:");
            report.Append(pose.hasPosition ? "sim" : "não");
            report.Append(", R:");
            report.Append(pose.hasRotation ? "sim" : "não");
            report.Append("]");
        }

        Debug.Log(report.ToString(), this);
    }
}
