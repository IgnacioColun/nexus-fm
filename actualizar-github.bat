@echo off
chcp 65001 >nul
title Actualizar NexusFM en GitHub (Nueva Version)
echo ============================================================================
echo   NexusFM - Publicar y Actualizar Nueva Version en GitHub
echo ============================================================================

git status -s
echo.
set /p COMMIT_MSG="Escribe la descripcion de la actualizacion (Enter para usar mensaje por defecto): "
if "%COMMIT_MSG%"=="" set COMMIT_MSG=feat(v2.0): Open Folder dialog interceptor, Drag and Drop, Ctrl+M folder search move, desktop Nexus icons and high-res game/app icons

echo.
echo [1/3] Agregando todos los archivos actualizados...
git add .

echo [2/4] Creando commit...
git commit -m "%COMMIT_MSG%"

echo [3/4] Sincronizando con cambios remotos (git pull --rebase)...
git pull --rebase origin main

echo [4/4] Subiendo cambios a GitHub (origin/main)...
git push origin main

echo.
echo [OK] Repositorio actualizado exitosamente en https://github.com/IgnacioColun/nexus-fm
pause
