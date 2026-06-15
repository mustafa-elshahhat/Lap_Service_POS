# Quickstart: Global Responsive Layout and Scrolling Fix

**Feature**: `001-fix-responsive-layout`
**Phase**: 1 – Design

This guide gets a developer from zero to a fully applied responsive-layout fix in one session.

---

## Prerequisites

- Visual Studio 2022+ or Rider with .NET 10 SDK and WPF workload installed
- Repository cloned: `d:\projects\Lap_Service_POS`
- No migration or database changes are needed; only XAML files are modified

---

## Build & Run

```powershell
# From repo root
dotnet build AlJohary.ServiceHub.sln
dotnet run --project AlJohary.ServiceHub.csproj
```

Or press **F5** in Visual Studio / Rider.

---

## Implementation Order

Apply changes in this exact order to keep the application buildable at each step.

### Step 1 — MainWindow.xaml: Window launches maximised (FR-013)

File: `Presentation/Views/MainWindow.xaml`

```xml
<!-- BEFORE -->
WindowState="Normal"

<!-- AFTER -->
WindowState="Maximized"
```

### Step 2 — MainWindow.xaml: Sidebar width 260 → 220 px (FR-002)

File: `Presentation/Views/MainWindow.xaml` (inside the two-column Grid at Grid.Row="1")

```xml
<!-- BEFORE -->
<ColumnDefinition Width="260"/>

<!-- AFTER -->
<ColumnDefinition Width="220"/>
```

### Step 3 — MainWindow.xaml: Logo section spacing (FR-003)

File: `Presentation/Views/MainWindow.xaml` (StackPanel containing the logo Border)

```xml
<!-- BEFORE -->
<StackPanel Grid.Row="0" Margin="0,50,0,30" HorizontalAlignment="Center">

<!-- AFTER -->
<StackPanel Grid.Row="0" Margin="0,20,0,16" HorizontalAlignment="Center">
```

### Step 4 — MainWindow.xaml: Logo size (FR-004)

File: `Presentation/Views/MainWindow.xaml` (Image inside the logo Border)

```xml
<!-- BEFORE -->
<Image Width="190" Height="90"

<!-- AFTER -->
<Image Width="160" Height="76"
```

### Step 5 — MainWindow.xaml: Main content area scrolling (FR-001, FR-009)

File: `Presentation/Views/MainWindow.xaml` (Grid.Column="1" content grid)

```xml
<!-- BEFORE -->
<Grid Grid.Column="1" Background="{DynamicResource BgMainBrush}">
     <Frame Content="{Binding CurrentPage}" 
            NavigationUIVisibility="Hidden"
            BorderThickness="0"
            Background="{x:Null}"
            Margin="0"
            Focusable="False"/>
</Grid>

<!-- AFTER -->
<Grid Grid.Column="1" Background="{DynamicResource BgMainBrush}">
    <ScrollViewer VerticalScrollBarVisibility="Auto"
                  HorizontalScrollBarVisibility="Disabled">
        <Frame Content="{Binding CurrentPage}" 
               NavigationUIVisibility="Hidden"
               BorderThickness="0"
               Background="{x:Null}"
               Margin="0"
               Focusable="False"/>
    </ScrollViewer>
</Grid>
```

### Step 6 — POSPage.xaml: Cart panel width 380 → 320 px (FR-008)

File: `Presentation/Views/POSPage.xaml` (second ColumnDefinition in the root Grid)

```xml
<!-- BEFORE -->
<ColumnDefinition Width="380"/>

<!-- AFTER -->
<ColumnDefinition Width="320"/>
```

### Step 7 — Toolbar buttons: Width → MinWidth (FR-007)

For each page listed below, change `Width="N"` to `MinWidth="N"` on toolbar `Button` elements.
Do **not** change `Width` on `DataGridTextColumn`, icon containers, or circular buttons.

**CustomersPage.xaml**
```xml
<!-- BEFORE -->
Width="120"   <!-- "عميل جديد" button -->
Width="140"   <!-- "عرض الكل" button -->

<!-- AFTER -->
MinWidth="120"
MinWidth="140"
```

**ExpensesPage.xaml**
```xml
<!-- BEFORE -->
Width="140"   <!-- "مصروف جديد" button -->
Width="140"   <!-- "عرض الكل" button -->

<!-- AFTER -->
MinWidth="140"
MinWidth="140"
```

For all remaining pages (`InvoicesPage.xaml`, `ReturnsPage.xaml`, `MaintenancePage.xaml`,
`InventoryPage.xaml`, `SuppliersPage.xaml`): search for `Width="` inside `<Button` elements
within toolbar `StackPanel` rows and apply the same `Width` → `MinWidth` substitution.

---

## Verification Checklist

After applying all steps, launch the app and verify each success criterion:

| Check | Steps |
|-------|-------|
| SC-001 App launches maximised | Launch → window fills monitor |
| SC-002 Sidebar is 220 px | Snipping Tool + pixel measurement, or check XAML |
| SC-003 All pages scroll | Open each page, reduce window to 700 px height → scrollbar appears, content reachable |
| SC-004 POS cart 320 px, ≥ 2 product columns | Open POS at 1280 px wide, verify visually |
| SC-005 No window-level horizontal scrollbar | Resize from 1024 to 1920 px → no H-scrollbar at any point |
| SC-006 RTL intact | Arabic text right-aligned on all modified pages |
| SC-007 No functional regressions | Navigate all pages, add/edit/delete a record on each |
| SC-008 Reports KPI cards wrap | Open Reports at 1366×768, narrow window → cards wrap to second row |

---

## Key Files Reference

| File | Changes |
|------|---------|
| `Presentation/Views/MainWindow.xaml` | WindowState, sidebar width, logo margin/size, ScrollViewer wrapper |
| `Presentation/Views/POSPage.xaml` | Cart ColumnDefinition Width 380→320 |
| `Presentation/Views/CustomersPage.xaml` | Button Width→MinWidth |
| `Presentation/Views/ExpensesPage.xaml` | Button Width→MinWidth |
| Other page XAMLs | Button Width→MinWidth (audit and apply) |

No C# files, no ViewModels, no database migrations, no new files are created.
