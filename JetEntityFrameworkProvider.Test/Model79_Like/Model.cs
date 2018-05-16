using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model79_Like
{
    [Table("User80")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string Description { get; set; }
    }


}
