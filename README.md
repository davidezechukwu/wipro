# wipro
Project Structure
Wipro 
  Wipro 
     Program.cs 
  WiproTest 
     ProgramTest.cs
  Publish 
     wipro.exe 
     wiprotest.exe 

To run app
1) Navigate to the directory ./wipro/publish 
2) Run wipro.exe [domain] [number of pages to index]
3) Alternatively just run wipro.exe for the command line options
 
To run tests (Tests uses MSTest)
1) Navigate to the directory ./wipro/publish 
2) Run [PATH TO vstest.console.exe] wiprotest.dll
   For Example: "D:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" wiprotest.dll
3) Alternatively Load the solution or projects in Visual Studio and carry out the tests from  the IDE


To Build Application
The Solution and projects were built using Visual Studio Enterprise 2017 hence Visual Studio or Visual Code is required.


Trade-offs
1 There is no threading involved - the use of thread would speed up the crawl process as the code is blocked each time a web request is made
2 There is no use of interfaces or abstract base classes or Object Orientation, all the crawl logic is located on one static class
3 There is a limit to the number of pages that could be indexed
4 There is no handling of sub-domains

Assumptions 
No XML Schema was provided however, the outputted xml closely mirror what was asked for
