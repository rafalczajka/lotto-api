# Lotto API

A **REST API** and **MCP server** providing draw results for Polish **Lotto** and **Lotto Plus** games.
The API supports JSON and CSV formats, includes automated updates for new draws,
and exposes its data to AI clients through the Model Context Protocol.

[![License: GPL-3.0](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

**Related project:** [Lotto Numbers Generator](https://github.com/rafalczajka/lotto-numbers-generator) -
a CLI for generating Lotto numbers using different strategies and backtesting them against historical draws.

## Features

- **Historical Data Endpoints:** Fetch draw results via `/api/draw-results`, `/api/draw-results/{date}`, or `/api/draw-results/latest`.
- **Sync Status:** Check if storage is up to date via `/api/sync`.
- **MCP Server:** Access Lotto draw results and synchronization status from MCP-compatible AI clients.
- **Multiple Formats:** Use `Accept: application/json` for JSON and `Accept: application/octet-stream` for CSV in `/api/draw-results`.
- **Auto-Update:** New results are added automatically 45 minutes after each draw (Tue/Thu/Sat at 22:45 CET/CEST).
- **Initialization Tools:** Python scripts to populate the database with historical data.

## API Endpoints

All routes use the default Azure Functions prefix: `/api`.
All HTTP endpoints require a Function key sent in header: `x-functions-key: <function-key>`.

<details>
<summary><code>GET /api/draw-results</code></summary>

Returns historical draw results from storage.

Optional query parameters:
- `dateFrom` (`yyyy-MM-dd`)
- `dateTo` (`yyyy-MM-dd`)
- `top` (positive integer)

Supported `Accept` headers:
- `application/json`, `application/*`, `*/*` -> JSON response
- `application/octet-stream` -> CSV file download

Possible status codes: `200`, `400`, `404`, `406`.

</details>

<details>
<summary><code>GET /api/draw-results/{date}</code></summary>

Returns draw results for a single date from storage. `date` must use `yyyy-MM-dd` format.

Possible status codes: `200`, `400`, `404`.

</details>

<details>
<summary><code>GET /api/draw-results/latest</code></summary>

Returns the latest draw results available in storage.

</details>

<details>
<summary><code>GET /api/sync</code></summary>

Returns synchronization status between storage and the external Lotto API.

</details>

## Getting Started

### Prerequisites
- .NET 10.0.100 or later
- Azure account with active subscription
- Azure CLI
- Python (for initialization scripts, version 3.12+ recommended)
- Lotto API key from [Lotto Developers Portal](https://developers.lotto.pl/)

## Deployment via GitHub Actions

1. **Initialize Azure resources**:
   Download the `init_azure.sh` script from the [rafalczajka/scripts](https://github.com/rafalczajka/scripts), then run it from the root of this repository (requires Bash shell: Linux/macOS, or WSL/Git Bash on Windows):

   ```bash
   chmod +x init_azure.sh
   ./init_azure.sh
   ```

   Follow prompts to create resources. The script generates an `azure-credentials.json` file.

2. **Configure secrets** in your GitHub repository:
   - `LOTTO_API_KEY`: Your Lotto.pl API key
   - `AZURE_CREDENTIALS`: Contents of `azure-credentials.json`
   - `AZURE_RESOURCE_GROUP`: Your Azure resource group name

3. **Trigger deployment workflow**:

   Push to main branch or manually run **Build and Deploy** action

## Post-Deployment Setup

1. **Prepare Python environment**:

   Linux / MacOS
   ```bash
   python -m venv .venv
   source .venv/bin/activate
   pip install -r requirements.txt
   ```
   Windows (PowerShell):
   ```powershell
   python -m venv .venv
   .\.venv\Scripts\Activate.ps1
   pip install -r requirements.txt
   ```
   Windows (Command Prompt):
   ```cmd
   python -m venv .venv
   .venv\Scripts\activate.bat
   pip install -r requirements.txt
   ```

2. **Configure environment**:
   Copy `.env.template` to `.env` and update values:
   ```env
   LOTTO_API_KEY="<your-api-key>"
   USER_AGENT="<your_name/contact_info>" # no spaces
   STORAGE_CONNECTION_STRING="<from Azure Portal>"
   ```

3. **Initialize data**:
   ```bash
   # Fetch data from Lotto.pl API (default start date: 2000-01-01)
   python -m tools fetch data.csv --from 2000-01-01

   # Upload to Azure Storage
   python -m tools upload data.csv
   ```
