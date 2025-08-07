@echo off
echo 🚀 Starting gRPC Chat Demo with Docker...
echo ==========================================

REM Check if Docker is running
docker info >nul 2>&1
if errorlevel 1 (
    echo ❌ Docker is not running. Please start Docker first.
    pause
    exit /b 1
)

REM Build and start services
echo 📦 Building and starting services...
docker-compose down --volumes --remove-orphans
docker-compose up --build

echo ✅ Services started!
echo Frontend: http://localhost:3000
echo Backend Health: http://localhost:5000/health
echo.
echo Press Ctrl+C to stop all services
pause
