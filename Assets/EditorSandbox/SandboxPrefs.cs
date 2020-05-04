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
    [CreateAssetMenu(fileName = "SandboxPrefs", menuName = "Spiral/SanboxPrefs", order = 1)]
    public class SandboxPrefs : ScriptableObject
    {
        public static string standartName = "SandboxPrefs";
        public static string standartCreatePath = "Assets/Resources/SandboxPrefs";
        private static SandboxPrefs prefs = null;

        public bool allowEditorSimulatior = true;
        public SandboxMinimalStep minimalStepMode = SandboxMinimalStep.FixedUpdate;
        public float customMinimalTimeStep = 0.01f;
        public float checkAverageDeltaEvery = 10f;
        public bool clearReferences = true;
        public bool secureMode = true;

        public static void LoadGlobal()
        {
            if (prefs == null) LoadFromResources();
            prefs.Load();
        }

        public static void SaveGlobal()
        {
            if (prefs == null) LoadFromResources();
            prefs.Save();
        }

        public void Load()
        {
            Sandbox.isRunning              = allowEditorSimulatior;
            Sandbox.minimalStepMode        = minimalStepMode;
            Sandbox.customMinimalTimeStep  = customMinimalTimeStep;
            Sandbox.checkAverageDeltaEvery = checkAverageDeltaEvery;
            Sandbox.clearReferences        = clearReferences;
            Sandbox.secureMode             = secureMode;
        }

        public void Save()
        {
            allowEditorSimulatior  = Sandbox.isRunning;
            minimalStepMode        = Sandbox.minimalStepMode;
            customMinimalTimeStep  = Sandbox.customMinimalTimeStep;
            checkAverageDeltaEvery = Sandbox.checkAverageDeltaEvery;
            clearReferences        = Sandbox.clearReferences;
            secureMode             = Sandbox.secureMode;
        }

        private static void LoadFromResources()
        {
            prefs = (SandboxPrefs)Resources.Load(standartName);
            if (prefs == null)
            {
                prefs = CreateInstance<SandboxPrefs>();
                AssetDatabase.CreateAsset(prefs, standartCreatePath);
                prefs.Save();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif