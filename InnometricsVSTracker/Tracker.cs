using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Text.Editor;

namespace InnometricsVSTracker
{
    public class Tracker
    {
        #region Fields
        private static DTE2 _objDte;

        // Settings
        public string ProjectLanguage { get; set; } = "undefined";
        public string ProjectName { get; set; } = "undefined";
        public List<Activity> Activities { get; set; } = new List<Activity>();

        #endregion

        public Tracker()
        {
            _objDte = (DTE2)Package.GetGlobalService(typeof(DTE));
        }

        public Tracker(string projectName, string projectLanguage)
        {
            ProjectName = projectName;
            ProjectLanguage = projectLanguage;
            _objDte = (DTE2)Package.GetGlobalService(typeof(DTE));
        }

        public void HandleActivity(SyntaxTree tree, int caretPosition)
        {
            var id = GetCurrentPath(tree, caretPosition);
            var last = Activities.LastOrDefault();
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

        private void StartActivity(string id)
        {
            var activity = new Activity("Visual Studio Extension", new List<Measurement>());
            var path = new Measurement("code path", "string", id);
            var startTimestamp = new Measurement("code begin time", "long", GetTimestamp().ToString());
            var filePath = new Measurement("file path", "string", _objDte.ActiveDocument.FullName);
            var ideName = new Measurement("version name", "string", _objDte.Name);
            var ideVersion = new Measurement("full version", "string", _objDte.Version);
            activity.Measurements.Add(path);
            activity.Measurements.Add(filePath);
            activity.Measurements.Add(startTimestamp);
            activity.Measurements.Add(ideName);
            activity.Measurements.Add(ideVersion);
            Activities.Add(activity);
        }

        public void StopLastActivity()
        {
            var lastActivity = Activities.Last();
            if (lastActivity.Measurements.Select(m => m).LastOrDefault(m => m.Name == "end_time") != null) return;
            var stopTimestamp = new Measurement("code end time", "long", GetTimestamp().ToString());
            lastActivity.Measurements.Add(stopTimestamp);
        }

        public string GetCurrentPath(SyntaxTree tree, int caretPosition)
        {
            var root = tree.GetRootAsync().Result;
            var span = root.FindToken(caretPosition).Span;
            var node = root.FindNode(span);
            var names = new List<string>();

            while (node != null)
            {
                if (node.IsKind(SyntaxKind.MethodDeclaration))
                {
                    names.Add("FUNC:" + (node as MethodDeclarationSyntax)?.Identifier.ValueText);
                }
                else if (node.IsKind(SyntaxKind.ClassDeclaration))
                {
                    names.Add("CLASS:" + (node as ClassDeclarationSyntax)?.Identifier.ValueText);
                }
                else if (node.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    names.Add("NS:" + (node as NamespaceDeclarationSyntax)?.Name);
                }
                else if (node.IsKind(SyntaxKind.ConstructorDeclaration))
                {
                    names.Add("FUNC:" + (node as ConstructorDeclarationSyntax)?.Identifier.ValueText);
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
            var line = tree.GetLineSpan(span).EndLinePosition.Line + 1;
            var kek = tree.GetLineSpan(span);
            var id = "PROJ:" + ProjectName + "|LANG:" + ProjectLanguage + "|" + String.Join("|", names.ToArray()) + "|LINE:" + line;
            return id;
        }

        public void ClearActivities()
        {
            Activities = new List<Activity>();
        }

        private int GetTimestamp()
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

    }
}