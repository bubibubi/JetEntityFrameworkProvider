using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JetEntityFrameworkProvider.Test.Model72_ComplexTypeWNavyProp
{


    [Table("Friend72")]
    public class Friend
    {
        public Friend()
        {Address = new FullAddress();}

        public int Id { get; set; }
        public string Name { get; set; }

        public FullAddress Address { get; set; }
    }


    [Table("City72")]
    public class City
    {
        public int Id { get; set; }
        public string Cap { get; set; }
        public string Name { get; set; }
    }

    [ComplexType]
    public class FullAddress
    {
        public string Street { get; set; }
        public City City { get; set; }
    }

}
