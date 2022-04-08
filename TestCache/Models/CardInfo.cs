using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TestCache.Models
{
    [Index("Id")]
    public class CardInfo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public double Balance { get; set; }

        public bool Locked { get; set; }
        
        public bool Blocked { get; set; }
    }
}
