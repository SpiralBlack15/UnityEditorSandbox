// *********************************************************************************
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

using System.Collections.Generic;
using UnityEngine;
using Spiral.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spiral.EditorToolkit.EditorSandbox
{

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SandboxField))]
    public class SandboxFieldDrawer : PropertyDrawer
    {
        private readonly float elementHeight = 18;
        private readonly float edgeIndentY = 2;
        private readonly float edgeIndentX = 3;
        private readonly float innerEdgeIndentX = 5;
        private readonly float innerEdgeIndentY = 3;
        private readonly float spaceY = 3;
        private readonly float spaceX = 3;
        private readonly int rows = 4;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return elementHeight * rows +
                   spaceY * (rows - 1) +
                   (innerEdgeIndentY + edgeIndentY) * 2 + 1;
        }

        private float GetRowY(int row)
        {
            float space = row == 0 ? 0 : spaceY * row;
            return elementHeight * row + space + innerEdgeIndentY + edgeIndentY;
        }

        private string[] GetObjectNames(List<object> hierarchy)
        {
            string[] names = new string[hierarchy.Count];

            int d = 0;
            for (int i = 0; i < names.Length; i++)
            {
                if (hierarchy[i] == null)
                {
                    names[i] = "[null]";
                    continue;
                }

                string variableName = serializationPath[i + d];
                if (variableName == "Array")
                {
                    d++;
                    variableName = serializationPath[i + d];
                }
                string typeName = hierarchy[i].GetType().Name;
                variableName = variableName.FirstLetterCapitalization();

                names[i] = $"{variableName} ({typeName})";
            }
            return names;
        }

        List<object> hierarchy = null;
        List<string> serializationPath = null;
        string[] objectNames = null;
        string[] methodNames = null;
        SandboxField sandboxField = null;
        object sandboxTarget = null;

        private void Init(SerializedProperty property)
        {
            serializationPath = property.GetPathNodes().ToList();
            serializationPath.Insert(0, property.GetRootParent().name);
            hierarchy = property.GetSerializationHierarchy(false);

            objectNames = GetObjectNames(hierarchy);

            object sandboxObject = hierarchy.GetLast();
            if (sandboxObject == null) return; // может случаться при вложенной сериализации
            sandboxField = sandboxObject as SandboxField;

            sandboxTarget = hierarchy[sandboxField.selectedTarget];
            if (sandboxTarget == null) return; // может случаться при вложенной сериализации

            // инициализируемся ТОЛЬКО если таргет не сходится, чтобы не делать этого слишком часто
            if (sandboxField.target != sandboxTarget)
            {
                sandboxField.InitSimulators(sandboxTarget);
            }
            methodNames = sandboxField.GetMethodNames();

            return;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);
            if (sandboxField == null || sandboxTarget == null) return; 
            // скипаем отрисовку - такая ситуация может возникать в случае, если объект
            // находится в составе массива/листа/и т.п., и только что был автоматически создан
            // и/или в массиве появилась новая позиция

            // ------------------------------------------------------------------------------------
            EditorGUI.BeginProperty(position, label, property);

            Rect indentedRect = EditorGUI.IndentedRect(position);

            float startX = indentedRect.x;
            float startY = indentedRect.y;
            float width  = indentedRect.width;
            float height = indentedRect.height;

            float indentX = startX - position.x;

            float elementStartX = startX - edgeIndentX;

            float innerWidth = width - spaceX - edgeIndentX - 2;
            float halfInnerWidth = innerWidth * 0.5f - innerEdgeIndentX * 2;
            float nameLX = startX + innerEdgeIndentX;

            float row0Y = startY + GetRowY(0);
            float row1Y = startY + GetRowY(1);
            float row2Y = startY + GetRowY(2);
            float row3Y = startY + GetRowY(3);

            float col1X = nameLX + halfInnerWidth + spaceX;
            float col1W = startX + width - col1X - innerEdgeIndentY - edgeIndentX;


            // подложка
            Rect boxRect = new Rect(elementStartX,
                                    startY + edgeIndentY,
                                    width + edgeIndentX,
                                    height - edgeIndentY);
            GUI.Box(boxRect, "", SpiralStyles.panel);

            // название переменной
            Rect propertyNameRect = new Rect(nameLX, row0Y, halfInnerWidth, elementHeight);
            GUI.Label(propertyNameRect, label, SpiralStyles.smallBoldLabel);

            GUI.enabled = false;
            MonoScript monoScript = SpiralEditorTools.GetMonoScript(GetType());
            Rect rect = new Rect(col1X - indentX, row0Y, col1W + indentX, elementHeight);
            EditorGUI.ObjectField(rect, monoScript, typeof(MonoScript), false);
            GUI.enabled = true;

            // выбор объекта инициализации
            Rect selectTargetContentRect = new Rect(nameLX, row1Y, halfInnerWidth, elementHeight);
            GUI.Label(selectTargetContentRect, "Target", SpiralStyles.smallBoxedLabel);
            Rect selectParent = new Rect(col1X - indentX, row1Y, col1W + indentX, elementHeight);
            if (sandboxField.selectedTarget < 0 || sandboxField.selectedTarget >= hierarchy.Count)
                sandboxField.selectedTarget = 0;
            sandboxField.selectedTarget = EditorGUI.Popup(selectParent,
                                                          sandboxField.selectedTarget,
                                                          objectNames,
                                                          SpiralStyles.miniPopupFont);

            // пространство метода
            Rect methodNamespaceRect = new Rect(nameLX, row2Y, halfInnerWidth, elementHeight);
            GUI.Label(methodNamespaceRect, "Method", SpiralStyles.smallBoxedLabel);

            // выпадающий списоок
            Rect popupRect = new Rect(col1X - indentX, row2Y, col1W + indentX, elementHeight);
            sandboxField.selected = EditorGUI.Popup(popupRect,
                                                    sandboxField.selected,
                                                    methodNames,
                                                    SpiralStyles.miniPopupFont);

            // положение кнопки
            Rect buttonPos = new Rect(col1X, row3Y, col1W, elementHeight);
            GUI.enabled = methodNames.Length != 0;
            if (GUI.Button(buttonPos, "Run in simulator"))
            {
                sandboxField.LaunchInSandbox();
            }
            GUI.enabled = true;

            EditorGUI.EndProperty();
        }
    }
#endif
}
