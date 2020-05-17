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
namespace Spiral.EditorToolkit.EditorSandbox
{
    [CreateAssetMenu(fileName = "SandboxPrefs", menuName = "Spiral/SanboxPrefs", order = 1)]
    public class SandboxPrefs : ScriptableObject
    {
        private static SandboxPrefs m_instance = null;
        public static SandboxPrefs instance
        {
            get
            {
                if (m_instance == null) LoadFromResources();
                return m_instance;
            }
        }

        public readonly static string standartName = "SandboxPrefs";
        public readonly static string standartCreatePath = "Assets/Resources/SandboxPrefs";

        // GLOBAL SIMULATION PARAMS ---------------------------------------------------------------
        // Настройки симулятора, сохраняющиеся в любом случае
        //-----------------------------------------------------------------------------------------
        [SerializeField]private Sandbox.MinimalStep m_minimalStepMode = Sandbox.MinimalStep.Uncontrollable;
        public Sandbox.MinimalStep minimalStepMode
        {
            get { return m_minimalStepMode; }
            set { m_minimalStepMode = value; }
        }

        [SerializeField]private float m_customMinimalTimeStep = 0.01f;
        public float customMinimalTimeStep
        {
            get { return m_customMinimalTimeStep; }
            set { m_customMinimalTimeStep = value.Clamp0P(); }
        }

        [SerializeField]private float m_checkAverageDeltaEvery = 10f;
        public float checkAverageDeltaEvery
        {
            get { return m_checkAverageDeltaEvery; }
            set { m_checkAverageDeltaEvery = value.ClampLow(0.001f); }
        }

        [SerializeField]private bool m_autoClearReferences = true;
        public bool autoClearReferences
        {
            get { return m_autoClearReferences; }
            set { m_autoClearReferences = value; }
        }

        [SerializeField]private bool m_secureMode = true;
        public bool secureMode
        {
            get { return m_secureMode; }
            set { m_secureMode = value; }
        }

        // LAST SESSION ---------------------------------------------------------------------------
        // Настройки симулятора, валидные только в пределах сессий между компиляциями сборки
        //-----------------------------------------------------------------------------------------
        [SerializeField]private bool m_lastSessionActive = false;
        public static bool lastSessionActive { get { return instance.m_lastSessionActive; } }

        [SerializeField]private bool m_waitForRestore = false;
        public static bool waitForRestore { get { return instance.m_waitForRestore; } }

        // INSTANCE METHODS ========================================================================
        private void OnEditorQuitting()
        {
            m_waitForRestore = false; // сбросить флаг обязательно
        }

        private void SaveSessionInstance()
        {
            m_lastSessionActive = Sandbox.isRunning;
            m_waitForRestore = true;
        }

        private void LoadSessionInstance()
        {
            Sandbox.isRunning = m_lastSessionActive;
            m_waitForRestore = false;
        }

        private void LoadInstance()
        {
            Sandbox.minimalStepMode        = minimalStepMode;
            Sandbox.customMinimalTimeStep  = customMinimalTimeStep;
            Sandbox.checkAverageDeltaEvery = checkAverageDeltaEvery;
            Sandbox.autoClearReferences    = autoClearReferences;
            Sandbox.secureMode             = secureMode;
        }

        private void SaveInstance()
        {
            minimalStepMode        = Sandbox.minimalStepMode;
            customMinimalTimeStep  = Sandbox.customMinimalTimeStep;
            checkAverageDeltaEvery = Sandbox.checkAverageDeltaEvery;
            autoClearReferences    = Sandbox.autoClearReferences;
            secureMode             = Sandbox.secureMode;
        }

        // STATIC =================================================================================
        public static void LoadSettings() { instance.LoadInstance(); }
        public static void SaveSettings() { instance.SaveInstance(); }
        public static void LoadSession()  { instance.LoadSessionInstance(); }
        public static void SaveSession()  { instance.SaveSessionInstance(); }

        private static void LoadFromResources()
        {
            if (m_instance != null)
            {
                EditorApplication.quitting -= instance.OnEditorQuitting;
                m_instance = null;
            }

            m_instance = (SandboxPrefs)Resources.Load(standartName);
            if (m_instance == null)
            {
                m_instance = CreateInstance<SandboxPrefs>();
                AssetDatabase.CreateAsset(m_instance, standartCreatePath);
                m_instance.SaveInstance();
                AssetDatabase.SaveAssets();
            }
            EditorApplication.quitting += instance.OnEditorQuitting;
        }
    }
}
#endif