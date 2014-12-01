using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace JetEntityFrameworkProvider
{
    public class TableWithSeveralFieldsType
    {

        public TableWithSeveralFieldsType()
        {
        }

        [Key]
        public int Id { get; set; }
        public int MyInt { get; set; }
        public double MyDouble { get; set; }
        public string MyString { get; set; }
        public DateTime MyDateTime { get; set; }
        public bool MyBool { get; set; }
        public bool MyNullableBool { get; set; }
    }
}
