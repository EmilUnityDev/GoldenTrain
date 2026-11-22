#if UNITY_EDITOR
using Tutorials;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(TutorialPlayer))]
public sealed class TutorialPlayerEditor : Editor
{
    private TutorialPlayer _p;
    private SerializedProperty _builderProp;
    private SerializedProperty _autoStartProp;
    private SerializedProperty _startIndexProp;

    private void OnEnable()
    {
        _p = (TutorialPlayer)target;
        _builderProp   = serializedObject.FindProperty("_builder");
        _autoStartProp = serializedObject.FindProperty("_autoStart");
        _startIndexProp= serializedObject.FindProperty("_startIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_builderProp, new GUIContent("Builder"));
        EditorGUILayout.PropertyField(_autoStartProp, new GUIContent("Auto Start"));
        EditorGUILayout.PropertyField(_startIndexProp, new GUIContent("Start Index"));

        var b = _p.Builder;
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (b == null)
            {
                EditorGUILayout.HelpBox("TutorialBuilder is not assigned.", MessageType.Warning);
            }
            else
            {
                var count = b.Screens?.Count ?? 0;
                var index = count == 0 ? -1 : b.CurrentIndex;
                EditorGUILayout.LabelField("Screens", $"{count}");
                EditorGUILayout.LabelField("Current", index < 0 ? "-" : (index + 1).ToString());

                EditorGUILayout.Space(4);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !Application.isPlaying && b != null && count > 0 && index != 0;
                    if (GUILayout.Button("<<", GUILayout.Height(24))) Exec(() => b.GoFirst(), "Go First");
                    GUI.enabled = !Application.isPlaying && b != null && b.CanGoPrev;
                    if (GUILayout.Button("<", GUILayout.Height(24)))  Exec(() => b.GoPrev(), "Go Prev");
                    GUI.enabled = !Application.isPlaying && b != null && b.CanGoNext;
                    if (GUILayout.Button(">", GUILayout.Height(24)))  Exec(() => b.GoNext(), "Go Next");
                    GUI.enabled = !Application.isPlaying && b != null && count > 0 && index != count - 1;
                    if (GUILayout.Button(">>", GUILayout.Height(24))) Exec(() => b.GoLast(), "Go Last");
                    GUI.enabled = true;
                }

                if (Application.isPlaying)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Start", GUILayout.Height(24)))   _p.StartTutorial(_startIndexProp.intValue);
                        if (GUILayout.Button("Restart", GUILayout.Height(24))) _p.Restart();
                        if (GUILayout.Button("SkipAll", GUILayout.Height(24))) _p.SkipAll();
                        if (GUILayout.Button("Complete Current", GUILayout.Height(24)))
                        {
                            var s = _p.CurrentScreen();
                            if (s != null) s.MarkCompleted();
                        }
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void Exec(System.Action action, string undoName)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.RecordObjects(Selection.gameObjects, undoName);
        action?.Invoke();
        EditorUtility.SetDirty(UnityEditor.Selection.activeObject);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif
