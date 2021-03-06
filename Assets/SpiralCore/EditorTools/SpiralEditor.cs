﻿// *********************************************************************************
// The MIT License (MIT)
// Copyright (c) 2020 SpiralBlack https://github.com/SpiralBlack15
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// *********************************************************************************

using UnityEngine;
using System;
using System.Collections.Generic;
using Spiral.Core;

#if UNITY_EDITOR
using UnityEditor;
namespace Spiral.EditorToolkit
{
    public enum GroupType { Vertical, Horizontal }

    public static class SpiralEditor
    {

        // GUI FUNCTIONS ==========================================================================
        // Заместители GUI
        //=========================================================================================
        public static GUIContent GetLabel(string text, string tooltip)
        {
            GUIContent output = new GUIContent(text, tooltip);
            return output;
        }

        // Buttons --------------------------------------------------------------------------------
        public static bool Button(string name, GUIStyle style, params GUILayoutOption[] options)
        {
            Color prevColor = GUI.color;
            GUI.color = SpiralStyles.defaultButtonColor;
            if (style == null) style = SpiralStyles.buttonNormal;
            bool result = GUILayout.Button(name, style, options);
            GUI.color = prevColor;
            return result;
        }

        public static bool Button(string name, params GUILayoutOption[] options)
        {
            Color prevColor = GUI.color;
            GUI.color = SpiralStyles.defaultButtonColor;
            bool result = GUILayout.Button(name, options);
            GUI.color = prevColor;
            return result;
        }

        public static bool Button(string name, Color color, GUIStyle style = null, params GUILayoutOption[] options)
        {
            Color prevColor = GUI.color;
            GUI.color = color;
            if (style == null) style = SpiralStyles.buttonNormal;
            bool result = GUILayout.Button(name, style, options);
            GUI.color = prevColor;
            return result;
        }

        public static bool CenteredButton(string name, float width = 150, GUIStyle style = null)
        {
            BeginGroup(GroupType.Horizontal);
            EditorGUILayout.Space();
            bool button = Button(name, style, GUILayout.Width(width));
            EditorGUILayout.Space();
            EndGroup();
            return button;
        }

        public static bool CenteredButton(string name, Color color, float width = 150, GUIStyle style = null)
        {
            BeginGroup(GroupType.Horizontal);
            EditorGUILayout.Space();
            bool button = Button(name, color, style, GUILayout.Width(width));
            EditorGUILayout.Space();
            EndGroup();
            return button;
        }

        // Captions -------------------------------------------------------------------------------
        public static void CaptionLabel(GUIContent content, bool selectable = false, bool small = false, params GUILayoutOption[] options)
        {
            GUIStyle style = small ? SpiralStyles.labelSmallBold : SpiralStyles.labelBold;
            if (!selectable) EditorGUILayout.LabelField(content, style, options);
            else EditorGUILayout.SelectableLabel(content.text, style, options);
        }

        public static void CaptionLabel(string content, bool selectable = false, bool small = false, params GUILayoutOption[] options)
        {
            GUIStyle style = small ? SpiralStyles.labelSmallBold : SpiralStyles.labelBold;
            if (!selectable) EditorGUILayout.LabelField(content, style, options);
            else EditorGUILayout.SelectableLabel(content, style, options);
        }

        public static void CaptionLabel(string content, bool small = false, params GUILayoutOption[] options)
        {
            GUIStyle style = small ? SpiralStyles.labelSmallBold : SpiralStyles.labelBold;
            EditorGUILayout.LabelField(content, style, options);
        }

        public static void CaptionLabel(GUIContent content, params GUILayoutOption[] options)
        {
            EditorGUILayout.LabelField(content, SpiralStyles.labelBold, options);
        }

        // Panels ---------------------------------------------------------------------------------
        private static readonly List<GroupType> panelTypesStack = new List<GroupType>();

        public static void BeginGroup(GroupType groupType)
        {
            if (groupType == GroupType.Vertical) EditorGUILayout.BeginVertical();
            else EditorGUILayout.BeginHorizontal();
            panelTypesStack.Add(groupType);
        }

        public static void BeginGroup(GroupType groupType, Color color)
        {
            Color prevColor = GUI.color;
            GUI.color = color;
            if (groupType == GroupType.Vertical) EditorGUILayout.BeginVertical();
            else EditorGUILayout.BeginHorizontal();
            panelTypesStack.Add(groupType);
            GUI.color = prevColor;
        }

        public static void BeginPanel(GroupType groupType, Color? color = null)
        {
            Color prevColor = GUI.color;
            GUI.color = color != null ? (Color)color : SpiralStyles.defaultPanelColor;
            if (groupType == GroupType.Vertical) EditorGUILayout.BeginVertical(SpiralStyles.panel);
            else EditorGUILayout.BeginHorizontal(SpiralStyles.panel);
            panelTypesStack.Add(groupType);
            GUI.color = prevColor;
        }

        public static void BeginPanel(string caption, bool smallCaption, params GUILayoutOption[] options)
        {
            BeginPanel(GroupType.Vertical);
            GUIStyle style = smallCaption ? SpiralStyles.labelSmallBold : SpiralStyles.labelBold;
            EditorGUILayout.LabelField(caption, style, options);
        }

        public static void BeginPanel(string caption, Color color, params GUILayoutOption[] options)
        {
            BeginPanel(GroupType.Vertical, color);
            EditorGUILayout.LabelField(caption, SpiralStyles.labelBold, options);
        }

        public static void BeginPanel(string caption, bool smallCaption = false, Color? color = null, params GUILayoutOption[] options)
        {
            Color sendColor = color != null ? (Color)color : SpiralStyles.defaultPanelColor;
            BeginPanel(GroupType.Vertical, sendColor);
            GUIStyle style = smallCaption ? SpiralStyles.labelSmallBold : SpiralStyles.labelBold;
            EditorGUILayout.LabelField(caption, style, options);
        }

        public static void BeginPanel(GUIContent caption, bool smallCaption = false, Color? color = null, params GUILayoutOption[] options)
        {
            Color sendColor = color != null ? (Color)color : SpiralStyles.defaultPanelColor;
            BeginPanel(GroupType.Vertical, sendColor);
            GUIStyle style = smallCaption ? SpiralStyles.labelSmallBold : SpiralStyles.labelBold;
            EditorGUILayout.LabelField(caption, style, options);
        }

        public static void EndPanel()
        {
            if (panelTypesStack.Count == 0)
            {
                Debug.LogWarning("No panels or groups to close");
                return;
            }
            GroupType panelType = panelTypesStack.GetLast();
            if (panelType == GroupType.Vertical) EditorGUILayout.EndVertical();
            else EditorGUILayout.EndHorizontal();
            panelTypesStack.RemoveLast();
        }

        public static void EndGroup()
        {
            EndPanel();
        }

        // FOLDOUT BLOCKS -------------------------------------------------------------------------
        public static bool BeginFoldoutGroup(ref bool foldout, GUIContent content, GUIStyle captionStyle = null, bool autoclose = true, bool autostart = true)
        {
            if (autostart) BeginPanel(GroupType.Vertical);
            if (captionStyle == null) captionStyle = SpiralStyles.foldoutIndentedBold;
            foldout = EditorGUILayout.Foldout(foldout, content, true, captionStyle);
            if (!foldout && autoclose)
            {
                EndFoldoutGroup();
            }
            return foldout;
        }

        public static bool BeginFoldoutGroup(ref bool foldout, string content, GUIStyle captionStyle = null, bool autoclose = true, bool autostart = true)
        {
            if (autostart) BeginPanel(GroupType.Vertical);
            if (captionStyle == null) captionStyle = SpiralStyles.foldoutIndentedBold;
            foldout = EditorGUILayout.Foldout(foldout, content, true, captionStyle);
            if (!foldout && autoclose)
            {
                EndFoldoutGroup();
            }
            return foldout;
        }

        public static void EndFoldoutGroup()
        {
            EndPanel(); // TODO: помедетировать над более безопасной идеей
        }

        // Script fields --------------------------------------------------------------------------
        #region DRAW SCRIPT FIELDS
        private static void DrawScriptFieldOnly(MonoScript monoScript, Type type, string content)
        {
            if (content == "") content = "Script";
            if (monoScript != null)
            {
                _ = EditorGUILayout.ObjectField(content, monoScript, type, false);
            }
            else
            {
                EditorGUILayout.LabelField(content, $"Single file of [{type.Name}] not found", SpiralStyles.panel);
            }
        }

        public static void DrawScriptField(SerializedObject serializedObject)
        {
            BeginPanel(GroupType.Vertical);
            SerializedProperty prop = serializedObject.FindProperty("m_Script");
            GUI.enabled = prop != null;
            EditorGUILayout.PropertyField(prop, true);
            if (!GUI.enabled) GUI.enabled = true;
            EndPanel();
        }

        public static void DrawScriptField(MonoScript monoScript, string content = "")
        {
            BeginPanel(GroupType.Vertical);
            GUI.enabled = false;
            if (content == "") content = "Script";
            EditorGUILayout.ObjectField(content, monoScript, typeof(MonoScript), false);
            GUI.enabled = true;
            EndPanel();
        }

        public static void DrawScriptField(Type type, string content = "")
        {
            BeginPanel(GroupType.Vertical);
            GUI.enabled = false;
            MonoScript monoScript = SpiralEditorTools.GetMonoScript(type);
            if (content == "") content = "Script";
            EditorGUILayout.ObjectField(content, monoScript, typeof(MonoScript), false);
            GUI.enabled = true;
            EndPanel();
        }

        public static void DrawScriptField(ScriptableObject scriptableObject, string content = "")
        {
            BeginPanel(GroupType.Vertical);
            GUI.enabled = false;
            Type type = scriptableObject.GetType();
            MonoScript monoScript = MonoScript.FromScriptableObject(scriptableObject);
            DrawScriptFieldOnly(monoScript, type, content);
            GUI.enabled = true;
            EndPanel();
        }
        #endregion

        // Quick Object Field ---------------------------------------------------------------------
        public static T DrawObjectField<T>(string content, T obj, bool allowScenePick = true) where T : UnityEngine.Object
        {
            Type type = typeof(T);
            return (T)EditorGUILayout.ObjectField(content, obj, type, allowScenePick);
        }

        // Misc -----------------------------------------------------------------------------------
        public static void DrawLogoLine(Color? color = null)
        {
            Color defaultColor = GUI.color;
            GUI.color = color != null ? (Color)color : SpiralStyles.defaultLogoColor;
            EditorGUILayout.BeginVertical(SpiralStyles.panel);
            EditorGUILayout.LabelField("SpiralBlack Scripts © 2020", SpiralStyles.labelLogo);
            EditorGUILayout.EndVertical();
            GUI.color = defaultColor;
        }

        public static void ShowHelp(string message, ref bool showHelp, MessageType messageType = MessageType.None)
        {
            BeginPanel(GroupType.Vertical);
            EditorGUI.indentLevel += 1;
            string helpFoldout = showHelp ? "Hide help" : "Show help";
            showHelp = EditorGUILayout.Foldout(showHelp, helpFoldout);
            EditorGUI.indentLevel -= 1;
            if (showHelp)
            {
                EditorGUILayout.HelpBox(message, messageType);
            }
            EndPanel();
        }

        // Colors ---------------------------------------------------------------------------------
        /// <summary>
        /// Быстро взять цвет хексом
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string GetHex(this Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color).ToLower();
        }

        public static string GetColorHex(float r, float g, float b, float a = 1)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(new Color(r, g, b, a)).ToLower();
        }
    }

}
#endif

