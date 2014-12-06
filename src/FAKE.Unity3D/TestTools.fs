namespace Fake

open System.IO
open Fake.TraceHelper
open Fake.Unity3DCommands
    
module Unity3DTestTools =
    [<Literal>]
    let internal ReportSample = 
        """<?xml version="1.0" encoding="utf-8"?>
        <!--This file represents the results of running a test suite-->
        <test-results name="Unity Tests" total="18" errors="1" failures="2" not-run="2" inconclusive="4" ignored="1" skipped="1" invalid="0" date="2014-12-05" time="22:04:27">
            <environment nunit-version="2.6.2-Unity" clr-version="2.0.50727.1433" os-version="Unix 14.0.0.0" platform="Unix" cwd="/Users/devboy/Development/wooga/test/New Unity Project" machine-name="Dominics-Air-2.fritz.box" user="devboy" user-domain="Dominics-Air-2.fritz.box" unity-version="5.0.0b9" unity-platform="Editor" />
            <culture-info current-culture="en-US" current-uiculture="en-US" />
            <test-suite name="Unit Tests" type="Assembly" executed="True" result="Failure" success="False" time="0.221">
            <results>
                <test-case name="UnityTest.SampleTests.CultureSpecificTest" executed="False" result="Skipped">
                <reason><message><![CDATA[Only supported under culture pl-PL]]></message></reason>
                </test-case>
                <test-case name="UnityTest.SampleTests.ExceptionTest" executed="True" result="Error" success="False" time="0.009">
                <failure><message><![CDATA[System.Exception : Exception throwing test]]></message>
                    <stack-trace>
        <![CDATA[at UnityTest.SampleTests.ExceptionTest () [0x00000] in /Users/devboy/Development/wooga/test/New Unity Project/Assets/UnityTestTools/Examples/UnitTestExamples/Editor/SampleTests.cs:17
        ]]>
                    </stack-trace>
                </failure>
                </test-case>
                <test-case name="UnityTest.SampleTests.ExpectedExceptionTest" executed="True" result="Success" success="True" time="0.001">
                </test-case>
                <test-case name="UnityTest.SampleTests.FailingTest" executed="True" result="Failure" success="False" time="0.000">
                <failure>
                    <message>
                    </message>
                    <stack-trace>
        <![CDATA[at UnityTest.SampleTests.FailingTest () [0x00000] in /Users/devboy/Development/wooga/test/New Unity Project/Assets/UnityTestTools/Examples/UnitTestExamples/Editor/SampleTests.cs:39
        ]]>
                    </stack-trace>
                </failure>
                </test-case>
                <test-case name="UnityTest.SampleTests.IgnoredTest" executed="False" result="Ignored">
                <reason>
                    <message>
        <![CDATA[Ignored test]]>
                    </message>
                </reason>
                </test-case>
                <test-case name="UnityTest.SampleTests.InconclusiveTest" executed="True" result="Inconclusive" success="False" time="0.000">
                <reason>
                    <message>
                    </message>
                </reason>
                </test-case>
            </results>
            </test-suite>
            <test-suite name="Unit Tests" type="Assembly" executed="True" result="Failure" success="False" time="0.221">
            <results />
            </test-suite>
        </test-results>
        """

    type private Report = 
        FSharp.Data.XmlProvider<ReportSample>
    let tryLoadReport (report:string) = 
        try report |> Report.Load |> Some with | exc -> None

    let private resultsFile unityParams = 
        Path.Combine(unityParams.projectDirectory,"unity-test-results.xml")
    type TestSuite = 
        Results of Report.TestCase list
    type TestCases =
        Report.TestCase seq
    type Results = 
        {skipped:TestCases; 
            succeded:TestCases; 
            failures:TestCases; 
            errors:TestCases;
            ignored:TestCases; 
            inconclusive:TestCases; } with 
        static member emptyReport =
            {skipped=Seq.empty; succeded=Seq.empty; failures=Seq.empty; errors=Seq.empty; ignored=Seq.empty; inconclusive=Seq.empty}
        static member ofReport (report:Report.TestResults) = 
            let ofGroup group report =
                match group with
                | "Skipped", cases      -> {report with skipped=cases}
                | "Success", cases      -> {report with succeded=cases}
                | "Failure", cases      -> {report with failures=cases}
                | "Error", cases        -> {report with errors=cases}
                | "Ignored", cases      -> {report with ignored=cases}
                | "Inconclusive", cases -> {report with inconclusive=cases}
                | _ -> report     
            let ofGroups groups =
                groups |> Seq.fold (fun report group -> ofGroup group report) Results.emptyReport    
            report.TestSuites 
            |> Array.fold (fun cases suite -> Array.append cases suite.Results.TestCases) [||] 
            |> Seq.ofArray
            |> Seq.groupBy (fun case -> case.Result)
            |> ofGroups
        member x.all = 
            Seq.concat [x.succeded;x.skipped;x.ignored;x.inconclusive;x.failures;x.errors]
        member x.successful =
            Seq.concat [x.failures; x.errors] |> Seq.isEmpty

    let private describeTestCase prefix (case:Report.TestCase) =
        let name =  sprintf "%s - %s" prefix case.Name
        let reason = 
            try Some case.Reason.Value.Message.Value 
            with | exc -> None
        let failure = 
            try Some case.Failure.Value.Message.Value 
            with | exc -> None
        let stacktrace = 
            try Some case.Failure.Value.StackTrace 
            with | exc -> None
        [reason;failure;stacktrace] 
        |> Seq.choose id 
        |> Seq.map (sprintf "%s   %s" prefix)
        |> Seq.append [name]
        |> String.concat "\n"
    
    let private describeGroup title cases =
            match Seq.length cases with
            | 0 -> ""
            | _ as count -> cases 
                            |> Seq.map (describeTestCase "\t") 
                            |> String.concat "\n"
                            |> sprintf "%s (%i): \n%s\n" title count    
        
    let private printResults results =
        tracefn "%s"    <| describeGroup "Succeded" results.succeded
        traceImportant  <| describeGroup "Skipped" results.skipped
        traceImportant  <| describeGroup "Ignored" results.ignored
        traceImportant  <| describeGroup "Inconclusive" results.inconclusive
        traceError      <| describeGroup "Failures" results.failures
        traceError      <| describeGroup "Errors" results.errors
        tracefn "Total (%i)" (Seq.length results.all)                 
                    
    let RunTestsAsync ignoreFailures customParams = 
        async { 
            let parameters = 
                customParams {defaultParams with executeMethod=Some "UnityTest.Batch.RunUnitTests";
                                                 args= Seq.append defaultParams.args [(sprintf "-resultFilePath=%s" <| resultsFile defaultParams)]}
            do DeleteFile <| resultsFile defaultParams
            let! exec = ExecAsync parameters
            let results = 
                match tryLoadReport <| resultsFile exec.Params with
                | Some report -> Results.ofReport report
                | _ -> Results.emptyReport
            printResults results
            match exec, results, ignoreFailures with
            | Success _, report, false when not report.successful -> failwith "Tests failed"
            | _, _, _ -> ()   
            return exec, results 
        }
    let RunTests ignoreFailures customParams = 
        RunTestsAsync ignoreFailures customParams |> Async.RunSynchronously

