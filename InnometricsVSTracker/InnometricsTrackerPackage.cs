using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using InnometricsVSTracker.Forms;
using Microsoft.VisualStudio.TextManager.Interop;
using NLog;
using NLog.Targets;
using NLog.Config;
using Document = EnvDTE.Document;
using Task = System.Threading.Tasks.Task;
using System.Reflection;

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

        private static DTE2 _objDte;
        private static Sender _sender;

        // Settings
        private static string _solutionName = string.Empty;
        private static string _projectName = string.Empty;
        private static List<Activity> _activities = new List<Activity>();
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
            _solutionEvents.Opened += SolutionEventsOnOpened;
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

        private void HandleActivity()
        {
            var id = GetCurrentPath();
            _logger.Debug("Activity path: " + id);
            var last = _activities.LastOrDefault();
            if (last == null)
            {
                StartActivity(id);
            }
            else if (last.Measurements.Select(m => m).FirstOrDefault(n => n.Name == "code path")?.Value != id)
            {
                StopLastActivity();
                StartActivity(id);
            }
        }

        private void StopLastActivity()
        {
            var lastActivity = _activities.Last();
            if (lastActivity.Measurements.Select(m => m).LastOrDefault(m => m.Name == "end_time") != null) return;
            var stopTimestamp = new Measurement("end_time", "long", GetTimestamp().ToString());
            lastActivity.Measurements.Add(stopTimestamp);
        }

        private void StartActivity(string id)
        {
            var activity = new Activity("Visual Studio Extension", new List<Measurement>());
            var path = new Measurement("code path", "string", id);
            var startTimestamp = new Measurement("start_time", "long", GetTimestamp().ToString());
            var filePath = new Measurement("file path", "string", _objDte.ActiveDocument.Path);
            activity.Measurements.Add(path);
            activity.Measurements.Add(filePath);
            activity.Measurements.Add(startTimestamp);
            _activities.Add(activity);
        }

        private string GetCurrentPath()
        {
            var textView = GetTextView();
            var caretPosition = textView.Caret.Position.BufferPosition;
            var document = caretPosition.Snapshot.GetOpenDocumentInCurrentContextWithChanges();

            var span = document.GetSyntaxRootAsync().Result.FindToken(caretPosition).Span;
            var node = document.GetSyntaxRootAsync().Result.FindNode(span);
            var names = new List<String>();

            while (node != null)
            {
                if (node.IsKind(SyntaxKind.MethodDeclaration))
                {
                    names.Add("FUNC:"+(node as MethodDeclarationSyntax)?.Identifier.ValueText);
                }
                else if (node.IsKind(SyntaxKind.ClassDeclaration))
                {
                    names.Add("CLASS:"+(node as ClassDeclarationSyntax)?.Identifier.ValueText);
                }
                else if (node.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    names.Add("NS:"+(node as NamespaceDeclarationSyntax)?.Name.ToString());
                }
                else if (node.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    names.Add("FUNC:"+(node as ConstructorDeclarationSyntax)?.Identifier.ValueText);
                }
                else if (node.IsKind(SyntaxKind.SimpleLambdaExpression))
                {
                    names.Add("FUNC:[LAMBDA]");
                }
                else if (node.IsKind(SyntaxKind.AnonymousMethodExpression))
                {
                    names.Add("FUNC:[ANONYMOUS]");
                }
                node = node.Parent;
            }

            names.Reverse();
            //var id = document.Project.Language + "|" + _projectName + "|" + String.Join("|", names.ToArray());
            var textSelection = (TextSelection)_objDte.ActiveDocument.Selection;
            var line = textSelection.ActivePoint.Line;
            var id = "PROJ:" + _projectName + "|LANG:" + document.Project.Language + "|" + String.Join("|", names.ToArray()) + "|LINE:" + line;
            return id;
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

        private void SolutionEventsOnOpened()
        {
            try
            {
                _solutionName = _objDte.Solution.FullName;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                Console.WriteLine(@"SolutionEventsOnOpened");
            }
        }

        private static string GetProjectName()
        {
            return !string.IsNullOrEmpty(_solutionName)
                ? Path.GetFileNameWithoutExtension(_solutionName)
                : !string.IsNullOrEmpty(_objDte.Solution?.FullName)
                    ? Path.GetFileNameWithoutExtension(_objDte.Solution.FullName)
                    : string.Empty;
        }

        private void SolutionEventsBeforeClosed()
        {
            SendActivities();

            _solutionName = string.Empty;
            _projectName = string.Empty;
            _activities = new List<Activity>();
        }

        private int GetTimestamp()
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                _projectName = GetProjectName();
                HandleActivity();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                Console.WriteLine(@"WindowEventsOnWindowActivated");
            }
        }

        private void DocumentEventsOnDocumentSaved(Document document)
        {
            SendActivities();
        }

        private void SendActivities()
        {
            if (_activities.Count > 0)
            {
                StopLastActivity();

                var sended = _sender.SendActivities(_activities);
                _logger.Debug("Sended: " + sended.ToString());
                if (sended)
                    _activities = new List<Activity>();
            }
        }

        #endregion
    }
}
