# wipro
Project Structure
1) App\Program.cs 
2) App\AppTest 
5) App\AppTest\ProgramTest.cs
6) App\Publish
7) App\Publish\program.exe 
8) App\Publish\program.exe 

To run app
1) Navigate to the directory ./App/publish 
2) Run program.exe [domain] [number of pages to index]
3) Alternatively just run program.exe for the command line options
 
To run tests (Tests uses MSTest)
1) Navigate to the directory ./app/publish 
2) Run [PATH TO vstest.console.exe] programtest.dll
   For Example: "D:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe" programtest.dll
3) Alternatively Load the solution or projects in Visual Studio and carry out the tests from  the IDE


To Build Application
The Solution and projects were built using Visual Studio Enterprise 2017 hence Visual Studio or Visual Code is required.


Trade-offs
1 There is no threading involved - the use of thread would speed up the crawl process as the code is blocked each time a web request is made
2 There is no use of interfaces or abstract base classes or Object Orientation, all the crawl logic is located on one static class
3 There is a limit to the number of pages that could be indexed
4 There is no handling of sub-domains
5 Test coverage might not be 100% 
6 No design patterns were used, however I could have used the strategy pattern to implement services for generating either XML, TEXT or JSON from the crawled page link
