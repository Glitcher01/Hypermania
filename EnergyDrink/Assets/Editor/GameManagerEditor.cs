using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var gm = (GameManager)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Room Join (Play Mode)", EditorStyles.boldLabel);

        // Room ID input
        ulong roomId = gm.EditorRoomId;
        roomId = (ulong)EditorGUILayout.LongField("Room ID", (long)roomId);
        if (roomId != gm.EditorRoomId)
        {
            Undo.RecordObject(gm, "Change Room ID");
            gm.EditorRoomId = roomId;
            EditorUtility.SetDirty(gm);
        }

        using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
        {
            if (GUILayout.Button("Join Room"))

            {
                try
                {
                    _ = gm.JoinRoomFromEditorAsync(roomId);
                    Debug.Log($"JoinRoomAsync started for room {roomId}");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use Join Room.", MessageType.Info);
        }
    }

    private static IEnumerator JoinRoomRoutine(GameManager gm, ulong roomId)
    {
        Task t;
        try
        {
            t = gm.JoinRoomFromEditorAsync(roomId);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            yield break;
        }

        while (!t.IsCompleted) yield return null;

        if (t.IsFaulted) Debug.LogException(t.Exception);
        else Debug.Log($"Joined room {roomId}");
    }
}
