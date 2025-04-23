using DoAnLTW.Models;

public class ProductVariant
{
    public int ProductVariantId { get; set; }
    public int ProductId { get; set; }
    public string Size { get; set; }
    public decimal Price { get; set; }  // Price for the specific size
    public Product Product { get; set; }
}
