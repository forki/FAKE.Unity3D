# Fake.Unity3D

```
#I @"./packages/FSharp.Data/lib/net40/"
#r @"./lib/FAKE.Unity3D.dll"

open Fake

let unity3dTestProject = "unity3d/Tests.Unity3D" |> Path.GetFullPath

Target "RunTestsOnUnity3D" (fun() ->
    let assetsDir = unity3dAssetsDir unity3dTestProject
    CopyWithSubfoldersTo (Path.Combine(assetsDir, "Editor")) [unity3dTests]

    Unity3DTestTools.RunTests true (fun p -> {p with projectDirectory=unity3dTestProject})
	|> ignore
)
```

More docs coming soon...
