using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model78_SimpleSample
{
    [Table("User78")]
    public class User
    {
        [Key]
        public int Id { get; set; }
        public virtual ICollection<Contact> Contacts { get; set; }
        public virtual User MyPreferredUser { get; set; }
    }

    [Table("Contact78")]
    public class Contact
    {
        [Key]
        public int Id { get; set; }
        public virtual User User { get; set; }
    }

}
