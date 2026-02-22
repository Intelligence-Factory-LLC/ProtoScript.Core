call "%~dp0set_params.bat"

set JSONWSCMD=C:\Rootrax\RooTrax.Utilities\JsonWsStubGenerator.Cmd\bin\Debug\net9.0\JsonWsStubGenerator.Cmd.exe
set WORKBENCH_NAMESPACE=ProtoScript.Workbench.Web
set WORKBENCH_JSONWS=%ROOT%\%WORKBENCH_NAMESPACE%\wwwroot\JsonWS
set WORKBENCH_DLL=%ROOT%\ProtoScript.Workbench.Api\bin\Debug\net9.0\ProtoScript.Workbench.Api.dll

%JSONWSCMD% -js %WORKBENCH_DLL% * %WORKBENCH_JSONWS% ProtoScript
%JSONWSCMD% -ashx %WORKBENCH_DLL% * %WORKBENCH_JSONWS%
