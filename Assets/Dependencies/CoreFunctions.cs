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

using System;
using System.Collections.Generic;

namespace Spiral.Core
{
    public static class CoreFunctions
    {
        public static string FirstLetterCapitalization(this string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Length == 1) return str.ToUpper();
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static List<T> Array2List<T>(this T[] ts)
        {
            if (ts == null) return new List<T>();
            if (ts.Length == 0) return new List<T>();
            return new List<T>(ts);
        }

        public static T GetLast<T>(this IList<T> ts) 
        {
            return ts[ts.Count - 1];
        }

        public static void RemoveLast<T>(this IList<T> ts)
        {
            ts.RemoveAt(ts.Count - 1);
        }

        public static int CountOf(this MulticastDelegate multicast, Delegate action)
        {
            if (multicast == null) return 0;
            var delegates = multicast.GetInvocationList();
            int count = 0;
            foreach (var del in delegates)
            {
                if (del.Equals(action)) count++;
            }
            return count;
        }

        public static int Count(this MulticastDelegate multicast)
        {
            if (multicast == null) return 0;
            var delegates = multicast.GetInvocationList();
            return delegates.Length;
        }

        public static bool HasDelegate(this MulticastDelegate multicast, Delegate action)
        {
            if (multicast == null) return false;
            var delegates = multicast.GetInvocationList();
            foreach (var del in delegates) { if (del.Equals(action)) return true; }
            return false;
        }

        public static void KillInvokations(ref Action action)
        {
            if (action == null) return;
            var delegates = action.GetInvocationList();
            foreach (var del in delegates)
            {
                action -= del as Action;
            }
        }

        public static void KillInvokations<T>(ref Action<T> action)
        {
            if (action == null) return;
            var delegates = action.GetInvocationList();
            foreach (var del in delegates)
            {
                action -= del as Action<T>;
            }
        }

        public static void KillInvokations<T1, T2>(ref Action<T1, T2> action)
        {
            if (action == null) return;
            var delegates = action.GetInvocationList();
            foreach (var del in delegates)
            {
                action -= del as Action<T1, T2>;
            }
        }
    }

}
