using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model03
{
    [Table("PanelLookup")]
    public class PanelLookup
    {
        [Column("Id")]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        public virtual ICollection<PanelTexture> PanelTextures { get; set; }
    }
}
