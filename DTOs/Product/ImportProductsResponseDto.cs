namespace stockmind.DTOs.Product
{
    public class ImportProductsResponseDto
    {
        public int Created { get; set; }

        public int Updated { get; set; }

        public int SkippedInvalid { get; set; }

        public int SkippedMissingCategory { get; set; }

        public int SkippedMissingSupplier { get; set; }

        public int Total => Created + Updated + SkippedInvalid + SkippedMissingCategory + SkippedMissingSupplier;
    }
}
