namespace PRODUCT.Model
{
    public class ImageTable
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public String Image_URL { get; set; }
        public ProductTable product { get; set; }
    }
}