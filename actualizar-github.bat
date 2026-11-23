@echo off
chcp 65001 >nul
title Actualizar NexusFM en GitHub (Nueva Version)
echo ============================================================================
echo   NexusFM - Publicar y Actualizar Nueva Version en GitHub
echo ============================================================================

git status -s
echo.
set /p COMMIT_MSG="Escribe la descripcion de la actualizacion (Enter para usar mensaje por defecto): "
if "%COMMIT_MSG%"=="" set COMMIT_MSG=feat(v2.1): folder descriptions and content summary, immutable local AppData icon engine, and Windows startup icon sync

echo.
echo [1/4] Agregando todos los archivos actualizados...
git add .

echo [2/4] Creando commit...
git commit -m "%COMMIT_MSG%"

echo [3/4] Sincronizando con cambios remotos (git pull --rebase)...
git pull --rebase -X ours origin main

echo [4/4] Subiendo cambios a GitHub (origin/main)...
git push origin main

echo.
echo [OK] Repositorio actualizado exitosamente en https://github.com/IgnacioColun/nexus-fm
pause
