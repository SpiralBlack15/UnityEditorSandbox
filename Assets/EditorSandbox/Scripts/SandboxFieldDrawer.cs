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

using UnityEngine;
using Spiral.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spiral.EditorToolkit.EditorSandbox
{

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SandboxField))]
    public class SandboxFieldDrawer : SpiralPropertyDrawer
    {
        private static MonoScript m_script;
        private static MonoScript monoScript { get { return CahsedMono<SandboxFieldDrawer>(ref m_script); } }

        protected override Borders GetOutline() { return new Borders(2, 3, 2, 3); }
        protected override Borders GetInnerStroke() { return new Borders(5, 7, 5, 5); }
        protected override Vector2 GetGridSpace() { return new Vector2(2, 3); }
        protected override float GetElementHeight() { return 18; }
        protected override int GridColumnCount() { return 2; }

        private string[] methodNames = null;
        private SandboxField sandboxField = null;
        private object sandboxTarget = null;
        private void InitCurrentSandboxField(SerializedProperty property)
        {
            InitSeraizliationTree(property);
            InitSerializationTreeNames();

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


        protected override int GridRowCount()
        {
            if (sandboxField == null) return 1;
            else return sandboxField.editorFoldout ? 4 : 1; 
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitCurrentSandboxField(property);
            return GetGridedPropertyHeight(0, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // sandboxField должен подтянуться из GetPropertyHeight, т.к. тот вызывается до OnGUI
            if (sandboxField == null || sandboxTarget == null) return;
            // скипаем отрисовку - такая ситуация может возникать в случае, если объект
            // находится в составе массива/листа/и т.п., и только что был автоматически создан
            // и/или в массиве появилась новая позиция

            InitializeSizesGeneral(position);
            InitializeGrid();

            // подложка
            DrawBackgroundPanel();

            // название переменной
            Rect rectPropertyName = GetGridCell(0, 0, false);
            bool newFoldout = DrawFoldout(sandboxField.editorFoldout, label, rectPropertyName, SpiralStyles.foldoutPropertySmallBold);

            // скрипт проперти
            Rect rectScript = GetGridCell(0, 1, false);
            DrawScriptFieldRect(monoScript, rectScript);

            if (sandboxField.editorFoldout)
            {
                // подпись объекта
                Rect rectSelectTargetContent = GetGridCell(1, 0, true);
                GUI.Label(rectSelectTargetContent, "Target", SpiralStyles.labelSmallBoxed);

                // выбор объекта инициализации
                Rect rectSelectTarget = GetGridCell(1, 1, false);
                if (sandboxField.selectedTarget < 0 || sandboxField.selectedTarget >= hierarchy.Count)
                    sandboxField.selectedTarget = 0;
                sandboxField.selectedTarget = EditorGUI.Popup(rectSelectTarget,
                                                              sandboxField.selectedTarget,
                                                              objectNames,
                                                              SpiralStyles.popupSmall);

                // подпись метода
                Rect rectSelectMethodContent = GetGridCell(2, 0, true);
                GUI.Label(rectSelectMethodContent, "Method", SpiralStyles.labelSmallBoxed);

                // выпадающий списоок
                Rect rectSelectMethod = GetGridCell(2, 1, false);
                sandboxField.selected = EditorGUI.Popup(rectSelectMethod,
                                                        sandboxField.selected,
                                                        methodNames,
                                                        SpiralStyles.popupSmall);

                // кнопка запуска симуляции
                Rect rectRunButton = GetGridCell(3, 1, true);
                GUI.enabled = methodNames.Length != 0;
                if (GUI.Button(rectRunButton, "Run in simulator"))
                {
                    sandboxField.LaunchInSandbox();
                }
                GUI.enabled = true;
            }

            sandboxField.editorFoldout = newFoldout;
        }
    }
#endif
}
