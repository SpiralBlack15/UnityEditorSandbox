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
using System;
using System.Diagnostics;
using Spiral.Core;

using static UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
namespace Spiral.EditorToolkit.EditorSandbox
{
    public static class Sandbox 
    {
        public enum MinimalStep
        {
            Uncontrollable = -1, // no minimal step value
            FixedUpdate = 0, // minimal step value == Time.fixedDeltaTime
            Custom = 1, // minimal step value defined by user
        }

        // PREFERENCES ----------------------------------------------------------------------------
        private static bool m_isRunning = false;
        /// <summary>
        /// Включает-выключает подписку основного цикла симулятора на EditorApplication.update.
        /// Подписки других объектов на сам симулятор остаются до их удаления тем или иным образом,
        /// т.е. нет необходимости заново подписываться на симулятор при его включении.
        /// Они не будут вызываться до тех пор, пока не будет включен симулятор.
        /// ВНИМАНИЕ: независимо от того, включена ли автмотическая очистка, при входе в цикл
        /// или выходе из нег все невалидные подписки будут уничтожены автоматически.
        /// Сохраняется только между сессиями во избежание ряда неприятностей
        /// </summary>
        public static bool isRunning 
        {
            get { return m_isRunning; }
            set
            {
                if (m_isRunning == value) return;
                OnBeforeParamChange();
                m_isRunning = value;
                SandboxIsRunning(value, SandboxSimulationCycle);
            }
        }

        public static bool autoLaunch = true;

        private static float m_customMinimalTimeStep = 0.01f;
        /// <summary>
        /// Минимальный шаг симуляции в эдиторе в режиме Custom. При выставлении в ноль
        /// режим ничем не отличается от Uncontrollable. Назначением этой функции является
        /// срезание слишком малого шага обновления. Обычно эта настройка не делает особой
        /// погоды, т.к. шаг обновления цикла симулятора в среднем держится стабильно 
        /// выше 0.1 даже в неконтролируемом режиме. 
        /// Однако эта настройка может быть полезной, если вам требуется выдерживать
        /// равномерный шаг на значениях 1 секунды и больше. На значениях ниже 1 секунды
        /// шаг обновления будет неравномерным почти в любом случае.
        /// </summary>
        public static float customMinimalTimeStep // in editor prefs
        {
            get { return m_customMinimalTimeStep; }
            set
            {
                value = value > 0 ? value : 0;
                if (m_customMinimalTimeStep == value) return;
                OnBeforeParamChange();
                m_customMinimalTimeStep = value;
            }
        }

        private static MinimalStep m_minimalStepMode = MinimalStep.FixedUpdate;
        /// <summary>
        /// Текущий режим отсечки по минимальному шагу.
        /// </summary>
        public static MinimalStep minimalStepMode // in editor prefs
        {
            get { return m_minimalStepMode; }
            set
            {
                if (m_minimalStepMode == value) return;
                OnBeforeParamChange();
                m_minimalStepMode = value;
            }
        }

        private static float m_checkAverageDeltaEvery = 15;
        /// <summary>
        /// Определяет то, как часто обновляется счётчик среднего шага.
        /// Желательно выставлять числа больше 1 секунды. Во-первых,
        /// так реже генерируется соответствующее событие, во-вторых,
        /// так точнее результат. Однако слишком большие значения также
        /// могут быть не очень надёжными, поскольку могут включать интервалы
        /// протормаживания ПО.
        /// Рекомендуемые значения: 5-20 с.
        /// </summary>
        public static float checkAverageDeltaEvery // in editor prefs
        {
            get { return m_checkAverageDeltaEvery; }
            set
            {
                value = value > 0.1f ? value : 0.1f;
                if (m_checkAverageDeltaEvery == value) return;
                OnBeforeParamChange();
                m_checkAverageDeltaEvery = value;
            }
        }

        /// <summary>
        /// Предполагает проверку валидности референсных подписок на каждом шагу.
        /// Это позволяет избежать большей части конфликтов, возникающих, например, при
        /// удалении объекта, которому принадлежит подписка, однако может замедлить работу
        /// симулятора при большом количестве подписок.
        /// Если вы работаете в другом режиме, вы должны сами озаботиться удалением
        /// подписок, как только те становятся невалидными.
        /// ВНИМАНИЕ: если этот режим не включен, стабильность симулятора зависит только
        /// от вас, как и все возможные приятные и неприятные последствия выключения
        /// этого режима!
        /// </summary>
        public static bool autoClearReferences = true; // in editor prefs

        /// <summary>
        /// Режим работы, запускающий симуляции в try-catch скобках и при обнаружении 
        /// любого вида конфликтов снимающий процесс, вызвавший конфликт.
        /// </summary>
        public static bool secureMode = true;

        // TIME -----------------------------------------------------------------------------------
        /// <summary>
        /// Минимально допустимый шаг времени обновления симулятора.
        /// Внимание: это НЕ реальный шаг времени, а только нижний порог
        /// отсечки!
        /// </summary>
        public static float minimalTimeStep
        {
            get
            {
                switch (minimalStepMode)
                {
                    case MinimalStep.Uncontrollable: return 0;
                    case MinimalStep.FixedUpdate: return Time.fixedDeltaTime;
                    case MinimalStep.Custom: return customMinimalTimeStep;
                    default: return 0;
                }
            }
        }

        /// <summary>
        /// Реальный последний шаг между обновлениями. 
        /// Можно использовать вместо deltaTime/fixedDeltaTime в симуляциях.
        /// </summary>
        public static float editorDeltaTime { get; private set; } = 0;

        /// <summary>
        /// Среднее по палате за обозначенный период времени.
        /// </summary>
        public static float averageDeltaTime { get; private set; } = 0;

        /// <summary>
        /// Вызывается всякий раз, когда пересчитывается среднее по палате.
        /// Озаботьтесь тем, чтобы эта штука не вызывалась слишком часто.
        /// </summary>
        public static event Action onAverageDeltaTimeUpdate;

        // PRIVATES FOR TIME CHECKING -------------------------------------------------------------
        /// <summary>
        /// Следит за временем, проходящим между циклами
        /// </summary>
        private static readonly Stopwatch m_cycleWatch;

        /// <summary>
        /// Следит за временем, затрачиваемым на выполнение нагрузки песчницы
        /// </summary>
        private static readonly Stopwatch m_actionWatch;



        // SUBSCRIPTIONS --------------------------------------------------------------------------
        /// <summary>
        /// Внутреннее событие, на которое подписываются внешние вызовы.
        /// В норме должно содержать все вызовы из журнала вызовов.
        /// Если журнал вызовов пуст, это событие не будет вызываться, так
        /// как выход произойдёт раньше.
        /// </summary>
        private static event Action onSandboxCycle;

        /// <summary>
        /// Количество подписок на событии onSandboxCycle.
        /// В норме этот параметр должен быть равен sandboxEventsCount. Их может быть меньше,
        /// если что-то поставлено на паузу.
        /// </summary>
        public static int sandboxCurrentCount
        {
            get
            {
                if (onSandboxCycle == null) return 0;
                return onSandboxCycle.GetInvocationList().Length;
            }
        }

        private static readonly List<SandboxEvent> sandboxEvents = new List<SandboxEvent>();
        /// <summary>
        /// Все подписки
        /// </summary>
        public static int sandboxTotalCount { get { return sandboxEvents.Count; } }

        /// <summary>
        /// Внешнее событие, вызывающееся в конце шага симулятора, если у симулятора есть 
        /// нагрузка (т.е. количество событий в журнале не равно 0).
        /// Внимание, это событие не контролируется ничем, а подписки на него НЕ отображаются
        /// в журнале симулятора!
        /// </summary>
        public static event Action onSandboxStep;

        // FUNCTIONS ==============================================================================
        // Собственно, вся рабочая начинка
        //=========================================================================================
        static Sandbox()
        {
            CompilationPipeline.compilationStarted -= OnCompilationStart;
            CompilationPipeline.compilationStarted += OnCompilationStart;

            EditorApplication.quitting -= OnEditorApplicationQuit;
            EditorApplication.quitting += OnEditorApplicationQuit;

            m_actionWatch = new Stopwatch();
            m_cycleWatch  = new Stopwatch();

            SandboxPrefs.LoadSettings();
            if (SandboxPrefs.waitForRestore) 
            {
                SandboxPrefs.LoadSession();
            }

            SandboxIsRunning(isRunning, SandboxSimulationCycle);
        }

        private static void FakeOnDestroy()
        {
            // doing some stuff here
        }

        private static void OnEditorApplicationQuit()
        {
            FakeOnDestroy();
        }

        private static void OnCompilationStart(object obj)
        {
            FakeOnDestroy();
            SandboxPrefs.SaveSession();
        }

        private static void SandboxIsRunning(bool isRunning, EditorApplication.CallbackFunction callback)
        {
            // safety reasons
            EditorApplication.update -= callback;
            if (isRunning)
            {
                EditorApplication.update += callback;
            }
        }

        private static void OnBeforeParamChange()
        {
            CalcAverageDeltaTime();
            if (autoClearReferences) CleanNullReferences();
        }

        private static void PerformNormal()
        {
            if (autoClearReferences) CleanNullReferences();
            try
            {
                onSandboxCycle?.Invoke();
            }
            catch (Exception error)
            {
                m_actionWatch.Stop();
                LogWarning($"<color=red><b>Sandbox</b></color> has crushed down and will be switched off. " +
                           $"See stack trace below\n\n" + 
                           error.StackTrace); 
                RemoveAll();
                GC.Collect();
            }
            SceneView.RepaintAll();
        }

        private static int currentSafeIDX = 0;
        public static void PerformSafe()
        {
            currentSafeIDX = -1;
            if (autoClearReferences) CleanNullReferences();
            
            try
            {
                for (int i = 0; i < sandboxEvents.Count; i++)
                {
                    currentSafeIDX = i;
                    if (sandboxEvents[i].paused) continue;
                    sandboxEvents[i].callback.Invoke();
                }
                currentSafeIDX = -1;
                SceneView.RepaintAll();
            }
            catch (Exception error)
            {
                m_actionWatch.Stop();
                string hex = SpiralStyles.hexDarkRed;

                // С большой вероятностью UnityEditor упадёт до того, как try-catch
                // каким-то чудом успеет обработать ошибки StackOverflow и OutOfMemory.
                // Причины довольно прозаичны: см. как работают эти исключения в C# версий
                // .NET выше 2.0. Но если уж это случилось, выведем большим шрифтом пользователю
                // что он редиска и не лечится.
                // Ошибку ThreadAbort мы не ловим просто потому что Unity умрёт сразу :)
                if (error is StackOverflowException)
                {
                    LogWarning($"<color={hex}><b>STACK OVERFLOW WARNING</b></color>");
                }
                else if (error is OutOfMemoryException)
                {
                    LogWarning($"<color={hex}><b>OUT OF MEMORY WARNING</b></color>");
                }

                string ename = error.GetType().Name;
                LogWarning($"<color={hex}><b>Event {currentSafeIDX}</b></color> has crushed down " +
                           $"due to <color={hex}><b>{ename}</b></color> and will be switched off. " +
                           $"See stack trace below:\n\n" +
                           error.StackTrace);

                RemoveCallback(currentSafeIDX);
            }
        }

        // TIME CHECKING --------------------------------------------------------------------------
        private static float m_editorElapsedTime  = 0;
        private static float m_overallEditorTicks = 0;
        private static int m_editorTicks = 0;

        private static void CalcAverageDeltaTime()
        {
            if (m_editorTicks == 0) return;
            if (m_cycleWatch.IsRunning) m_cycleWatch.Stop();
            CalcAverageDeltaTimeUnsafe();
        }

        private static void CalcAverageDeltaTimeUnsafe()
        {
            averageDeltaTime = m_overallEditorTicks / m_editorTicks;
            m_editorTicks = 0;
            m_overallEditorTicks = 0;
            onAverageDeltaTimeUpdate?.Invoke();
        }

        private static void SandboxSimulationCycle()
        {
            m_cycleWatch.Stop();
            if (m_actionWatch.IsRunning) m_actionWatch.Stop();

            if (m_overallEditorTicks > checkAverageDeltaEvery)
            {
                CalcAverageDeltaTimeUnsafe();
            }
            else
            {
                m_editorTicks++;
                m_overallEditorTicks += editorDeltaTime;
            }

            if (minimalStepMode == MinimalStep.Uncontrollable)
            {
                editorDeltaTime = m_cycleWatch.Elapsed.Milliseconds * 0.001f;
            }
            else
            {
                m_editorElapsedTime += m_cycleWatch.Elapsed.Milliseconds * 0.001f;
                editorDeltaTime = m_editorElapsedTime;
            }

            if (sandboxTotalCount == 0)
            {
                m_cycleWatch.Start();
                return;
            }

            m_actionWatch.Start();
            if (minimalStepMode == MinimalStep.Uncontrollable)
            {
                if (secureMode) PerformSafe(); else PerformNormal();
            }
            else
            {
                if (m_editorElapsedTime >= minimalTimeStep)
                {
                    m_editorElapsedTime = 0;
                    if (secureMode) PerformSafe(); else PerformNormal();
                }
            }
            m_actionWatch.Stop();

            onSandboxStep?.Invoke();
            m_cycleWatch.Start();
        }

        // EVENT FINDER ===========================================================================
        // Управление подписками
        //=========================================================================================

        /// <summary>
        /// Берёт данные по его индексу (нужно для Sandbox Editor Window)
        /// </summary>
        /// <param name="idx">Индекс</param>
        /// <returns></returns>
        public static SandboxEventInfo GetEventInfo(int idx)
        {
            if (idx < 0 || idx >= sandboxEvents.Count) return default;

            var sandboxEvent = sandboxEvents[idx];
            SandboxEventInfo info = new SandboxEventInfo()
            {
                isReferenceUnityObject = sandboxEvent.isReferenceUnityObject,
                callback = sandboxEvent.callback,
                hasReference = sandboxEvent.hasReference,
                referenceObject = sandboxEvent.referenceObject,
                referenceObjectType = sandboxEvent.referenceObjectType,
                senderType = sandboxEvent.senderType,
                paused = sandboxEvent.paused,
                idx = idx,
            };
            return info;
        }

        /// <summary>
        /// Ищет запись в журнале, соответствующую этому объекту и этому типу объекта-отправителя.
        /// Применять в том случае, если объект живой, известен отправитель, но неизвестен адрес
        /// исходного коллбека. Это может быть, например, в ситуации, когда подписка шла из
        /// Custom Editor'a и подписывалось событие внутри этого Editor'a. В таком случае мы имеем ситуацию,
        /// когда инспектируемый объект ещё живой, известен тип отправителя, но сам Custom Editor мог быть
        /// уже пересоздан, соответственно, адрес его функции с тем же названием НЕ соответствует адресу
        /// реальной подписки.
        /// </summary>
        /// <param name="anchor">Проверочный объект</param>
        /// <param name="senderType">Тип объекта-подписчика</param>
        /// <param name="callback">Вызов (от него будет использоваться только имя)</param>
        /// <returns>Возвращает номер учтётной записи в журнале, если параметры валидны
        /// Возвращает -1, если запись не найдена, а также если объект или отправитель == null</returns>
        public static int FindCallbackIDX(object anchor, Type senderType, Action callback)
        {
            if (anchor == null) return -1;
            if (senderType == null) return -1; // да, и такое может быть
            return sandboxEvents.FindIndex(x => (x.senderType == senderType) 
                                                && (x.referenceObject == anchor)
                                                && (x.callback.Method.Name == callback.Method.Name));
        }

        /// <summary>
        /// Проверка на минималках, когда у нас не известен ни проверочный объект, ни вызыватель
        /// </summary>
        /// <param name="callback">Функция обратного вызова</param>
        /// <returns></returns>
        public static int FindCallbackIDX(Action callback)
        {
            return sandboxEvents.FindIndex(x => x.callback == callback);
        }


        /// <summary>
        /// Проверка на минималках, когда у нас не известен ни проверочный объект, ни вызыватель
        /// </summary>
        /// <param name="callback">Функция обратного вызова</param>
        /// <returns></returns>
        public static int FindCallbackIDX(Type senderType, Action callback)
        {
            return sandboxEvents.FindIndex(x => (x.senderType == senderType) && 
                                           (x.callback.Method.Name == callback.Method.Name));
        }

        // ADD CALLBACKS ==========================================================================
        // Добавляет в песочницу коллбеки разными способами
        //=========================================================================================
        public static bool AddCallback(object reference, Type senderType, Action callback)
        {
            if (senderType == null || callback == null) return false;
            AddCallbackNoValidation(reference, senderType, callback);
            return true;
        }

        public static bool AddCallback(object sender, Action callback)
        {
            if (sender == null || callback == null) return false;
            AddCallbackNoValidation(sender, sender.GetType(), callback);
            return true;
        }

        public static bool AddCallback(Type sender, Action callback)
        {
            if (sender == null || callback == null) return false;
            AddCallbackNoValidation(null, sender, callback);
            return true;
        }

        /// <summary>
        /// Синхронизирует подписки песочницы с активным событием
        /// (для !secure режима)
        /// </summary>
        private static void RefreshCycleCalbacks()
        {
            int count = sandboxEvents.Count;
            EventsTools.KillInvokations(ref onSandboxCycle);
            for (int i = 0; i < count; i++)
            {
                if (!sandboxEvents[i].paused)
                    onSandboxCycle += sandboxEvents[i].callback;
            }
        }

        /// <summary>
        /// Заменяет коллбек по указанному адресу (без валидации)
        /// </summary>
        /// <param name="callback">Коллбек</param>
        /// <param name="jidx">Адрес</param>
        private static void ChangeCallbackNoValidation(Action callback, int jidx)
        {
            List<Action> actions = new List<Action>();
            int count = sandboxEvents.Count;
            for (int i = 0; i < count; i++)
            {
                Action existingCallback = sandboxEvents[i].callback;
                actions.Add(existingCallback);
                onSandboxCycle -= existingCallback;
            }
            for (int i = 0; i < count; i++)
            {
                Action existingCallback = sandboxEvents[i].callback;
                if (i != jidx)
                {
                    onSandboxCycle += existingCallback;
                }
                else
                {
                    onSandboxCycle += callback;
                }
            }
            sandboxEvents[jidx].UpdateCallback(callback);
        }

        /// <summary>
        /// Добавление коллбека (без валидации)
        /// </summary>
        /// <param name="reference">Референсный объект (если есть)</param>
        /// <param name="sender">Класс-отправитель</param>
        /// <param name="callback">Коллбек (реальный для нулевого референса, аналогичный для ненулевого)</param>
        private static void AddCallbackNoValidation(object reference, Type sender, Action callback)
        {
            int jidx = reference != null ?
                       FindCallbackIDX(reference, sender, callback) :
                       FindCallbackIDX(callback);
            if (jidx >= 0) // мы добавим симуляцию заново, чтобы держать порядок симуляций такой же, как и порядок вызовов
            {
                ChangeCallbackNoValidation(callback, jidx);
            }
            else
            {
                SandboxEvent entry = new SandboxEvent(reference, sender, callback); // работает и reference == null
                sandboxEvents.Add(entry);
                onSandboxCycle -= callback; // защита на всякий случай
                onSandboxCycle += callback;
            }
            if (autoLaunch) isRunning = true;
        }

        // REMOVE CALLBACKS =======================================================================
        //=========================================================================================
        /// <summary>
        /// Удаляет вызов из цикла симулятора принудительно 
        /// </summary>
        /// <param name="callback">Вызов</param>
        /// <returns>TRUE - если вызов был удалён; FALSE - если вызов не был найден</returns>
        public static bool RemoveCallback(Action callback) 
        {
            int jidx = FindCallbackIDX(callback);
            if (jidx < 0) return false;
            return RemoveCallback(jidx);
        }

        /// <summary>
        /// Удаляет вызов из цикла симулятора, используя индекс вызова в журнале вызовов
        /// </summary>
        /// <param name="callbackIDX">Индекс вызова в журнале вызовов</param>
        /// <returns>TRUE - если вызов был удалён; FALSE - если вызов не был найден</returns>
        public static bool RemoveCallback(int callbackIDX)
        {
            if (callbackIDX < 0) return false;
            if (callbackIDX >= sandboxEvents.Count) return false;

            Action callback = sandboxEvents[callbackIDX].callback;
            onSandboxCycle -= callback;
            sandboxEvents.RemoveAt(callbackIDX);

            return true;
        }

        /// <summary>
        /// Масс-килл всех подписок
        /// </summary>
        public static void RemoveAll()
        {
            EventsTools.KillInvokations(ref onSandboxCycle);
            sandboxEvents.Clear();
        }

        // STOP CALL ==============================================================================
        //=========================================================================================
        public static bool PauseCallback(int callbackIDX)
        {
            if (callbackIDX < 0 || callbackIDX >= sandboxTotalCount) return false;
            sandboxEvents[callbackIDX].paused = true;
            onSandboxCycle -= sandboxEvents[callbackIDX].callback;
            return true;
        }

        public static bool UnpauseCallback(int callbackIDX)
        {
            if (callbackIDX < 0 || callbackIDX >= sandboxTotalCount) return false;
            if (sandboxEvents[callbackIDX].paused == false) return false; 
            sandboxEvents[callbackIDX].paused = false;
            RefreshCycleCalbacks();
            return true;
        }

        public static void MoveUp(int callbackIDX)
        {
            if (callbackIDX < 1 || callbackIDX >= sandboxTotalCount) return;

            int prev = callbackIDX - 1;

            SandboxEvent event0 = sandboxEvents[prev];
            SandboxEvent event1 = sandboxEvents[callbackIDX];

            sandboxEvents.Remove(event0);
            sandboxEvents.Remove(event1);

            sandboxEvents.Insert(prev, event0);
            sandboxEvents.Insert(prev, event1); 

            RefreshCycleCalbacks();
        }

        public static void MoveDown(int callbackIDX)
        {
            if (callbackIDX < 0 || callbackIDX >= sandboxTotalCount - 1) return;

            int next = callbackIDX + 1;

            SandboxEvent event0 = sandboxEvents[callbackIDX];
            SandboxEvent event1 = sandboxEvents[next];

            sandboxEvents.Remove(event0);
            sandboxEvents.Remove(event1);

            sandboxEvents.Insert(callbackIDX, event0);
            sandboxEvents.Insert(callbackIDX, event1);

            RefreshCycleCalbacks();
        }

        // CLEANER ================================================================================
        // Удаление невалидных вызовов
        //=========================================================================================
        private static readonly List<SandboxEvent> killEntries = new List<SandboxEvent>();
        /// <summary>
        /// Принудительно чистит все вызовы, имеющие проверочный объект == null
        /// при hasReference == true. Нужен для принудительного снятия симуляций
        /// для разрушенных объектов.
        /// </summary>
        public static void CleanNullReferences()
        {
            if (sandboxEvents.Count == 0) return;
            killEntries.Clear();
            for (int i = 0; i < sandboxEvents.Count; i++)
            {
                SandboxEvent sandboxEventData = sandboxEvents[i];
                if (!sandboxEventData.hasReference) return; 

                if (sandboxEventData.isReferenceUnityObject) // Для юнити объектов
                {
                    var obj = sandboxEventData.referenceObject as UnityEngine.Object;
                    if (obj == null)
                    {
                        onSandboxCycle -= sandboxEventData.callback;
                        killEntries.Add(sandboxEventData);
                    }
                }
                else // Для дженерик пропертей
                {
                    var obj = sandboxEventData.referenceObject;
                    if (obj == null)
                    {
                        onSandboxCycle -= sandboxEventData.callback;
                        killEntries.Add(sandboxEventData);
                    }
                }
            }
            for (int i = 0; i < killEntries.Count; i++)
            {
                sandboxEvents.Remove(killEntries[i]);
            }
        }

        // SANDBOX EVENT DATA =========================================================================
        // Учётки подписок песочницы
        //=============================================================================================
        /// <summary>
        /// Данные о подписке, передаваемые вовне (в эдитор, в основном)
        /// </summary>
        public class SandboxEventInfo
        {
            public bool paused;
            public Action callback;
            public bool hasReference;
            public object referenceObject;
            public bool isReferenceUnityObject;
            public Type referenceObjectType;
            public Type senderType;
            public int idx;
        }

        /// <summary>
        /// Внутренние данные о подписке
        /// </summary>
        private class SandboxEvent
        {
            public bool paused = false;

            // callback -------------------------------------------------------------------------------
            public Action callback { get; private set; } = null;

            // has reference object -------------------------------------------------------------------
            public bool hasReference { get; private set; } = false;

            // reference object -----------------------------------------------------------------------
            public object referenceObject { get; private set; } = null;
            public Type referenceObjectType { get; private set; } = null;
            public bool isReferenceUnityObject { get; private set; } = false;

            // sender data ----------------------------------------------------------------------------
            public Type senderType { get; private set; } = null;

            // INITIALIZATION =========================================================================
            public SandboxEvent(object sender, Action callback)
            {
                if (sender == null) throw new ArgumentNullException("Sender cannot be null");
                if (sender is UnityEngine.Object)
                {
                    var usender = sender as UnityEngine.Object;
                    if (usender == null) throw new ArgumentNullException("Sender cannot be null");
                }
                UnreferencedInitialization(sender.GetType(), callback);
            }

            public SandboxEvent(object obj, Type sender, Action callback)
            {
                if (obj == null) UnreferencedInitialization(sender, callback);
                else ReferencedInitialization(obj, sender, callback);
            }

            public SandboxEvent(object obj, object sender, Action callback)
            {
                if (sender == null) throw new ArgumentNullException("Sender cannot be null");
                if (sender is UnityEngine.Object)
                {
                    var usender = sender as UnityEngine.Object;
                    if (usender == null) throw new ArgumentNullException("Sender cannot be null. If you need such call, you need to use Sender Type instead");
                }
                if (obj == null) UnreferencedInitialization(sender.GetType(), callback);
                else ReferencedInitialization(obj, sender.GetType(), callback);
            }

            private void ReferencedInitialization(object obj, Type sender, Action callback)
            {
                this.referenceObject = obj ?? throw new ArgumentNullException("Anchor object cannot be null");
                referenceObjectType = obj.GetType();
                isReferenceUnityObject = obj is UnityEngine.Object;
                if (isReferenceUnityObject)
                {
                    UnityEngine.Object uobj = obj as UnityEngine.Object;
                    if (uobj == null) throw new ArgumentNullException("Anchor object cannot be null");
                }
                hasReference = true;

                senderType = sender ?? throw new ArgumentNullException("Sender type cannot be null. If you need such call, you need to use Sender Type instead");
                this.callback = callback ?? throw new ArgumentNullException("Callback object cannot be null");
            }

            private void UnreferencedInitialization(Type sender, Action callback)
            {
                senderType = sender ?? throw new ArgumentNullException("Sender type cannot be null");
                this.callback = callback ?? throw new ArgumentNullException("Callback object cannot be null");
            }

            public void UpdateCallback(Action callback)
            {
                this.callback = null;
                this.callback = callback;
            }
        }
    }
}
#endif
