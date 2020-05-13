using UnityEngine;
using Spiral.EditorToolkit.EditorSandbox;
using System;
using System.Collections.Generic;

namespace Spiral.EditorToolkit.EditorSandbox.Examples
{
    public class ExampleNestedDemo : MonoBehaviour
    {
        [Serializable]
        public class ExampleMoreNestedClass
        {
            public SandboxField exampleNestedSandbox;
            public List<SandboxField> evenMoreNested;

            [RunInSandbox]
            public void ExampleMoreNested()
            {
                Debug.Log("Example More Nested running");
            }
        }

        [Serializable]
        public class ExampleNestedClass
        {
            public SandboxField exampleNestedSandbox;
            public List<ExampleMoreNestedClass> exampleSandboxesList;

            [RunInSandbox]
            public void ExampleNested()
            {
                Debug.Log("Example Nested running");
            }
        }

        [Space]
        [Header("Example Sandbox External Class")]
        public ExampleExternalClass externalClassVariable;

        [Space]
        [Header("Example Nested Class")]
        public ExampleNestedClass nestedClassVariable;

        [RunInSandbox]
        protected void SomeSimulation()
        {
            Debug.Log("Some simulation running");
        }
    }

    [Serializable]
    public class ExampleExternalClass
    {
        public SandboxField exampleExternalSandbox;

        [RunInSandbox]
        public void RunExternalInstance()
        {
            Debug.Log("External class instance");
        }

        [RunInSandbox]
        public static void RunExternalStatic()
        {
            Debug.Log("External class static public");
        }
    }
}
