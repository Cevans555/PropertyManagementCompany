namespace PropertyManagement.Web.Models.Grid;

public static class PageMath
{
    public static int TotalPages(int totalCount, int pageSize)
    {
        return Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public static int ClampPage(int page, int totalCount, int pageSize)
    {
        return Math.Clamp(page, 1, TotalPages(totalCount, pageSize));
    }
}
