using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model18_CompositeKeys
{
    public class GoodsIssueProcess
    {

        [Key, Column(Order = 1), MaxLength(128)]
        public string DeliveryNote { get; set; }

        [Key, Column(Order = 2), ForeignKey("Product")]
        public int ProductId { get; set; }
        public Product Product { get; set; }
    }

    public class Product
    {
        // the unique ID of the product
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(36)]
        [Index("IX_ArticleNumber", 1, IsUnique = true)]
        public string ArticleNumber { get; set; }
    }
}
