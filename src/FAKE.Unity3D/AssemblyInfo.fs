namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FAKE.Unity3D")>]
[<assembly: AssemblyProductAttribute("FAKE.Unity3D")>]
[<assembly: AssemblyDescriptionAttribute("Unity3D tasks for FAKE")>]
[<assembly: AssemblyVersionAttribute("0.0.3")>]
[<assembly: AssemblyFileVersionAttribute("0.0.3")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.3"
