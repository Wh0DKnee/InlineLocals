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
        // TODO: create an event that every tagger subscribes to,
        // we then fire the event when the locals changed, and supply
        // the fileName of the debugged file. That way, taggers of 
        // another file can ignore the event.

        private static readonly Lazy<DebuggerCallback> lazy =
            new Lazy<DebuggerCallback> (() => new DebuggerCallback());

        public static DebuggerCallback Instance { get { return lazy.Value; } }
        private DebuggerCallback() { }

        public int Event(IDebugEngine2 pEngine, IDebugProcess2 pProcess, IDebugProgram2 pProgram, IDebugThread2 pThread, IDebugEvent2 pEvent, ref Guid riidEvent, uint dwAttrib) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE DTE = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            if (DTE == null) {
                Console.WriteLine("Could not get DTE service.");
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
                EnvDTE.Expressions members = local.DataMembers;
                
                // Do this section recursively, looking down in each expression for 
                // the next set of data members. This will build the tree.
                // DataMembers is never null, instead just iterating over a 0-length list.
            }

            return VSConstants.S_OK;
        }
    }
}
