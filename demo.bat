@echo off
echo ===========================================
echo gRPC Inter-service Communication Demo
echo ===========================================
echo.

echo Building the project...
dotnet build GrpcService.csproj
if %ERRORLEVEL% neq 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Choose demo mode:
echo 1. Server mode (run server only)
echo 2. Client mode (test all RPC patterns)
echo 3. Multi-client demo (requires multiple terminals)
echo 4. Show usage help
echo.

set /p choice="Enter your choice (1-4): "

if "%choice%"=="1" goto server
if "%choice%"=="2" goto client
if "%choice%"=="3" goto multiclient
if "%choice%"=="4" goto help

echo Invalid choice!
pause
exit /b 1

:server
echo.
echo Starting gRPC Server on port 5000...
echo Press Ctrl+C to stop the server
echo.
dotnet run --mode=server --port=5000
goto end

:client
echo.
echo Make sure you have a server running on port 5000 first!
echo Starting in 3 seconds...
timeout /t 3 /nobreak > nul
echo.
echo Testing all gRPC patterns...
dotnet run --mode=client --server=https://localhost:5000 --client-mode=all
goto end

:multiclient
echo.
echo Multi-client Demo Instructions:
echo.
echo 1. First, start the server:
echo    dotnet run --mode=server --port=5000
echo.
echo 2. Then in separate terminals, run:
echo    dotnet run --mode=client --server=https://localhost:5000 --client-mode=unary
echo    dotnet run --mode=client --server=https://localhost:5000 --client-mode=chat
echo    dotnet run --mode=client --server=https://localhost:5000 --client-mode=serverstream
echo.
echo Starting server now...
dotnet run --mode=server --port=5000
goto end

:help
echo.
echo Usage Examples:
echo.
echo Server mode:
echo   dotnet run --mode=server --port=5000
echo.
echo Client modes:
echo   dotnet run --mode=client --server=https://localhost:5000 --client-mode=unary
echo   dotnet run --mode=client --server=https://localhost:5000 --client-mode=serverstream
echo   dotnet run --mode=client --server=https://localhost:5000 --client-mode=clientstream
echo   dotnet run --mode=client --server=https://localhost:5000 --client-mode=chat
echo   dotnet run --mode=client --server=https://localhost:5000 --client-mode=all
echo.
echo Client modes explanation:
echo   unary       - Simple request-response
echo   serverstream - Server streaming messages
echo   clientstream - Client streaming messages
echo   chat        - Bidirectional streaming (chat)
echo   all         - Run unary, client stream, then server stream
echo.

:end
echo.
pause
