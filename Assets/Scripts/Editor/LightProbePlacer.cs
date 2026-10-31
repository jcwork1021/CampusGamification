#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.SceneManagement;

public class LightProbePlacer : EditorWindow
{
    private LightProbeGroup group;
    private bool probeSelected = false;

    // Bounds & Grid settings
    private Vector3 size = new Vector3(50, 10, 50);
    private float probeSpacing = 3.0f;

    // Mode
    private bool useGroundRaycast = true;
    private float groundOffset = 1.0f; // Height above ground for player body
    private bool addSecondHeightLayer = true;
    private float secondLayerOffset = 2.5f; // Extra height layer

    // Intersection Avoidance
    private bool avoidIntersection = true;
    private float intersectionRange = 0.3f;
    private LayerMask groundAndObstacleMask = ~0; // Default to Everything

    [MenuItem("Tools/Campus Light Probe Generator")]
    static void Init()
    {
        LightProbePlacer window = (LightProbePlacer)EditorWindow.GetWindow(typeof(LightProbePlacer));
        window.titleContent = new GUIContent("Probe Generator");
        window.Show();
    }

    private void OnSelectionChange()
    {
        group = null;
        Transform selected = Selection.activeTransform;
        if (selected != null)
            group = selected.GetComponent<LightProbeGroup>();

        probeSelected = (group != null);
        Repaint();
    }

    private void OnGUI()
    {
        GUILayout.Label("Campus Light Probe Generator", EditorStyles.boldLabel);

        if (!probeSelected)
        {
            EditorGUILayout.HelpBox("Select a GameObject with a LightProbeGroup component in the Hierarchy to begin.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(5);
        GUILayout.Label("Area Dimensions", EditorStyles.boldLabel);
        size = EditorGUILayout.Vector3Field("Area Size (Local)", size);
        probeSpacing = Mathf.Max(0.5f, EditorGUILayout.FloatField("Probe Spacing (Meters)", probeSpacing));

        EditorGUILayout.Space(10);
        GUILayout.Label("Ground Raycasting Settings", EditorStyles.boldLabel);
        useGroundRaycast = EditorGUILayout.Toggle("Snap to Ground / Walkable", useGroundRaycast);

        if (useGroundRaycast)
        {
            EditorGUI.indentLevel++;
            groundOffset = EditorGUILayout.FloatField("Height Above Ground", groundOffset);
            addSecondHeightLayer = EditorGUILayout.Toggle("Add Second Height Layer", addSecondHeightLayer);
            if (addSecondHeightLayer)
            {
                secondLayerOffset = EditorGUILayout.FloatField("2nd Layer Offset (+Y)", secondLayerOffset);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("Obstacle Avoidance", EditorStyles.boldLabel);
        avoidIntersection = EditorGUILayout.Toggle("Avoid Inside Colliders", avoidIntersection);

        if (avoidIntersection || useGroundRaycast)
        {
            EditorGUI.indentLevel++;
            intersectionRange = EditorGUILayout.FloatField("Collider Margin Radius", intersectionRange);

            LayerMask tempMask = EditorGUILayout.MaskField("Ground & Obstacle Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(groundAndObstacleMask), InternalEditorUtility.layers);
            groundAndObstacleMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);
        int currentCount = group.probePositions != null ? group.probePositions.Length : 0;
        EditorGUILayout.HelpBox($"Current Light Probe Count: {currentCount}", MessageType.None);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Generate Light Probes", GUILayout.Height(30)))
        {
            GenerateProbes();
        }
    }

    private void GenerateProbes()
    {
        Undo.RecordObject(group, "Generate Light Probe Positions");
        EditorSceneManager.MarkSceneDirty(group.gameObject.scene);

        float minX = -size.x / 2.0f;
        float maxX = size.x / 2.0f;
        float minY = -size.y / 2.0f;
        float maxY = size.y / 2.0f;
        float minZ = -size.z / 2.0f;
        float maxZ = size.z / 2.0f;

        List<Vector3> positions = new List<Vector3>();

        if (useGroundRaycast)
        {
            // Grid raycast down to ground level
            for (float x = minX; x <= maxX; x += probeSpacing)
            {
                for (float z = minZ; z <= maxZ; z += probeSpacing)
                {
                    Vector3 localRayStart = new Vector3(x, maxY, z);
                    Vector3 worldRayStart = group.transform.TransformPoint(localRayStart);

                    if (Physics.Raycast(worldRayStart, Vector3.down, out RaycastHit hit, size.y * 1.5f, groundAndObstacleMask))
                    {
                        // Ground probe
                        Vector3 worldPos1 = hit.point + (Vector3.up * groundOffset);
                        if (!IsBlocked(worldPos1))
                        {
                            positions.Add(group.transform.InverseTransformPoint(worldPos1));
                        }

                        // Optional 2nd height layer probe
                        if (addSecondHeightLayer)
                        {
                            Vector3 worldPos2 = worldPos1 + (Vector3.up * secondLayerOffset);
                            if (!IsBlocked(worldPos2))
                            {
                                positions.Add(group.transform.InverseTransformPoint(worldPos2));
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // Standard 3D Grid Volume
            for (float x = minX; x <= maxX; x += probeSpacing)
            {
                for (float y = minY; y <= maxY; y += probeSpacing)
                {
                    for (float z = minZ; z <= maxZ; z += probeSpacing)
                    {
                        Vector3 localPos = new Vector3(x, y, z);
                        Vector3 worldPos = group.transform.TransformPoint(localPos);

                        if (!avoidIntersection || !IsBlocked(worldPos))
                        {
                            positions.Add(localPos);
                        }
                    }
                }
            }
        }

        group.probePositions = positions.ToArray();
        Debug.Log($"[Light Probe Placer] Generated {positions.Count} probes successfully!");
    }

    private bool IsBlocked(Vector3 worldPos)
    {
        if (!avoidIntersection) return false;
        return Physics.CheckSphere(worldPos, intersectionRange, groundAndObstacleMask);
    }

    private void OnFocus() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDestroy() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnSceneGUI(SceneView view)
    {
        if (!probeSelected || group == null) return;

        Handles.color = Color.cyan;
        Handles.matrix = group.transform.localToWorldMatrix;
        Handles.DrawWireCube(Vector3.zero, size);
    }
}
#endif