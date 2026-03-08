call "%~dp0set_params.bat"
set LIB_ROOT=%ROOT%\lib\
set DEPLOY_ROOT=%ROOT%\Deploy\
set ONTOLOGY_DEPLOY=%ROOT%\..\ontology-core\Deploy\

xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\BasicUtilities.dll %LIB_ROOT%*.*
xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\WebAppUtilities.dll %LIB_ROOT%*.*

xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Cache.dll %LIB_ROOT%*.*
xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Common.dll %LIB_ROOT%*.*
xcopy /y c:\dev\RooTrax\RooTrax.Utilities\Deploy\RooTrax.Common.DB.dll %LIB_ROOT%*.*

xcopy /y C:\dev\Buffaly.Development\Deploy\CSharp.dll %LIB_ROOT%*.*
xcopy /y C:\dev\Buffaly.Development\Deploy\CSharp.Parsers.dll %LIB_ROOT%*.*

if exist "%ONTOLOGY_DEPLOY%Ontology.dll" xcopy /y "%ONTOLOGY_DEPLOY%Ontology*.dll" "%DEPLOY_ROOT%*.*"




