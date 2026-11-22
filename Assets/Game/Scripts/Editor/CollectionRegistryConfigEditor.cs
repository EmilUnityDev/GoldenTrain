using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollectionRegistryConfig))]
public class CollectionRegistryConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        if (GUILayout.Button("Auto Fill 42 Cards", GUILayout.Height(30)))
            AutoFill((CollectionRegistryConfig)target);
    }

    private void AutoFill(CollectionRegistryConfig config)
    {
        var entries = new List<CollectionRegistryConfig.CollectionEntry>();

        void Add(CollectionTag tag, int count)
        {
            for (int i = 0; i < count; i++)
                entries.Add(new CollectionRegistryConfig.CollectionEntry { tag = tag, itemId = i });
        }

        Add(CollectionTag.Postcards, 9);
        Add(CollectionTag.Luggage,   9);
        Add(CollectionTag.Tickets,   6);
        Add(CollectionTag.Watches,   9);
        Add(CollectionTag.Badges,    9);

        var so = new SerializedObject(config);
        var prop = so.FindProperty("_entries");
        prop.ClearArray();

        for (int i = 0; i < entries.Count; i++)
        {
            prop.InsertArrayElementAtIndex(i);
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("tag").enumValueIndex = (int)entries[i].tag;
            el.FindPropertyRelative("itemId").intValue = entries[i].itemId;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CollectionRegistry] Auto-filled {entries.Count} entries");
    }
}

