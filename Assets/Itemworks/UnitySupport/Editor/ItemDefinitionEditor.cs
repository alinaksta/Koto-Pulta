using Itemworks.Core;
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Itemworks.UnityEngine
{
    public class ItemDefinitionEditor : EditorWindow
    {
        private const string WINDOW_NAME = "Item Editor";
        private const string ITEMNAME_PROP = "Id";
        private const string PROPERTIES_PROP = "Properties";
        private const string TAGS_PROP = "Tags";

        private ItemDefinitionAsset asset;
        private SerializedObject serializedObject;
        private SerializedProperty propertiesProperty;
        private SerializedProperty tagsProperty;

        private Vector2 scrollPosition;

        [MenuItem("Window/Item Definition Editor")]
        public static void ShowWindow() => GetWindow<ItemDefinitionEditor>(WINDOW_NAME);

        public static void Open(ItemDefinitionAsset asset)
        {
            var w = GetWindow<ItemDefinitionEditor>(WINDOW_NAME);
            w.Load(asset);
        }

        private void Load(ItemDefinitionAsset item)
        {
            asset = item;
            if (asset == null)
                return;

            serializedObject = new SerializedObject(asset);
            propertiesProperty = serializedObject.FindProperty(PROPERTIES_PROP);
            tagsProperty = serializedObject.FindProperty(TAGS_PROP);
        }

        private void OnGUI()
        {
            if (asset is null)
            {
                EditorGUILayout.HelpBox("Select an item to edit", MessageType.Info);
                return;
            }

            if (serializedObject == null || serializedObject.targetObject == null)
                Load(asset);

            serializedObject.Update();

            DrawEditor();

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Save", GUILayout.Height(25)))
                AssetDatabase.SaveAssetIfDirty(asset);
        }

        private void DrawEditor()
        {
            EditorGUILayout.BeginVertical();

            // Item Name
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(ITEMNAME_PROP),
                new GUIContent("Id"));

            EditorGUILayout.Space(10);

            // Properties Section
            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawProperties();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Add Component", GUILayout.Height(30)))
                ShowAddComponentMenu();

            EditorGUILayout.EndVertical();
        }

        private void DrawProperties()
        {
            if (propertiesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This item does not have any properties", MessageType.Info);
                return;
            }

            for (int i = 0; i < propertiesProperty.arraySize; i++)
                DrawProperty(i);
        }

        private void DrawProperty(int index)
        {
            var element = propertiesProperty.GetArrayElementAtIndex(index);
            var propertyObject = element.managedReferenceValue;

            if (propertyObject == null)
            {
                EditorGUILayout.HelpBox($"Null property at index {index}", MessageType.Error);
                return;
            }

            string typeName = propertyObject.GetType().Name;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            element.isExpanded = EditorGUILayout.Foldout(
                element.isExpanded, typeName, true, EditorStyles.foldoutHeader);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("X", GUILayout.Width(20), GUILayout.Height(18)))
            {
                propertiesProperty.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
                return;
            }

            EditorGUILayout.EndHorizontal();

            if (element.isExpanded)
            {
                EditorGUI.indentLevel++;
                DrawPropertyFields(element);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawPropertyFields(SerializedProperty element)
        {
            SerializedProperty iterator = element.Copy();
            bool enterChildren = true;

            while (iterator.Next(enterChildren))
            {
                if (!iterator.propertyPath.StartsWith(element.propertyPath + "."))
                    break;

                EditorGUILayout.PropertyField(iterator, true);
                enterChildren = false;
            }
        }

        private void ShowAddComponentMenu()
        {
            GenericMenu menu = new GenericMenu();
            var propertyTypes = GetAllItemPropertyTypes();

            foreach (var type in propertyTypes)
                menu.AddItem(new GUIContent(type.Name), false, () => AddProperty(type));

            menu.ShowAsContext();
        }

        private void AddProperty(Type propertyType)
        {
            serializedObject.Update();

            propertiesProperty.arraySize++;
            var newElement = propertiesProperty.GetArrayElementAtIndex(propertiesProperty.arraySize - 1);
            newElement.managedReferenceValue = Activator.CreateInstance(propertyType);
            newElement.isExpanded = true;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private Type[] GetAllItemPropertyTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(ItemProperty).IsAssignableFrom(type) &&
                               !type.IsAbstract &&
                               type != typeof(ItemProperty))
                .OrderBy(type => type.Name)
                .ToArray();
        }

        public static class ItemDefinitionOpenHandler
        {
            [OnOpenAsset]
            public static bool OnOpen(int instanceID, int line)
            {
                var obj = EditorUtility.EntityIdToObject(instanceID);
                if (obj is ItemDefinitionAsset itemAsset)
                {
                    ItemDefinitionEditor.Open(itemAsset);
                    return true;
                }

                return false;
            }
        }
    }
}