# Odoo 17 Integration: CV Position Viewer

This folder contains the Docker Compose environment and the custom Odoo 17 Community addon (`cv_position_viewer`) that integrates with CVPlatform's external read-only API.

---

## Architecture & Integration Flow

1. **CVPlatform** generates position-scoped API tokens (`cvp_live_...`) with constant-time SHA-256 verification and exposes read-only endpoints:
   - `GET /api/external/v1/position`
   - `GET /api/external/v1/position/aggregates`
2. **Odoo Addon (`cv_position_viewer`)** communicates with CVPlatform through an import wizard, synchronizing position details and candidate attribute aggregates into read-only models:
   - `cv.position`
   - `cv.position.attribute`
   - `cv.position.attribute.value`
3. **Network Bridge**: Odoo runs inside Docker containers and reaches CVPlatform on the host machine via `https://host.docker.internal:7052`.

---

## Step-by-Step Setup and Demonstration

### Step 1: Run CVPlatform and Generate an API Token

1. Run the CVPlatform Web application from the project root:
   ```powershell
   dotnet run --project CVPlatform.Web
   ```
2. Open your browser and navigate to `https://localhost:7052` (or `http://localhost:5000`).
3. Sign in as a recruiter or administrator.
4. Go to **Positions** in the navigation bar and select a position to edit (e.g., `/Positions/Edit/1`).
5. Scroll to the **External Integration & API Token** section.
6. Click **Generate API Token**.
7. Copy the generated token (`cvp_live_...`). Note that this plaintext token is displayed only once.

---

### Step 2: Launch Odoo via Docker Compose

1. In a terminal, navigate to the `odoo` directory:
   ```bash
   cd odoo
   docker compose up -d
   ```
2. Verify that both containers (`odoo-web-1` and `odoo-db-1`) are healthy and running:
   ```bash
   docker compose ps
   ```
3. Open your browser to `http://localhost:8069`.

---

### Step 3: Initialize Database and Install Addon

1. If prompted by Odoo's database manager, fill in the fields:
   - **Database Name**: `odoo_cv`
   - **Email**: `admin@example.com`
   - **Password**: `admin`
   - Click **Create database**.
2. Once logged in, go to **Settings** -> Scroll to the bottom and click **Activate the developer mode**.
3. Go to **Apps** in the top navigation.
4. Click **Update Apps List** in the top menu and click **Update**.
5. Clear the default `Apps` filter in the search box, search for `CV Position Viewer`, and click **Activate**.

---

### Step 4: Import Position Data

1. Click on the main app menu and select **CV Positions**.
2. Click **Import Position** (or click the **Import Position** sub-menu).
3. In the modal:
   - **CVPlatform Base URL**: `https://host.docker.internal:7052`
   - **API Token**: Paste the token generated in Step 1.
   - Click **Import / Synchronize**.
4. The imported position record will open automatically, displaying:
   - Title, Company, Level, Published CV Count, and Last Synced timestamp.
   - The **Attributes & Aggregates** tab showing response counts, formatted summaries, and detailed sub-views (numeric min/max/average, date ranges, boolean counts, and value frequency breakdowns).
   - The **Description** tab containing the role description.

---

### Step 5: Running Automated Tests

To run the automated test suite for the addon inside the Docker container:

```bash
docker compose exec web odoo -i cv_position_viewer --test-enable --stop-after-init -d odoo_cv
```
