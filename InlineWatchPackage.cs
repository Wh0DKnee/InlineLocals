using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;


using Task = System.Threading.Tasks.Task;

namespace InlineWatch
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(InlineWatchPackage.PackageGuidString)]
    public sealed class InlineWatchPackage : AsyncPackage, IVsSolutionEvents2
    {
        /// <summary>
        /// InlineWatchPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "4fb5b72b-b60a-4906-8a89-12dbbdd14576";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);


            /*IVsDebugger debugger = GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
            if (!(debugger is null)) {
                debugger.AdviseDebugEventCallback(DebuggerCallback.Instance);
            }*/
            IVsDebugger debugger = await GetServiceAsync(typeof(SVsShellDebugger)) as IVsDebugger;
            Assumes.Present(debugger);
            if (!(debugger is null)) {
                debugger.AdviseDebugEventCallback(DebuggerCallback.Instance);
            } else {
                Console.WriteLine("Couldnt get debugger service.");
            }

            IVsSolution solution = GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(solution);
            uint cookie = 0;
            solution.AdviseSolutionEvents(this, out cookie);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution) {
            // init stuff here?
            /*Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            IVsDebugger debugger = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SVsShellDebugger)) as IVsDebugger;
            if (!(debugger is null)) {
                debugger.AdviseDebugEventCallback(DebuggerCallback.Instance);
            } else {
                Console.WriteLine("Couldnt get debugger service.");
                return VSConstants.E_FAIL;
            }*/

            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }

        public int OnAfterMergeSolution(object pUnkReserved) {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
