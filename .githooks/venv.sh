#!/bin/sh

set_venv_python() {
  root_directory="$1"
  venv_python="$root_directory/.venv/Scripts/python.exe"

  if [ ! -f "$venv_python" ]; then
    venv_python="$root_directory/.venv/bin/python"
  fi

  if [ ! -f "$venv_python" ]; then
    echo "Python venv not found. Create it in $root_directory/.venv first." >&2
    return 1
  fi

  VENV_PY="$venv_python"
  return 0
}
