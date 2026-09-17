# Reusing the grid

The applications list is rendered by a reusable grid: `GridViewComponent` draws the table shell, and `wwwroot/js/grid.js` fetches rows from a JSON endpoint. Nothing in `Models/Grid` knows about applications.

## The contract
The endpoint accepts `page`, `pageSize`, `sort` and `direction`, plus any fields from an optional filter form. It returns a `PagedResult<T>`:

```json
{ "items": [ ... ], "totalCount": 42, "page": 1, "pageSize": 20 }
```

- Filter, sort and page in SQL before the query runs, and count the filtered total in the same request.
- End every sort with a unique key, such as `Id`, so rows can't move between pages.
- Clamp a page past the end to the last page. `PageMath.ClampPage` does this.
- Validate parameters and return **400** problem details for unknown values or a page size above the maximum.

## Steps
1. **A query** that takes the filter and paging and returns `PagedResult<TRow>`. Model it on `Web/Queries/Applications/ApplicationListQuery.cs`, including `QueryableSortExtensions`, which applies the sort direction.
2. **An API action** under `/api/...` that binds a request model with validation, like `ApplicationListRequest`, calls the query and returns the result. Add `ProducesResponseType` attributes and XML doc comments so the OpenAPI document describes it.
3. **A `GridViewModel`** describing the grid:
   ```csharp
   new GridViewModel
   {
       Id = "unit-grid",
       DataUrl = Url.Action("List", "UnitsApi")!,
       Caption = "Units",
       DefaultSort = "number",
       EmptyText = "No units match these filters.",
       FilterFormId = "unit-filters",
       Columns =
       [
           new GridColumn { Key = "unitNumber", Title = "Unit", SortKey = "number" },
           new GridColumn { Key = "createdAt", Title = "Added", SortKey = "created", Format = GridCellFormat.Date },
           new GridColumn { Key = "detailsUrl", Title = "Actions", Format = GridCellFormat.Link, LinkText = "Open", HideTitle = true }
       ]
   }
   ```
   Column formats are `Text`, `Date`, `Badge` (with `BadgeClasses` mapping values to CSS classes) and `Link`. `SecondaryKey` adds a muted second line under a text value; on a badge it names the field shown as the badge text.
4. **Render it:** `@await Component.InvokeAsync("Grid", new { grid = ... })`. If the grid has filters, give the filter form the id named in `FilterFormId`, and the grid will submit its fields with each request.

## Behaviour you get for free
- Grid state lives in the URL, so refresh, bookmarks and Back/Forward all work.
- Sort buttons set `aria-sort`, the summary ("Showing 1–20 of 42") is announced to screen readers, and the pager hides when there's only one page.
- Cells are written with `textContent`, so data from the endpoint is never parsed as HTML.
- A 401 from the endpoint (session expired) is handled instead of rendering the login page as rows.
