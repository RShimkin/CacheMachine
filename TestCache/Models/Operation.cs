using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestCache.Models
{
    public class Operation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string CardNumber { get; set; }

        public int OperationCode { get; set; }

        public DateTime Time { get; set; }

        public string Details { get; set; }

        public int Sum { get; set; }

        public Operation()
        {
            Time = DateTime.Now;
            Details = null;
            Sum = 0;
        }
    }
}
