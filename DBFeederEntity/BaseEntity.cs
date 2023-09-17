using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBFeederEntity
{

    public class BaseEntity
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid ID { get; set; }
        public DateTime DT_CREATION { get; set; }
        public DateTime DT_UPDATE { get; set; }

        public override string ToString()
        {
            //TBI
            return base.ToString();
        }
    }
}
