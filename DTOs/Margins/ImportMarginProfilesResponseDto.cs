namespace stockmind.DTOs.Margins
{
    public class ImportMarginProfilesResponseDto
    {
        public int Created { get; set; }

        public int Updated { get; set; }

        public int SkippedInvalid { get; set; }

        public int SkippedMissingCategory { get; set; }

        public int Total =>
            Created + Updated + SkippedInvalid + SkippedMissingCategory;
    }
}
