namespace stockmind.DTOs.Categories;

public class ImportCategoriesResponseDto
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int SkippedMissingParent { get; set; }
    public int SkippedInvalid { get; set; }
    public int Total => Created + Updated + SkippedMissingParent + SkippedInvalid;
}
