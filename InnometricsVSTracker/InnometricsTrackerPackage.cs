using System;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using InnometricsVSTracker.Forms;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using NLog;
using NLog.Targets;
using NLog.Config;
using Document = EnvDTE.Document;
using Task = System.Threading.Tasks.Task;

namespace InnometricsVSTracker
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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0.0.0", IconResourceID = 400)]
    [Guid("1AF4B41B-F2DF-4F49-965A-816A103ADFEF")]
    [ProvideAutoLoad("ADFC4E64-0397-11D1-9F4E-00A0C911004F")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class InnometricsTrackerPackage : Package
    {
        public const string PackageGuidString = "522fc1b1-a511-4cdb-8be9-db382af4ff22";
        #region Fields

        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private SolutionEvents _solutionEvents;
        private TextEditorEvents _editorEvents;
        private DTEEvents _dteEvents;
        private IServiceProvider _serviceProvider;
        
        private Logger _logger;
        private Tracker _tracker;

        private static DTE2 _objDte;
        private static Sender _sender;

        // Settings
        private static readonly ConfigFile ConfigFile = new ConfigFile();
        #endregion

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            _objDte = (DTE2)GetService(typeof(DTE));
            _serviceProvider = ServiceProvider.GlobalProvider;
            _dteEvents = _objDte.Events.DTEEvents;
            _dteEvents.OnStartupComplete += OnOnStartupComplete;
            _tracker = new Tracker();

            InitializeLog();

            Task.Run(() =>
            {
                InitializeAsync();
            });
        }

        private void InitializeLog()
        {
            var config = new LoggingConfiguration();
            
            var consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);

            var fileTarget = new FileTarget();
            config.AddTarget("file", fileTarget);
            
            consoleTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";
            fileTarget.FileName = "${basedir}/file.txt";
            fileTarget.Layout = "${message}";
            
            var rule1 = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(rule1);

            var rule2 = new LoggingRule("*", LogLevel.Debug, fileTarget);
            config.LoggingRules.Add(rule2);
            
            LogManager.Configuration = config;
            //LogManager.ThrowExceptions = true;
            
            _logger = LogManager.GetLogger("InnoLog");
            _logger.Info("Program started");
        }

        private void InitializeAsync()
        {
            // VisualStudio Object                
            _docEvents = _objDte.Events.DocumentEvents;
            _windowEvents = _objDte.Events.WindowEvents;
            _solutionEvents = _objDte.Events.SolutionEvents;
            _editorEvents = _objDte.Events.TextEditorEvents;

            // setup event handlers
            _docEvents.DocumentSaved += DocumentEventsOnDocumentSaved;
            _editorEvents.LineChanged += TextEditorEventsOnLineChanged;
            _solutionEvents.BeforeClosing += SolutionEventsBeforeClosed;
            _windowEvents.WindowActivated += WindowEventsOnWindowActivated;
        }

        private void OnOnStartupComplete()
        {
            ConfigFile.Read();
            if (string.IsNullOrEmpty(ConfigFile.Token))
            {
                var form = new LoginForm();
                form.ShowDialog();
            }
            _sender = new Sender();
            LoginCommand.Initialize(this);
        }

        private void TextEditorEventsOnLineChanged(TextPoint startPoint, TextPoint endPoint, int hint)
        {
            HandleActivity();
        }

        private static SyntaxTree GetSyntaxTree(SnapshotPoint caretPosition)
        {
            return caretPosition.Snapshot.GetOpenDocumentInCurrentContextWithChanges().GetSyntaxRootAsync().Result.SyntaxTree;
        }

        private SnapshotPoint GetCaretPosition()
        {
            var textView = GetTextView();
            var caretPosition = textView.Caret.Position.BufferPosition;
            return caretPosition;
        }

        private Microsoft.VisualStudio.Text.Editor.IWpfTextView GetTextView()
        {
            var textManager = (IVsTextManager)_serviceProvider.GetService(typeof(SVsTextManager));
            textManager.GetActiveView(1, null, out var textView);
            return GetEditorAdaptersFactoryService().GetWpfTextView(textView);
        }
        private Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService GetEditorAdaptersFactoryService()
        {
            Microsoft.VisualStudio.ComponentModelHost.IComponentModel componentModel =
                (Microsoft.VisualStudio.ComponentModelHost.IComponentModel)_serviceProvider.GetService(
                    typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel));
            return componentModel.GetService<Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService>();
        }

        private static string GetProjectName()
        {
            string solutionName;
            try
            {
                solutionName = _objDte.Solution.FullName;
            }
            catch (Exception)
            {
                solutionName = null;
            }
            return !string.IsNullOrEmpty(solutionName)
                ? Path.GetFileNameWithoutExtension(solutionName)
                : !string.IsNullOrEmpty(_objDte.Solution?.FullName)
                    ? Path.GetFileNameWithoutExtension(_objDte.Solution.FullName)
                    : string.Empty;
        }

        private void SolutionEventsBeforeClosed()
        {
            SendActivities();
        }
       
        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                HandleActivity();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                Console.WriteLine(@"WindowEventsOnWindowActivated");
            }
        }

        private void HandleActivity()
        {
            var caretPosition = GetCaretPosition();
            var tree = GetSyntaxTree(caretPosition);
            _tracker.ProjectName = GetProjectName();
            _tracker.ProjectLanguage = caretPosition.Snapshot.GetOpenDocumentInCurrentContextWithChanges().Project.Language;
            _tracker.HandleActivity(tree, caretPosition);
        }

        private void DocumentEventsOnDocumentSaved(Document document)
        {
            SendActivities();
        }

        private void SendActivities()
        {
            if (_tracker.Activities.Count <= 0) return;
            _tracker.StopLastActivity();
            var sended = _sender.SendActivities(_tracker.Activities);
            _logger.Debug("Sended: " + sended.ToString());
            if (sended)
            {
                _tracker.ClearActivities();
            }
        }
        #endregion
    }
}
