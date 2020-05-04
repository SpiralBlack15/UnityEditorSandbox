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

#if UNITY_EDITOR
using UnityEditor;
namespace Spiral.EditorToolkit.EditorSandbox
{
    public class SandboxEditorWindow : SpiralCustomEditorWindow
    {
        private Vector2 scrollPos;
        private Vector2 scrollPosSubscriptions;

        [MenuItem("Spiral Tools/Editor Sandbox")]
        public static void Init()
        {
            SandboxEditorWindow window = (SandboxEditorWindow)GetWindow(typeof(SandboxEditorWindow));
            window.Show();
        }

        private void OnEnable()
        {
            Sandbox.onAverageDeltaTimeUpdate += Repaint;
        }

        private void OnGUI()
        {
            titleContent.text = "Editor Sandbox";
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(position.height));
            OpenStandartBack();

            DrawMainSettings();
            DrawHealthMonitor();
            DrawSubscriptions();

            CloseStandartBack();
            EditorGUILayout.EndScrollView();
        }

        private void DrawMainSettings()
        {
            SpiralEditor.BeginPanel("Global Editor Simulation params");
            EditorGUI.indentLevel += 1;

            Sandbox.isRunning = EditorGUILayout.Toggle("Editor simulation", Sandbox.isRunning);
            Sandbox.minimalStepMode = (SandboxMinimalStep)EditorGUILayout.EnumPopup("Step mode", Sandbox.minimalStepMode);
            if (Sandbox.minimalStepMode == SandboxMinimalStep.Custom)
            {
                GUIContent contentMTS = new GUIContent("Minimal time step");
                Sandbox.customMinimalTimeStep = EditorGUILayout.FloatField(contentMTS, Sandbox.customMinimalTimeStep);
            }
            Sandbox.checkAverageDeltaEvery = EditorGUILayout.FloatField("Check average every", Sandbox.checkAverageDeltaEvery);

            Sandbox.clearReferences = EditorGUILayout.Toggle("Clear references", Sandbox.clearReferences);
            Sandbox.secureMode = EditorGUILayout.Toggle("Secure mode", Sandbox.secureMode);
            EditorGUI.indentLevel -= 1;

            SpiralEditor.BeginGroup(GroupType.Horizontal);
            EditorGUILayout.Space();
            if (SpiralEditor.DrawRoundButton("Save current prefs", GUILayout.Width(150)))
            {
                SandboxPrefs.SaveGlobal();
            }
            EditorGUILayout.Space();
            SpiralEditor.EndGroup();

            EditorGUILayout.Space();
            SpiralEditor.EndPanel();
        }

        private void DrawHealthMonitor()
        {
            SpiralEditor.BeginPanel("Simulator's Health Monitor", SpiralEditor.colorLightRed);
            EditorGUI.indentLevel += 1;

            float average = Sandbox.averageDeltaTime;
            string strAverage = average.ToString("F4");

            EditorGUILayout.LabelField("Average real time step: ", 
                                       $"{strAverage} s.", 
                                       SpiralEditor.normalLabel);
            EditorGUILayout.LabelField("Events count: ", 
                                       $"{Sandbox.sandboxCurrentCount} / {Sandbox.sandboxTotalCount}", 
                                       SpiralEditor.normalLabel);
            EditorGUI.indentLevel -= 1;

            SpiralEditor.BeginGroup(GroupType.Horizontal);
            EditorGUILayout.Space();
            if (SpiralEditor.DrawRoundButton("Kill all events", GUILayout.Width(150)))
            {
                Sandbox.RemoveAll();
            }
            EditorGUILayout.Space();
            SpiralEditor.EndGroup();

            EditorGUILayout.Space();
            SpiralEditor.EndPanel();
        }

        private static readonly int minHeight = 25;
        private static readonly int entryHeight = 115;
        private static readonly int maxHeight = 350;
        private int GetSubscriptionsHeight(int sandboxCount)
        {
            if (sandboxCount == 0) return minHeight;
            if (sandboxCount < 3) return entryHeight * sandboxCount;
            return maxHeight;
        }

        private void DrawSubscriptions()
        {
            SpiralEditor.BeginPanel("Subscriptions:", Color.grey);
            int sandboxCount = Sandbox.sandboxTotalCount;
            int fieldHeight  = GetSubscriptionsHeight(sandboxCount);
            GUILayoutOption option = GUILayout.MaxHeight(fieldHeight);
            scrollPosSubscriptions = EditorGUILayout.BeginScrollView(scrollPosSubscriptions, option);
            if (sandboxCount == 0)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.LabelField("No subscription yet");
                EditorGUI.indentLevel -= 1;
            }
            else
            {
                for (int i = 0; i < sandboxCount; i++)
                {
                    var sandboxEventInfo = Sandbox.GetEventInfo(i);
                    DrawSanboxEvent(sandboxEventInfo);
                    if (i != sandboxCount - 1) EditorGUILayout.Space(2);
                }
            }
            EditorGUILayout.EndScrollView();
            SpiralEditor.EndPanel();
        }

        private bool CheckEventHealth(Sandbox.EventInfo eventInfo)
        {
            if (eventInfo.callback == null) return false;
            if (eventInfo.hasReference)
            {
                if (eventInfo.isReferenceUnityObject)
                {
                    if (eventInfo.referenceObject as UnityEngine.Object == null) return false;
                }
                else
                {
                    if (eventInfo.referenceObject == null) return false;
                }
            }
            return true;
        }

        private void DrawSanboxEvent(Sandbox.EventInfo sandboxInfo)
        {
            GUILayoutOption buttonWidth = GUILayout.Width(100);
            if (sandboxInfo == null)
            {
                SpiralEditor.BeginPanel(GroupType.Vertical, SpiralEditor.colorLightRed);
                EditorGUILayout.LabelField($"IDX {sandboxInfo.idx} broken entry", SpiralEditor.panel);
                if (SpiralEditor.DrawRoundButton("Kill callback", buttonWidth)) Sandbox.RemoveCallback(sandboxInfo.idx); 
                EditorGUILayout.Space(2);
                SpiralEditor.EndPanel();
            }
            else
            {
                bool good = CheckEventHealth(sandboxInfo);
                Color color;
                if (good)
                {
                    color = sandboxInfo.paused ? SpiralEditor.colorLightYellow : SpiralEditor.colorLightGreen;
                }
                else
                {
                    color = sandboxInfo.paused ? SpiralEditor.colorLightOrange : SpiralEditor.colorLightRed;
                }

                SpiralEditor.BeginPanel(GroupType.Vertical, color);
                string isPaused = sandboxInfo.paused ? "PAUSED" : "IS RUNNING";
                EditorGUILayout.LabelField($"IDX {sandboxInfo.idx} {isPaused}", SpiralEditor.panel);

                EditorGUI.indentLevel += 1;
                GUI.enabled = false;
                if (sandboxInfo.hasReference) // имеет опорный объект
                {
                    if (sandboxInfo.referenceObject != null) // опорный объект существует
                    {
                        if (sandboxInfo.isReferenceUnityObject)
                        {
                            _ = EditorGUILayout.ObjectField("UnityObject: ",
                                sandboxInfo.referenceObject as UnityEngine.Object,
                                sandboxInfo.referenceObjectType,
                                true);
                        }
                        else
                        {
                            string name = "System.Object";
                            string data = $"{sandboxInfo.referenceObject} [{sandboxInfo.referenceObjectType}]";
                            EditorGUILayout.LabelField(name, data);
                        }
                    }
                    else // опорный объект утерян
                    {
                        if (sandboxInfo.isReferenceUnityObject)
                        {
                            _ = EditorGUILayout.ObjectField("UnityObject: ", null, sandboxInfo.referenceObjectType, true);
                        }
                        else
                        {
                            string name = "System.Object";
                            string data = $"<missing> [{sandboxInfo.referenceObjectType}]";
                            EditorGUILayout.LabelField(name, data);
                        }
                    }
                }
                else // запуск без опорного объекта
                {
                    EditorGUILayout.LabelField("No reference object");
                }

                EditorGUILayout.LabelField("Sender Type", $"[{sandboxInfo.senderType}]");

                string callbackName;
                object target = sandboxInfo.callback.Target;
                string parent = $"[{target}]";
                callbackName = $"{sandboxInfo.callback.Method.Name} {parent}";

                EditorGUILayout.LabelField("Method", $"{callbackName}");

                EditorGUI.indentLevel -= 1;
                GUI.enabled = true;

                SpiralEditor.BeginGroup(GroupType.Horizontal);
                EditorGUILayout.Space(2); 
                GUI.enabled = sandboxInfo.idx != Sandbox.sandboxTotalCount - 1;
                if (SpiralEditor.DrawRoundButton("Move Down", buttonWidth))
                {
                    Sandbox.MoveDown(sandboxInfo.idx);
                }
                GUI.enabled = true;
                EditorGUILayout.Space(2);
                if (SpiralEditor.DrawRoundButton("Kill callback", buttonWidth)) Sandbox.RemoveCallback(sandboxInfo.idx);
                string pause = sandboxInfo.paused ? "Unpause" : "Pause";
                if (SpiralEditor.DrawRoundButton(pause, buttonWidth))
                {
                    if (sandboxInfo.paused) Sandbox.UnpauseCallback(sandboxInfo.idx);
                    else Sandbox.PauseCallback(sandboxInfo.idx);
                }
                EditorGUILayout.Space(2);
                GUI.enabled = sandboxInfo.idx != 0;
                if (SpiralEditor.DrawRoundButton("Move Up", buttonWidth))
                {
                    Sandbox.MoveUp(sandboxInfo.idx);
                }
                GUI.enabled = true;
                EditorGUILayout.Space(2);
                SpiralEditor.EndGroup();

                EditorGUILayout.Space(2);
                SpiralEditor.EndPanel();
            }
        }
    }
}
#endif
