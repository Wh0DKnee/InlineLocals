using EnvDTE90a;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InlineWatch
{
    class DebuggerCallback : IDebugEventCallback2
    {
        public delegate void LocalsChangedEventHandler(object sender, StackFrame2 frame);
        public event LocalsChangedEventHandler LocalsChangedEvent = delegate{};

        public delegate void AfterLocalsChangedEventHandler(object sender);
        public event AfterLocalsChangedEventHandler AfterLocalsChangedEvent = delegate{};

        private static readonly Lazy<DebuggerCallback> lazy =
            new Lazy<DebuggerCallback> (() => new DebuggerCallback());

        public static DebuggerCallback Instance { get { return lazy.Value; } }
        private DebuggerCallback() { }

        public int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            Guid debugExpressionsDirtyEventGuid = new Guid("ce6f92d3-4222-4b1e-830d-3ecff112bf22");
            if (!debugExpressionsDirtyEventGuid.Equals(riidEvent)) {
                return VSConstants.S_OK;
            }
            EnvDTE.DTE DTE = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            if (DTE == null) {
                Console.WriteLine("Could not get DTE service.");
                return VSConstants.E_UNEXPECTED;
            }
            if (DTE.Debugger == null) {
                Console.WriteLine("Could not get Debugger.");
                return VSConstants.E_UNEXPECTED;
            }
            if (DTE.Debugger.CurrentStackFrame == null) {
                // No current stack frame.
                return VSConstants.E_UNEXPECTED;
            }
            StackFrame2 stackFrame = DTE.Debugger.CurrentStackFrame as StackFrame2;
            if (stackFrame == null) {
                Console.WriteLine("CurrentStackFrame is not a StackFrame2.");
                return VSConstants.E_UNEXPECTED;
            }
            EnvDTE.Expressions locals = stackFrame.Locals;
            foreach (EnvDTE.Expression local in locals) {
                //EnvDTE.Expressions members = local.Name;
            }

            if(LocalsChanged()) {
                LocalsChangedEvent(this, stackFrame);
            }

            return VSConstants.S_OK;
        }

        private bool LocalsChanged() {
            return true; // TODO: properly implement so we dont send out updates when it's not needed
        }

        public void InvokeAfterLocalsChangedEvent() {
            AfterLocalsChangedEvent(this);
        }
    }
}
