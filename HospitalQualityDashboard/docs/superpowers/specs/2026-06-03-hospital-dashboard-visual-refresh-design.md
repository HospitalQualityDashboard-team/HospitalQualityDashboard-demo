# Hospital Dashboard Visual Refresh Design

## Context

`HospitalQualityDashboard` is an ASP.NET MVC 4 application on .NET Framework 4.7.2. The UI is built with Razor `.cshtml`, Bootstrap, jQuery, Chart.js, and custom CSS in `Content/Site.css`. The app is an internal hospital operations dashboard, not a public landing page.

The current request is to modernize the dashboard and management UI so the application can manage the core hospital quality workflows clearly: departments, employees, quality indicators, assignments, reporting periods, reports, notifications, profile, and authentication screens.

The user selected scope **B**:

- Redesign the shared application layout and design system.
- Apply the new system first to the main screens that already exist.
- Do not introduce a new frontend framework.
- Keep the MVC/Razor structure and Bootstrap-compatible markup.

The approved visual direction is the sidebar administration concept saved at:

```text
HospitalQualityDashboard/docs/superpowers/concepts/2026-06-03-dashboard-sidebar-concept.png
```

## Goals

- Create a modern healthcare admin dashboard that is clean, professional, practical, and trustworthy.
- Make daily operations faster by improving navigation, page headers, filter bars, data tables, forms, status badges, and action buttons.
- Preserve role-based navigation: Admin sees all management modules; User sees only dashboard, assigned indicators, own reports, notifications, and account actions.
- Improve readability for dense tables and reporting workflows.
- Keep the UI responsive on desktop, tablet, and mobile.
- Use consistent design tokens for color, spacing, border, radius, shadow, and typography.

## Non-Goals

- Do not build a marketing landing page or large hero-style homepage.
- Do not replace ASP.NET MVC Razor with React, Vue, Angular, or another frontend framework.
- Do not change database schema, controller permissions, report business rules, import logic, or notification automation behavior as part of the visual refresh.
- Do not add decorative image-heavy backgrounds, orb/blob effects, or stock photography.
- Do not make large unrelated refactors outside the UI surface needed for this scope.

## Selected Approach

Use a **left sidebar administration shell** with a compact top utility bar.

This approach was chosen because the app has many operational modules and repeated data-management screens. A sidebar gives stable navigation, clearer active state, and more room for dense page content than a top-only navbar.

### Shell Structure

- `app-shell`: full-height application wrapper.
- `app-sidebar`: fixed desktop sidebar with brand block and role-aware navigation.
- `app-content`: main content column.
- `app-topbar`: compact utility bar above page content with current role, profile, logout, and optional page-level controls.
- `app-main`: constrained but wide content area optimized for tables.
- `app-footer`: minimal footer.

On mobile and tablet, the sidebar collapses behind a menu toggle. The main content remains first-class; tables can scroll horizontally when needed.

### Navigation

Admin menu:

- Tổng quan
- Khoa/phòng
- Nhân viên
- Chỉ số
- Phân công
- Kỳ báo cáo
- Báo cáo
- Thông báo

User menu:

- Tổng quan
- Chỉ số
- Báo cáo của tôi
- Thông báo

Account actions:

- Role chip
- Hồ sơ
- Đăng xuất

The active nav item must be visually obvious through background, color, and left accent treatment. Navigation should use compact icon-compatible markup, but icon usage can be implemented with CSS or a consistent icon set later without blocking the shell redesign.

## Design System

### Color Tokens

- Background: light cool gray, suitable for long work sessions.
- Surface: white.
- Surface soft: very light gray-blue for table headers and filter bands.
- Text: dark neutral.
- Muted text: slate gray.
- Primary: healthcare teal.
- Accent: restrained modern blue.
- Success: green for completed/submitted/active.
- Warning: amber for due soon or review-needed states.
- Danger: red for overdue, locked-error, and destructive actions.
- Border: low-contrast gray-blue.

Status must use both color and text. Color alone is not sufficient.

### Typography

Use the existing practical system font stack, centered on `Segoe UI`, with clear hierarchy:

- Page title: strong, compact, not hero-sized.
- Section heading: smaller dashboard-panel heading.
- Table header: uppercase or strong label style.
- Form labels: compact, high weight, readable.
- Body/table text: no negative letter spacing and no viewport-based font scaling.

### Component Families

The shared CSS should define reusable classes for:

- Page header: title, description, primary actions.
- KPI cards: compact cards for counts and percentages.
- Dashboard panels: chart/table/list containers.
- Filter panels: search, select, date, and action layout.
- Data tables: readable rows, hover state, responsive overflow.
- Status badges: draft, submitted, locked, overdue, read/unread, open/closed, active/inactive.
- Buttons: primary, secondary, outline, danger, ghost/icon-style.
- Forms: grouped fields, validation messages, helper text.
- Empty states: clear message and optional next action.
- Alerts: success, warning, danger, info.

Cards should use 6-8px radius. Avoid nested cards; repeated cards are acceptable for KPI tiles, period cards, and compact repeated items.

## Screen Application Scope

The first implementation pass should update these production surfaces:

1. Shared layout and global CSS tokens.
2. Dashboard overview.
3. Department list.
4. Employee list.
5. Indicator list and details where practical.
6. Assignment index, preserving its existing three-view workflow.
7. Reporting period list and automatic schedule screen.
8. Report list and report entry/edit screen where practical.
9. Notification list, detail, and create screen where practical.
10. Profile and password/auth screens where practical.
11. Error page and Home/About/Contact if still reachable.

If implementation time or risk requires a smaller first cut, prioritize:

1. `_Layout.cshtml`
2. `Content/Site.css`
3. `Views/Dashboard/Index.cshtml`
4. `Views/Report/Index.cshtml`
5. `Views/Department/Index.cshtml`
6. `Views/Indicator/Index.cshtml`
7. `Views/Notification/Index.cshtml`

This preserves the selected scope B by establishing the shared system first and applying it to the most visible management pages.

## Dashboard Overview

The dashboard should present:

- Title: `Trung tâm điều hành chất lượng`.
- Reporting period filter when supported by existing model/controller data.
- KPI cards:
  - Tổng chỉ số or Chỉ số được phân công.
  - Báo cáo đã gửi.
  - Báo cáo chờ nhập/còn thiếu.
  - Quá hạn.
  - Tỷ lệ hoàn thành.
- Chart.js progress chart for department reporting progress.
- Task list for overdue and due-soon missing reports.
- Department progress table/list with status colors and completion bars.

Admin sees system-wide data. User sees only their department-scoped data.

## Data Management Screens

List screens should share the same structure:

- Page header with title, short description, and primary action.
- Filter/search panel.
- Optional import/export action group for Admin screens.
- Data table with readable columns and compact row actions.
- Empty state when there is no data.
- Danger actions keep confirmation prompts.

Forms should use grouped sections instead of one long unstructured column. Long indicator forms should separate basic information, formula/unit, data source, frequency, targets, and related departments.

## Responsive Rules

- Desktop: fixed sidebar and wide content area.
- Tablet: sidebar may collapse; filters wrap to two columns.
- Mobile: sidebar becomes a toggle menu; content stacks; filter buttons become full-width when needed.
- Tables can scroll horizontally on small viewports.
- Text must not overflow buttons, badges, form fields, or table cells.

## Technical Constraints

- Use existing ASP.NET MVC Razor views and Bootstrap-compatible markup.
- Keep JavaScript minimal and local to interaction needs already present.
- Continue using Chart.js for the dashboard chart.
- Add or update CSS in `Content/Site.css`; do not add a new frontend build chain.
- Do not depend on external icon libraries unless the project already includes them or the implementation plan explicitly adds one.

## Verification

Implementation is acceptable when:

- MVC build succeeds with views compiled when possible.
- Existing verification scripts relevant to report/dashboard/notification flows still pass when available.
- Dashboard and list pages render without overlapping text or clipped controls.
- Desktop, tablet, and mobile widths are manually checked.
- The rendered dashboard is compared against the approved concept image for layout, palette, density, typography, sidebar behavior, KPI treatment, table readability, and status badges.
- Role-based menu visibility remains correct for Admin and User.

## Approved Concept Notes

The approved concept uses:

- Left sidebar with brand block at top.
- Active nav item highlighted with teal soft background.
- Topbar for role/account utilities.
- Light gray app background with white content surfaces.
- Compact KPI cards and dashboard panels.
- Dense but readable operational layout.

This concept is the visual north star, but implementation must remain code-native Razor/CSS rather than shipping the concept image as UI.
