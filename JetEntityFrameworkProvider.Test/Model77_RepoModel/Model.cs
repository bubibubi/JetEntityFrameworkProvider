using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model77_RepoModel
{
    [Table("Item77")]
    public class Item
    {
        public Guid Id { get; set; }
        public string OwnerId { get; set; }
        public DateTime CreationDate { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsStoreItem { get; set; }
        public decimal Price { get; set; }
        public int Count { get; set; }
        public string TopBidderId { get; set; }
        public Material Material { get; set; }
        public ItemStatus Status { get; set; }
        public int KrauseNumber { get; set; }
    }

    public enum ItemStatus
    {
        One,
        Two
    }

    public enum Material
    {
        One,
        Two
    }
}
