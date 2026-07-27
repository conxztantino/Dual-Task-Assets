using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using uOSC;

/// <summary>
/// Pose mais recente recebida para um tracker virtual do SlimeVR.
/// O protocolo OSC usado no projeto transmite posição e rotação em mensagens separadas.
/// </summary>
[Serializable]
public struct SlimeTrackerPose
{
    public Vector3 position;
    public Quaternion rotation;
    public bool hasPosition;
    public bool hasRotation;
    public float lastPositionTime;
    public float lastRotationTime;
}

/// <summary>
/// Recebe as mensagens /tracking/trackers/{id}/position e /rotation.
///
/// Responsabilidade única: interpretar OSC e guardar a pose mais recente.
/// Este script NÃO altera Transforms da cena. Isso evita misturar comunicação,
/// alinhamento de referenciais e animação em uma única classe.
/// </summary>
public class SlimeOscPoseReceiver : MonoBehaviour
{
    [Header("Diagnóstico")]
    [SerializeField] private bool logFirstMessageFromEachTracker = true;
    [SerializeField] private bool logMalformedMessages = true;

    private readonly Dictionary<string, SlimeTrackerPose> poses =
        new Dictionary<string, SlimeTrackerPose>();

    private readonly HashSet<string> announcedTrackers =
        new HashSet<string>();

    /// <summary>
    /// Conecte este método ao evento On Data Received do componente uOscServer.
    /// </summary>
    public void OnDataReceived(Message message)
    {
        if (string.IsNullOrEmpty(message.address) ||
            message.values == null ||
            message.values.Length < 3)
        {
            LogMalformed("Mensagem vazia ou com menos de três valores.", message.address);
            return;
        }

        string trackerId;
        string dataKind;
        if (!TryParseAddress(message.address, out trackerId, out dataKind))
        {
            // Outros endereços OSC podem existir no mesmo servidor; por isso apenas ignoramos.
            return;
        }

        Vector3 value;
        if (!TryReadVector3(message.values, out value))
        {
            LogMalformed("Os três valores não puderam ser convertidos para float.", message.address);
            return;
        }

        SlimeTrackerPose pose;
        if (!poses.TryGetValue(trackerId, out pose))
        {
            pose = new SlimeTrackerPose
            {
                rotation = Quaternion.identity
            };
        }

        if (dataKind == "position")
        {
            pose.position = value;
            pose.hasPosition = true;
            pose.lastPositionTime = Time.unscaledTime;
        }
        else // rotation
        {
            // O protocolo dos OSC Trackers usa graus e ordem Z-X-Y.
            // Quaternion.Euler(x,y,z) no Unity usa essa mesma ordem internamente.
            pose.rotation = Quaternion.Euler(value.x, value.y, value.z);
            pose.hasRotation = true;
            pose.lastRotationTime = Time.unscaledTime;
        }

        poses[trackerId] = pose;

        if (logFirstMessageFromEachTracker && announcedTrackers.Add(trackerId))
        {
            Debug.Log(
                "SlimeVR: primeiro dado recebido do tracker '" + trackerId +
                "'. Endereço: " + message.address,
                this);
        }
    }

    public bool TryGetPose(string trackerId, out SlimeTrackerPose pose)
    {
        return poses.TryGetValue(trackerId, out pose);
    }

    public bool IsFresh(string trackerId, float maximumAgeSeconds)
    {
        SlimeTrackerPose pose;
        if (!poses.TryGetValue(trackerId, out pose))
        {
            return false;
        }

        float newestTimestamp = Mathf.Max(pose.lastPositionTime, pose.lastRotationTime);
        return Time.unscaledTime - newestTimestamp <= maximumAgeSeconds;
    }

    public void GetKnownTrackerIds(List<string> destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        destination.Clear();
        foreach (string trackerId in poses.Keys)
        {
            destination.Add(trackerId);
        }

        destination.Sort(StringComparer.Ordinal);
    }

    private static bool TryParseAddress(
        string address,
        out string trackerId,
        out string dataKind)
    {
        trackerId = null;
        dataKind = null;

        // Uma divisão exata impede que o ID "1" seja confundido com "10".
        // Exemplo válido: /tracking/trackers/5/rotation
        string[] parts = address.Split('/');
        if (parts.Length != 5 ||
            parts[0] != string.Empty ||
            parts[1] != "tracking" ||
            parts[2] != "trackers")
        {
            return false;
        }

        if (string.IsNullOrEmpty(parts[3]))
        {
            return false;
        }

        if (parts[4] != "position" && parts[4] != "rotation")
        {
            return false;
        }

        trackerId = parts[3];
        dataKind = parts[4];
        return true;
    }

    private static bool TryReadVector3(object[] values, out Vector3 result)
    {
        result = Vector3.zero;

        try
        {
            float x = Convert.ToSingle(values[0], CultureInfo.InvariantCulture);
            float y = Convert.ToSingle(values[1], CultureInfo.InvariantCulture);
            float z = Convert.ToSingle(values[2], CultureInfo.InvariantCulture);

            if (float.IsNaN(x) || float.IsInfinity(x) ||
                float.IsNaN(y) || float.IsInfinity(y) ||
                float.IsNaN(z) || float.IsInfinity(z))
            {
                return false;
            }

            result = new Vector3(x, y, z);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void LogMalformed(string reason, string address)
    {
        if (!logMalformedMessages)
        {
            return;
        }

        Debug.LogWarning(
            "SlimeVR: " + reason + " Endereço: '" + address + "'.",
            this);
    }
}
