namespace Fake

open System
open System.IO
open Fake

module Unity3DCommands =
    [<Literal>] 
    let private defaultWinExecutable = "C:\Program Files (x86)\Unity\Editor\Unity.exe"
    [<Literal>] 
    let private defaultOSXExecutable = "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
    let private defaultExecutable =
        if isUnix then defaultOSXExecutable else defaultWinExecutable   
    
    type Unity3DParams = 
        {executable:string; 
         projectDirectory:string; 
         batchmode:bool; quit:bool; 
         executeMethod:string option;
         stopOnFailure:bool; 
         args:string seq;
         timeout:TimeSpan}
    type Unity3DExecutionResult = 
        Success of Unity3DParams | Failure of Unity3DParams * int * string with
        member x.Params = match x with | Success p | Failure(p, _, _) -> p 
    let internal defaultParams = 
        {executable=defaultExecutable;
         projectDirectory="./"; 
         batchmode=true; 
         quit=true; 
         executeMethod=None; 
         args=[];
         timeout=TimeSpan.FromMinutes 6.0;
         stopOnFailure=true}
    let private logFile unityParams = 
        Path.Combine(unityParams.projectDirectory,"unity.log")
    let private logs unityParams =
        ReadFileAsString <| logFile unityParams
    let private args unityParams = 
        [Some(sprintf "-projectPath %s" unityParams.projectDirectory);
         (match unityParams.quit with true -> Some("-quit") | _ -> None);
         (match unityParams.batchmode with true -> Some("-batchmode") | _ -> None);
         Some(sprintf "-logFile %s" <| logFile unityParams);
         (match unityParams.executeMethod with Some m -> Some <| sprintf "-executeMethod %s" m | _ -> None)
         unityParams.args |> String.concat "" |> Some;]
        |> List.choose id |> String.concat " "
    let private exec unityParams = 
        match Shell.Exec(unityParams.executable, args unityParams) with 
        | 0 -> Success(unityParams) 
        | _ as c when not unityParams.stopOnFailure -> Failure(unityParams, c, logs unityParams) 
        | _ as c -> failwithf "exit-code:%i\n%s" c (logs unityParams)
    let ExecAsync parames = 
        async { return exec parames }
    let Exec customParams = 
        let parames = customParams defaultParams
        let timeout = parames.timeout.TotalMilliseconds |> int
        Async.RunSynchronously(ExecAsync parames, timeout) 