namespace stockmind.DTOs.Grns;

public class ListGrnQueryDto
{
    private const int MaxPageSize = 100;

    public int PageNum { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public void Normalize()
    {
        if (PageNum <= 0)
        {
            PageNum = 1;
        }

        if (PageSize <= 0)
        {
            PageSize = 20;
        }

        if (PageSize > MaxPageSize)
        {
            PageSize = MaxPageSize;
        }
    }
}
