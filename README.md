# innometrics-agent-ide-visualstudio

### Innometrics plugin for Visual Studio (2015, 2017)

Installation: 

Compile source code, run InnometricsVSTracker.vsix from bin folder and select necessary IDEs version.

Collecting measurements:

Innometrics Activity name: "Visual Studio Extension"

- "file path" - absolute file path, e.g. C:\Users\Артем\source\repos\TestApp\TestApp\Program.cs
- "code path" - code elements path, e.g. PROJ:TestApp|LANG:C#|NS:TestApp|CLASS:Program|FUNC:Test228|LINE:25. 
Element labels: 
    - PROJ - project name
    - LANG - programming language name
    - NS - namespace, package or module name
    - CLASS - class name
    - FUNC - function or method name
    - LINE - line in the file
- "code begin time" - UTC timestamp in ms, e.g. 1511857258858
- "code end time" - UTC timestamp in ms, e.g. 1511857258858
- "version name" - name of the IDE application, e.g. Microsoft Visual Studio
- "full version" - version of the IDE application, e.g. 15.0

Anonymous classes are denoted as CLASS:[ANONYMOUS].

Anonymous methods are denoted as FUNC:[ANONYMOUS].

Lambda expressions are denoted as FUNC:[LAMBDA].

Sending measurements and Settings:

Measurements sends automatically when file saved or tab switched.
