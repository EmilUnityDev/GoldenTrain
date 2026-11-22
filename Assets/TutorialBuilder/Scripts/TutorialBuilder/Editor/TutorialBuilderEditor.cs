#if UNITY_EDITOR
using System.Linq;
using Tutorials;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(TutorialBuilder))]
public sealed class TutorialBuilderEditor : Editor
{
    private TutorialBuilder _b;
    private SerializedProperty _hierarchyProp;
    private SerializedProperty _screensProp;
    private ReorderableList _screensList;

    private void OnEnable()
    {
        _b = (TutorialBuilder)target;
        _hierarchyProp = serializedObject.FindProperty("_hierarchy");
        _screensProp = serializedObject.FindProperty("_screens");

        _screensList = new ReorderableList(serializedObject, _screensProp, true, true, true, true);
        _screensList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Screens");
        _screensList.drawElementCallback = (rect, index, active, focused) =>
        {
            var element = _screensProp.GetArrayElementAtIndex(index);
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(rect, element, new GUIContent($"[{index}]"));
        };
        _screensList.onSelectCallback = list =>
        {
            var el = _screensProp.GetArrayElementAtIndex(list.index);
            var obj = el.objectReferenceValue as Object;
            if (obj != null) EditorGUIUtility.PingObject(obj);
        };
        _screensList.onAddDropdownCallback = (buttonRect, list) =>
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Insert Null"), false, () =>
            {
                serializedObject.Update();
                int i = list.index >= 0 ? list.index + 1 : _screensProp.arraySize;
                _screensProp.InsertArrayElementAtIndex(i);
                var el = _screensProp.GetArrayElementAtIndex(i);
                el.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
            });
            var h = _b.Hierarchy;
            bool canPrefab = h != null && h.Canvas != null && h.ScreenPrefab != null;
            if (canPrefab)
            {
                menu.AddItem(new GUIContent("Instantiate From Hierarchy.ScreenPrefab"), false, () =>
                {
                    int i = list.index >= 0 ? list.index + 1 : _screensProp.arraySize;
                    AddAt(i);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Instantiate From Hierarchy.ScreenPrefab"));
            }
            if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
            {
                menu.AddItem(new GUIContent("Add From Selection (Hierarchy)"), false, () =>
                {
                    serializedObject.Update();
                    var screens = Selection.gameObjects
                        .Select(go => go.GetComponent<TutorialScreen>())
                        .Where(s => s != null)
                        .ToList();
                    int insertIndex = list.index >= 0 ? list.index + 1 : _screensProp.arraySize;
                    foreach (var s in screens)
                    {
                        _screensProp.InsertArrayElementAtIndex(insertIndex);
                        var el = _screensProp.GetArrayElementAtIndex(insertIndex);
                        el.objectReferenceValue = s;
                        insertIndex++;
                    }
                    serializedObject.ApplyModifiedProperties();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Add From Selection (Hierarchy)"));
            }
            menu.ShowAsContext();
        };
        _screensList.onRemoveCallback = list =>
        {
            if (list.index < 0 || list.index >= _screensProp.arraySize) return;
            serializedObject.Update();
            var el = _screensProp.GetArrayElementAtIndex(list.index);
            var obj = el.objectReferenceValue as GameObject;
            _screensProp.DeleteArrayElementAtIndex(list.index);
            serializedObject.ApplyModifiedProperties();
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_hierarchyProp, new GUIContent("Hierarchy (Canvas & Prefab)"));
        EditorGUILayout.Space(6);
        _screensList.DoLayoutList();

        var h = _b.Hierarchy;
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = _b.Screens.Count > 0 && _b.CurrentIndex != 0;
            if (GUILayout.Button("<<", GUILayout.Height(28))) Exec(() => _b.GoFirst(), "Go First");
            GUI.enabled = _b.CanGoPrev;
            if (GUILayout.Button("<", GUILayout.Height(28))) Exec(() => _b.GoPrev(), "Go Prev");

            GUI.enabled = true;
            GUILayout.Label($"Текущая страница: {(_b.Screens.Count == 0 ? "-" : (_b.CurrentIndex + 1).ToString())}/{Mathf.Max(1,_b.Screens.Count)}",
                            EditorStyles.helpBox, GUILayout.Height(28));

            GUI.enabled = _b.CanGoNext;
            if (GUILayout.Button(">", GUILayout.Height(28))) Exec(() => _b.GoNext(), "Go Next");
            GUI.enabled = _b.Screens.Count > 0 && _b.CurrentIndex != _b.Screens.Count - 1;
            if (GUILayout.Button(">>", GUILayout.Height(28))) Exec(() => _b.GoLast(), "Go Last");
            GUI.enabled = true;
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = h != null && h.Canvas != null && h.ScreenPrefab != null;
            if (GUILayout.Button("Instantiate Prefab After Current", GUILayout.Height(28)))
                AddAt(_b.CurrentIndex + 1);
            GUI.enabled = _b.Screens.Count > 0;
            if (GUILayout.Button("Remove Current Ref", GUILayout.Height(28)))
                DeleteAt(_b.CurrentIndex);
            GUI.enabled = true;
        }

        if (GUILayout.Button("Compact"))
            Exec(() => _b.Compact(), "Compact Screens");

        serializedObject.ApplyModifiedProperties();
    }

    private void AddAt(int index)
    {
        var h = _b.Hierarchy;
        if (h == null || h.Canvas == null || h.ScreenPrefab == null)
        {
            EditorUtility.DisplayDialog("Не хватает ссылок", "Нужно заполнить TutorialHierarchy: Canvas и Screen Prefab.", "Ок");
            return;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(h.ScreenPrefab, h.Canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add Tutorial Screen");
        var screen = instance.GetComponent<TutorialScreen>();
        if (screen == null) screen = instance.AddComponent<TutorialScreen>();
        Exec(() => _b.InsertAt(index, screen), "Insert Screen Ref");
        Selection.activeObject = instance;
        EditorGUIUtility.PingObject(instance);
        serializedObject.Update();
        var ins = _screensProp.FindPropertyRelative("Array");
        serializedObject.ApplyModifiedProperties();
    }

    private void DeleteAt(int index)
    {
        if (_b.Screens.Count == 0) return;
        var screen = _b.Screens[index];
        var go = screen != null ? screen.gameObject : null;
        Exec(() => _b.RemoveAt(index), "Remove Screen Ref");
        if (go != null) Undo.DestroyObjectImmediate(go);
    }

    private static void Exec(System.Action action, string undoName)
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.RecordObjects(Selection.gameObjects, undoName);
        action.Invoke();
        EditorUtility.SetDirty(UnityEditor.Selection.activeObject);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif
