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
using System.Reflection;
using System;

namespace Spiral.EditorToolkit.EditorSandbox
{
    /// <summary>
    /// Поле, позволяющее вызывать с родительского методы, помеченные как RunInSandbox.
    /// </summary>
    [Serializable]
    public class SandboxField
    {
        [SerializeReference]
        private object m_target = null;
        public object target { get { return m_target; } private set { m_target = value; } }

        [SerializeReference] // TODO: check it
        private Type m_targetType;
        public Type targetType { get { return m_targetType; } }

        [SerializeField] private int m_selectedTarget = 0;
        public int selectedTarget { get { return m_selectedTarget; } set { m_selectedTarget = value; } }

        [SerializeField] private int m_selected = -1;
        public int selected
        {
            get
            {
                // forced validation
                if (m_selected < -1) m_selected = -1;
                if (m_selected >= m_methodNames.Count) // achtung!
                {
                    Update();
                    if (m_selected >= m_methodNames.Count)
                    {
                        m_selected = (m_methodNames.Count == 0) ? -1 : m_methodNames.Count - 1;
                    }
                }
                return m_selected;
            }
            set
            {
                if (m_methodNames.Count == 0) { m_selected = -1; return; }
                m_selected = Mathf.Clamp(value, 0, m_methodNames.Count - 1);
            }
        }

        private List<string> m_methodNames = new List<string>();
        private List<MethodInfo> m_methodInfos = new List<MethodInfo>();

        public void InitSimulators(object target)
        {
            this.target = target;
            Update();
        }

        private void Update()
        {
            if (m_methodNames == null) m_methodNames = new List<string>();
            else m_methodNames.Clear();
            if (m_methodInfos == null) m_methodInfos = new List<MethodInfo>();
            else m_methodInfos.Clear();
            m_targetType = null;

            if (target == null)
            {
                return;
            }

            m_targetType = target.GetType();
            m_methodInfos = RunInSandbox.GetTypeSandboxes(m_targetType);

            if (m_methodInfos.Count == 0) return;

            for (int i = 0; i < m_methodInfos.Count; i++)
            {
                MethodInfo methodInfo = m_methodInfos[i];

                string name = methodInfo.Name;
                bool isPrivate = methodInfo.IsPrivate;
                if (isPrivate) name += " [Private]";
                else
                {
                    bool isPublic = methodInfo.IsPublic;
                    if (isPublic) name += " [Public]";
                    else
                    {
                        bool isFamily = methodInfo.IsFamily;
                        if (isFamily) name += " [Protected]"; // TODO: не работает с protected internal
                    }
                }

                bool isStatic = methodInfo.IsStatic;
                if (isStatic) name += " [Static]";
                
                m_methodNames.Add(name);
            }
        }

        public string[] GetMethodNames()
        {
            return m_methodNames.ToArray();
        }

        private Action m_action;
        private bool InitializeAction()
        {
            if (this.target == null) return false;
            if (selected == -1) return false;
            Type actionType = typeof(Action);
            MethodInfo methodInfo = m_methodInfos[selected];
            if (methodInfo == null) return false;

            object target = methodInfo.IsStatic ? null : this.target;
            m_action = Delegate.CreateDelegate(actionType, target, methodInfo) as Action;
            
            if (m_action == null) return false;
            return true;
        }

#if UNITY_EDITOR
        public void LaunchInSandbox()
        {
            if (!InitializeAction()) return;
            bool isStatic = m_action.Method.IsStatic;
            object target = isStatic ? null : this.target;
            Sandbox.AddCallback(target, typeof(SandboxField), m_action);
        }
#endif
    }
}
