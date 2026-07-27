using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Registra, no mesmo relógio e no mesmo índice de amostra:
/// - pose mundial da cabeça do Quest;
/// - pose bruta recebida do SlimeVR;
/// - pose SlimeVR após a política de alinhamento;
/// - métricas simples do percurso;
/// - contagem provisória de passos.
///
/// O formato longo usa uma linha QUEST_HEAD e uma linha por tracker em cada
/// sample_index. Isso facilita sincronização e processamento posterior.
/// </summary>
public class QuestSlimeSynchronizedCsvLogger : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SlimeOscPoseReceiver receiver;
    [SerializeField] private QuestSlimeSpaceAligner spaceAligner;
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private GaitStepDetector stepDetector;
    [SerializeField] private QuestCourseMetrics courseMetrics;

    [Header("Registro")]
    [Tooltip("IDs a registrar. Vazio registra todos os IDs conhecidos.")]
    [SerializeField] private string[] trackerIds;

    [Range(1f, 120f)]
    [SerializeField] private float samplesPerSecond = 30f;

    [Min(0.1f)]
    [SerializeField] private float flushIntervalSeconds = 1f;

    [SerializeField] private string filePrefix = "Quest_SlimeVR_Synchronized";
    [SerializeField] private bool startLoggingOnEnable = false;

    private readonly List<string> knownIds = new List<string>();
    private readonly StringBuilder rowBuilder = new StringBuilder(768);
    private StreamWriter writer;
    private float nextSampleTime;
    private float nextFlushTime;
    private long sampleIndex;
    private string currentFilePath;

    public string CurrentFilePath { get { return currentFilePath; } }
    public bool IsLogging { get { return writer != null; } }

    private void OnEnable()
    {
        if (startLoggingOnEnable)
        {
            StartLogging();
        }
    }

    private void OnDisable()
    {
        StopLogging();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && writer != null)
        {
            writer.Flush();
        }
    }

    private void Update()
    {
        if (writer == null || Time.unscaledTime < nextSampleTime)
        {
            return;
        }

        nextSampleTime = Time.unscaledTime +
                         1f / Mathf.Max(1f, samplesPerSecond);

        WriteSynchronizedSample();

        if (Time.unscaledTime >= nextFlushTime)
        {
            writer.Flush();
            nextFlushTime = Time.unscaledTime + flushIntervalSeconds;
        }
    }

    [ContextMenu("Iniciar registro sincronizado")]
    public void StartLogging()
    {
        if (writer != null)
        {
            return;
        }

        if (receiver == null || centerEyeAnchor == null)
        {
            Debug.LogError(
                "QuestSlimeSynchronizedCsvLogger: atribua Receiver e Center Eye Anchor.",
                this);
            return;
        }

        Directory.CreateDirectory(Application.persistentDataPath);

        string timestamp = DateTime.UtcNow.ToString(
            "yyyyMMdd_HHmmss_UTC",
            CultureInfo.InvariantCulture);

        currentFilePath = Path.Combine(
            Application.persistentDataPath,
            filePrefix + "_" + timestamp + ".csv");

        writer = new StreamWriter(currentFilePath, false, Encoding.UTF8);
        writer.WriteLine(
            "sample_index;unity_time_s;utc_iso;record_type;tracker_id;" +
            "raw_has_position;raw_pos_x_m;raw_pos_y_m;raw_pos_z_m;" +
            "raw_has_rotation;raw_rot_x;raw_rot_y;raw_rot_z;raw_rot_w;" +
            "aligned_pos_x_m;aligned_pos_y_m;aligned_pos_z_m;" +
            "aligned_rot_x;aligned_rot_y;aligned_rot_z;aligned_rot_w;" +
            "quest_pos_x_m;quest_pos_y_m;quest_pos_z_m;" +
            "quest_rot_x;quest_rot_y;quest_rot_z;quest_rot_w;" +
            "position_age_s;rotation_age_s;left_steps;right_steps;total_steps;" +
            "quest_horizontal_path_m;quest_course_coordinate_m;" +
            "quest_max_outbound_m;quest_return_progress_m;position_policy");

        sampleIndex = 0;
        nextSampleTime = Time.unscaledTime;
        nextFlushTime = Time.unscaledTime + flushIntervalSeconds;

        Debug.Log(
            "Registro Quest+SlimeVR iniciado em: " + currentFilePath,
            this);
    }

    [ContextMenu("Encerrar registro sincronizado")]
    public void StopLogging()
    {
        if (writer == null)
        {
            return;
        }

        writer.Flush();
        writer.Dispose();
        writer = null;

        Debug.Log(
            "Registro Quest+SlimeVR encerrado: " + currentFilePath,
            this);
    }

    private void WriteSynchronizedSample()
    {
        sampleIndex++;
        float unityTime = Time.unscaledTime;
        string utcIso = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        Vector3 questPosition = centerEyeAnchor.position;
        Quaternion questRotation = centerEyeAnchor.rotation;

        int leftSteps = stepDetector != null ? stepDetector.LeftSteps : 0;
        int rightSteps = stepDetector != null ? stepDetector.RightSteps : 0;
        int totalSteps = stepDetector != null ? stepDetector.TotalSteps : 0;

        float horizontalPath = courseMetrics != null
            ? courseMetrics.HorizontalPathMeters
            : 0f;
        float courseCoordinate = courseMetrics != null
            ? courseMetrics.CourseCoordinateMeters
            : 0f;
        float maximumOutbound = courseMetrics != null
            ? courseMetrics.MaximumOutboundCoordinateMeters
            : 0f;
        float returnProgress = courseMetrics != null
            ? courseMetrics.ReturnProgressMeters
            : 0f;

        string policyName = spaceAligner != null
            ? spaceAligner.PositionPolicy.ToString()
            : "None";

        WriteQuestRow(
            unityTime,
            utcIso,
            questPosition,
            questRotation,
            leftSteps,
            rightSteps,
            totalSteps,
            horizontalPath,
            courseCoordinate,
            maximumOutbound,
            returnProgress,
            policyName);

        if (trackerIds != null && trackerIds.Length > 0)
        {
            for (int i = 0; i < trackerIds.Length; i++)
            {
                WriteTrackerRow(
                    trackerIds[i],
                    unityTime,
                    utcIso,
                    questPosition,
                    questRotation,
                    leftSteps,
                    rightSteps,
                    totalSteps,
                    horizontalPath,
                    courseCoordinate,
                    maximumOutbound,
                    returnProgress,
                    policyName);
            }
        }
        else
        {
            receiver.GetKnownTrackerIds(knownIds);
            for (int i = 0; i < knownIds.Count; i++)
            {
                WriteTrackerRow(
                    knownIds[i],
                    unityTime,
                    utcIso,
                    questPosition,
                    questRotation,
                    leftSteps,
                    rightSteps,
                    totalSteps,
                    horizontalPath,
                    courseCoordinate,
                    maximumOutbound,
                    returnProgress,
                    policyName);
            }
        }
    }

    private void WriteQuestRow(
        float unityTime,
        string utcIso,
        Vector3 questPosition,
        Quaternion questRotation,
        int leftSteps,
        int rightSteps,
        int totalSteps,
        float horizontalPath,
        float courseCoordinate,
        float maximumOutbound,
        float returnProgress,
        string policyName)
    {
        BeginRow(unityTime, utcIso, "QUEST_HEAD", "head");

        AppendInt(1);
        AppendVector3(questPosition);
        AppendInt(1);
        AppendQuaternion(questRotation);

        AppendVector3(questPosition);
        AppendQuaternion(questRotation);
        AppendVector3(questPosition);
        AppendQuaternion(questRotation);

        AppendFloat(0f);
        AppendFloat(0f);
        AppendInt(leftSteps);
        AppendInt(rightSteps);
        AppendInt(totalSteps);
        AppendFloat(horizontalPath);
        AppendFloat(courseCoordinate);
        AppendFloat(maximumOutbound);
        AppendFloat(returnProgress);
        AppendText(policyName, false);

        writer.WriteLine(rowBuilder.ToString());
    }

    private void WriteTrackerRow(
        string trackerId,
        float unityTime,
        string utcIso,
        Vector3 questPosition,
        Quaternion questRotation,
        int leftSteps,
        int rightSteps,
        int totalSteps,
        float horizontalPath,
        float courseCoordinate,
        float maximumOutbound,
        float returnProgress,
        string policyName)
    {
        if (string.IsNullOrEmpty(trackerId))
        {
            return;
        }

        SlimeTrackerPose pose;
        if (!receiver.TryGetPose(trackerId, out pose))
        {
            return;
        }

        Vector3 alignedPosition = pose.hasPosition && spaceAligner != null
            ? spaceAligner.ToWorldPosition(pose.position)
            : pose.position;

        Quaternion alignedRotation = pose.hasRotation && spaceAligner != null
            ? spaceAligner.ToWorldRotation(pose.rotation)
            : pose.rotation;

        float positionAge = pose.hasPosition
            ? Time.unscaledTime - pose.lastPositionTime
            : -1f;

        float rotationAge = pose.hasRotation
            ? Time.unscaledTime - pose.lastRotationTime
            : -1f;

        BeginRow(unityTime, utcIso, "SLIME_TRACKER", trackerId);

        AppendInt(pose.hasPosition ? 1 : 0);
        AppendVector3(pose.position);
        AppendInt(pose.hasRotation ? 1 : 0);
        AppendQuaternion(pose.rotation);

        AppendVector3(alignedPosition);
        AppendQuaternion(alignedRotation);
        AppendVector3(questPosition);
        AppendQuaternion(questRotation);

        AppendFloat(positionAge);
        AppendFloat(rotationAge);
        AppendInt(leftSteps);
        AppendInt(rightSteps);
        AppendInt(totalSteps);
        AppendFloat(horizontalPath);
        AppendFloat(courseCoordinate);
        AppendFloat(maximumOutbound);
        AppendFloat(returnProgress);
        AppendText(policyName, false);

        writer.WriteLine(rowBuilder.ToString());
    }

    private void BeginRow(
        float unityTime,
        string utcIso,
        string recordType,
        string trackerId)
    {
        rowBuilder.Length = 0;
        rowBuilder.Append(sampleIndex.ToString(CultureInfo.InvariantCulture));
        rowBuilder.Append(';');
        rowBuilder.Append(unityTime.ToString("F6", CultureInfo.InvariantCulture));
        rowBuilder.Append(';');
        rowBuilder.Append(utcIso);
        rowBuilder.Append(';');
        rowBuilder.Append(recordType);
        rowBuilder.Append(';');
        rowBuilder.Append(trackerId);
        rowBuilder.Append(';');
    }

    private void AppendVector3(Vector3 value)
    {
        AppendFloat(value.x);
        AppendFloat(value.y);
        AppendFloat(value.z);
    }

    private void AppendQuaternion(Quaternion value)
    {
        AppendFloat(value.x, "F7");
        AppendFloat(value.y, "F7");
        AppendFloat(value.z, "F7");
        AppendFloat(value.w, "F7");
    }

    private void AppendFloat(float value, string format = "F6")
    {
        rowBuilder.Append(value.ToString(format, CultureInfo.InvariantCulture));
        rowBuilder.Append(';');
    }

    private void AppendInt(int value)
    {
        rowBuilder.Append(value.ToString(CultureInfo.InvariantCulture));
        rowBuilder.Append(';');
    }

    private void AppendText(string value, bool appendSeparator)
    {
        rowBuilder.Append(value ?? string.Empty);
        if (appendSeparator)
        {
            rowBuilder.Append(';');
        }
    }
}
