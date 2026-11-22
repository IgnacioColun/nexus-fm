#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use chrono::{DateTime, Local};
use serde::Serialize;
use std::fs;
use std::io::Read;
use std::path::{Path, PathBuf};
use std::process::Command;

#[derive(Serialize, Clone)]
pub struct FileEntry {
    pub name: String,
    pub path: String,
    pub is_dir: bool,
    pub size: u64,
    pub modified: String,
    pub extension: String,
    pub readonly: bool,
}

#[derive(Serialize)]
pub struct DriveInfo {
    pub letter: String,
    pub path: String,
    pub label: String,
}

#[derive(Serialize)]
pub struct TerminalOutput {
    pub stdout: String,
    pub stderr: String,
    pub exit_code: i32,
    pub new_cwd: String,
}

#[tauri::command]
fn list_directory(path: String) -> Result<Vec<FileEntry>, String> {
    let dir_path = Path::new(&path);
    if !dir_path.exists() {
        return Err(format!("La ruta no existe: {}", path));
    }

    let entries = fs::read_dir(dir_path).map_err(|e| format!("Error al leer directorio: {}", e))?;
    let mut result: Vec<FileEntry> = Vec::new();

    for entry_res in entries {
        if let Ok(entry) = entry_res {
            let p = entry.path();
            let name = entry.file_name().to_string_lossy().to_string();
            let metadata = match entry.metadata() {
                Ok(m) => m,
                Err(_) => continue,
            };

            let is_dir = metadata.is_dir();
            let size = if is_dir { 0 } else { metadata.len() };
            let readonly = metadata.permissions().readonly();
            let modified = metadata
                .modified()
                .ok()
                .map(|t| {
                    let dt: DateTime<Local> = t.into();
                    dt.format("%Y-%m-%d %H:%M").to_string()
                })
                .unwrap_or_else(|| "-".to_string());

            let extension = if is_dir {
                "DIR".to_string()
            } else {
                p.extension()
                    .map(|e| e.to_string_lossy().to_uppercase())
                    .unwrap_or_else(|| "FILE".to_string())
            };

            result.push(FileEntry {
                name,
                path: p.to_string_lossy().to_string(),
                is_dir,
                size,
                modified,
                extension,
                readonly,
            });
        }
    }

    // Ordenar carpetas primero, luego alfabéticamente (estilo Dolphin)
    result.sort_by(|a, b| {
        b.is_dir
            .cmp(&a.is_dir)
            .then_with(|| a.name.to_lowercase().cmp(&b.name.to_lowercase()))
    });

    Ok(result)
}

#[tauri::command]
fn list_drives() -> Vec<DriveInfo> {
    let mut drives = Vec::new();
    for b in b'A'..=b'Z' {
        let letter = format!("{}:", b as char);
        let path = format!("{}\\", letter);
        if Path::new(&path).exists() {
            drives.push(DriveInfo {
                letter: letter.clone(),
                path,
                label: format!("Disco local ({})", letter),
            });
        }
    }
    drives
}

#[tauri::command]
fn create_directory(parent_path: String, name: String) -> Result<String, String> {
    let target = Path::new(&parent_path).join(&name);
    fs::create_dir_all(&target).map_err(|e| format!("No se pudo crear la carpeta: {}", e))?;
    Ok(target.to_string_lossy().to_string())
}

#[tauri::command]
fn rename_item(old_path: String, new_name: String) -> Result<String, String> {
    let old = Path::new(&old_path);
    let parent = old.parent().ok_or("Ruta padre inválida")?;
    let new_path = parent.join(new_name);
    fs::rename(old, &new_path).map_err(|e| format!("Error al renombrar: {}", e))?;
    Ok(new_path.to_string_lossy().to_string())
}

#[tauri::command]
fn delete_item(path: String) -> Result<(), String> {
    let p = Path::new(&path);
    if p.is_dir() {
        fs::remove_dir_all(p).map_err(|e| format!("Error al eliminar carpeta: {}", e))
    } else {
        fs::remove_file(p).map_err(|e| format!("Error al eliminar archivo: {}", e))
    }
}

fn copy_dir_recursive(src: &Path, dst: &Path) -> std::io::Result<()> {
    fs::create_dir_all(dst)?;
    for entry in fs::read_dir(src)? {
        let entry = entry?;
        let file_type = entry.file_type()?;
        if file_type.is_dir() {
            copy_dir_recursive(&entry.path(), &dst.join(entry.file_name()))?;
        } else {
            fs::copy(entry.path(), dst.join(entry.file_name()))?;
        }
    }
    Ok(())
}

#[tauri::command]
fn copy_item(src_path: String, target_dir: String) -> Result<String, String> {
    let src = Path::new(&src_path);
    let file_name = src.file_name().ok_or("Nombre de archivo inválido")?;
    let dst = Path::new(&target_dir).join(file_name);

    if src.is_dir() {
        copy_dir_recursive(src, &dst).map_err(|e| format!("Error al copiar carpeta: {}", e))?;
    } else {
        fs::copy(src, &dst).map_err(|e| format!("Error al copiar archivo: {}", e))?;
    }
    Ok(dst.to_string_lossy().to_string())
}

#[tauri::command]
fn move_item(src_path: String, target_dir: String) -> Result<String, String> {
    let src = Path::new(&src_path);
    let file_name = src.file_name().ok_or("Nombre de archivo inválido")?;
    let dst = Path::new(&target_dir).join(file_name);

    if fs::rename(src, &dst).is_err() {
        // Fallback si están en distintas particiones
        copy_item(src_path.clone(), target_dir)?;
        delete_item(src_path)?;
    }
    Ok(dst.to_string_lossy().to_string())
}

#[tauri::command]
fn read_text_preview(path: String, max_bytes: usize) -> Result<String, String> {
    let mut file = fs::File::open(&path).map_err(|e| format!("No se pudo abrir: {}", e))?;
    let mut buffer = vec![0u8; max_bytes];
    let bytes_read = file
        .read(&mut buffer)
        .map_err(|e| format!("Error de lectura: {}", e))?;
    buffer.truncate(bytes_read);
    Ok(String::from_utf8_lossy(&buffer).to_string())
}

#[tauri::command]
fn open_with_system(path: String) -> Result<(), String> {
    open::that(&path).map_err(|e| format!("No se pudo abrir con el sistema: {}", e))
}

#[tauri::command]
fn execute_terminal_command(cwd: String, command: String) -> Result<TerminalOutput, String> {
    let trimmed = command.trim();

    // Manejo nativo de cambio de directorio `cd` para sincronizar con el panel activo
    if trimmed.starts_with("cd ") || trimmed == "cd" {
        let arg = trimmed.strip_prefix("cd").unwrap_or("").trim().trim_matches('"');
        let next_path: PathBuf = if arg.is_empty() || arg == "~" {
            std::env::var("USERPROFILE")
                .map(PathBuf::from)
                .unwrap_or_else(|_| PathBuf::from(&cwd))
        } else {
            let candidate = Path::new(&cwd).join(arg);
            candidate.canonicalize().unwrap_or(candidate)
        };

        if next_path.exists() && next_path.is_dir() {
            let clean = next_path.to_string_lossy().replace("\\\\?\\", "");
            return Ok(TerminalOutput {
                stdout: format!("Directorio cambiado a {}", clean),
                stderr: String::new(),
                exit_code: 0,
                new_cwd: clean,
            });
        } else {
            return Ok(TerminalOutput {
                stdout: String::new(),
                stderr: format!("cd: no existe el directorio '{}'", arg),
                exit_code: 1,
                new_cwd: cwd,
            });
        }
    }

    let output = Command::new("powershell")
        .args(["-NoProfile", "-Command", trimmed])
        .current_dir(&cwd)
        .output()
        .map_err(|e| format!("Error al ejecutar PowerShell: {}", e))?;

    Ok(TerminalOutput {
        stdout: String::from_utf8_lossy(&output.stdout).to_string(),
        stderr: String::from_utf8_lossy(&output.stderr).to_string(),
        exit_code: output.status.code().unwrap_or(-1),
        new_cwd: cwd,
    })
}

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![
            list_directory,
            list_drives,
            create_directory,
            rename_item,
            delete_item,
            copy_item,
            move_item,
            read_text_preview,
            open_with_system,
            execute_terminal_command
        ])
        .run(tauri::generate_context!())
        .expect("Error al iniciar NexusFM");
}
