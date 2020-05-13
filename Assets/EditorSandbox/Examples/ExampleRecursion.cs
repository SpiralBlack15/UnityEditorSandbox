using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Spiral.EditorToolkit.EditorSandbox.Examples
{
    public class ExampleRecursion : MonoBehaviour
    {
        public SandboxField field = new SandboxField();

        [RunInSandbox]
        private void RecursiveCall()
        {
            StackTrace trace = new StackTrace();
            UnityEngine.Debug.Log(trace.FrameCount);
            RecursiveCall();
        }
    }
}
