namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FAKE.Unity3D")>]
[<assembly: AssemblyProductAttribute("FAKE.Unity3D")>]
[<assembly: AssemblyDescriptionAttribute("Unity3D tasks for FAKE")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
