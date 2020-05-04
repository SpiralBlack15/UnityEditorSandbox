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

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Spiral.EditorToolkit.EditorSandbox
{
    /// <summary>
    /// Атрибут, позволяющий SandboxAdapter'у и Sandbox'у запускать методы в песочнице.
    /// Обратите внимание, что аттрибут бесполезен на функциях с параметрами,
    /// но может применяться к процедурам, возвращающим что угодно, а не только void
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RunInSandbox : Attribute
    {
        private static List<MethodInfo> SelectSandboxes(IEnumerable<MethodInfo> methods)
        {
            Type attributeType = typeof(RunInSandbox);
            methods = methods.Where(x => x.GetCustomAttribute(attributeType, true) != null);
            methods = methods.Where(x => x.GetParameters().Length == 0);
            methods = methods.Where(x => x.ReturnType == typeof(void));
            return new List<MethodInfo>(methods);
        }

        public static List<MethodInfo> GetAssemblySandboxes(Assembly assembly = null)
        {
            if (assembly == null) assembly = Assembly.GetExecutingAssembly();

            var types = assembly.GetTypes();
            IEnumerable<MethodInfo> methods = types.SelectMany(x => x.GetMethods());

            return SelectSandboxes(methods);
        }

        public static List<MethodInfo> GetTypeSandboxes(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | 
                                          BindingFlags.NonPublic | 
                                          BindingFlags.Instance | 
                                          BindingFlags.Public |
                                          BindingFlags.Static); // ищём всё, даже бездну, даже дьявола
            return SelectSandboxes(methods);
        }
    }
}
